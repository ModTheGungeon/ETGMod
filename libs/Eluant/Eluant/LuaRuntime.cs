//
// LuaRuntime.cs
//
// Author:
//       Chris Howie <me@chrishowie.com>
//
// Copyright (c) 2013 Chris Howie
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Eluant.ObjectBinding;
using System.Linq;
using System.Text;

namespace Eluant
{
    public class LuaRuntime : IDisposable
    {
        // These are our only two callbacks.  They need to be static methods for iOS, where the runtime cannot create C
        // function pointers from instance methods.
        private static readonly LuaApi.lua_CFunction clrObjectGcCallbackWrapper;
        private static readonly LuaApi.lua_CFunction methodWrapperCallCallbackWrapper;
        private static readonly LuaApi.lua_CFunction cFunctionCallback;

        public const bool LUAJIT = LuaApi.LUAJIT;

        static LuaRuntime()
        {
            clrObjectGcCallbackWrapper = ClrObjectGcCallbackWrapper;
            methodWrapperCallCallbackWrapper = MethodWrapperCallCallbackWrapper;
            cFunctionCallback = CFunctionCallback;
        }

        [UnmanagedFunctionPointer(LuaApi.LUA_CALLING_CONVENTION)]
        protected internal delegate IntPtr LuaAllocator(IntPtr ud, IntPtr ptr, IntPtr osize, IntPtr nsize);

        protected internal IntPtr LuaState { get; private set; }

        // A self-referential weak handle used by the static callback methods to locate the LuaRuntime instance.
        private GCHandle selfHandle;

        protected GCHandle SelfHandle
        {
            get { return selfHandle; }
        }

        public enum LuaExceptionMode {
            SingleSpliced,
            // LuaExceptions will be thrown with no inner exceptions (or inner exceptions of a type other than LuaException).
            // The tracebacks of LuaExceptions will be merged in such a way that the exception will contain the deepest
            // Lua traceback with CLR (C#) stack traces inserted into the traceback in the right places in the correct order.
            // The exception will contain the Lua value that `error` was called with, if applicable.
            // This is the default.
            NestedSeparate
            // LuaExceptions will be thrown with an inner LuaException (and that exception might have an inner LuaException
            // too, etc.). Each exception will contain a different Lua traceback (the deeper into the inner exceptions,
            // the deeper the traceback) and a different CLR stack trace.
            // Only the deepest inner exception will contain the Lua value that `error` was called with, if applicable.
            // You may use this if you suspect that the single LuaException thrown in SingleSpliced mode contains
            // inaccurate information (unlikely, but might happen as a result of a bug).
        }

        public enum LuaMethodMode {
            PassSelf,
            // requires you to pass self as the first argument (colon syntax)
            PassJustArgs
            // does not require you to pass self as the first argument (standard dot syntax)
        }

        private ObjectReferenceManager<LuaClrObjectValue> objectReferenceManager = new ObjectReferenceManager<LuaClrObjectValue>();

        // Separate field for the corner case where customAllocator was collected first.
        private bool hasCustomAllocator = false;
        private LuaAllocator customAllocator;

        public LuaExceptionMode ExceptionMode = LuaExceptionMode.SingleSpliced;
        public LuaMethodMode MethodMode = LuaMethodMode.PassSelf;

        // Old (pre-2015) versions of Mono have an issue where the exception
        // handling does not exactly mimic .NET.
        // Those versions of Mono will preserve the stack trace of an exception
        // when rethrown if the exception already has a stack trace, when the
        // stack trace of an exception should always point to the last time it
        // was thrown.
        // Our nice tracebacks depend on this behavior, and so we
        // emulate it if MonoStackTraceWorkaround is true (which you should set
        // if you are using old versions of Mono, for example in Unity).
        //
        // You can see the Mono PR that solved this issue here:
        // https://github.com/mono/mono/pull/1668/commits/f985c6809b976935c0c5031042bb76339cbe5a72
        //
        // The exact date is May 12, 2015, so if your Mono build is newer than that,
        // you shouldn't need this workaround.
        public bool MonoStackTraceWorkaround = false;

        // The below constants are used for making the tracebacks pretty.
        // The line numbers should match BindingSupport.lua.

        // name for the BindingSupport.lua chunk
        private const string RESERVED_CHUNK_NAME = "@EluantBindings";

        // name that'll appear in the stacktrace
        private const string RESERVED_CHUNK_TRACE_NAME = "EluantBindings";

        // placeholder for entries in the lua trace where a C# stack trace should be inserted
        private const string CLR_STACKTRACE_PLACEHOLDER = "@[[CLR STACKTRACE]]";

        // process_managed_call, condition marked with '-- pcall'
        private const int ERROR_FROM_LUA_LINENO = 29;
            
        // process_managed_call, condition marked with '-- CLR'
        private const int ERROR_FROM_CLR_LINENO = 32;

        // the line that returns the return value of process_managed_call
        // in the anonymous function returned by eluant_create_managed_call_wrapper
        private const int CALL_OVER_CLR_BOUNDARY_LINENO = 40;

        private const string MAIN_THREAD_KEY = "eluant_main_thread";
        private const string REFERENCES_KEY = "eluant_references";

        private const string WEAKREFERENCE_METATABLE = "eluant_weakreference";
        private const string OPAQUECLROBJECT_METATABLE = "eluant_opaqueclrobject";

        private const int TRACEBACKERR_MSG_KEY = 0;
        private const int TRACEBACKERR_TRACE_KEY = 1;

        private Dictionary<string, LuaFunction> metamethodCallbacks = new Dictionary<string, LuaFunction>();

        private LuaFunction createManagedCallWrapper;

        private ConcurrentQueue<int> releasedReferences = new ConcurrentQueue<int>();

        public LuaRuntime()
        {
            try {
                selfHandle = GCHandle.Alloc(this, GCHandleType.WeakTrackResurrection);

                IntPtr customState;
                customAllocator = CreateAllocatorDelegate(out customState);

                if (customAllocator != null) {
                    hasCustomAllocator = true;
                    //LuaState = LuaApi.luaL_newstate();
                    if (LuaApi.LUAJIT && IntPtr.Size == 8) {
                        throw new InvalidOperationException("Can't use custom allocators with LuaJIT on 64 bit architectures");
                    } else {
                        LuaState = LuaApi.lua_newstate(customAllocator, customState);
                    }
                } else {
                    hasCustomAllocator = false;
                    LuaState = LuaApi.luaL_newstate();
                }

                Globals = new LuaGlobalsTable(this);

                Initialize();
            } catch {
                Dispose();
                throw;
            }
        }

        // This is to support accounting (see MemoryConstrainedLuaRuntime).  Returning a delegate is wonky, but there
        // are good reasons for doing it this way.
        //
        // 1. Virtual method means we have to either implement a base allocator ourselves (instead of letting
        //    luaL_newstate() use its default) or we need to use reflection to detect if the method was overridden.
        //
        // 2. Abstract method means that LuaRuntime is abstract, and subclasses MUST implement their own allocator.
        //
        // So instead we have a method that returns a delegate, and a null return value means use the default allocator.
        protected virtual LuaAllocator CreateAllocatorDelegate(out IntPtr customState)
        {
            customState = IntPtr.Zero;
            return null;
        }

        protected virtual void PreInitialize() { }

        protected virtual void PostInitialize() { }

        internal static IntPtr GetMainThread(IntPtr state)
        {
            LuaApi.lua_getfield(state, LuaApi.LUA_REGISTRYINDEX, MAIN_THREAD_KEY);
            var mainThread = LuaApi.lua_touserdata(state, -1);
            LuaApi.lua_pop(state, 1);

            return mainThread;
        }

        private LuaFunction CreateCallbackWrapper(LuaApi.lua_CFunction callback)
        {
            var top = LuaApi.lua_gettop(LuaState);

            try {
                Push(createManagedCallWrapper);
                PushCFunction(callback);

                if (LuaApi.lua_pcall(LuaState, 1, 1, 0) != 0) {
                    throw new InvalidOperationException("Unable to create delegate wrapper.");
                }

                return (LuaFunction)Wrap(-1);
            } finally {
                LuaApi.lua_settop(LuaState, top);
            }
        }

        protected void PushSelf()
        {
            var ud = LuaApi.lua_newuserdata(LuaState, new UIntPtr(unchecked((ulong)IntPtr.Size)));
            Marshal.WriteIntPtr(ud, (IntPtr)selfHandle);
        }

        protected static LuaRuntime GetSelf(IntPtr state, int index)
        {
            var ud = LuaApi.lua_touserdata(state, index);
            var handle = (GCHandle)Marshal.ReadIntPtr(ud);

            return handle.Target as LuaRuntime;
        }

