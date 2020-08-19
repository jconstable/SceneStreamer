using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using System.Linq;
#endif

namespace Unity.Extensions.SceneLoading
{
    // This class creates a MonoBehavior that allows for asynchronously loading one or more Scenes. A given SceneLoader
    // is linked back to the SceneLoader that spawned it via the Origin property.
    [ExecuteInEditMode]
    public class SceneLoader : MonoBehaviour
    {
        // Enum to track the state of the current SceneLoader instance
        public enum LoadStatus
        {
            Unloaded,
            InProgress,
            Loaded
        }

        // Instance scene caches that maintain a global list of currently open scenes, mapped to the Addressables GUID
        static SceneCache s_globalCache = new AddressablesSceneCache();

        [Tooltip("This SceneLoader will allow LoadSceneAsync operations to Activate immediately")]
        [SerializeField] bool m_automaticSceneActivation = false;

        [Tooltip("Automatically unload this SceneLoader's origin when Scene Activation completes")]
        [SerializeField] bool m_automaticUnloadOrigin = false;

        // Set to true when scene activation can continue
        bool m_continueSceneActivation;

        // Instance scene caches that maintain a list scenes opened by this instance, mapped to the Addressables GUID
        SceneCache m_instanceCache = new AddressablesSceneCache();

        // The GUID of the scene that should be made the active scene, to control lighting data
        [SerializeField][HideInInspector] string m_lightingScene = string.Empty;

        [Tooltip("This SceneLoader will open scenes outside of Play mode")]
        [SerializeField] bool m_loadInEditMode = false;

        [Tooltip("Immediate start loading Scenes on GameObject Start")]
        [SerializeField] bool m_loadOnStart = false;

        [Tooltip("LoadSceneMode which controls whether the original scenes are left opened")]
        [SerializeField] LoadSceneMode m_loadSceneMode = LoadSceneMode.Additive;

        [Tooltip("All Scenes have completed Activation")]
        [SerializeField] UnityEvent m_onScenesLoaded = new UnityEvent();

        [Tooltip("SceneLoadAsync operations have paused, waiting for Activation")]
        [SerializeField] UnityEvent m_onScenesReady = new UnityEvent();

        [Tooltip("Scenes in this SceneLoader have all been unloaded")]
        [SerializeField] UnityEvent m_onScenesUnloaded = new UnityEvent();

        [Tooltip("Scenes that this SceneLoader will control")]
        [SerializeField] List<AssetReference> m_scenes = new List<AssetReference>();
        
        // The SceneLoader that loaded this GameObject
        SceneLoader m_origin;
        
        // The status of the loader
        LoadStatus m_status;

        // Collect SceneLoaders from a given Scene
        public static IEnumerable<SceneLoader> ScanSceneForSceneLoaders(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var loader in root.GetComponentsInChildren<SceneLoader>(true))
                yield return loader;
        }
        
        public bool AutomaticSceneActivation
        {
            get { return m_automaticSceneActivation; }
            set { m_automaticSceneActivation = value; }
        }

        public LoadSceneMode LoadSceneMode
        {
            get { return m_loadSceneMode; }
            set { m_loadSceneMode = value; }
        }

        // If the AutomaticSceneActivation field is set to false, external code must tell this object that it should
        // continue with Scene activation. Called from user scripts or UnityEvents.
        public void ContinueSceneActivation()
        {
            m_continueSceneActivation = true;
        }

        void Awake()
        {
            m_status = LoadStatus.Unloaded;
        }

        void Start()
        {
            // Tell us if a map is removed from the Global cache
            m_instanceCache.OnMapRemoved.AddListener(s_globalCache.Remove);

            // Tell the Global cache if a map is added to the instance
            m_instanceCache.OnMapAdded.AddListener(s_globalCache.Add);

            if (m_loadOnStart)
            {
                if (Application.isPlaying || m_loadInEditMode) // For UnityEditor support when entering playmode directly in a scene
                {
                    Load();
                }
            }
        }

        void OnDestroy()
        {
            if (m_instanceCache != null)
            {
                m_instanceCache.OnMapAdded.RemoveListener(s_globalCache.Add);
                s_globalCache.OnMapRemoved.RemoveListener(m_instanceCache.Remove);
                m_instanceCache.Clear();
            }
        }

        // Begin loading Scenes. The parameter-less version of this method must exist in order to be linked via
        // UnityEvents.
        public void Load()
        {
            Load(null);
        }

        // Begin loading Scenes.
        public void Load(Action callback)
        {
            if (m_status != LoadStatus.Unloaded)
            {
                Debug.LogWarning($"SceneLoader {gameObject.name} has already been loaded.");
                return;
            }

            StartCoroutine(LoadCoroutine(m_automaticSceneActivation, callback));
        }
        
