using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

public delegate object DynamicMethodDelegate(object target, params object[] args);
/// <summary>
/// Stolen from http://theinstructionlimit.com/fast-net-reflection and FEZ. Thanks, Renaud!
/// </summary>

// I will be in the list of contributors! And no one will know that all I did was change
// field names from camelCase to PascalCase.

public static class ReflectionHelper {
    private static readonly Type[] _EmptyTypes = new Type[0];
    private static readonly Type[] _ManyObjects = new Type[2] {typeof(object), typeof(object[])};
    private static readonly object[] _NoObjects = new object[0];
    private static readonly Dictionary<MethodInfo, DynamicMethodDelegate> _MethodCache = new Dictionary<MethodInfo, DynamicMethodDelegate>();
    private static readonly Dictionary<Type, DynamicMethodDelegate> _ConstructorCache = new Dictionary<Type, DynamicMethodDelegate>();

    public static DynamicMethodDelegate CreateDelegate(this MethodBase method) {
        var dynam = new DynamicMethod(string.Empty, typeof(object), _ManyObjects, typeof(ReflectionHelper).Module, true);
        ILGenerator il = dynam.GetILGenerator();

        ParameterInfo[] args = method.GetParameters();

        Label argsOK = il.DefineLabel();

        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldlen);
        il.Emit(OpCodes.Ldc_I4, args.Length);
        il.Emit(OpCodes.Beq, argsOK);

        il.Emit(OpCodes.Newobj, typeof(TargetParameterCountException).GetConstructor(Type.EmptyTypes));
        il.Emit(OpCodes.Throw);

        il.MarkLabel(argsOK);

        if (!method.IsStatic && !method.IsConstructor) {
            il.Emit(OpCodes.Ldarg_0);
            if (method.DeclaringType.IsValueType) {
                il.Emit(OpCodes.Unbox, method.DeclaringType);
            }
        }

        for (int i = 0; i < args.Length; i++) {
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldelem_Ref);

            if (args[i].ParameterType.IsValueType) {
                il.Emit(OpCodes.Unbox_Any, args[i].ParameterType);
            }
        }

        if (method.IsConstructor) {
            il.Emit(OpCodes.Newobj, (ConstructorInfo) method);
        } else if (method.IsFinal || !method.IsVirtual) {
            il.Emit(OpCodes.Call, (MethodInfo) method);
        } else {
            il.Emit(OpCodes.Callvirt, (MethodInfo) method);
        }

        Type returnType = method.IsConstructor ? method.DeclaringType : ((MethodInfo) method).ReturnType;
        if (returnType != typeof(void)) {
            if (returnType.IsValueType) {
                il.Emit(OpCodes.Box, returnType);
            }
        } else {
            il.Emit(OpCodes.Ldnull);
        }

        il.Emit(OpCodes.Ret);

        return (DynamicMethodDelegate) dynam.CreateDelegate(typeof(DynamicMethodDelegate));
    }

    public static DynamicMethodDelegate GetDelegate(this MethodInfo method) {
        DynamicMethodDelegate dmd;
        if (_MethodCache.TryGetValue(method, out dmd)) {
            return dmd;
        }

        dmd = CreateDelegate(method);
        _MethodCache.Add(method, dmd);

        return dmd;
    }

    public static object Instantiate(Type type) {
        if (type.IsValueType) {
            return Activator.CreateInstance(type);
        }
        if (type.IsArray) {
            return Array.CreateInstance(type.GetElementType(), 0);
        }
        DynamicMethodDelegate dmd;
        lock (_ConstructorCache) {
            if (!_ConstructorCache.TryGetValue(type, out dmd)) {
                dmd = CreateDelegate(type.GetConstructor(_EmptyTypes));
                _ConstructorCache.Add(type, dmd);
            }
        }
        return dmd(null, _NoObjects);
    }

    public static object InvokeMethod(MethodInfo info, object targetInstance, params object[] arguments) {
        return GetDelegate(info)(targetInstance, arguments);
    }

    public static object GetValue(PropertyInfo member, object instance) {
        return InvokeMethod(member.GetGetMethod(true), instance, _NoObjects);
    }

    public static object GetValue(MemberInfo member, object instance) {
        if (member is PropertyInfo) {
            return GetValue((PropertyInfo) member, instance);
        } else if (member is FieldInfo) {
            return ((FieldInfo) member).GetValue(instance);
        }
        throw new NotImplementedException();
    }

    public static void SetValue(PropertyInfo member, object instance, object value) {
        InvokeMethod(member.GetSetMethod(true), instance, new object[1] { value });
    }

    public static void SetValue(MemberInfo member, object instance, object value) {
        if (member is PropertyInfo) {
            SetValue((PropertyInfo) member, instance, value);
        } else if (member is FieldInfo){
            ((FieldInfo) member).SetValue(instance, value);
        } else {
            throw new NotImplementedException();
        }
    }

    public static Action<byte[]> CreateRPCDelegate(this MethodInfo info, object instance = null) {
        return delegate (byte[] b) {
            info.Invoke(instance, new object[] { b });
        };
    }

}
