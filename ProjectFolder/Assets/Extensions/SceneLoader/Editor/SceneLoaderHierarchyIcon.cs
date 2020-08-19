using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unity.Extensions.SceneLoading
{
    // This class handles adding an icon next to each SceneLoader in the hierarchy window
    [InitializeOnLoad]
    class SceneLoaderHierarchyIcon
    {
        // The icon texture
        static Texture2D s_texture;
        // A list of GameObject instance IDs for SceneLoader GameObjects
        static HashSet<int> s_markedObjects = new HashSet<int>();

        static SceneLoaderHierarchyIcon()
        {
            // Find the icon asset
            var guids = AssetDatabase.FindAssets("SceneLoaderIco t:Texture2D");
            if (guids.Length > 0)
            {
                string firstGUID = guids[0];
                string assetPath = AssetDatabase.GUIDToAssetPath(firstGUID);
                s_texture = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D)) as Texture2D;
            }

            // Potentially draw the SceneLoader icon for each window item in the hierarchy
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyItemCB;

            // Register delegates to re-scan the hierarchy for SceneLoaders
            EditorSceneManager.sceneLoaded += UpdateFromSceneLoadCB;
            EditorSceneManager.sceneDirtied += UpdateFromDirtyScene;
            EditorSceneManager.sceneOpened += UpdateFromSceneOpen;
            EditorApplication.playModeStateChanged += UpdateFromEditorPlaymodeChange;

            RebuildSceneLoaderList();
        }

        // Callback for SceneManager.sceneLoaded
        static void UpdateFromSceneLoadCB(Scene scene, LoadSceneMode mode)
        {
            RebuildSceneLoaderList();
        }

        // Callback for EditorSceneManager.sceneOpened
        static void UpdateFromSceneOpen(Scene scene, OpenSceneMode mode)
        {
            RebuildSceneLoaderList();
        }

        // Callback for EditorSceneManager.sceneDirtied
        static void UpdateFromDirtyScene(Scene scene)
        {
            RebuildSceneLoaderList();
        }

        // Called from EditorApplication.playModeStateChanged
        static void UpdateFromEditorPlaymodeChange(PlayModeStateChange change)
        {
            RebuildSceneLoaderList();
        }

        static void RebuildSceneLoaderList()
        {
            s_markedObjects.Clear();
            
            // Search the hierarchy for SceneLoaders
            var loaders = Object.FindObjectsOfType(typeof(SceneLoader)) as SceneLoader[];
            foreach (var loader in loaders)
            {
                s_markedObjects.Add(loader.gameObject.GetInstanceID());
            }
        }

        static void HierarchyItemCB(int instanceID, Rect selectionRect)
        {
            if (s_markedObjects.Contains(instanceID))
            {
                // place the icoon to the right of the list:
                var r = new Rect(selectionRect);
                r.x = r.width + 42;
                r.width = 18;
                
                // Draw the texture if it's a light (e.g.)
                GUI.Label(r, s_texture);
            }
        }
    }
}
