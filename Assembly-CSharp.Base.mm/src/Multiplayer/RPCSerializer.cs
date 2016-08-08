using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

namespace ETGMultiplayer {
    public class RPCSerializer {

        private static Dictionary<Type, SerializePair> registeredTypes = new Dictionary<Type, SerializePair>();
        private static Dictionary<ushort, Type> typeSignatures = new Dictionary<ushort, Type>();
        private static Dictionary<Type, int> byteSize = new Dictionary<Type, int>();
        private static HashSet<Type> dynamicSizes = new HashSet<Type>();

        private static ushort signatureLast;

        public static void RegisterType(Type t, Func<object, byte[]> serializeFunction, Func<byte[], object> deserializeFunction, bool isDynamicSize = false) {
            registeredTypes.Add(t, new SerializePair(serializeFunction, deserializeFunction));
            typeSignatures.Add((ushort)(signatureLast+1), t);
            signatureLast+=1;
            if (isDynamicSize)
                dynamicSizes.Add(t);
        }

        public static void RegisterType(Type t, Func<object, byte[]> serializeFunction, Func<byte[], object> deserializeFunction, int size = 0) {
            registeredTypes.Add(t, new SerializePair(serializeFunction, deserializeFunction));
            typeSignatures.Add((ushort)( signatureLast+1 ), t);
            signatureLast+=1;
            byteSize.Add(t, size);
        }

        public static void RegisterType(Type t, Func<object, byte[]> serializeFunction, Func<byte[], object> deserializeFunction) {
            registeredTypes.Add(t, new SerializePair(serializeFunction, deserializeFunction));
            typeSignatures.Add((ushort)( signatureLast+1 ), t);
            signatureLast+=1;
        }

        public static byte[] GetSerializedType<T>(T instance) {
            Type getType = instance.GetType();

            if (registeredTypes.ContainsKey(getType)) {
                return registeredTypes[getType].SerializeType(instance);
            } else {
                Debug.Log("No registry entry for type "+instance.GetType().Name+" so we're defaulting to object serializing.");
                return GetSerializedType<object>(instance);
            }
        }

        public static T GetDeserializedType<T>(byte[] data) {
            Type getType = typeof(T);

            if (registeredTypes.ContainsKey(getType)) {
                return (T)registeredTypes[getType].DeserializeType(data);
            } else {
                Debug.Log("No registry entry for type "+getType.Name+" so we're defaulting to object serializing.");
                return (T)GetDeserializedType<object>(data);
            }
        }

        public static void Init() {
            RegisterType(typeof(object), ObjectSerialize, ObjectDeSerialize, true);
            RegisterType(typeof(int), IntSerialize, IntDeSerialize, sizeof(int));
            RegisterType(typeof(float), FloatSerialize, FloatDeSerialize, sizeof(float));
            RegisterType(typeof(string), StringSerialize, StringDeSerialize, true);
            RegisterType(typeof(Vector2), Vector2Serialize, Vector2Deserialize, sizeof(float)*2);
            RegisterType(typeof(Vector3), Vector3Serialize, Vector3Deserialize, sizeof(float)*3);
            RegisterType(typeof(Vector4), Vector4Serialize, Vector4Deserialize, sizeof(float)*4);
        }

        static byte[] IntSerialize(object instance) {
            return BitConverter.GetBytes((int)instance);
        }

        static object IntDeSerialize(byte[] data) {
            return BitConverter.ToInt32(data, 0);
        }

        static byte[] FloatSerialize(object instance) {
            return BitConverter.GetBytes((float)instance);
        }

        static object FloatDeSerialize(byte[] data) {
            return BitConverter.ToSingle(data, 0);
        }

        static byte[] StringSerialize(object instance) {
            return Encoding.ASCII.GetBytes((string)instance);
        }

        static object StringDeSerialize(byte[] data) {
            return Encoding.ASCII.GetString(data);
        }

        static byte[] BoolSerialize(object instance) {
            return BitConverter.GetBytes((bool)instance);
        }

        static object BoolDeserialize(byte[] data) {
            return BitConverter.ToBoolean(data, 0);
        }

        static byte[] Vector2Serialize(object instance) {
            byte[] rArray = new byte[sizeof(float)*2];
            BitConverter.GetBytes(( (Vector2)instance ).x).CopyTo(rArray, 0);
            BitConverter.GetBytes(( (Vector2)instance ).y).CopyTo(rArray, sizeof(float)*1);
            return rArray;
        }

