using UnityEditor;
using UnityEngine;

namespace VoxelEngine.Editor
{
    public class VoxImporter : EditorWindow
    {
        [MenuItem("Tools/VoxelEngine/Magica Voxel Importer (.vox)", false)]
        public static void ShowWindow() {
            EditorWindow.GetWindow(typeof(VoxImporter));
        }

        private void OnGUI() {
            titleContent.text = "Magica Voxel Importer";
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Load custom palette (optional)");
            if(GUILayout.Button("Load")) {
                string filePath = EditorUtility.OpenFilePanel("Load palette", "", "png");
                if(string.IsNullOrEmpty(filePath)) {
                    return;
                }
                //TODO: Load file
            }
            
            EditorGUILayout.LabelField("Import .vox file");
            if(GUILayout.Button("Import")) {
                string filePath = EditorUtility.OpenFilePanel("Import file", "", "vox");
                if(string.IsNullOrEmpty(filePath)) {
                    return;
                }
                //TODO: Load file
            }
            EditorGUILayout.EndVertical();
        }
    }
}
