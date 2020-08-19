using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Unity.Extensions.SceneLoading
{
    // SceneWrapper implementation that handles Scenes that need to be loaded through the Addressables system.
    public class AddressablesSceneWrapper : SceneWrapper
    {
        AsyncOperationHandle<SceneInstance> m_loadOp;
        AsyncOperation m_activateOp;
        AsyncOperationHandle m_unloadOp;

        public override Scene GetScene()
        {
            return m_loadOp.Result.Scene;
        }

        public override string GetName()
        {
            if (m_loadOp.IsValid())
            {
                var scene = m_loadOp.Result;
                if (scene.Scene.IsValid()) return scene.Scene.name;
            }

            return string.Empty;
        }

        public override void Clear()
        {
            if (m_loadOp.IsValid()) m_loadOp.Task.Dispose();
        }

        public override bool LoadInProgress()
        {
            return (m_loadOp.Task.Status != TaskStatus.RanToCompletion);
        }

        public override bool UnloadInProgress()
        {
            if (m_unloadOp.IsValid())
            {
                return !m_unloadOp.IsDone;
            }

            return false;
        }

        public override bool ActivationInProgress()
        {
            Debug.Assert(m_activateOp != null);
            return (!m_activateOp.isDone);
        }

        public override void Load(AssetReference scene, LoadSceneMode mode)
        {
            m_loadOp = Addressables.LoadSceneAsync(scene,
                mode,
                false);
        }

        public override void Activate()
        {
            if (m_loadOp.IsValid())
            {
                var result = m_loadOp.Result;
                m_activateOp = result.ActivateAsync();
            }
        }

        public override void Unload()
        {
            m_unloadOp = Addressables.UnloadSceneAsync(m_loadOp);
        }
    }
}