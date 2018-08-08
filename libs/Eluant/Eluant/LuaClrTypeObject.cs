//
// LuaClrTypeObject.cs
//
// Authors:
//       Zatherz <zatherz@zatherz.eu>
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
using Eluant.ObjectBinding;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace Eluant
{
    public class LuaClrTypeObject : LuaClrObjectValue, IEquatable<LuaClrTypeObject>, IBindingContext
    {
        public IBindingSecurityPolicy BindingSecurityPolicy { get; private set; }
        public ILuaBinder Binder { get; private set; } = new ReflectionLuaBinder(BindingFlags.Static | BindingFlags.Public, obj_is_type: true);
        private LuaClrTypeObjectProxy proxy;
        private static readonly IBindingSecurityPolicy defaultSecurityPolicy = new BasicBindingSecurityPolicy(MemberSecurityPolicy.Permit);

        public Type Type { get; private set; }

        public LuaClrTypeObject(Type type, IBindingSecurityPolicy binding_security_policy = null) : base(type)
        {
            if (binding_security_policy != null) BindingSecurityPolicy = binding_security_policy;
            else BindingSecurityPolicy = defaultSecurityPolicy;

            Type = type;
            proxy = new LuaClrTypeObjectProxy(this, type);
        }

        internal override void Push(LuaRuntime runtime)
        {
            runtime.PushCustomClrObject(this);
        }

        public override bool Equals(LuaValue other)
        {
            return Equals(other as LuaClrTypeObject);
        }

        public bool Equals(LuaClrTypeObject obj)
        {
            return obj != null && obj.ClrObject == ClrObject;
        }

        internal override object BackingCustomObject {
            get { return proxy; }
        }

        private class LuaClrTypeObjectProxy : ILuaTableBinding, ILuaEqualityBinding, ILuaToStringBinding, ILuaCallBinding
        {
            private Type type;
            private LuaClrTypeObject clrObject;

            public LuaClrTypeObjectProxy(LuaClrTypeObject clr_object, Type type)
            {
                this.clrObject = clr_object;
                this.type = type;
            }

            private static LuaClrTypeObject GetObjectValue(LuaValue v)
            {
                var r = v as LuaClrObjectReference;
                if (r != null) {
                    return r.ClrObjectValue as LuaClrTypeObject;
                }

                return null;
            }

            #region ILuaToStringBinding implementation
            public LuaString ToLuaString(LuaRuntime runtime)
            {
                return $"CLR type {type.ToString()}";
            }
            #endregion

            #region ILuaEqualityBinding implementation
            public LuaValue Equals(LuaRuntime runtime, LuaValue left, LuaValue right)
            {
                var leftObj = GetObjectValue(left);
                var rightObj = GetObjectValue(right);

                if (ReferenceEquals(leftObj, rightObj)) {
                    return true;
                }

                if (leftObj == null || rightObj == null) {
                    return false;
                }

                return leftObj.ClrObject == rightObj.ClrObject;
            }

            #endregion

            private static string KeyToString(LuaValue key)
            {
                var str = key as LuaString;
                if (str != null) {
                    return str.Value;
                }

                var num = key as LuaNumber;
                if (num != null) {
                    return num.Value.ToString();
                }

                return null;
            }

            private List<MemberInfo> GetMembers(LuaValue keyValue)
            {
                var key = KeyToString(keyValue);

                if (key != null) {
                    return clrObject.Binder.GetMembersByName(type, key)
                                    .Where(i => clrObject.BindingSecurityPolicy.GetMemberSecurityPolicy(i) == MemberSecurityPolicy.Permit)
                            .ToList();
                }

                return new List<MemberInfo>();
            }

            #region ILuaCallBinding implementation

            public LuaVararg Call(LuaRuntime runtime, LuaValue self, LuaVararg arguments)
            {
                var constructors = type.GetConstructors();
                var found_ctor = false;
                object [] ary = null;

                for (int i = 0; i < constructors.Length; i++) {
                    var ctor = constructors [i];
                    var parms = ctor.GetParameters();

                    if (parms.Length != arguments.Count) continue;

                    ary = new object [arguments.Count];
                    for (int j = 0; j < parms.Length; j++) {
                        var parm = parms [j];


                        try {
                            ary [j] = Convert.ChangeType(arguments[j], parm.ParameterType);
                        } catch {
                            try {
                                ary [j] = Convert.ChangeType(arguments[j].CLRMappedObject, parm.ParameterType);
                            } catch {
                                goto next_ctor;
                            }
                        }
                    }

                    found_ctor = true;

                    next_ctor:;
                }

                if (!found_ctor) {
                    throw new LuaException($"The type {type.FullName} does not have a matching constructor.");
                }

                var inst = Activator.CreateInstance(type, args: ary);
                arguments.Dispose();

                return new LuaVararg(new LuaValue [] { new LuaTransparentClrObject(inst, autobind: true) }, false);
            }

            #endregion

            #region ILuaTableBinding implementation

            public LuaValue this [LuaRuntime runtime, LuaValue keyValue] {
                get {
                    var members = GetMembers(keyValue);

                    if (members.Count == 1) {
                        var method = members [0] as MethodInfo;
                        if (method != null) {
                            return runtime.CreateFunctionFromMethodWrapper(new LuaRuntime.MethodWrapper(type, method, @static: true));
                        }


                        var property = members [0] as PropertyInfo;
                        if (property != null) {
                            var getter = property.GetGetMethod();
                            if (getter == null) {
                                throw new LuaException("Property is write-only.");
                            }
                            if (getter.GetParameters().Length != 0) {
                                throw new LuaException("Cannot get an indexer.");
                            }

                            var ret = property.GetValue(type, null);
                            return clrObject.Binder.ObjectToLuaValue(ret, clrObject, runtime);
                        }

                        var field = members [0] as FieldInfo;
                        if (field != null) {
                            return clrObject.Binder.ObjectToLuaValue(field.GetValue(type), clrObject, runtime);
                        }
                    }

                    return LuaNil.Instance;
                }
                set {
                    var members = GetMembers(keyValue);

                    if (members.Count == 1) {
                        var property = members [0] as PropertyInfo;
                        if (property != null) {
                            var setter = property.GetSetMethod();
                            if (setter == null) {
                                throw new LuaException("Property is read-only.");
                            }
                            if (setter.GetParameters().Length != 1) {
                                throw new LuaException("Cannot set an indexer.");
                            }

                            object v;
                            try {
                                v = value.ToClrType(property.PropertyType);
                            } catch {
                                throw new LuaException("Value is incompatible with this property.");
                            }

                            property.SetValue(null, v, null);
                            return;
                        }

                        var field = members [0] as FieldInfo;
                        if (field != null) {
                            object v;
                            try {
                                v = value.ToClrType(field.FieldType);
                            } catch {
                                throw new LuaException("Value is incompatible with this property.");
                            }

                            field.SetValue(null, v);
                            return;
                        }
                    }

                    throw new LuaException("Property/field not found: " + keyValue.ToString());
                }
            }

            #endregion
        }
    }
}

