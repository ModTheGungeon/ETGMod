using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

namespace ETGMultiplayer {
    public static class RPCSerializer {

        private static Dictionary<Type, SerializePair> _RegisteredTypes = new Dictionary<Type, SerializePair>();
        private static Dictionary<ushort, Type> _TypeSignatures = new Dictionary<ushort, Type>();
        private static Dictionary<Type, int> _ByteSize = new Dictionary<Type, int>();
        private static HashSet<Type> _DynamicSizes = new HashSet<Type>();

        private static ushort _SignatureLast;

        private static Type t_object = typeof(object);
        private static MethodInfo m_RPCSerializer_GetDeserializedType = typeof(RPCSerializer).GetMethod("GetDeserializedType");
        private static MethodInfo m_RPCSerializer_GetSerializedType = typeof(RPCSerializer).GetMethod("GetSerializedType");

        private static object[] deserializedTypeParams = new object[2];
        private static object[] serializedTypeParams = new object[1];
        private static List<object> tmpObjectData = new List<object>();
        private static List<byte> tmpByteData = new List<byte>();

        public static void RegisterType(Type t, Func<object, byte[]> serializeFunction, Func<byte[], object> deserializeFunction, bool isDynamicSize = false) {
            _RegisteredTypes.Add(t, new SerializePair(serializeFunction, deserializeFunction));
            _TypeSignatures.Add((ushort) (_SignatureLast + 1), t);
            _SignatureLast += 1;
            if (isDynamicSize) _DynamicSizes.Add(t);
        }

        public static void RegisterType(Type t, Func<object, byte[]> serializeFunction, Func<byte[], object> deserializeFunction, int size = 0) {
            _RegisteredTypes.Add(t, new SerializePair(serializeFunction, deserializeFunction));
            _TypeSignatures.Add((ushort) (_SignatureLast + 1), t);
            _SignatureLast += 1;
            _ByteSize.Add(t, size);
        }

        public static void RegisterType(Type t, Func<object, byte[]> serializeFunction, Func<byte[], object> deserializeFunction) {
            _RegisteredTypes.Add(t, new SerializePair(serializeFunction, deserializeFunction));
            _TypeSignatures.Add((ushort) (_SignatureLast + 1), t);
            _SignatureLast += 1;
        }

        public static byte[] GetSerializedType(object instance) {
            return GetSerializedType(instance.GetType(), instance);
        }
        public static byte[] GetSerializedType(Type t, object instance) {
            if (_RegisteredTypes.ContainsKey(t)) {
                return _RegisteredTypes[t].SerializeType(instance);
            }
            Debug.Log("No registry entry for type " + instance.GetType().Name + " so we're defaulting to object serializing.");
            return GetSerializedType(t_object, instance);
        }

        public static object GetDeserializedType(Type t, byte[] data) {
            if (_RegisteredTypes.ContainsKey(t)) {
                return _RegisteredTypes[t].DeserializeType(data);
            }
            Debug.Log("No registry entry for type " + t.Name + " so we're defaulting to object serializing.");
            return GetDeserializedType(t_object, data);
        }

        public static void Init() {
            RegisterType(typeof(object),    ObjectSerialize,    ObjectDeserialize,  true);
            RegisterType(typeof(int),       IntSerialize,       IntDeSerialize,     sizeof(int  ));
            RegisterType(typeof(float),     FloatSerialize,     FloatDeSerialize,   sizeof(float));
            RegisterType(typeof(string),    StringSerialize,    StringDeSerialize,  true);
            RegisterType(typeof(Vector2),   Vector2Serialize,   Vector2Deserialize, sizeof(float) * 2);
            RegisterType(typeof(Vector3),   Vector3Serialize,   Vector3Deserialize, sizeof(float) * 3);
            RegisterType(typeof(Vector4),   Vector4Serialize,   Vector4Deserialize, sizeof(float) * 4);
        }

        public static byte[] IntSerialize(object instance) {
            return BitConverter.GetBytes((int) instance);
        }

        public static object IntDeSerialize(byte[] data) {
            return BitConverter.ToInt32(data, 0);
        }

        public static byte[] FloatSerialize(object instance) {
            return BitConverter.GetBytes((float) instance);
        }

        public static object FloatDeSerialize(byte[] data) {
            return BitConverter.ToSingle(data, 0);
        }

        public static byte[] StringSerialize(object instance) {
            return Encoding.ASCII.GetBytes((string) instance);
        }

        public static object StringDeSerialize(byte[] data) {
            return Encoding.ASCII.GetString(data);
        }

        public static byte[] BoolSerialize(object instance) {
            return BitConverter.GetBytes((bool) instance);
        }

        public static object BoolDeserialize(byte[] data) {
            return BitConverter.ToBoolean(data, 0);
        }

