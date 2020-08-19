using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Unity.Extensions.SceneLoading
{
    // SceneWrapper implementation that handles scenes that are opened via the Editor
    public class EditorSceneWrapper : SceneWrapper
    {
        Scene m_editorScene;

        public void SetScene(Scene scene)
        {
            m_editorScene = scene;
        }

        public override Scene GetScene()
        {
            return m_editorScene;
        }

        public override string GetName()
        {
            return m_editorScene.name;
        }

        public override void Clear()
        {
            // noop
        }

        public override bool LoadInProgress()
        {
            return false;
        }

        public override bool UnloadInProgress()
        {
            return false;
        }

        public override bool ActivationInProgress()
        {
            return false;
        }

        public override void Load(AssetReference sceneAssetReference, LoadSceneMode mode)
        {
            var scenePath = AssetDatabase.GUIDToAssetPath(sceneAssetReference.AssetGUID);
            var openMode = mode == LoadSceneMode.Additive
                ? OpenSceneMode.Additive
                : OpenSceneMode.Single;
            m_editorScene = EditorSceneManager.OpenScene(scenePath, openMode);
        }

        public override void Activate()
        {
            // noop
        }

        public override void Unload()
        {
            EditorSceneManager.CloseScene(m_editorScene, true);
        }
    }
}