        // Coroutine that handles Scene loading
        IEnumerator LoadCoroutine(bool continueSceneActivation, Action callback)
        {
            m_continueSceneActivation = continueSceneActivation;
            m_status = LoadStatus.InProgress;
            
            // Background thread loading of scenes should not cause hitches on the main thread
            Application.backgroundLoadingPriority = ThreadPriority.Low;

            yield return CreateSceneLoadWrappers();
            yield return WaitForScenesToLoad();
            yield return WaitForPermissionToActivate();
            
            // Allow activation to use as much frame time as needed
            Application.backgroundLoadingPriority = ThreadPriority.High;
            
            yield return WaitForSceneActivation();
            
            Application.backgroundLoadingPriority = ThreadPriority.Normal;

            HandleLoadedScenes(callback);
            m_status = LoadStatus.Loaded;
        }

        // Begin unloading Scenes. The parameter-less version of this method must exist in order to be linked via
        // UnityEvents.
        public void Unload()
        {
            Unload(null);
        }

        // Begin unloading Scenes
        public void Unload(Action callback = null)
        {
            Debug.Assert(m_status == LoadStatus.Loaded, "SceneLoader cannot be unloaded until it has been loaded.");

            StartCoroutine(UnloadCoroutine(callback));
        }
        
        // Coroutine responsible for Scene unloading
        IEnumerator UnloadCoroutine(Action callback)
        {
            m_instanceCache.UnloadAll();
            
            yield return WaitForScenesToUnload();
            
            m_onScenesUnloaded.Invoke();
            callback?.Invoke();
            m_status = LoadStatus.Unloaded;
        }

        IEnumerator CreateSceneLoadWrappers()
        {
            // Begin Scene load for all listed AssetReferences
            foreach (var scene in m_scenes)
            {
#if UNITY_EDITOR
                // Edit mode must be handled specially, as the API for opening Scenes in the Editor does not use
                // any SceneManager workflow.
                var scenePath = UnityEditor.AssetDatabase.GUIDToAssetPath(scene.AssetGUID);
                if (EditorTestIsSceneOpen(scenePath))
                {
                    continue;
                }
#endif
                
                // If the global cache already contains the scene, continue
                if (s_globalCache.Find(scene.AssetGUID) != null)
                {
                    continue;
                }

                m_instanceCache.Load(scene, LoadSceneMode);
            }

            yield return null;
        }

        IEnumerator WaitForScenesToLoad()
        {
            // Wait for scenes to load up to Scene Activation
            while (m_instanceCache.SceneLoadInProgress())
            {
                yield return null;
            }
            
            // Scenes are ready for Activation
            m_onScenesReady.Invoke();
        }

        IEnumerator WaitForPermissionToActivate()
        {
            // Wait for someone to tell us we can activate the scenes
            while (!m_continueSceneActivation)
            {
                yield return null;
            }
        }

        IEnumerator WaitForSceneActivation()
        {
            // Wait for Scene Activation to complete
            m_instanceCache.ActivateAll();
            while (m_instanceCache.ScenesAreActivating())
            {
                yield return null;
            }
        }

        void HandleLoadedScenes(Action callback)
        {
            SetOriginForSpawnedSceneLoaders();

            // If this SceneLoader has actually loaded scenes, it should be in DontDestroyOnLoad
            if (m_instanceCache.Count > 0)
            {
                gameObject.transform.parent = null;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            
            // Scenes have completed load and activation
            m_onScenesLoaded.Invoke();
            callback?.Invoke();
            
            ActivateLighting();
            
            // Possibly unload the previous SceneLoader. This behavior would be similar to the LoadSceneMode.Single option.
            if (m_automaticUnloadOrigin && m_origin != null)
            {
                m_origin.Unload();
            }
        }

        // If the loader has a lightning scene set, then make that scene the active scene
        void ActivateLighting()
        {
            if (!string.IsNullOrEmpty(m_lightingScene))
            {
                var scene = m_instanceCache.Find(m_lightingScene);
                if (scene != null)
                {
                    SceneManager.SetActiveScene(scene.GetScene());
                }
            }
        }

        IEnumerator WaitForScenesToUnload()
        {
            while(m_instanceCache.SceneUnloadInProgress())
            {
                yield return null;
            }
        }

        // Iterate over all scenes we just loaded, and tell any new SceneLoaders where they came from
        void SetOriginForSpawnedSceneLoaders()
        {
            foreach (var scene in m_instanceCache.CollectScenes())
            {
                foreach (var loader in ScanSceneForSceneLoaders(scene))
                {
                    loader.m_origin = this;
                }
            }
        }

#if UNITY_EDITOR
        public static bool EditorTestIsSceneOpen(string sceneName)
        {
            var sceneFileName = sceneName.Split('/').Last().Replace(".unity", "");
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name.Equals(sceneFileName))
                    return true;
            }

            return false;
        }
#endif
    }
}
