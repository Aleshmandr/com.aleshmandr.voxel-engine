using System;
using UnityEngine;

namespace VoxelEngine.Editor
{
    [Serializable]
    public class TransformData {

        [Serializable]
        public class FrameData {
            public Vector3 Rotation;
            public Vector3 Position;
            public Vector3 Scale;
        }

        public int ChildID;
        public int LayerID;
        public string Name;
        public bool Hidden;
        public int Reserved;
        public FrameData[] Frames;
    }
}
