using System;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace VoxelEngine
{
    public struct NativeArray3d<T> where T : struct, IComparable 
    {
        public readonly int SizeX;
        public readonly int SizeY;
        public readonly int SizeZ;

        public NativeArray<T> NativeArray;

        public bool IsCreated => NativeArray.IsCreated;

        public T this[int x, int y, int z] {
            get => NativeArray[x + SizeX * (y + SizeY * z)];
            set => NativeArray[x + SizeX * (y + SizeY * z)] = value;
        }
        
        public NativeArray3d(int sizeX, int sizeY, int sizeZ) {
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            NativeArray = new NativeArray<T>(sizeX * sizeY * sizeZ, Allocator.Persistent);
        }
        
        public NativeArray3d(int sizeX, int sizeY, int sizeZ, T[] array) {
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            NativeArray = new NativeArray<T>(array, Allocator.Persistent);
        }
        
        public NativeArray3d(int sizeX, int sizeY, int sizeZ, NativeArray<T> array) {
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            NativeArray = array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsCoordsValid(int x, int y, int z) {
            return x >= 0 && x < SizeX && y >= 0 && y < SizeY && z >= 0 && z < SizeZ;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CoordToIndex(int x, int y, int z) {
            return x + SizeX * (y + SizeY * z);
        }
        
        public T[] ToArray() {
            return NativeArray.ToArray();
        }
        
        public NativeArray<T> AllocateNativeDataCopy(Allocator allocator) {
            return new NativeArray<T>(NativeArray, allocator);
        }
        
        public NativeArray3d<T> Copy(Allocator allocator) {
            return new NativeArray3d<T>(SizeX, SizeY, SizeZ, AllocateNativeDataCopy(allocator));
        }

        public void Dispose() {
            if(NativeArray.IsCreated) {
                NativeArray.Dispose();
            }
        }
    }
}
