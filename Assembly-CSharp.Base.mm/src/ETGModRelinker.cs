using System;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ionic.Zip;
using Mono.Cecil;
using System.Security.Cryptography;
using Mono.Cecil.Cil;

public static class ETGModRelinker {

    public static ModuleDefinition ETGModule;
    public static string ETGChecksum;

    public static Assembly GetRelinkedAssembly(this ETGModuleMetadata metadata, Stream stream) {
        if (ETGModule == null) {
            ETGModule = ModuleDefinition.ReadModule(Assembly.GetAssembly(typeof(ETGModRelinker)).Location, new ReaderParameters(ReadingMode.Immediate));
        }

        if (!Directory.Exists(ETGMod.RelinkCacheDirectory)) {
            Directory.CreateDirectory(ETGMod.RelinkCacheDirectory);
        }

        string cachedName = metadata.DLL.Substring(0, metadata.DLL.Length - 3) + "dat";
        string cachedPath = Path.Combine(
            ETGMod.RelinkCacheDirectory,
            cachedName
        );
        string cachedChecksumPath = Path.Combine(
            ETGMod.RelinkCacheDirectory,
            cachedName + ".sum"
        );

        string[] checksums = new string[2];
        using (MD5 md5 = MD5.Create()) {
            if (ETGChecksum == null) {
                using (FileStream fs = File.OpenRead(Assembly.GetAssembly(typeof(ETGModRelinker)).Location)) {
                    ETGChecksum = md5.ComputeHash(fs).ToHexadecimalString();
                }
            }
            checksums[0] = ETGChecksum;

            string modPath = metadata.Archive;
            if (modPath.Length == 0) {
                modPath = metadata.DLL;
            }
            using (FileStream fs = File.OpenRead(modPath)) {
                checksums[1] = md5.ComputeHash(fs).ToHexadecimalString();
            }
        }

        if (File.Exists(cachedPath) && File.Exists(cachedChecksumPath)) {
            if (checksums.ChecksumsEqual(File.ReadAllLines(cachedChecksumPath))) {
                return Assembly.LoadFrom(cachedPath);
            }
        }

        ModuleDefinition md = ModuleDefinition.ReadModule(stream, new ReaderParameters(ReadingMode.Immediate));

        foreach (TypeDefinition type in md.Types) {
            RelinkType(type);
        }

        if (File.Exists(cachedPath)) {
            File.Delete(cachedPath);
        }
        using (FileStream fs = File.OpenWrite(cachedPath)) {
            md.Write(fs);
        }

        md = ModuleDefinition.ReadModule(cachedPath, new ReaderParameters(ReadingMode.Immediate));

        if (File.Exists(cachedChecksumPath)) {
            File.Delete(cachedChecksumPath);
        }
        File.WriteAllLines(cachedChecksumPath, checksums);

        return Assembly.LoadFrom(cachedPath);
    }

    public static void RelinkType(TypeDefinition type) {
        foreach (TypeDefinition nested in type.NestedTypes) {
            RelinkType(nested);
        }

        type.BaseType = type.BaseType.RelinkedReference(type);
        for (int i = 0; i < type.Interfaces.Count; i++) {
            InterfaceImplementation interf = new InterfaceImplementation(type.Interfaces[i].InterfaceType.RelinkedReference(type));
            for (int cai = 0; cai < type.Interfaces[i].CustomAttributes.Count; cai++) {
                CustomAttribute oca = type.Interfaces[i].CustomAttributes[cai];
                // TODO relink that method
                CustomAttribute ca = new CustomAttribute(oca.Constructor/*.Relinked(type)*/, oca.GetBlob());
                for (int caii = 0; caii < oca.ConstructorArguments.Count; caii++) {
                    //TODO do more with the attributes
                    CustomAttributeArgument ocaa = oca.ConstructorArguments[caii];
                    ca.ConstructorArguments.Add(new CustomAttributeArgument(ocaa.Type.RelinkedReference(type),
                        ocaa.Value is TypeReference ? ocaa.Type.RelinkedReference(type) :
                        ocaa.Value
                    ));
                }
                interf.CustomAttributes.Add(ca);
            }
            type.Interfaces[i] = interf;
        }
        
        foreach (FieldDefinition field in type.Fields) {
            field.FieldType = field.FieldType.RelinkedReference(type);
        }

        foreach (PropertyDefinition property in type.Properties) {
            property.PropertyType = property.PropertyType.RelinkedReference(type);

            if (property.GetMethod != null) {
                property.GetMethod.ReturnType = property.GetMethod.ReturnType.RelinkedReference(type);
                for (int i = 0; i < property.GetMethod.Parameters.Count; i++) {
                    property.GetMethod.Parameters[i].ParameterType = property.GetMethod.Parameters[i].ParameterType.RelinkedReference(type);
                }
            }

            if (property.SetMethod != null) {
                property.SetMethod.ReturnType = property.GetMethod.ReturnType.RelinkedReference(type);
                for (int i = 0; i < property.SetMethod.Parameters.Count; i++) {
                    property.SetMethod.Parameters[i].ParameterType = property.SetMethod.Parameters[i].ParameterType.RelinkedReference(type);
                }
            }
        }

        foreach (MethodDefinition method in type.Methods) {
            method.ReturnType = method.ReturnType.RelinkedReference(method);
            for (int i = 0; i < method.Parameters.Count; i++) {
                method.Parameters[i].ParameterType = method.Parameters[i].ParameterType.RelinkedReference(method);
            }

            for (int i = 0; method.HasBody && i < method.Body.Instructions.Count; i++) {
                Instruction instruction = method.Body.Instructions[i];
                object operand = instruction.Operand;

                if (operand is MethodReference) {
                    operand = ((MethodReference) operand).RelinkedReference(method);

                } else if (operand is FieldReference) {
                    FieldReference old = (FieldReference) operand;
                    FieldReference relink = new FieldReference(old.Name, old.FieldType.RelinkedReference(method), old.DeclaringType.RelinkedReference(method));
                    operand = type.Module.ImportReference(relink);

                } else if (operand is TypeReference) {
                    operand = ((TypeReference) operand).RelinkedReference(method);
                }

                instruction.Operand = operand;
            }

            for (int i = 0; method.HasBody && i < method.Body.Variables.Count; i++) {
                method.Body.Variables[i].VariableType = method.Body.Variables[i].VariableType.RelinkedReference(method);
            }
        }


    }

