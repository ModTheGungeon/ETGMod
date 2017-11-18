using System;
using System.Reflection;
using System.Collections.Generic;

using TrampolineMap = System.Collections.Generic.Dictionary<long, System.Reflection.MethodInfo>;
using System.Reflection.Emit;
using System.Linq.Expressions;
using MonoMod.Detour;
using Eluant;

namespace ETGMod {
    public class RuntimeHooks {
        private static TrampolineMap _Trampolines = new TrampolineMap();
        private static HashSet<long> _DispatchHandlers = new HashSet<long>();

        private static Logger _Logger = new Logger("RuntimeHooks");

        public static long MethodToken(MethodBase method)
            => (long)((ulong)method.MetadataToken) << 32 | (
                (uint)((method.Module.Name.GetHashCode() << 5) + method.Module.Name.GetHashCode()) ^
                (uint)method.Module.Assembly.FullName.GetHashCode());

        public static void InstallDispatchHandler(MethodBase method) {
            var method_token = MethodToken(method);
            if (_DispatchHandlers.Contains(method_token)) {
                _Logger.Debug($"Not installing dispatch handler for {method.Name} ({method_token}) - it's already installed");
                return;
            }

            var parms = method.GetParameters();
            var ptypes = new Type[parms.Length + 1];
            ptypes[0] = method.DeclaringType;
            for (int i = 0; i < parms.Length; i++) {
                ptypes[i + 1] = parms[i].ParameterType;
            }

            var method_returns = false;
            if (method is MethodInfo) {
                if (((MethodInfo)method).ReturnType != typeof(void)) method_returns = true;
            }

            var dm = new DynamicMethod(
                $"DISPATCH HANDLER FOR METHOD TOKEN {method_token}",
                method is MethodInfo ? ((MethodInfo)method).ReturnType : typeof(void),
                ptypes,
                method.Module,
                skipVisibility: true
            );

            var il = dm.GetILGenerator();

            var loc_args = il.DeclareLocal(typeof(object[]));
            loc_args.SetLocalSymInfo("args");

            var loc_target = il.DeclareLocal(typeof(object));
            loc_target.SetLocalSymInfo("target");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stloc, loc_target);

            il.Emit(OpCodes.Ldc_I4, ptypes.Length - 1);
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Stloc, loc_args);

            for (int i = 0; i < ptypes.Length - 1; i++) {
                il.Emit(OpCodes.Ldloc, loc_args);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldarg, i + 1);
                if (ptypes[i + 1].IsValueType) {
                    il.Emit(OpCodes.Box, ptypes[i + 1]);
                }
                il.Emit(OpCodes.Stelem, typeof(object));
            }

            var callback = typeof(RuntimeHooks).GetMethod("_HandleDispatch", BindingFlags.Static | BindingFlags.NonPublic);

            il.Emit(OpCodes.Ldc_I8, method_token);
            il.Emit(OpCodes.Ldloc, loc_target);
            il.Emit(OpCodes.Ldloc, loc_args);

            il.Emit(OpCodes.Call, callback);

            if (!method_returns) il.Emit(OpCodes.Pop);
            if (method_returns && method is MethodInfo && ((MethodInfo)method).ReturnType.IsValueType) {
                il.Emit(OpCodes.Unbox_Any, ((MethodInfo)method).ReturnType);
            }
            il.Emit(OpCodes.Ret);

            RuntimeDetour.Detour(
                from: method,
                to: dm
            );

            _Trampolines[method_token] = RuntimeDetour.CreateOrigTrampoline(method);
            _DispatchHandlers.Add(method_token);

            _Logger.Debug($"Installed dispatch handler for {method.Name} (token {method_token})");
        }

        private static object _RunLuaHook(ModLoader.ModInfo mod, LuaRuntime runtime, long method_token, object target, object[] args, out bool returned) {
            returned = false;

            if (mod.Hooks != null) {
                _Logger.Debug($"Running hook '{method_token}' in mod {mod.Name}");
                var obj = mod.Hooks.TryRun(runtime, method_token, target, args, out returned);
                if (returned) return obj;
            }
            if (mod.HasAnyEmbeddedMods) {
                for (int i = 0; i < mod.EmbeddedMods.Count; i++) {
                    var obj = _RunLuaHook(mod.EmbeddedMods[i], runtime, method_token, target, args, out returned);
                    if (returned) return obj;
                }
            }

            return null;
        }

        private static object _HandleDispatch(long method_token, object target, object[] args) {
            string target_name;
            if (target == null) target_name = "NULL";
            else target_name = $"[{target.GetType()}] {target}";
            _Logger.Debug($"Handling dispatch for {method_token} with target {target_name} and {args.Length} argument(s)");
            for (int i = 0; i < args.Length; i++) {
                _Logger.DebugIndent(args[i]);
            }

            for (int i = 0; i < ETGMod.ModLoader.LoadedMods.Count; i++) {
                var mod = ETGMod.ModLoader.LoadedMods[i];
                bool returned;

                var obj = _RunLuaHook(mod, ETGMod.ModLoader.LuaState, method_token, target, args, out returned);

                if (returned) {
                    _Logger.Debug($"Short circuit - hook from mod {mod.Name} returned");
                    return obj;
                }
            }

            MethodInfo trampoline;
            if (_Trampolines.TryGetValue(method_token, out trampoline)) {
                var targs = new object[args.Length + 1];
                targs[0] = target;
                args.CopyTo(targs, 1);

                return trampoline.Invoke(target, targs);
            }

            throw new Exception("Tried to handle dispatch without a trampoline having been set up");
        }
    }
}