        public static byte[] Vector2Serialize(object instance) {
            byte[] rArray = new byte[sizeof(float) * 2];
            BitConverter.GetBytes(((Vector2) instance).x).CopyTo(rArray, 0);
            BitConverter.GetBytes(((Vector2) instance).y).CopyTo(rArray, sizeof(float) * 1);
            return rArray;
        }

        public static object Vector2Deserialize(byte[] data) {
            Vector2 obj = new Vector2(
                BitConverter.ToSingle(data, 0),
                BitConverter.ToSingle(data, sizeof(float))
            );
            return obj;
        }

        public static byte[] Vector3Serialize(object instance) {
            byte[] rArray = new byte[sizeof(float) * 3];
            BitConverter.GetBytes(((Vector3) instance).x).CopyTo(rArray, 0);
            BitConverter.GetBytes(((Vector3) instance).y).CopyTo(rArray, sizeof(float));
            BitConverter.GetBytes(((Vector3) instance).z).CopyTo(rArray, sizeof(float) * 2);
            return rArray;
        }

        public static object Vector3Deserialize(byte[] data) {
            Vector3 obj = new Vector3(
                BitConverter.ToSingle(data, 0),
                BitConverter.ToSingle(data, sizeof(float)),
                BitConverter.ToSingle(data, sizeof(float) * 2)
            );
            return obj;
        }


        public static byte[] Vector4Serialize(object instance) {
            byte[] rArray = new byte[sizeof(float) * 4];
            BitConverter.GetBytes(((Vector4) instance).x).CopyTo(rArray, 0);
            BitConverter.GetBytes(((Vector4) instance).y).CopyTo(rArray, sizeof(float));
            BitConverter.GetBytes(((Vector4) instance).z).CopyTo(rArray, sizeof(float) * 2);
            BitConverter.GetBytes(((Vector4) instance).w).CopyTo(rArray, sizeof(float) * 3);
            return rArray;
        }

        public static object Vector4Deserialize(byte[] data) {
            Vector4 obj = new Vector4(
                BitConverter.ToSingle(data, 0),
                BitConverter.ToSingle(data, sizeof(float)),
                BitConverter.ToSingle(data, sizeof(float) * 2),
                BitConverter.ToSingle(data, sizeof(float) * 3)
            );
            return obj;
        }

        public static byte[] ObjectSerialize(object instance) {
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream()) {
                bf.Serialize(ms, instance);
                return ms.ToArray();
            }
        }

        public static object ObjectDeserialize(byte[] data) {
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream()) {
                ms.Write(data, 0, data.Length);
                ms.Seek(0, SeekOrigin.Begin);
                return bf.Deserialize(ms);
            }
        }

        public static object[] GetCompleteDeserializedData(byte[] allData) {
            tmpObjectData.Clear();
            int byteIndex = 0;
            while (byteIndex < allData.Length) {
                ushort signature = BitConverter.ToUInt16(allData, byteIndex);
                byteIndex += 2;

                Type t = _TypeSignatures[signature];
                int size = 0;
                if (_DynamicSizes.Contains(t)) {
                    size = BitConverter.ToInt32(allData, byteIndex);
                    byteIndex += 4;
                } else {
                    size = _ByteSize[t];
                }

                byte[] data = new byte[size];
                allData.CopyTo(data, byteIndex);
                byteIndex += size;

                deserializedTypeParams[0] = t;
                deserializedTypeParams[1] = data;
                tmpObjectData.Add(ReflectionHelper.InvokeMethod(m_RPCSerializer_GetDeserializedType, null, deserializedTypeParams));
            }

            return tmpObjectData.ToArray();
        }

        public static byte[] GetCompleteSerializedData(params object[] args) {
            tmpByteData.Clear();

            for (int i = 0; i<args.Length; i++) {

                Type t = args[i].GetType();
                object obj = args[i];

                if (!_TypeSignatures.ContainsValue(t)) {
                    Debug.Log("No registry entry for type " + t.Name + " so we're skipping it.");
                    continue;
                }

                // TODO @Zandra hint: Cache the result if possible. Linq is slow as fuck. --0x0ade
                ushort signature = _TypeSignatures.FirstOrDefault(x => x.Value == t).Key;

                tmpByteData.AddRange(BitConverter.GetBytes(signature));

                serializedTypeParams[0] = obj;
                byte[] objData = (byte[]) ReflectionHelper.InvokeMethod(m_RPCSerializer_GetSerializedType, null, serializedTypeParams);

                if (_DynamicSizes.Contains(t)) tmpByteData.AddRange(BitConverter.GetBytes(objData.Length));
                tmpByteData.AddRange(objData);
            }

            return tmpByteData.ToArray();
        }
    }

    public class SerializePair {

        public Func<object, byte[]> SerializeType;
        public Func<byte[], object> DeserializeType;

        public SerializePair(Func<object, byte[]> ser, Func<byte[], object> deser) {
            SerializeType = ser;
            DeserializeType = deser;
        }

    }
}
