using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Unity.Extensions.SceneLoading
{
    // SceneCache used to wrap currently opened Scenes when entering playmode in the Editor
    public class EditorSceneCache : SceneCache
    {
        public override IEnumerable<Scene> CollectScenes()
        {
            foreach (var map in m_wrappers)
            {
                var editorMap = map;
                yield return editorMap.GetScene();
            }
        }

        public override void Load(AssetReference assetReference, LoadSceneMode mode)
        {
            throw new NotImplementedException();
        }

        public override bool ScenesAreActivating()
        {
            return false;
        }

        public override string GetName(string guid)
        {
            foreach (var map in m_wrappers)
                if (map.GUID.Equals(guid))
                    return map.GetScene().name;

            return "Unknown";
        }

        public override void Clear()
        {
            m_wrappers.Clear();
        }

        public override void Add(SceneWrapper map)
        {
            m_wrappers.Add(map);
            OnMapAdded?.Invoke(map);
        }

        public override void Remove(SceneWrapper map)
        {
            m_wrappers.Remove(map);
            OnMapRemoved?.Invoke(map);
        }

        public override bool SceneLoadInProgress()
        {
            return false;
        }

        public override void ActivateAll()
        {
        }

        public override void UnloadAll()
        {
            foreach (var map in m_wrappers)
            {
                SceneManager.UnloadSceneAsync(map.GetScene());
                OnMapRemoved?.Invoke(map);
            }
        }

        public override bool SceneUnloadInProgress()
        {
            return false;
        }
    }
}
