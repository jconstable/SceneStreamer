using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.Extensions.SceneLoading
{
    // This class implements the inspector UI for SceneLoaders, providing convenience functionality
    [CustomEditor(typeof(SceneLoader))]
    [CanEditMultipleObjects]
    public class SceneLoaderEditor : Editor
    {
        // Static fields on SceneLoader class
        FieldInfo m_globalCacheField;

        // Instance fields on SceneLoader class
        FieldInfo m_instanceCacheField;

        // Determine if any serialized properties were modified, in order to apply changes
        bool m_isDirty;
        
        // Fields used to extract debugging info
        FieldInfo m_originField;
        FieldInfo m_statusField;
        FieldInfo m_cacheImplementationsField;

        // SerializedProperties
        SerializedProperty m_lightingSceneField;
        SerializedProperty m_scenesProperty;
        

        void OnEnable()
        {
            // Fields used to directly reference SceneLoader instance
            m_globalCacheField =
                typeof(SceneLoader).GetField("s_globalCache", BindingFlags.Static | BindingFlags.NonPublic);
            m_instanceCacheField =
                typeof(SceneLoader).GetField("m_instanceCache", BindingFlags.Instance | BindingFlags.NonPublic);
            m_statusField = 
                typeof(SceneLoader).GetField("m_status", BindingFlags.Instance | BindingFlags.NonPublic);
            m_originField = 
                typeof(SceneLoader).GetField("m_origin", BindingFlags.Instance | BindingFlags.NonPublic);
            m_cacheImplementationsField = 
                typeof(SceneCache).GetField("m_wrappers", BindingFlags.Instance | BindingFlags.NonPublic);

            // Serialized Properties
            m_scenesProperty = serializedObject.FindProperty("m_scenes");
            m_lightingSceneField = serializedObject.FindProperty("m_lightingScene");
        }

        // Drawn every update if a SceneLoader is selected
        public override void OnInspectorGUI()
        {
            var loader = target as SceneLoader;
            List<string> sceneNames;
            List<string> sceneGUIDs;
            int currentSceneIndex;
            m_isDirty = false;
            
            serializedObject.Update();

            DrawDefaultInspector();
            
            BuildSceneNameAndGuidList(out currentSceneIndex, out sceneNames, out sceneGUIDs);

            DrawSelectLigtingDataField(currentSceneIndex, sceneNames, sceneGUIDs);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                DrawDebugInfo(loader);

                DrawLoadAndUnloadButtons(loader);
            }
            EditorGUILayout.EndVertical();

            // Apply property modifications, if necessary
            if (m_isDirty)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        // Create two lists, in order to create a more user-friendly scene name dropdown
        void BuildSceneNameAndGuidList(out int currentSceneIndex, out List<string> sceneNames, out List<string> sceneGUIDs)
        {
            sceneNames = new List<string> { "None" };
            sceneGUIDs = new List<string> { "" };
            currentSceneIndex = 0;
            for (var i = 0; i < m_scenesProperty.arraySize; i++)
            {
                var sceneEntry = m_scenesProperty.GetArrayElementAtIndex(i);
                var refGUID = sceneEntry.FindPropertyRelative("m_AssetGUID").stringValue;
                if (!string.IsNullOrEmpty(refGUID))
                {
                    var sceneName = Path.GetFileName(AssetDatabase.GUIDToAssetPath(refGUID));
                    sceneNames.Add(sceneName);
                    sceneGUIDs.Add(refGUID);
                    if (refGUID.Equals(m_lightingSceneField.stringValue)) currentSceneIndex = i + 1;
                }
            }
        }

        // Allow user to pick a scene currently in the loader's list to use for lighting data
        void DrawSelectLigtingDataField(int currentSceneIndex, List<string> sceneNames, List<string> sceneGUIDs)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Use Lighting Data");
                var newCurrentScene = EditorGUILayout.Popup(currentSceneIndex, sceneNames.ToArray());
                if (newCurrentScene != currentSceneIndex)
                {
                    m_isDirty |= true;
                    m_lightingSceneField.stringValue = sceneGUIDs[newCurrentScene];
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // Output some useful info about the current state of the SceneLoader
        void DrawDebugInfo(SceneLoader loader)
        {
            var originName = "unknown";
            if (m_originField.GetValue(loader) as SceneLoader is SceneLoader origin)
            {
                if (origin == null || origin.Equals(null))
                    originName = "deleted";
                else
                    originName = origin.name;
            }
            
            EditorGUILayout.LabelField($"Origin: {originName}");

            EditorGUILayout.LabelField("Scenes opened by this loader:");
            var local = m_instanceCacheField.GetValue(loader) as SceneCache;
            DisplayCacheDebugInfo(local);

            EditorGUILayout.LabelField("Scenes opened by all loaders:");
            var global = m_globalCacheField.GetValue(loader) as SceneCache;
            DisplayCacheDebugInfo(global);
        }

        // Draw the buttons that handle loading and unloading of a SceneLoader
        void DrawLoadAndUnloadButtons(SceneLoader loader)
        {
            var status = (SceneLoader.LoadStatus)m_statusField.GetValue(loader);
            if (status == SceneLoader.LoadStatus.Loaded)
            {
                if (GUILayout.Button("Unload"))
                {
                    var unloadMethod = typeof(SceneLoader).GetMethod("UnloadCoroutine", BindingFlags.Instance | BindingFlags.NonPublic);
                    var cr = unloadMethod.Invoke(loader, new object[] { null }) as IEnumerator;
                    EditorCoroutineUtility.StartCoroutine(cr, loader);
                }
            }
            else if (status == SceneLoader.LoadStatus.Unloaded)
            {
                if (GUILayout.Button("Load"))
                {
                    var loadMethod = typeof(SceneLoader).GetMethod("LoadCoroutine", BindingFlags.Instance | BindingFlags.NonPublic);
                    var cr = loadMethod.Invoke(loader, new object[] { true, null }) as IEnumerator;
                    EditorCoroutineUtility.StartCoroutine(cr, loader);
                }
            }
        }

        // Build a display string for the state of the Scene cache
        void DisplayCacheDebugInfo(SceneCache cache)
        {
            if (cache.Count > 0)
            {
                List<SceneWrapper> cacheContents = m_cacheImplementationsField.GetValue(cache) as List<SceneWrapper>;
                for (var i = 0; i < cacheContents.Count; i++)
                {
                    var map = cacheContents[i];
                    EditorGUILayout.LabelField($"   {cache.GetName(map.GUID)} ({map.GUID})");
                }
            }
            else
            {
                EditorGUILayout.LabelField("   None");
            }
        }
    }
}
