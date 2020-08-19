using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Unity.Extensions.SceneLoading
{
    // SceneCache that handles scenes referenced by Addressables data. 
    public class AddressablesSceneCache : SceneCache
    {
        public override IEnumerable<Scene> CollectScenes()
        {
            for (var i = 0; i < m_wrappers.Count; i++)
            {
                yield return m_wrappers[i].GetScene();
            }
        }

        public override void Load(AssetReference assetReference, LoadSceneMode mode)
        {
            SceneWrapper map = SceneWrapperFactory.Create();

            map.GUID = assetReference.AssetGUID.ToLower();
            map.Load(assetReference, mode);

            m_wrappers.Add(map);
            OnMapAdded.Invoke(map);
        }

        public override bool ScenesAreActivating()
        {
            foreach (var map in m_wrappers)
            {
                if (map.ActivationInProgress())
                {
                    return true;
                }
            }

            return false;
        }

        public override string GetName(string guid)
        {
            foreach (var map in m_wrappers)
                if (map.GUID.Equals(guid))
                {
                    return map.GetName();
                }

            return "Unknown";
        }

        public override void Clear()
        {
            for (var i = m_wrappers.Count - 1; i >= 0; i--)
            {
                m_wrappers[i].Clear();
                Remove(m_wrappers[i]);
            }
        }

        public override void Add(SceneWrapper map)
        {
            m_wrappers.Add(map);
            
            OnMapAdded?.Invoke(map);
        }

        public override void Remove(SceneWrapper mapToRemove)
        {
            for (var i = 0; i < m_wrappers.Count; i++)
            {
                var map = m_wrappers[i];
                if (map.GUID.Equals(mapToRemove.GUID))
                {
                    Debug.Log($"SceneCache: Guid {mapToRemove.GUID} removed from map.");
                    m_wrappers.RemoveAt(i);
                    OnMapRemoved.Invoke(mapToRemove);
                    return;
                }
            }
        }

        public override bool SceneLoadInProgress()
        {
            if (!Application.isPlaying)
                return false;

            for (var i = 0; i < m_wrappers.Count; i++)
            {
                if(m_wrappers[i].LoadInProgress()) return true;
            }

            return false;
        }

        public override void ActivateAll()
        {
            foreach (var map in m_wrappers)
            {
                map.Activate();
            }
        }

        public override void UnloadAll()
        {
            for (int i = 0; i < m_wrappers.Count; i++)
            {
                m_wrappers[i].Unload();
            }
        }

        public override bool SceneUnloadInProgress()
        {
            for(int i = m_wrappers.Count - 1; i >= 0; i--)
            {
                var map = m_wrappers[i];
                if (map.UnloadInProgress())
                {
                    return true;
                }
                else
                {
                    Remove(map);
                }
            }

            return false;
        }
    }
}
