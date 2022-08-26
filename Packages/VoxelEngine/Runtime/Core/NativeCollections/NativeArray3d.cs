using System;
using Unity.Collections;

namespace VoxelEngine
{
    public struct NativeArray3d<T> where T : struct, IComparable 
    {
        public readonly int SizeX;
        public readonly int SizeY;
        public readonly int SizeZ;

        private NativeArray<T> nativeArray;

        public NativeArray<T> NativeArray => nativeArray;
        
        public T this[int x, int y, int z] {
            get => nativeArray[x + SizeX * (y + SizeY * z)];
            set => nativeArray[x + SizeX * (y + SizeY * z)] = value;
        }
        
        public T this[int index] {
            get => nativeArray[index];
            set => nativeArray[index] = value;
        }

        public NativeArray3d(int sizeX, int sizeY, int sizeZ) {
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            nativeArray = new NativeArray<T>(sizeX * sizeY * sizeZ, Allocator.Persistent);
        }
        
        public NativeArray3d(int sizeX, int sizeY, int sizeZ, T[] array) {
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            nativeArray = new NativeArray<T>(array, Allocator.Persistent);
        }
        
        public NativeArray3d(int sizeX, int sizeY, int sizeZ, NativeArray<T> array) {
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            nativeArray = array;
        }

        public bool IsCoordsValid(int x, int y, int z) {
            return x >= 0 && x < SizeX && y >= 0 && y < SizeY && z >= 0 && z < SizeZ;
        }
        
        public int CoordToIndex(int x, int y, int z) {
            return x + SizeX * (y + SizeY * z);
        }
        
        public bool IsIndexValid(int index) {
            return index >= 0 && index < nativeArray.Length;
        }

        public T[] ToArray() {
            return nativeArray.ToArray();
        }
        
        public NativeArray<T> AllocateNativeDataCopy(Allocator allocator) {
            return new NativeArray<T>(nativeArray, allocator);
        }
        
        public NativeArray3d<T> Copy(Allocator allocator) {
            return new NativeArray3d<T>(SizeX, SizeY, SizeZ, AllocateNativeDataCopy(allocator));
        }

        public void Dispose() {
            if(nativeArray.IsCreated) {
                nativeArray.Dispose();
            }
        }
    }
}