        private void Initialize()
        {
            PreInitialize();

            LuaApi.lua_newtable(LuaState);
            LuaApi.lua_setfield(LuaState, LuaApi.LUA_REGISTRYINDEX, REFERENCES_KEY);

            LuaApi.luaL_openlibs(LuaState);

            LuaApi.lua_pushlightuserdata(LuaState, LuaState);
            LuaApi.lua_setfield(LuaState, LuaApi.LUA_REGISTRYINDEX, MAIN_THREAD_KEY);

            LuaApi.luaL_newmetatable(LuaState, OPAQUECLROBJECT_METATABLE);

            LuaApi.lua_pushstring(LuaState, "__gc");
            PushSelf();
            LuaApi.lua_pushcclosure(LuaState, clrObjectGcCallbackWrapper, 1);
            LuaApi.lua_settable(LuaState, -3);

            LuaApi.lua_pushstring(LuaState, "__metatable");
            LuaApi.lua_pushboolean(LuaState, 0);
            LuaApi.lua_settable(LuaState, -3);

            LuaApi.lua_pushstring(LuaState, "is_clr_object");
            LuaApi.lua_pushboolean(LuaState, 1);
            LuaApi.lua_settable(LuaState, -3);

            LuaApi.lua_pop(LuaState, 1);

            LuaApi.luaL_newmetatable(LuaState, WEAKREFERENCE_METATABLE);

            LuaApi.lua_pushstring(LuaState, "__mode");
            LuaApi.lua_pushstring(LuaState, "v");
            LuaApi.lua_settable(LuaState, -3);

            LuaApi.lua_pop(LuaState, 1);

            DoStringInternal(Scripts.BindingSupport, chunk_name: RESERVED_CHUNK_NAME).Dispose();

            createManagedCallWrapper = (LuaFunction)Globals["eluant_create_managed_call_wrapper"];

            Globals["eluant_create_managed_call_wrapper"] = null;

            metamethodCallbacks["__newindex"] = CreateCallbackWrapper(NewindexCallback);
            metamethodCallbacks["__index"] = CreateCallbackWrapper(IndexCallback);
            metamethodCallbacks["__tostring"] = CreateCallbackWrapper(ToStringCallback);
            metamethodCallbacks ["__call"] = CreateCallbackWrapper(CallCallback);
            
            metamethodCallbacks["__add"] = CreateCallbackWrapper(state => BinaryOperatorCallback<ILuaAdditionBinding>(state, (i, a, b) => i.Add(this, a, b)));
            metamethodCallbacks["__sub"] = CreateCallbackWrapper(state => BinaryOperatorCallback<ILuaSubtractionBinding>(state, (i, a, b) => i.Subtract(this, a, b)));
            metamethodCallbacks["__mul"] = CreateCallbackWrapper(state => BinaryOperatorCallback<ILuaMultiplicationBinding>(state, (i, a, b) => i.Multiply(this, a, b)));
            metamethodCallbacks["__div"] = CreateCallbackWrapper(state => BinaryOperatorCallback<ILuaDivisionBinding>(state, (i, a, b) => i.Divide(this, a, b)));
            metamethodCallbacks["__mod"] = CreateCallbackWrapper(state => BinaryOperatorCallback<ILuaModuloBinding>(state, (i, a, b) => i.Modulo(this, a, b)));
            metamethodCallbacks["__pow"] = CreateCallbackWrapper(state => BinaryOperatorCallback<ILuaExponentiationBinding>(state, (i, a, b) => i.Power(this, a, b)));
            metamethodCallbacks["__unm"] = CreateCallbackWrapper(state => UnaryOperatorCallback<ILuaUnaryMinusBinding>(state, i => i.Minus(this)));
            metamethodCallbacks["__concat"] = CreateCallbackWrapper(state => BinaryOperatorCallback<ILuaConcatenationBinding>(state, (i, a, b) => i.Concatenate(this, a, b)));
            metamethodCallbacks["__len"] = CreateCallbackWrapper(state => UnaryOperatorCallback<ILuaLengthBinding>(state, i => i.GetLength(this)));
            metamethodCallbacks["__eq"] = CreateCallbackWrapper(state => BinaryOperatorCallback<ILuaEqualityBinding>(state, (i, a, b) => i.Equals(this, a, b)));
            metamethodCallbacks["__lt"] = CreateCallbackWrapper(state => BinaryOperatorCallback<ILuaLessThanBinding>(state, (i, a, b) => i.LessThan(this, a, b)));
            metamethodCallbacks["__le"] = CreateCallbackWrapper(state => BinaryOperatorCallback<ILuaLessThanOrEqualToBinding>(state, (i, a, b) => i.LessThanOrEqualTo(this, a, b)));

            PostInitialize();
        }

        ~LuaRuntime()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void OnEnterLua() { }

        protected virtual void OnEnterClr() { }

        protected virtual void Dispose(bool disposing)
        {
            GC.SuppressFinalize(this);

            if (LuaState != IntPtr.Zero) {
                if (hasCustomAllocator && Environment.HasShutdownStarted) {
                    // This is the perfect storm.  The CLR is shutting down, and we created the Lua state with a custom
                    // allocator.  The allocator delegate may have already been finalized, which (at least on Mono)
                    // would mean that the unmanaged->managed trampoline has been collected.  Any action we take now
                    // (including lua_close()) would call this potentially missing trampoline.  If the trampoline is
                    // missing then this causes a segfault or access violation, taking the runtime down hard.
                    //
                    // The only sane thing to do here is skip lua_close() and let the OS clean up the Lua allocation.
                    //
                    // This means that Lua objects won't be collected, so hopefully no finalizations there were of a
                    // critical nature (or things that the OS won't do when the runtime process quits, anyway).
                    //
                    // Consumers should make sure that they dispose Lua runtimes before the CLR begins shutting down to
                    // avoid this scenario.
                    LuaState = IntPtr.Zero;
                } else {
                    LuaApi.lua_close(LuaState);
                    LuaState = IntPtr.Zero;
                }
            }

            if (selfHandle.IsAllocated) {
                selfHandle.Free();
            }
        }

