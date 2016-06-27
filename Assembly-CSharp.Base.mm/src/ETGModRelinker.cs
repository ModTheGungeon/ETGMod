using System;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ionic.Zip;
using Mono.Cecil;
using System.Security.Cryptography;
using Mono.Cecil.Cil;

internal static class ETGModRelinker {

    internal static ModuleDefinition ETGModule;
    internal static string ETGChecksum;

    internal static Assembly GetRelinkedAssembly(this ETGModuleMetadata metadata, ZipFile zip, Stream stream) {
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
            using (FileStream fs = File.OpenRead(metadata.Archive)) {
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

    internal static void RelinkType(TypeDefinition type) {
        foreach (TypeDefinition nested in type.NestedTypes) {
            RelinkType(nested);
        }
        

        type.BaseType = type.BaseType.Relinked(type);
        for (int i = 0; i < type.Interfaces.Count; i++) {
            type.Interfaces[i] = type.Interfaces[i].Relinked(type);
        }
        
        foreach (FieldDefinition field in type.Fields) {
            field.FieldType = field.FieldType.Relinked(type);
        }

        foreach (PropertyDefinition property in type.Properties) {
            property.PropertyType = property.PropertyType.Relinked(type);

            if (property.GetMethod != null) {
                property.GetMethod.ReturnType = property.GetMethod.ReturnType.Relinked(type);
                for (int i = 0; i < property.GetMethod.Parameters.Count; i++) {
                    property.GetMethod.Parameters[i].ParameterType = property.GetMethod.Parameters[i].ParameterType.Relinked(type);
                }
            }

            if (property.SetMethod != null) {
                property.SetMethod.ReturnType = property.GetMethod.ReturnType.Relinked(type);
                for (int i = 0; i < property.SetMethod.Parameters.Count; i++) {
                    property.SetMethod.Parameters[i].ParameterType = property.SetMethod.Parameters[i].ParameterType.Relinked(type);
                }
            }
        }

        foreach (MethodDefinition method in type.Methods) {
            method.ReturnType = method.ReturnType.Relinked(method);
            for (int i = 0; i < method.Parameters.Count; i++) {
                method.Parameters[i].ParameterType = method.Parameters[i].ParameterType.Relinked(method);
            }

            for (int i = 0; method.HasBody && i < method.Body.Instructions.Count; i++) {
                Instruction instruction = method.Body.Instructions[i];
                object operand = instruction.Operand;

                if (operand is MethodReference) {
                    MethodReference old = (MethodReference) operand;
                    MethodReference relink = new MethodReference(old.Name, old.ReturnType.Relinked(method), old.DeclaringType.Relinked(method));
                    foreach (ParameterDefinition param in old.Parameters) {
                        param.ParameterType = param.ParameterType.Relinked(method);
                        relink.Parameters.Add(param);
                    }
                    foreach (GenericParameter param in old.GenericParameters) {
                        relink.GenericParameters.Add(param.Relinked(method));
                    }
                    relink.CallingConvention = old.CallingConvention;
                    relink.ExplicitThis = old.ExplicitThis;
                    relink.HasThis = old.HasThis;
                    operand = type.Module.ImportReference(relink);

                } else if (operand is FieldReference) {
                    FieldReference old = (FieldReference) operand;
                    FieldReference relink = new FieldReference(old.Name, old.FieldType.Relinked(method), old.DeclaringType.Relinked(method));
                    operand = type.Module.ImportReference(relink);
                } else if (operand is TypeReference) {
                    operand = ((TypeReference) operand).Relinked(method);
                }

                instruction.Operand = operand;
            }

            for (int i = 0; method.HasBody && i < method.Body.Variables.Count; i++) {
                method.Body.Variables[i].VariableType = method.Body.Variables[i].VariableType.Relinked(method);
            }
        }


    }

    public static T Relinked<T>(this T type, MemberReference context) where T : TypeReference {
        if (type == null) {
            return null;
        }
        if (!type.Scope.Name.EndsWith(".mm")) {
            return type;
        }

        // TODO Generic types? Array types? My blood when I summoned Brent? --0x0ade
        TypeReference relink = new TypeReference(type.Namespace, type.Name, ETGModule, ETGModule, type.IsValueType);
        relink.DeclaringType = type.DeclaringType.Relinked(context);

        return context.Module.ImportReference(relink) as T;
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