        static object Vector2Deserialize(byte[] data) {
            Vector2 obj = new Vector2(
                BitConverter.ToSingle(data, 0), 
                BitConverter.ToSingle(data, sizeof(float))
                );
            return obj;
        }

        static byte[] Vector3Serialize(object instance) {
            byte[] rArray = new byte[sizeof(float)*3];
            BitConverter.GetBytes(( (Vector3)instance ).x).CopyTo(rArray, 0);
            BitConverter.GetBytes(( (Vector3)instance ).y).CopyTo(rArray, sizeof(float));
            BitConverter.GetBytes(( (Vector3)instance ).z).CopyTo(rArray, sizeof(float)*2);
            return rArray;
        }

        static object Vector3Deserialize(byte[] data) {
            Vector3 obj = new Vector3(
                BitConverter.ToSingle(data, 0),
                BitConverter.ToSingle(data, sizeof(float)),
                BitConverter.ToSingle(data, sizeof(float)*2)
                );
            return obj;
        }


        static byte[] Vector4Serialize(object instance) {
            byte[] rArray = new byte[sizeof(float)*4];
            BitConverter.GetBytes(( (Vector4)instance ).x).CopyTo(rArray, 0);
            BitConverter.GetBytes(( (Vector4)instance ).y).CopyTo(rArray, sizeof(float));
            BitConverter.GetBytes(( (Vector4)instance ).z).CopyTo(rArray, sizeof(float)*2);
            BitConverter.GetBytes(( (Vector4)instance ).w).CopyTo(rArray, sizeof(float)*3);
            return rArray;
        }

        static object Vector4Deserialize(byte[] data) {
            Vector4 obj = new Vector4(
                BitConverter.ToSingle(data, 0),
                BitConverter.ToSingle(data, sizeof(float)),
                BitConverter.ToSingle(data, sizeof(float)*2),
                BitConverter.ToSingle(data, sizeof(float)*3)
                );
            return obj;
        }

        static byte[] ObjectSerialize(object instance) {
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream()) {
                bf.Serialize(ms, instance);
                return ms.ToArray();
            }
        }

        static object ObjectDeSerialize(byte[] data) {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(data, 0, data.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            System.Object obj = (System.Object)binForm.Deserialize(memStream);

            return obj;
        }

        public static object[] GetCompleteDeserializedData(byte[] allData) {
            List<object> rData = new List<object>();

            int byteIndex = 0;

            while (byteIndex<allData.Length) {

                ushort signature = BitConverter.ToUInt16(allData, byteIndex);
                byteIndex+=2;

                Type t = typeSignatures[signature];

                int size = 0;

                if (dynamicSizes.Contains(t)) {
                    size=BitConverter.ToInt32(allData, byteIndex);
                    byteIndex+=4;
                } else {
                    size=byteSize[t];
                }

                byte[] getData = new byte[size];

                for (int i = 0; i<getData.Length; i++) {
                    getData[i]=allData[byteIndex+i];
                }

                byteIndex+=size;

                MethodInfo method = typeof(RPCSerializer).GetMethod("GetDeserializedType");
                MethodInfo generic = method.MakeGenericMethod(t);
                object getDeSer = generic.Invoke(null, new object[] { getData });

                rData.Add(getDeSer);
            }

            return rData.ToArray();
        }

        public static byte[] GetCompleteSerializedData(params object[] args) {
            List<byte> data = new List<byte>();

            for (int i = 0; i<args.Length; i++) {

                Type t = args[i].GetType();
                object obj = args[i];

                if (!typeSignatures.ContainsValue(t)) {
                    Debug.Log("No registry entry for type "+t.Name+" so we're skipping it.");
                    continue;
                }

                ushort signature = typeSignatures.FirstOrDefault(x => x.Value==t).Key;

                data.AddRange(BitConverter.GetBytes(signature));

                MethodInfo method = typeof(RPCSerializer).GetMethod("GetSerializedType");
                MethodInfo generic = method.MakeGenericMethod(t);
                byte[] getSer = (byte[])generic.Invoke(null, new object[] { obj });

                if (dynamicSizes.Contains(t))
                    data.AddRange(BitConverter.GetBytes(getSer.Length));
                data.AddRange(getSer);
            }

            return data.ToArray();
        }
    }

    class SerializePair {

        public Func<object, byte[]> SerializeType;
        public Func<byte[], object> DeserializeType;

        public SerializePair(Func<object, byte[]> ser, Func<byte[], object> deser) {
            SerializeType=ser;
            DeserializeType=deser;
        }

    }
}
