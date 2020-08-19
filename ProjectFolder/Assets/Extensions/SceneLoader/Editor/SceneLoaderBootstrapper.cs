using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Extensions.SceneLoading
{
    // SceneLoaderBootstrapper alters the behaviour of SceneLoader in the Editor.
    // The appropriate SceneWrapper must be created based on playmode, and currently open Scenes
    // must be brute force added to the global scene cache.
    [InitializeOnLoad]
    public static class SceneLoaderBootstrapper
    {
        static SceneLoaderBootstrapper()
        {
            // The SceneWrapperFactory should return a SceneWrapper based on playmode state
            SceneWrapperFactory.SetCreateFunction(() =>
            {
                if (!Application.isPlaying)
                {
                    return new EditorSceneWrapper();
                }
                return new AddressablesSceneWrapper();
            });
        }
        
        // When entering play mode with Scenes already open, some of these Scenes may contain SceneLoaders. The expectation
        // is that a SceneLoader has a notion of its origin, so that it can close the Scenes previously in the flow.
        // Because these origins do not exist for Scenes not opened using SceneLoaders, we need to 
        // give them a special Origin that allows for unloading these Scenes in the future.
        [RuntimeInitializeOnLoadMethod]
        public static void CollectSceneOpenedByEditor()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var sceneLoaderInstanceCacheField =
                typeof(SceneLoader).GetField("InstanceCache", BindingFlags.Instance | BindingFlags.NonPublic);
            var sceneLoaderOriginProperty =
                typeof(SceneLoader).GetProperty("Origin", BindingFlags.Instance | BindingFlags.Public);
            var sceneLoaderGlobalCache =
                typeof(SceneLoader).GetField("GlobalCache", BindingFlags.Static | BindingFlags.NonPublic);
            var sceneLoaderStatusProperty =
                typeof(SceneLoader).GetProperty("Status", BindingFlags.Instance | BindingFlags.Public);

            var addressableScenes = new List<Scene>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                var guid = AssetDatabase.AssetPathToGUID(scene.path);
                if (settings.FindAssetEntry(guid) != null) addressableScenes.Add(scene);
            }

            if (addressableScenes.Count > 0)
            {
                var cache = new EditorSceneCache();
                var globalCache = new AddressablesSceneCache();
                cache.OnMapAdded.AddListener(globalCache.Add);
                foreach (var scene in addressableScenes)
                {
                    var guid = AssetDatabase.AssetPathToGUID(scene.path);
                    EditorSceneWrapper map = new EditorSceneWrapper();
                    map.SetScene(scene);
                    map.GUID = guid.ToLower();
                    cache.Add(map);
                }

                sceneLoaderGlobalCache.SetValue(null, globalCache);

                var editorLoader = CreateLoader("Editor open scenes");
                sceneLoaderInstanceCacheField.SetValue(editorLoader, cache);
                sceneLoaderStatusProperty.SetValue(editorLoader, SceneLoader.LoadStatus.Loaded);

                for (var i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    foreach (var loader in SceneLoader.ScanSceneForSceneLoaders(scene)) sceneLoaderOriginProperty.SetValue(loader, editorLoader);
                }
            }
        }
        
        // Create a SceneLoader from static context
        public static SceneLoader CreateLoader(string source)
        {
            var o = new GameObject($"SceneLoader ({source})");
            var loader = o.AddComponent<SceneLoader>();
            loader.LoadSceneMode = LoadSceneMode.Single;
            loader.AutomaticSceneActivation = true;

            GameObject.DontDestroyOnLoad(o);

            return loader;
        }
    }
}
