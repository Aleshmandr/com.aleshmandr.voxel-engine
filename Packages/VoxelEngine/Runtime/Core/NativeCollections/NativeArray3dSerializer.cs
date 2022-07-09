using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;

namespace VoxelEngine
{
    public static class NativeArray3dSerializer
    {
        [System.Serializable]
        public class NativeArray3dSerializationData<T> where T : struct, IComparable
        {
            public readonly int SizeX;
            public readonly int SizeY;
            public readonly int SizeZ;
            public readonly T[] Array;

            public NativeArray3dSerializationData(NativeArray3d<T> nativeArray) {
                SizeX = nativeArray.SizeX;
                SizeY = nativeArray.SizeY;
                SizeZ = nativeArray.SizeZ;
                Array = nativeArray.ToArray();
            }
        }

        public static byte[] Serialize<T>(NativeArray3d<T> nativeArray, bool zip) where T : struct, IComparable {
            var obj = new NativeArray3dSerializationData<T>(nativeArray);

            //Write 1 to the first byte in case of compression, otherwise write 0
            using var memoryStream = new MemoryStream();
            if(zip) {
                memoryStream.WriteByte(1);
                using var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress);
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(gZipStream, obj);
            } else {
                var binaryFormatter = new BinaryFormatter();
                memoryStream.WriteByte(0);
                binaryFormatter.Serialize(memoryStream, obj);
            }
            return memoryStream.ToArray();
        }


        public static NativeArray3d<T> Deserialize<T>(byte[] bytes) where T : struct, IComparable {
            if(bytes == null || bytes.Length == 0) {
                return default;
            }
            var memoryStream = new MemoryStream(bytes);

            NativeArray3dSerializationData<T> serializationData;

            //Check if file compressed
            bool unzip = memoryStream.ReadByte() == 1;
            if(unzip) {
                using var decompressor = new GZipStream(memoryStream, CompressionMode.Decompress);
                serializationData = (NativeArray3dSerializationData<T>)new BinaryFormatter().Deserialize(decompressor);
            } else {
                serializationData = (NativeArray3dSerializationData<T>)new BinaryFormatter().Deserialize(memoryStream);
            }
            return new NativeArray3d<T>(serializationData.SizeX, serializationData.SizeY, serializationData.SizeZ, serializationData.Array);
        }
    }
}
