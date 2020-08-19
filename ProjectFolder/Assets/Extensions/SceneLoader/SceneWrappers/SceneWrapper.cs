using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Unity.Extensions.SceneLoading
{
    public abstract class SceneWrapper
    {
        public string GUID { get; set; }
        public abstract Scene GetScene();
        public abstract string GetName();
        public abstract void Clear();
        public abstract bool LoadInProgress();
        public abstract bool UnloadInProgress();
        public abstract bool ActivationInProgress();
        public abstract void Load(AssetReference sceneAssetReference,LoadSceneMode mode);
        public abstract void Activate();
        public abstract void Unload();
    }
}
