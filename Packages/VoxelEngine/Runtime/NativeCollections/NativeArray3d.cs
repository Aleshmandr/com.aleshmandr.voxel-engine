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

        public T this[int x, int y, int z] {
            get => nativeArray[x + SizeX * (y + SizeY * z)];
            set => nativeArray[x + SizeX * (y + SizeY * z)] = value;
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

        public T[] ToArray() {
            return nativeArray.ToArray();
        }

        public void Dispose() {
            if(nativeArray.IsCreated) {
                nativeArray.Dispose();
            }
        }
    }
}