    public static TypeReference RelinkedReference(this TypeReference type, MemberReference context) {
        if (type == null) {
            return null;
        }
        if (!type.Scope.Name.EndsWithInvariant(".mm")) {
            return type;
        }

        TypeReference elemType = (type as TypeSpecification)?.ElementType?.RelinkedReference(context);

        if (type.IsGenericParameter) {
            if (context == null) {
                return null;
            }

            if (context is MethodReference) {
                MethodReference r = ((MethodReference) context).GetElementMethod();
                for (int gi = 0; gi < r.GenericParameters.Count; gi++) {
                    GenericParameter genericParam = r.GenericParameters[gi];
                    if (genericParam.FullName == type.FullName) {
                        //TODO variables hate MonoMod, maybe they hate the new relinker too, import otherwise
                        return genericParam;
                    }
                }
                if (type.Name.StartsWithInvariant("!!")) {
                    int i;
                    if (int.TryParse(type.Name.Substring(2), out i)) {
                        return r.GenericParameters[i];
                    }
                    throw new InvalidOperationException("Failed parsing \"" + type.Name + "\" (method) for " + context.Name + " while relinking said type!");
                }
            }
            if (context is TypeReference) {
                TypeReference r = ((TypeReference) context).GetElementType();
                for (int gi = 0; gi < r.GenericParameters.Count; gi++) {
                    GenericParameter genericParam = r.GenericParameters[gi];
                    if (genericParam.FullName == type.FullName) {
                        //TODO variables hate me, import otherwise
                        return genericParam;
                    }
                }
                if (type.Name.StartsWithInvariant("!!")) {
                    return type.RelinkedReference(context.DeclaringType);
                } else if (type.Name.StartsWithInvariant("!")) {
                    int i;
                    if (int.TryParse(type.Name.Substring(1), out i)) {
                        return r.GenericParameters[i];
                    } else {
                        new InvalidOperationException("Failed parsing \"" + type.Name + "\" (type) for " + context.Name + " while relinking said type!");
                    }
                }
            }
            if (context.DeclaringType != null) {
                return type.RelinkedReference(context.DeclaringType);
            }
            return type;
        }

        if (type.IsByReference) {
            return new ByReferenceType(elemType);
        }

        if (type.IsArray) {
            return new ArrayType(elemType, ((ArrayType) type).Dimensions.Count);
        }

        if (type.IsGenericInstance) {
            GenericInstanceType typeg = (GenericInstanceType) type;
            GenericInstanceType relinkg = new GenericInstanceType(elemType);
            foreach (TypeReference arg in typeg.GenericArguments) {
                relinkg.GenericArguments.Add(arg.RelinkedReference(context));
            }
            return typeg;
        }

        TypeReference relink = new TypeReference(type.Namespace, type.Name, ETGModule, ETGModule, type.IsValueType);
        relink.DeclaringType = type.DeclaringType.RelinkedReference(context);

        return context.Module.ImportReference(relink);
    }

    public static MethodReference RelinkedReference(this MethodReference method, MemberReference context) {
        if (method.IsGenericInstance) {
            GenericInstanceMethod methodg = ((GenericInstanceMethod) method);
            GenericInstanceMethod relinkg = new GenericInstanceMethod(methodg.ElementMethod.RelinkedReference(context));

            foreach (TypeReference arg in methodg.GenericArguments) {
                relinkg.GenericArguments.Add(arg.RelinkedReference(context) ?? arg.RelinkedReference(relinkg));
            }

            return relinkg;
        }

        MethodReference relink = new MethodReference(method.Name, method.ReturnType, method.DeclaringType.RelinkedReference(context));
        relink.ReturnType = method.ReturnType?.RelinkedReference(relink);

        relink.CallingConvention = method.CallingConvention;
        relink.ExplicitThis = method.ExplicitThis;
        relink.HasThis = method.HasThis;

        foreach (ParameterDefinition param in method.Parameters) {
            param.ParameterType = param.ParameterType.RelinkedReference(context);
            relink.Parameters.Add(param);
        }

        foreach (GenericParameter param in method.GenericParameters) {
            GenericParameter paramN = new GenericParameter(param.Name, param.Owner) {
                Attributes = param.Attributes,
                // MetadataToken = param.MetadataToken
            };

            foreach (TypeReference constraint in param.Constraints) {
                paramN.Constraints.Add(constraint.RelinkedReference(context));
            }

            relink.GenericParameters.Add(paramN);
        }

        return method.Module.ImportReference(relink);
    }

    public static string ToHexadecimalString(this byte[] data) {
        return BitConverter.ToString(data).Replace("-", string.Empty);
    }

    public static bool ChecksumsEqual(this string[] a, string[] b) {
        if (a.Length != b.Length) {
            return false;
        }
        for (int i = 0; i < a.Length; i++) {
            if (a[i].Trim() != b[i].Trim()) {
                return false;
            }
        }
        return true;
    }

}