        protected void CheckDisposed()
        {
            if (LuaState == IntPtr.Zero) {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        public LuaGlobalsTable Globals { get; private set; }

        internal void Push(LuaValue value)
        {
            if (value == null) {
                // Special case for null.
                LuaApi.lua_pushnil(LuaState);
            } else {
                value.Push(this);
            }
        }

        private void RewriteReferenceTable()
        {
            LuaApi.lua_getfield(LuaState, LuaApi.LUA_REGISTRYINDEX, REFERENCES_KEY);
            LuaApi.lua_newtable(LuaState);

            LuaApi.lua_pushnil(LuaState);
            while (LuaApi.lua_next(LuaState, -3) != 0) {
                // Stack: reftable newtable key value
                // Goal:  reftable newtable key key value

                // reftable newtable key value key
                LuaApi.lua_pushvalue(LuaState, -2);
                // reftable newtable key key value
                LuaApi.lua_insert(LuaState, LuaApi.abs_index(LuaState, -2));

                // reftable newtable key
                LuaApi.lua_settable(LuaState, -4);

                // All set for next iteration.
            }

            // Swap out old table for the new table.
            LuaApi.lua_setfield(LuaState, LuaApi.LUA_REGISTRYINDEX, REFERENCES_KEY);

            // Pop the old table.
            LuaApi.lua_pop(LuaState, 1);
        }

        private int referenceSeq = 0;
        private int lastReference = 0;

        private int GetNextReference()
        {
            if (++referenceSeq == 100) {
                // Every hundred references taken, reset lastReference so that we try to reuse slots in the reference
                // table.  Otherwise the table is going to grow very large.
                referenceSeq = 0;
                lastReference = 0;
            }

            var reference = lastReference;
            do {
                // Increment the reference number with wraparound.
                if (reference == int.MaxValue) {
                    reference = 1;
                } else {
                    ++reference;
                }

                // Get the type of Lua value at this index.
                LuaApi.lua_rawgeti(LuaState, -1, reference);
                var type = LuaApi.lua_type(LuaState, -1);
                LuaApi.lua_pop(LuaState, 1);

                // If the entry at that slot was nil, it's a valid open slot.
                if (type == LuaApi.LuaType.Nil) {
                    lastReference = reference;

                    return reference;
                }

                // Stop looping if we traversed the entire reference-space (unlikely...).
            } while (reference != lastReference);

            throw new InvalidOperationException("Too many references.");
        }

        // The following methods are how we implement CLR references to Lua objects.  Similar to luaL_ref(), we do so
        // in a table, but rather than using the registry we use a table stored in the registry.  This gives us a bit
        // more flexibility while also avoiding clashing with other stuff that may use numeric keys into the registry.
        //
        // The idea is that we put a Lua object at a numeric index that is not in use.  Then the Lua GC will not collect
        // the object, and that numeric index becomes the reference ID used by the CLR object wrapper.  To push the
        // reference we just look up the ID in the reference table.  To destroy the reference, we set that table slot to
        // nil, which releases that Lua reference.
        private int CreateReference(int index)
        {
            index = LuaApi.abs_index(LuaState, index);

            // Need the table first, and NextReference depends on it being pushed first.
            LuaApi.lua_getfield(LuaState, LuaApi.LUA_REGISTRYINDEX, REFERENCES_KEY);
            var reference = GetNextReference();

            LuaApi.lua_pushvalue(LuaState, index);
            LuaApi.lua_rawseti(LuaState, -2, reference);

            LuaApi.lua_pop(LuaState, 1);

            return reference;
        }

        private int destroySeq = 0;

        private void DestroyReference(int reference)
        {
            LuaApi.lua_getfield(LuaState, LuaApi.LUA_REGISTRYINDEX, REFERENCES_KEY);
            LuaApi.lua_pushnil(LuaState);
            LuaApi.lua_rawseti(LuaState, -2, reference);

            LuaApi.lua_pop(LuaState, 1);

            // Every 1000 destroys, rewrite the reference table to try to reduce its memory footprint.
            if (++destroySeq == 1000) {
                destroySeq = 0;
                RewriteReferenceTable();
            }
        }

        internal void PushReference(int reference)
        {
            LuaApi.lua_getfield(LuaState, LuaApi.LUA_REGISTRYINDEX, REFERENCES_KEY);
            LuaApi.lua_rawgeti(LuaState, -1, reference);
            LuaApi.lua_remove(LuaState, LuaApi.abs_index(LuaState, -2));
        }

        internal LuaValue Wrap(int index)
        {
            var type = LuaApi.lua_type(LuaState, index);

            switch (type) {
                case LuaApi.LuaType.Nil:
                    return LuaNil.Instance;

                case LuaApi.LuaType.Boolean:
                    return (LuaBoolean)(LuaApi.lua_toboolean(LuaState, index) != 0);

                case LuaApi.LuaType.Number:
                    return (LuaNumber)LuaApi.lua_tonumber(LuaState, index);

                case LuaApi.LuaType.String:
                    return (LuaString)LuaApi.lua_tostring(LuaState, index);

                case LuaApi.LuaType.Table:
                    return new LuaTable(this, CreateReference(index));

                case LuaApi.LuaType.Function:
                    return new LuaFunction(this, CreateReference(index));

                case LuaApi.LuaType.LightUserdata:
                    return new LuaLightUserdata(this, CreateReference(index));

                case LuaApi.LuaType.Userdata:
                    if (IsClrObject(index)) {
                        return new LuaClrObjectReference(this, CreateReference(index));
                    }

                    return new LuaUserdata(this, CreateReference(index));

                case LuaApi.LuaType.Thread:
                    return new LuaThread(this, CreateReference(index));
            }

            throw new InvalidOperationException("Don't know how to wrap Lua type " + type.ToString());
        }

        private bool HasMetatable(int index, string tableName)
        {
            var top = LuaApi.lua_gettop(LuaState);

            try {
                if (LuaApi.lua_getmetatable(LuaState, index) == 0) {
                    return false;
                }

                LuaApi.luaL_getmetatable(LuaState, tableName);
                return LuaApi.lua_rawequal(LuaState, -1, -2) != 0;
            } finally {
                LuaApi.lua_settop(LuaState, top);
            }
        }

        internal void DisposeReference(int reference, bool isExplicit)
        {
            // If the Lua state is gone then this is a successful no-op.
            if (LuaState == IntPtr.Zero) { return; }

            if (isExplicit) {
                // If Dispose() was called then assume that there is no contention for the Lua runtime.
                DestroyReference(reference);

                // This is probably a good time to destroy any other pending disposed references.
                ProcessReleasedReferences();
            } else if (Environment.HasShutdownStarted) {
                // If the CLR is terminating then do nothing; see LuaRuntime.Dispose(bool) for an explanation.
            } else {
                // Otherwise we have to arrange to have the reference released at a later time, since we can't be sure
                // that the runtime isn't in use on another thread.
                releasedReferences.Enqueue(reference);
            }
        }

        private void ProcessReleasedReferences()
        {
            int reference;
            while (releasedReferences.TryDequeue(out reference)) {
                DestroyReference(reference);
            }
        }

        internal void SetFenv(LuaTable env, LuaValue val) {
            Push(val);
            Push(env);
            if (LuaApi.lua_setfenv(LuaState, -2) == 0) {
                throw new LuaException("Can't set environment of a value that's not a function, thread or userdata");
            }

            LuaApi.lua_pop(LuaState, 1);
        }

        internal LuaTable GetFenv(LuaValue val) {
            Push(val);
            LuaApi.lua_getfenv(LuaState, -1);
            var tab = Wrap(-1) as LuaTable;
            LuaApi.lua_pop(LuaState, 1);

            return tab;
        }

        private const int MAX_CHUNK_NAME_LENGTH = 8;
        private void LoadString(string str, string chunk_name = null)
        {
            if (LuaApi.luaL_loadbuffer(LuaState, str, (UIntPtr)str.Length, chunk_name ?? str) != 0) {
                var error = LuaApi.lua_tostring(LuaState, -1);
                LuaApi.lua_pop(LuaState, 1);

                throw new LuaException(error);
            }
        }

        public LuaVararg DoString(string str, string chunk_name = null) {
            if (str == RESERVED_CHUNK_NAME || chunk_name == RESERVED_CHUNK_NAME) {
                throw new Exception($"Chunk name '{RESERVED_CHUNK_NAME}' is reserved for Eluant only.");
            }
            return DoStringInternal(str, chunk_name);
        }

        private LuaVararg DoStringInternal(string str, string chunk_name = null)
        {
            if (str == null) { throw new ArgumentNullException("str"); }

            CheckDisposed();

            PushPcallMessageHandler();
            LoadString(str, chunk_name: chunk_name);
            // Compiled code is on the stack, now call it.
            var res = Call(LuaValue.EmptyArray);
            return res;
        }

        public LuaFunction CompileString(string str)
        {
            if (str == null) { throw new ArgumentNullException("str"); }

            CheckDisposed();

            LoadString(str);

            var fn = Wrap(-1);

            LuaApi.lua_pop(LuaState, 1);

            return (LuaFunction)fn;
        }

        private void LoadFile(string path) {
            if (LuaApi.luaL_loadfile(LuaState, path) != 0) {
                var error = LuaApi.lua_tostring(LuaState, -1);
                LuaApi.lua_pop(LuaState, 1);

                throw new LuaException(error);
            }
        }

        public LuaFunction CompileFile(string path) {
            if (path == null) { throw new ArgumentNullException("path"); }

            CheckDisposed();

            LoadFile(path);

            var fn = Wrap(-1);

            LuaApi.lua_pop(LuaState, 1);

            return (LuaFunction)fn;
        }

        public LuaVararg DoFile(string path)
        {
            if (path == null) { throw new ArgumentNullException(path); }

            CheckDisposed();

            PushPcallMessageHandler();
            LoadFile(path);

            // Compiled code is on the stack, now call it.
            var res = Call(LuaValue.EmptyArray);
            return res;
        }

        internal LuaVararg Call(LuaFunction fn, IList<LuaValue> args)
        {
            if (fn == null) { throw new ArgumentNullException("fn"); }
            if (args == null) { throw new ArgumentNullException("args"); }

            CheckDisposed();

            PushPcallMessageHandler();
            Push(fn);

            var res = Call(args);
            return res;
        }

        private void PushPcallMessageHandler()
        {
            PushSelf();
            LuaApi.lua_pushcclosure(LuaState, PcallErrorHandler, 1);
        }

        private static void PushTraceback(IntPtr state, bool use_clr_call_placeholders) {
            var ar = new LuaApi.lua_Debug();
            var firstpart = true;
            int level = 2;
            var top = LuaApi.lua_gettop(state);
            bool first_line = true;

            int skip_lines = 0;

            while (LuaApi.lua_getstack(state, level++, ref ar) != 0) {
                if (level > LEVELS1 && firstpart) {
                    /* no more than `LEVELS2' more levels? */
                    if (LuaApi.lua_getstack(state, level + LEVELS2, ref ar) == 0)
                        level--;  /* keep going */
                    else {
                        LuaApi.lua_pushstring(state, "\n\t..."); /* too many levels */
                        while (LuaApi.lua_getstack(state, level + LEVELS2, ref ar) != 0)  /* find last levels */
                            level++;
                    }
                    firstpart = false;
                    continue;
                }
                if (skip_lines > 0) {
                    skip_lines -= 1;
                    continue;
                }

                LuaApi.lua_getinfo(state, "Snl", ref ar);

                if (ar.short_src == RESERVED_CHUNK_TRACE_NAME) {
                    string pretty_trace = null;
                    
                    // if trace entry is from Eluant bindings, make the output prettier
                    if (ar.currentline == ERROR_FROM_CLR_LINENO) {
                        skip_lines = 1;
                        if (use_clr_call_placeholders) {
                            pretty_trace = CLR_STACKTRACE_PLACEHOLDER;
                        } else {
                            pretty_trace = $"[eluant]: passing on error from CLR";
                        }
                    } else if (ar.currentline == ERROR_FROM_LUA_LINENO) {
                        skip_lines = 1;
                        pretty_trace = $"[eluant]: passing on error from Lua";
                    } else if (ar.currentline == CALL_OVER_CLR_BOUNDARY_LINENO) {
                        if (use_clr_call_placeholders) {
                            pretty_trace = CLR_STACKTRACE_PLACEHOLDER;
                        } else {
                            pretty_trace = $"[eluant]: call to CLR";
                        }
                    }

                    if (pretty_trace != null) {
                        if (!first_line) LuaApi.lua_pushstring(state, "\n");
                        LuaApi.lua_pushstring(state, pretty_trace);
                        first_line = false;
                        continue;
                    }
                } else if (ar.short_src == $"[C]") {
                    var future = new LuaApi.lua_Debug();
                    if (LuaApi.lua_getstack(state, level + 1, ref future) != 0) {
                        LuaApi.lua_getinfo(state, "Snl", ref future);
                        // this is two levels forward - level will already be 1 higher than the one in this loop due
                        // to the postfix increment in the while at the top

                        if (future.short_src == RESERVED_CHUNK_TRACE_NAME && future.currentline == CALL_OVER_CLR_BOUNDARY_LINENO) {
                            // if in two trace lines we're going to get the trace of the lua part of the CLR call
                            // remove this and the next line as they're useless garbage
                            // (just "[C]: ?" and "[C]: in function 'real_pcall")

                            skip_lines = 1;
                            continue;
                        }
                    }
                }

                if (!first_line) LuaApi.lua_pushstring(state, "\n");
                first_line = false;

                LuaApi.lua_pushstring(state, $"{ar.short_src}:");
                if (ar.currentline > 0) LuaApi.lua_pushstring(state, $"{ar.currentline}:");


                if (ar.namewhat != null && ar.namewhat.Length > 0 && ar.namewhat [0] != '\0') { /* is there a name? */
                    LuaApi.lua_pushstring(state, $" in function '{ar.name}'");
                } else if (ar.what != null) {
                    if (ar.what [0] == 'm') { /* main? */
                        LuaApi.lua_pushstring(state, " in main chunk");
                    } else if (ar.what [0] == 'C' || ar.what [0] == 't') {
                        LuaApi.lua_pushstring(state, " ?");  /* C function or tail call */
                    } else {
                        LuaApi.lua_pushstring(state, $" in function <{ar.short_src}:{ar.linedefined}>");
                    }
                }
                LuaApi.lua_concat(state, LuaApi.lua_gettop(state) - top);
            }
            LuaApi.lua_concat(state, LuaApi.lua_gettop(state) - top);
        }

        public string DoTraceback() {
            PushTraceback(LuaState, ExceptionMode == LuaExceptionMode.SingleSpliced);
            var trace = LuaApi.lua_tostring(LuaState, -1);
            LuaApi.lua_remove(LuaState, -1);
            return trace;
        }

        // Used for preserving the stack trace of exceptions
        private class WrapException : Exception {
            public WrapException(Exception inner) : base("(wrap)", inner) {}
        }

        private static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search, StringComparison.InvariantCulture);
            if (pos < 0) {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        private const int LEVELS1 = 12;
        private const int LEVELS2 = 10;
        private static int PcallErrorHandler(IntPtr state)
        {
            var runtime = GetSelf(state, LuaApi.lua_upvalueindex(1));

            string error_msg = null;
            Exception inner = null;
            LuaValue value = null;

            if (LuaApi.lua_isstring(state, -1) != 0) {
                error_msg = LuaApi.lua_tostring(state, -1);
                value = error_msg;
            } else {
                var wrap = runtime.Wrap(-1);
                if (wrap is IClrObject) {
                    var obj = (IClrObject)wrap;
                    if (obj.ClrObject is WrapException) {
                        inner = ((WrapException)obj.ClrObject).InnerException;
                        error_msg = ((WrapException)obj.ClrObject).InnerException.Message;
                    }
                    else if (obj.ClrObject is Exception) {
                        inner = (Exception)obj.ClrObject;
                        error_msg = ((Exception)obj.ClrObject).Message;
                    } else {
                        value = obj as LuaValue;
                    }
                } else {
                    value = wrap;
                }
            }
            LuaApi.lua_remove(state, -1);
            
            LuaApi.lua_newtable(state); // t

            string trace = null;
            if (runtime.ExceptionMode == LuaExceptionMode.SingleSpliced && inner != null && inner is LuaException) {
                if (((LuaException)inner).tracebackString != null) {
                    trace = ((LuaException)inner).tracebackString;
                } else {
                    PushTraceback(state, runtime.ExceptionMode == LuaExceptionMode.SingleSpliced);
                    trace = LuaApi.lua_tostring(state, -1);
                    LuaApi.lua_remove(state, -1);
                }

                // Replace the first occurence of CLR_STACKTRACE_PLACEHOLDER in the traceback with the stacktrace
                // of the inner exception
                int pos = trace.IndexOf(CLR_STACKTRACE_PLACEHOLDER, StringComparison.InvariantCulture);
                if (pos >= 0) {
                    var stacktrace_fragment = inner.StackTrace.Replace("  at", "[clr]:");
                    trace = trace.Substring(0, pos) + stacktrace_fragment + trace.Substring(pos + CLR_STACKTRACE_PLACEHOLDER.Length); ;
                }

                ((LuaException)inner).tracebackString = trace;
                runtime.PushCustomClrObject(new LuaTransparentClrObject(inner));
            } else {
                PushTraceback(state, runtime.ExceptionMode == LuaExceptionMode.SingleSpliced);
                trace = LuaApi.lua_tostring(state, -1);
                LuaApi.lua_remove(state, -1);

                int pos = trace.IndexOf(CLR_STACKTRACE_PLACEHOLDER, StringComparison.InvariantCulture);
                if (pos >= 0 && inner != null) {
                    string stacktrace_fragment = "";
                    // small optimization - avoid using the StringBuilder
                    // stuff if we're only dealing with an inner exception
                    // that doesn't have any inner exceptions
                    if (inner.InnerException != null) {
                        var s = new StringBuilder();
                        var exceptions = new List<Exception>();

                        Exception e = inner;
                        while (e != null) {
                            exceptions.Add(e);
                            e = e.InnerException;
                        }

                        for (int i = exceptions.Count - 1; i >= 0; i--) {
                            if (i != exceptions.Count - 1) {
                                if (i == 0) s.AppendLine();
                                s.AppendLine($"[throw]: inner exception {exceptions [i + 1].GetType()}: {exceptions [i + 1].Message}");
                            } else if (i != 0) {
                                s.AppendLine($"[throw]: exception {exceptions [i - 1].GetType()}");
                            }
                            s.Append(exceptions [i].StackTrace.Replace("  at", "[clr]:"));
                        }

                        stacktrace_fragment = s.ToString();
                    } else {
                        stacktrace_fragment = inner.StackTrace.Replace("  at", "[clr]:");
                    }
                    trace = trace.Substring(0, pos) + stacktrace_fragment + trace.Substring(pos + CLR_STACKTRACE_PLACEHOLDER.Length);
                }
                var ex = new LuaException(error_msg, runtime.ExceptionMode == LuaExceptionMode.SingleSpliced ? null : inner, value, trace);
                runtime.PushCustomClrObject(new LuaTransparentClrObject(ex));
            }

            return 1;
        }

        public void Collect()
        {
            ProcessReleasedReferences();
            LuaApi.lua_gc(LuaState, LuaApi.LuaGcOperation.Collect, 0);
        }

        // Calls a function that has already been pushed (with a message handler that has already been pushed).
        // We need this functionality to support DoString().
        // Call CheckDisposed() before calling this method!
        private LuaVararg Call(IList<LuaValue> args)
        {
            if (args == null) { throw new ArgumentNullException("args"); }

            // Top should point to the frame BELOW the function and the message handler,
            // which should have already been pushed.
            var top = LuaApi.lua_gettop(LuaState) - 2;

            var msgh_index = top + 1;

            bool needEnterClr = false;

            LuaValue[] results = null;

            try {
                // Ensure room for function + args.
                if (LuaApi.lua_checkstack(LuaState, 1 + args.Count) == 0) {
                    throw new LuaException("Cannot grow stack for call arguments.");
                }

                // Whenever we cross a Lua/CLR boundary, release any references that were cleaned up by the CLR's
                // garbage collector.
                ProcessReleasedReferences();

                foreach (var arg in args) {
                    Push(arg);
                }

                needEnterClr = true;
                OnEnterLua();

                var pcall_result = LuaApi.lua_pcall(LuaState, args.Count, LuaApi.LUA_MULTRET, msgh_index);
                if (pcall_result != 0) {
                    needEnterClr = false;
                    OnEnterClr();

                    if (IsClrObject(-1)) { // error is actually a CLR object?
                        var obj = Wrap(-1);
                        if (obj is LuaClrObjectReference) {
                            var clrobj = ((LuaClrObjectReference)obj).ClrObject;
                            if (clrobj is LuaException) {
                                var ex = (LuaException)clrobj;

                                // Read the comment on MonoStackTraceWorkaround for an
                                // explanation on why this is necessary
                                if (MonoStackTraceWorkaround) {
                                    ex.forcedStackTrace = new System.Diagnostics.StackTrace().ToString();
                                }
                                throw ex;
                            } else if (clrobj is Exception) {
                                throw new LuaException(((Exception)clrobj).Message, (Exception)clrobj, obj);
                            } else {
                                throw new LuaException("An error has occured in Lua code.", null, value: obj);
                            }
                        } else {
                            throw new LuaException("An error has occured in Lua code.", null, value: obj);
                        }
                    } else {
                        throw new LuaException(LuaApi.lua_tostring(LuaState, -1));
                    }
                    // Finally block will take care of popping the error message
                    // (and the custom error with traceback handler things if necessary).
                }
                needEnterClr = false;
                OnEnterClr();

                // Results are in the stack, last result on the top.
                var newTop = LuaApi.lua_gettop(LuaState);
                var nresults = newTop - top - 1;
                results = new LuaValue[nresults];

                if (nresults > 0) {
                    // We may need one additional stack slot to wrap a reference.
                    if (LuaApi.lua_checkstack(LuaState, 1) == 0) {
                        throw new LuaException("Cannot grow stack for call results.");
                    }

                    for (int i = 0; i < nresults; ++i) {
                        results[i] = Wrap(top + 2 + i);
                    }
                }

                // Clean up any references again.
                ProcessReleasedReferences();

                var ret = new LuaVararg(results, true);

                // Clear out results so the finally block doesn't dispose of the references we are returning.
                results = null;

                return ret;
            } finally {
                if (needEnterClr) { OnEnterClr(); }

                // Takes care of resetting the stack after processing results or retrieving the error message.
                LuaApi.lua_settop(LuaState, top);

                // results will be non-null if an exception was thrown before we could return the LuaVararg.  Clean up
                // any references, since the caller will never get the chance to dispose them.
                if (results != null) {
                    foreach (var r in results) {
                        if (r != null) {
                            r.Dispose();
                        }
                    }
                }
            }
        }

        private void PushNewReferenceValue(int reference)
        {
            var userData = LuaApi.lua_newuserdata(LuaState, (UIntPtr)Marshal.SizeOf(typeof(IntPtr)));
            Marshal.WriteIntPtr(userData, new IntPtr(reference));
        }

        private bool IsClrObject(int index)
        {
            if (LuaApi.lua_getmetatable(LuaState, index) == 0) {
                return false;
            }

            LuaApi.lua_pushstring(LuaState, "is_clr_object");
            LuaApi.lua_gettable(LuaState, -2);

            var is_clr_object = LuaApi.lua_toboolean(LuaState, -1) != 0;

            LuaApi.lua_pop(LuaState, 2);

            return is_clr_object;
        }

        private int? TryGetReference(int index)
        {
            // Make sure this Lua value represents a CLR object.  There are security implications if things go wrong
            // here, so we check to make absolutely sure that the Lua value represents one of our CLR object types.
            if (LuaApi.lua_type(LuaState, index) == LuaApi.LuaType.Userdata) {
                if (IsClrObject(index)) {
                    var userData = LuaApi.lua_touserdata(LuaState, index);
                    var handlePtr = Marshal.ReadIntPtr(userData);

                    return handlePtr.ToInt32();
                }
            }

            return null;
        }

        internal T GetClrObject<T>(int index)
            where T : LuaClrObjectValue
        {
            var obj = TryGetClrObject<LuaClrObjectValue>(index);

            if (obj == null) {
                throw new InvalidOperationException("Attempt to obtain CLR object from a Lua object that does not represent a CLR object.");
            }

            var typedObj = obj as T;
            if (typedObj == null) {
                throw new InvalidOperationException(string.Format("CLR object of type {0} was found, but CLR object of incompatible type {1} was expected.",
                                                                  obj.GetType().FullName,
                                                                  typeof(T).FullName));
            }

            return typedObj;
        }

        internal T TryGetClrObject<T>(int index)
            where T : LuaClrObjectValue
        {
            var reference = TryGetReference(index);

            if (!reference.HasValue) {
                return null;
            }

            return objectReferenceManager.GetReference(reference.Value) as T;
        }

        // This provides a general-purpose mechanism to push C functions into Lua without needing an instance method
        // (for iOS support).
        internal void PushCFunction(LuaApi.lua_CFunction fn)
        {
            PushSelf();
            PushOpaqueClrObject(new LuaOpaqueClrObject(fn));
            LuaApi.lua_pushcclosure(LuaState, cFunctionCallback, 2);
        }

#if (__IOS__ || MONOTOUCH)
        [MonoTouch.MonoPInvokeCallback(typeof(LuaApi.lua_CFunction))]
#endif
        private static int CFunctionCallback(IntPtr state)
        {
            LuaApi.lua_CFunction fn;
            try {
                var runtime = GetSelf(state, LuaApi.lua_upvalueindex(1));

                fn = (LuaApi.lua_CFunction)runtime.TryGetClrObject<LuaOpaqueClrObject>(LuaApi.lua_upvalueindex(2)).ClrObject;
            } catch {
                LuaApi.lua_pushboolean(state, 0);
                LuaApi.lua_pushstring(state, "Unexpected error processing callback.");

                return 2;
            }

            return fn(state);
        }

        public LuaTable ClrNamespace(Assembly ass, string @namespace) {
            var types = ass.GetTypes();

            // optimization
            if (types.Length == 0) return CreateTable();

            var tab = CreateTable();

            for (int i = 0; i < types.Length; i++) {
                if (string.Equals(types[i].Namespace, @namespace, StringComparison.Ordinal)) {
                    tab [types [i].Name] = new LuaClrTypeObject(types [i]);
                }
            }
            return tab;
        }

        public LuaTransparentClrObject ClrMetaType(Type static_type) {
            return new LuaTransparentClrObject(static_type, autobind: true);
        }

        public LuaClrTypeObject ClrStaticType(Type meta_type) {
            return new LuaClrTypeObject(meta_type);
        }

        public LuaClrTypeObject ClrType(Assembly ass, string full_name) {
            return new LuaClrTypeObject(ass.GetType(full_name));
        }

        public LuaTransparentClrObject ClrAssembly(string name) {
            var ass = Assembly.Load(name);
            return new LuaTransparentClrObject(ass);
        }

        public LuaTransparentClrObject ClrUnrestricted(LuaTransparentClrObject obj) {
            var inner = obj.ClrObject;
            return new LuaTransparentClrObject(inner, new ReflectionLuaBinder(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance));
        }

        public void InitializeClrPackage() {
            using (var tab = CreateTable()) {
                Globals ["clr"] = tab;

                using (var func = CreateFunctionFromDelegate(
                    new Func<string, LuaTransparentClrObject>(ClrAssembly)
                )) tab ["assembly"] = func;

                using (var func = CreateFunctionFromDelegate(
                    new Func<Assembly, string, LuaTable>(ClrNamespace)
                )) tab ["namespace"] = func;

                using (var func = CreateFunctionFromDelegate(
                    new Func<Type, LuaTransparentClrObject>(ClrMetaType)
                )) tab ["metatype"] = func;

                using (var func = CreateFunctionFromDelegate(
                    new Func<Type, LuaClrTypeObject>(ClrStaticType)
                )) tab ["statictype"] = func;

                using (var func = CreateFunctionFromDelegate(
                    new Func<Assembly, string, LuaClrTypeObject>(ClrType)
                )) tab ["type"] = func;

                using (var func = CreateFunctionFromDelegate(
                    new Func<LuaTransparentClrObject, LuaTransparentClrObject>(ClrUnrestricted)
                )) tab["unrestricted"] = func;
            }
        }

        internal void SetMetatable(LuaTable mt, LuaValue val) {
            Push(val);
            Push(mt);
            if (LuaApi.lua_setmetatable(LuaState, -2) == 0) {
                throw new LuaException("Couldn't set metatable");
            }
            LuaApi.lua_pop(LuaState, 1);
        }

        internal LuaTable GetMetatable(LuaValue val) {
            Push(val);
            if (LuaApi.lua_getmetatable(LuaState, -1) == 0) {
                // invalid index or no metatable
                // we don't error and instead return null
                // so that it's possible to detect when there is no metatable easily

                return null;
            }
            var tab = Wrap(-1) as LuaTable;
            LuaApi.lua_pop(LuaState, -1);
            return tab;
        }

        internal void PushOpaqueClrObject(LuaOpaqueClrObject obj)
        {
            // We don't check for null, intentionally.
            PushNewReferenceValue(objectReferenceManager.CreateReference(obj));
            LuaApi.luaL_getmetatable(LuaState, OPAQUECLROBJECT_METATABLE);
            LuaApi.lua_setmetatable(LuaState, -2);
        }

        internal void PushCustomClrObject(LuaClrObjectValue obj)
        {
            if (obj == null || obj.ClrObject == null) {
                LuaApi.lua_pushnil(LuaState);
                return;
            }

            var reference = objectReferenceManager.CreateReference(obj);

            try {
                PushNewReferenceValue(reference);

                // We will build up a unique metatable for this object based on the bindings it has implemented.
                LuaApi.lua_newtable(LuaState);

                // Set flag so that TryGetReference knows that this is a CLR object.
                LuaApi.lua_pushstring(LuaState, "is_clr_object");
                LuaApi.lua_pushboolean(LuaState, 1);
                LuaApi.lua_settable(LuaState, -3);

                // Protect the metatable.
                LuaApi.lua_pushstring(LuaState, "__metatable");
                LuaApi.lua_pushboolean(LuaState, 0);
                LuaApi.lua_settable(LuaState, -3);

                // __gc is required to clean up the reference.  The callback will determine if it implements the
                // interface.
                LuaApi.lua_pushstring(LuaState, "__gc");
                PushSelf();
                LuaApi.lua_pushcclosure(LuaState, clrObjectGcCallbackWrapper, 1);
                LuaApi.lua_settable(LuaState, -3);

                // For all others, we use MetamethodAttribute on the interface to make this code less repetitive.
                var metamethods = obj.BackingCustomObject.GetType().GetInterfaces()
                    .SelectMany(iface => iface.GetCustomAttributes(typeof(MetamethodAttribute), false).Cast<MetamethodAttribute>());

                foreach (var metamethod in metamethods) {
                    LuaApi.lua_pushstring(LuaState, metamethod.MethodName);
                    Push(metamethodCallbacks[metamethod.MethodName]);
                    LuaApi.lua_settable(LuaState, -3);
                }

                LuaApi.lua_setmetatable(LuaState, -2);
            } catch {
                objectReferenceManager.DestroyReference(reference);
                throw;
            }
        }

        private int NewindexCallback(IntPtr state)
        {
            return LuaToClrBoundary(state, toDispose => {
                // Arguments: Userdata (CLR object), key (property), value
                var obj = GetClrObject<LuaClrObjectValue>(1).BackingCustomObject as ILuaTableBinding;

                if (obj == null) {
                    throw new LuaException("CLR object does not support indexing.");
                }

                var key = Wrap(2);
                toDispose.Add(key);

                var value = Wrap(3);
                toDispose.Add(value);

                obj[this, key] = value;

                return 0;
            });
        }

        private int IndexCallback(IntPtr state)
        {
            return LuaToClrBoundary(state, toDispose => {
                // Arguments: Userdata (CLR object), key (property)
                var obj = GetClrObject<LuaClrObjectValue>(1).BackingCustomObject as ILuaTableBinding;

                if (obj == null) {
                    throw new LuaException("CLR object does not support indexing.");
                }

                var key = Wrap(2);
                toDispose.Add(key);

                var value = obj[this, key];
                toDispose.Add(value);

                Push(value);

                return 1;
            });
        }

        private int ToStringCallback(IntPtr state) {
            return LuaToClrBoundary(state, toDispose => {
                var obj = GetClrObject<LuaClrObjectValue>(1).BackingCustomObject as ILuaToStringBinding;

                Push(obj.ToLuaString(this));

                return 1;
            });
        }

        private int CallCallback(IntPtr state)
        {
            return LuaToClrBoundary(state, toDispose => {
                var obj = GetClrObject<LuaClrObjectValue>(1).BackingCustomObject as ILuaCallBinding;

                if (obj == null) {
                    throw new LuaException("CLR object is not callable.");
                }

                var nargs = LuaApi.lua_gettop(LuaState) - 1;
                var self = Wrap(-1);
                var args = new LuaValue[nargs - 1];

                for (int i = 0; i < nargs - 1; ++i) {
                    args[i] = Wrap(i + 2);
                    toDispose.Add(args[i]);
                }

                var vararg = new LuaVararg(args, true);

                var results = obj.Call(this, self, vararg);
                toDispose.Add(results);

                if (LuaApi.lua_checkstack(LuaState, 1 + results.Count) == 0) {
                    throw new LuaException("Cannot grow stack for results.");
                }

                foreach (var v in results) {
                    Push(v);
                }

                return results.Count;
            });
        }

        private int UnaryOperatorCallback<T>(IntPtr state, Func<T, LuaValue> oper)
            where T : class
        {
            return LuaToClrBoundary(state, toDispose => {
                var binding = GetClrObject<LuaClrObjectValue>(1).BackingCustomObject as T;

                if (binding == null) {
                    throw new LuaException("Unary operator not found for CLR object.");
                }

                var result = oper(binding);
                toDispose.Add(result);

                Push(result);
                return 1;
            });
        }

        private int BinaryOperatorCallback<T>(IntPtr state, Func<T, LuaValue, LuaValue, LuaValue> oper)
            where T : class
        {
            return LuaToClrBoundary(state, toDispose => {
                // For binary operators, the right argument could be a CLR object while the left argument might not, and
                // only one is guaranteed to support the given interface.  So we need to do some tests.
                LuaClrObjectValue obj;
                T binding = null;

                if ((obj = TryGetClrObject<LuaClrObjectValue>(1)) != null) {
                    binding = obj.BackingCustomObject as T;
                }

                if (binding == null && (obj = TryGetClrObject<LuaClrObjectValue>(2)) != null) {
                    binding = obj.BackingCustomObject as T;
                }

                if (binding == null) {
                    throw new LuaException("Binary operator not found for CLR object.");
                }

                var left = Wrap(1);
                toDispose.Add(left);

                var right = Wrap(2);
                toDispose.Add(right);

                var result = oper(binding, left, right);
                toDispose.Add(result);

                Push(result);
                return 1;
            });
        }

        public LuaClrObjectReference CreateClrObjectReference(LuaClrObjectValue obj)
        {
            if (obj == null) { throw new ArgumentNullException("obj"); }

            Push(obj);

            var wrap = Wrap(-1);

            LuaApi.lua_pop(LuaState, 1);

            return (LuaClrObjectReference)wrap;
        }

#if (__IOS__ || MONOTOUCH)
        [MonoTouch.MonoPInvokeCallback(typeof(LuaApi.lua_CFunction))]
#endif
        private static int ClrObjectGcCallbackWrapper(IntPtr state)
        {
            var runtime = GetSelf(state, LuaApi.lua_upvalueindex(1));

            // If it's null then the runtime has already been finalized.  In that case, all objects are already eligible
            // for collection anyway and we can just do nothing.
            if (runtime == null) { return 0; }

            return runtime.ClrObjectGcCallback(state);
        }

        private int ClrObjectGcCallback(IntPtr state)
        {
            // Don't CheckDisposed() here... we were called from Lua, so lua_close() could not have been called yet.

            var reference = TryGetReference(1);
            if (!reference.HasValue) {
                // Not good, but what can we do?
                return 0;
            }

            var obj = objectReferenceManager.GetReference(reference.Value);

            objectReferenceManager.DestroyReference(reference.Value);

            if (obj != null) {
                var finalizedBinding = obj.BackingCustomObject as ILuaFinalizedBinding;

                if (finalizedBinding != null) {
                    try { finalizedBinding.Finalized(this); }
                    catch { }
                }
            }

            return 0;
        }

        public LuaTable CreateTable()
        {
            CheckDisposed();

            LuaApi.lua_newtable(LuaState);
            var wrap = Wrap(-1);
            LuaApi.lua_pop(LuaState, 1);

            return (LuaTable)wrap;
        }

        // Useful when building an array; saves two p/invoke calls per element (table push and pop).
        public LuaTable CreateTable(IEnumerable<LuaValue> values)
        {
            if (values == null) { throw new ArgumentNullException("values"); }

            CheckDisposed();

            LuaApi.lua_newtable(LuaState);

            int i = 1;
            foreach (var v in values) {
                Push(v);
                LuaApi.lua_rawseti(LuaState, -2, i);
                ++i;
            }

            var wrap = Wrap(-1);
            LuaApi.lua_pop(LuaState, 1);

            return (LuaTable)wrap;
        }

        private int? CheckOnMainThread(IntPtr state)
        {
            if (state != GetMainThread(state)) {
                LuaApi.lua_pushboolean(state, 0);
                LuaApi.lua_pushstring(state, "Cannot enter the CLR from inside of a Lua coroutine.");
                return 2;
            }

            return null;
        }

#if (__IOS__ || MONOTOUCH)
        [MonoTouch.MonoPInvokeCallback(typeof(LuaApi.lua_CFunction))]
#endif
        private static int MethodWrapperCallCallbackWrapper(IntPtr state)
        {
            var runtime = GetSelf(state, LuaApi.lua_upvalueindex(1));

            if (runtime == null) {
                // The runtime doesn't exist, so Lua code shouldn't even be running now, Just return nothing at all.
                // This will be seen as an error by the bindings (assuming the bindings even still exist in memory), but
                // without any error message.

                return 0;
            }

            return runtime.MethodWrapperCallCallback(state);
        }

        private int MethodWrapperCallCallback(IntPtr state)
        {
            // We need to do this check as early as possible to avoid using the wrong state pointer.
            {
                var ret = CheckOnMainThread(state);
                if (ret.HasValue) { return ret.Value; }
            }

            OnEnterClr();
            try {
                var wrapper = (MethodWrapper)(GetClrObject<LuaClrObjectValue>(LuaApi.lua_upvalueindex(2)).ClrObject);

                return MakeManagedCall(state, wrapper);
            } finally {
                OnEnterLua();
            }
        }

        public LuaFunction CreateFunctionFromDelegate(Delegate d)
        {
            if (d == null) { throw new ArgumentNullException("d"); }

            return CreateFunctionFromMethodWrapper(new MethodWrapper(d));
        }

        internal LuaFunction CreateFunctionFromMethodWrapper(MethodWrapper wrapper)
        {
            if (wrapper == null) { throw new ArgumentNullException("wrapper"); }

            CheckDisposed();

            var top = LuaApi.lua_gettop(LuaState);

            try {
                Push(createManagedCallWrapper);

                PushSelf();
                Push(new LuaOpaqueClrObject(wrapper));
                LuaApi.lua_pushcclosure(LuaState, methodWrapperCallCallbackWrapper, 2);

                if (LuaApi.lua_pcall(LuaState, 1, 1, 0) != 0) {
                    throw new InvalidOperationException("Unable to create wrapper function.");
                }

                return (LuaFunction)Wrap(-1);
            } finally {
                LuaApi.lua_settop(LuaState, top);
            }
        }

        // Helper for handling the transition period when Lua calls into the CLR.
        //
        // Delegate should return the number of arguments it pushed.
        private delegate int LuaToClrBoundaryCallback(IList<IDisposable> toDispose);

        private int LuaToClrBoundary(IntPtr state, LuaToClrBoundaryCallback callback)
        {
            // We need to do this check as early as possible to avoid using the wrong state pointer.
            {
                var ret = CheckOnMainThread(state);
                if (ret.HasValue) { return ret.Value; }
            }

            var toDispose = new List<IDisposable>();

            var oldTop = LuaApi.lua_gettop(LuaState);

            OnEnterClr();
            try {
                // Pre-push the success flag.
                LuaApi.lua_pushboolean(LuaState, 1);

                return callback(toDispose) + 1;
            } catch (LuaException ex) {
                // If something bad happens, we can't be sure how much space is left on the stack.  Lua guarantees 20
                // free slots from the top, so restore the top back to the initial value to make sure we have enough
                // space to report the error.
                //
                // The same thing goes for the other exception handler.
                LuaApi.lua_settop(state, oldTop);

                LuaApi.lua_pushboolean(LuaState, 0);
                LuaApi.lua_pushstring(LuaState, ex.Message);
                return 2;
            } catch (Exception ex) {
                LuaApi.lua_settop(state, oldTop);

                LuaApi.lua_pushboolean(state, 0);

                PushCustomClrObject(new LuaTransparentClrObject(ex));
                return 2;
            } finally {
                try {
                    foreach (var i in toDispose) {
                        if (i != null) {
                            i.Dispose();
                        }
                    }
                } finally {
                    // If something bad happens while disposing stuff that's okay... but we CAN'T skip this, or Lua code
                    // running under a MemoryConstrainedLuaRuntime would be able to allocate more memory than the limit.
                    OnEnterLua();
                }
            }
        }

        public object ToClrObject(LuaValue val, Type target_type) {
            if (val is LuaNil) return null;
            if (val is LuaBoolean) {
                if (!target_type.IsAssignableFrom(typeof(bool))) {
                    throw new LuaException(string.Format("Expected a {0}, got a bool.", target_type));
                }
                return val.ToBoolean();
            }
            if (val is LuaFunction) {
                if (!target_type.IsAssignableFrom(typeof(LuaFunction))) {
                    throw new LuaException(string.Format("Expected a {0}, got a function.", target_type));
                }
                return val;
            }
            if (val is LuaLightUserdata) {
                if (!target_type.IsAssignableFrom(typeof(LuaLightUserdata))) {
                    throw new LuaException(string.Format("Expected a {0}, got light userdata.", target_type));
                }
                return val;
            }
            if (val is LuaNumber) {
                var num = val.ToNumber();

                if (target_type.IsEnum) {
                    return Convert.ChangeType(num, Enum.GetUnderlyingType(target_type));
                }

                try {
                    return Convert.ChangeType(num, target_type);
                } catch {
                    throw new LuaException(string.Format("Expected a {0}, got a number.", target_type));
                }
            }
            if (val is LuaString) {
                if (!target_type.IsAssignableFrom(typeof(string))) {
                    throw new LuaException(string.Format("Expected a {0}, got a string.", target_type));
                }

                return val.ToString();
            }
            if (val is LuaTable) {
                if (target_type.IsAssignableFrom(typeof(LuaTable))) {
                    return val;
                } else if (target_type.IsArray) {
                    return ((LuaTable)val).ConvertToArray(target_type.GetElementType());
                } else {
                    throw new LuaException(string.Format("Expected a {0}, got a table.", target_type));
                }
            }
            if (val is LuaThread) {
                if (!target_type.IsAssignableFrom(typeof(LuaThread))) {
                    throw new LuaException(string.Format("Expected a {0}, got a thread.", target_type));
                }
                return val;
            }
            if (val is LuaUserdata) {
                Push(val);
                try {
                    LuaClrObjectValue clrObject;
                    if ((clrObject = TryGetClrObject<LuaClrObjectValue>(-1)) != null) {
                        return clrObject.ClrObject;
                    } else if (target_type.IsAssignableFrom(typeof(LuaUserdata))) {
                        return val;
                    } else {
                        throw new LuaException(string.Format("Expected a {0}, got userdata.", target_type));
                    }
                } finally {
                    LuaApi.lua_remove(LuaState, -1);
                }
            }

            throw new LuaException(string.Format("Cannot convert Lua type {0} to CLR type {1}.", val.GetType(), target_type));
        }

        private int MakeManagedCall(IntPtr state, MethodWrapper wrapper)
        {
            var toDispose = new List<IDisposable>();
            try {
                // As with Call(), we are crossing a Lua<->CLR boundary, so release any references that have been 
                // queued to be released.
                ProcessReleasedReferences();

                var nargs = LuaApi.lua_gettop(state);

                // By Lua convention, extra arguments are ignored.  For omitted/nil arguments, we will first see if the
                // managed argument declaration specifies a default value.  Otherwise, for reference/nullable arguments,
                // we will pass null (by Lua convention).  Otherwise, we will raise an error.
                //
                // For numeric types will try to be smart and convert the argument, if possible.
                var parms = wrapper.Method.GetParameters();

                object[] args;
                object target = null;

                var uses_self_arg = MethodMode == LuaMethodMode.PassSelf && !wrapper.IsDelegate && !wrapper.IsStatic;

                LuaValue wrapped;

                if (parms.Length == 1 && parms[0].ParameterType == typeof(LuaVararg)) {
                    if (uses_self_arg) {
                        var luaType = LuaApi.lua_type(state, 1);

                        var ptype = wrapper.Method.DeclaringType;

                        if (LuaApi.lua_type(state, 1) != LuaApi.LuaType.Userdata) {
                            throw new LuaException(string.Format("Argument self: Expected a {0}, got a non-userdata. Did you mean to run something:something(a, b, c) but instead ran something.something(a, b, c)?", ptype));
                        }

                        var obj = Wrap(1);
                        if (!ptype.IsAssignableFrom(obj.CLRMappedType)) {
                            throw new LuaException(string.Format("Argument self: Expected a {0}, got a {1}. Did you mean to run something:something(a, b, c) but instead ran something.something(a, b, c)?", ptype, obj.CLRMappedType));
                        }

                        target = obj.CLRMappedObject;
                        toDispose.Add(obj);
                    }

                    // Special case: wrap all arguments into a vararg.
                    //
                    // We still use toDispose instead of disposing the vararg later, because any exception thrown from
                    // Wrap() could leak some objects.  It's safer to add the wrapped objects to toDisposed as we
                    // create them to prevent this possibility.
                    var varargs = new LuaValue[nargs - 1];

                    for (int i = 1; i < nargs; ++i) {
                        varargs[i - 1] = wrapped = Wrap(i + 1);
                        toDispose.Add(wrapped);
                    }

                    // "Retain ownership" is true here because we don't want references copied.  Since we don't call
                    // Dispose() on the vararg, they won't be disposed anyway.  This is what we want.  (The finally
                    // block will take care of that.)
                    args = new object[] { new LuaVararg(varargs, true) };
                } else {
                    if (uses_self_arg) {
                        if (nargs >= 1) {
                            var ptype = wrapper.Method.DeclaringType;

                            if (LuaApi.lua_type(state, 1) != LuaApi.LuaType.Userdata) {
                                throw new LuaException(string.Format("Argument self: Expected a {0}, got a non-userdata. Did you mean to run something:something(a, b, c) but instead ran something.something(a, b, c)?", ptype));
                            }

                            var obj = Wrap(1);
                            if (!ptype.IsAssignableFrom(obj.CLRMappedType)) {
                                throw new LuaException(string.Format("Argument self: Expected a {0}, got a {1}. Did you mean to run something:something(a, b, c) but instead ran something.something(a, b, c)?", ptype, obj.CLRMappedType));
                            }

                            target = obj.CLRMappedObject;
                            toDispose.Add(obj);
                        } else {
                            throw new LuaException("Argument self is never optional. Did you mean to run something:something() but instead ran something.something()?");
                        }
                    }

                    args = new object[parms.Length];

                    for (int i = 0; i < parms.Length; ++i) {
                        var ptype = parms [i].ParameterType;

                        var lua_i = i + 1;
                        if (uses_self_arg) lua_i = i + 2;
                        
                        var luaType = i >= nargs ? LuaApi.LuaType.None : LuaApi.lua_type(state, lua_i);

                        switch (luaType) {
                            case LuaApi.LuaType.None:
                            case LuaApi.LuaType.Nil:
                                // Omitted/nil argument.
                                if (parms[i].IsOptional) {
                                    args[i] = parms[i].DefaultValue;
                                } else if (!ptype.IsValueType || (ptype.IsGenericType && ptype.GetGenericTypeDefinition() == typeof(Nullable<>))) {
                                    args[i] = null;
                                } else {
                                    throw new LuaException(string.Format("Argument {0} is not optional.", i + 1));
                                }
                                break;

                            case LuaApi.LuaType.Boolean:
                                // Bool means bool.
                                if (!ptype.IsAssignableFrom(typeof(bool))) {
                                    throw new LuaException(string.Format("Argument {0}: Expected a {1}, got a bool.", i + 1, ptype));
                                }

                                args[i] = LuaApi.lua_toboolean(state, lua_i) != 0;
                                break;

                            case LuaApi.LuaType.Function:
                                if (!ptype.IsAssignableFrom(typeof(LuaFunction))) {
                                    throw new LuaException(string.Format("Argument {0}: Expected a {1}, got a function.", i + 1, ptype));
                                }

                                args[i] = wrapped = Wrap(lua_i);
                                toDispose.Add(wrapped);
                                break;

                            case LuaApi.LuaType.LightUserdata:
                                if (ptype.IsAssignableFrom(typeof(LuaLightUserdata))) {
                                    args[i] = wrapped = Wrap(lua_i);
                                    toDispose.Add(wrapped);
                                } else {
                                    throw new LuaException(string.Format("Argument {0}: Expected a {1}, got light userdata.", i + 1, ptype));
                                }
                                break;

                            case LuaApi.LuaType.Number:
                                if (ptype.IsEnum) {
                                    var num = LuaApi.lua_tonumber(state, lua_i);
                                    args [i] = Convert.ChangeType(num, Enum.GetUnderlyingType(ptype));
                                    break;
                                }

                                try {
                                    args[i] = Convert.ChangeType(LuaApi.lua_tonumber(state, lua_i), ptype);
                                } catch (Exception e) {
                                    throw new LuaException(string.Format("Argument {0}: Expected a {1}, got a number.", i + 1, ptype));
                                }
                                break;

                            case LuaApi.LuaType.String:
                                if (!ptype.IsAssignableFrom(typeof(string))) {
                                    throw new LuaException(string.Format("Argument {0}: Expected a {1}, got a string.", i + 1, ptype));
                                }

                                args[i] = LuaApi.lua_tostring(state, lua_i);
                                break;

                            case LuaApi.LuaType.Table:
                                if (ptype.IsAssignableFrom(typeof(LuaTable))) {
                                    args [i] = wrapped = Wrap(lua_i);
                                    toDispose.Add(wrapped);
                                } else if (ptype.IsArray) {
                                    var tab = (LuaTable)Wrap(lua_i);
                                    args [i] = tab.ConvertToArray(ptype.GetElementType());
                                    wrapped = tab;
                                    toDispose.Add(wrapped);
                                } else {
                                    throw new LuaException(string.Format("Argument {0}: Expected a {1}, got a table.", i + 1, ptype));
                                }
                                break;

                            case LuaApi.LuaType.Thread:
                                if (!ptype.IsAssignableFrom(typeof(LuaThread))) {
                                    throw new LuaException(string.Format("Argument {0}: Expected a {1}, got a thread.", i + 1, ptype));
                                }

                                args[i] = wrapped = Wrap(lua_i);
                                toDispose.Add(wrapped);
                                break;

                            case LuaApi.LuaType.Userdata:
                                // With CLR objects, we have ambiguity.  We could test if the parameter type is
                                // compatible with LuaUserdata first, and if so wrap the Lua object.  But, perhaps the
                                // opaque object IS a LuaUserdata instance?  There's really no way to be smart in that
                                // situation.  Therefore, we will just unwrap any CLR object and pray that was the
                                // right thing to do.  (Especially since it's kind of silly to hand Lua code userdata
                                // wrapped in userdata.  Further, we are trying to map to CLR types; if code wants Eluant
                                // objects then it should take a LuaVararg instead.)
                                LuaClrObjectValue clrObject;
                                if ((clrObject = TryGetClrObject<LuaClrObjectValue>(lua_i)) != null) {
                                    if (ptype.IsAssignableFrom(typeof(LuaClrObjectValue))) {
                                        args[i] = clrObject;
                                    } else if (ptype.IsAssignableFrom(typeof(LuaTransparentClrObject))) {
                                        args[i] = clrObject;
                                    } else if (ptype.IsAssignableFrom(typeof(LuaClrTypeObject))) {
                                       args[i] = clrObject;
                                    } else args[i] = clrObject.ClrObject;
                                } else if (ptype.IsAssignableFrom(typeof(LuaUserdata))) {
                                    args[i] = wrapped = Wrap(lua_i);
                                    toDispose.Add(wrapped);
                                } else {
                                    throw new LuaException(string.Format("Argument {0}: Expected {1}, got userdata.", i + 1, ptype));
                                }

                                break;

                            default:
                                throw new LuaException(string.Format("Argument {0}: Cannot proxy Lua type {1}.", i + 1, luaType));
                        }
                    }
                }

                object ret;
                try {
                    ret = wrapper.Invoke(target, args);
                } catch (MemberAccessException) {
                    throw new LuaException("Invalid argument(s).");
                } catch (TargetInvocationException ex) {
                    if (ex.InnerException is LuaException) {
                        throw new WrapException(ex.InnerException);
                    }
                    throw;
                }

                // Process any released references again.
                ProcessReleasedReferences();

                // If the method was declared to return void we can just stop now.
                if (wrapper.Method.ReturnType == typeof(void)) {
                    LuaApi.lua_pushboolean(state, 1);
                    return 1;
                }

                // If a vararg is returned, unpack the results.
                var retVararg = ret as LuaVararg;
                if (retVararg != null) {
                    // We do need to dispose the vararg.  If the calling code wants to retain references then it can
                    // pass takeOwnership:false to the LuaVararg constructor.  If we didn't dispose of it then the
                    // called method would have no way to dispose of references that it didn't need anymore.
                    toDispose.Add(retVararg);

                    LuaApi.lua_pushboolean(state, 1);

                    if (LuaApi.lua_checkstack(LuaState, 1 + retVararg.Count) == 0) {
                        throw new LuaException("Cannot grow stack for results.");
                    }

                    foreach (var a in retVararg) {
                        Push(a);
                    }

                    return retVararg.Count + 1;
                }
                var retValue = AsLuaValue(ret);
                if (retValue == null) {
                    throw new LuaException(string.Format("Unable to convert object of type {0} to Lua value.",
                                                         ret.GetType().FullName));
                }

                // Similar to the vararg case, we always dispose the returned value object.
                //
                // 1. If we created it ourselves, we need to dispose of it anyway.
                //
                // 2. If the callee created an object with the sole purpose of being returned (tables are probably a
                //    common case of that) they would have no way to dispose of the CLR reference to the object.  So we
                //    do that here.  (If they didn't want the reference disposed they could return value.CopyHandle().)
                toDispose.Add(retValue);

                LuaApi.lua_pushboolean(state, 1);
                Push(retValue);
                return 2;
            } catch (LuaException ex) {
                LuaApi.lua_pushboolean(state, 0);
                PushCustomClrObject(new LuaTransparentClrObject(ex));
                return 2;
            } catch (Exception ex) {
                LuaApi.lua_pushboolean(state, 0);
                PushCustomClrObject(new LuaTransparentClrObject(ex));
                return 2;
            } finally {
                // Dispose whatever we need to.  It's okay to dispose result objects, as that will only release the CLR
                // reference to them; they will still be alive on the Lua stack.
                foreach (var o in toDispose) {
                    if (o != null && (!(o is LuaReference) || ((LuaReference)o).DisposeAfterManagedCall)) {
                        o.Dispose();
                    }
                }
            }
        }

        public int StackTop { get { return LuaApi.lua_gettop(LuaState); } }

        public LuaValue AsLuaValue(object obj)
        {
            CheckDisposed();

            if (obj == null) {
                return LuaNil.Instance;
            }

            var luaValue = obj as LuaValue;
            if (luaValue != null) {
                return luaValue;
            }

            if (obj is bool) {
                return (LuaBoolean)(bool)obj;
            }

            var delegateObject = obj as Delegate;
            if (delegateObject != null) {
                return CreateFunctionFromDelegate(delegateObject);
            }

            var str = obj as string;
            if (str != null) {
                return (LuaString)str;
            }

            var type = obj as Type;
            if (type != null) {
                return new LuaClrTypeObject(type);
            }

            try {
                return (LuaNumber)(double)Convert.ChangeType(obj, typeof(double));
            } catch { }


            try {
                return new LuaTransparentClrObject(obj, autobind: true);
            } catch { }

            return null;
        }

        public LuaWeakReference<T> CreateWeakReference<T>(T reference)
            where T : LuaReference
        {
            CheckDisposed();

            if (reference == null) { throw new ArgumentNullException("reference"); }

            reference.CheckDisposed();
            reference.AssertRuntimeIs(this);

            LuaApi.lua_newtable(LuaState);

            LuaApi.luaL_getmetatable(LuaState, WEAKREFERENCE_METATABLE);
            LuaApi.lua_setmetatable(LuaState, -2);

            Push(reference);
            LuaApi.lua_rawseti(LuaState, -2, 1);

            var refTable = (LuaTable)Wrap(-1);
            LuaApi.lua_pop(LuaState, 1);

            return new LuaWeakReference<T>(refTable);
        }

        internal void PushWeakReference<T>(LuaWeakReference<T> reference)
            where T : LuaReference
        {
            CheckDisposed();

            Push(reference.WeakTable);
            LuaApi.lua_rawgeti(LuaState, -1, 1);
        }

        internal T GetWeakReference<T>(LuaWeakReference<T> reference)
            where T : LuaReference
        {
            PushWeakReference(reference);

            var wrapped = Wrap(-1);

            LuaApi.lua_pop(LuaState, 2);

            if (wrapped == LuaNil.Instance) {
                return null;
            }

            return (T)wrapped;
        }

        private class ObjectReferenceManager<T> where T : class
        {
            private Dictionary<int, T> references = new Dictionary<int, T>();
            private int nextReference = 1;

            public int ReferenceCount {
                get {
                    return nextReference - 1;
                }
            }

            public ObjectReferenceManager() { }

            public T GetReference(int reference)
            {
                if (reference == 0) {
                    return null;
                }

                T obj;
                if (!references.TryGetValue(reference, out obj)) {
                    throw new InvalidOperationException("No such reference: " + reference);
                }

                return obj;
            }

            public void DestroyReference(int reference)
            {
                references.Remove(reference);

                nextReference = Math.Min(nextReference, reference);
            }

            public int CreateReference(T obj)
            {
                if (obj == null) {
                    return 0;
                }

                var start = nextReference;
                while (references.ContainsKey(nextReference)) {
                    if (nextReference == int.MaxValue) {
                        nextReference = 1;
                    } else {
                        ++nextReference;
                    }

                    if (nextReference == start) {
                        throw new InvalidOperationException("Reference key space exhausted.");
                    }
                }

                references[nextReference] = obj;

                return nextReference;
            }
        }

        // Delegate-like, but doesn't need a particular delegate type to do its work (which would be a problem for
        // functions auto-generated from a CLR object method).
        internal class MethodWrapper
        {
            public bool IsDelegate { get; private set; } = false;
            public bool IsStatic { get; private set; } = false;
            public object Target { get; private set; }
            public MethodInfo Method { get; private set; }

            public MethodWrapper(object target, MethodInfo method, bool @static = false)
            {
                if (method == null) { throw new ArgumentNullException("method"); }

                Target = target;
                Method = method;
                IsDelegate = false;
                IsStatic = @static;
            }

            public MethodWrapper(Delegate d, bool @static = false)
            {
                if (d == null) { throw new ArgumentNullException("d"); }

                Target = d.Target;
                Method = d.Method;
                IsDelegate = true;
                IsStatic = @static;
            }

            public object Invoke(object target, params object[] parms)
            {
                target = target ?? Target;

                try {
                    return Method.Invoke(target, parms);
                } catch (TargetInvocationException e) {
                    throw e.InnerException == null ? e : (Exception)new WrapException(e.InnerException);
                }
            }
        }
    }
}



