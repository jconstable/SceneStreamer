using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Unity.Extensions.SceneLoading
{
    // This base class provides an API wrapping the implementation details of loading Scenes
    public abstract class SceneCache
    {
        // Events to which internal components can register 
        public UnityEvent<SceneWrapper> OnMapAdded = new UnityEvent<SceneWrapper>();
        public UnityEvent<SceneWrapper> OnMapRemoved = new UnityEvent<SceneWrapper>();
        
        // Internal list of implementations
        protected List<SceneWrapper> m_wrappers = new List<SceneWrapper>();

        // Search the maps and find if a scene with the given GUID is loaded
        public SceneWrapper Find(string guid)
        {
            string guidLowerCase = guid.ToLower();
            foreach (var map in m_wrappers)
            {
                if (map.GUID.Equals(guidLowerCase)) return map;
            }

            return null;
        }

        // The count of items in the implementations list
        public int Count
        {
            get { return m_wrappers.Count; }
        }

        // Return the name of a scene mapped to the given GUID
        public abstract string GetName(string guid);

        // Perform any cleanup associated with the cache
        public abstract void Clear();
        
        // Enumerate all of the scenes in the cache
        public abstract IEnumerable<Scene> CollectScenes();
        
        // Loading scenes
        public abstract void Load(AssetReference assetReference, LoadSceneMode mode);
        public abstract bool SceneLoadInProgress();
        
        // Activating scenes
        public abstract void ActivateAll();
        public abstract bool ScenesAreActivating();

        // Unloading scenes
        public abstract void UnloadAll();
        public abstract bool SceneUnloadInProgress();

        // Hooks to link a scene cache up to another cache
        public abstract void Add(SceneWrapper map);
        public abstract void Remove(SceneWrapper map);

        

        

        
    }
}
