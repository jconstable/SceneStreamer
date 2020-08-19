using UnityEngine;

namespace Unity.Extensions.SceneLoading
{
    // This factory class handles creating a new instance of a SceneImplementation wrapper.
    public static class SceneWrapperFactory
    {
        // The delegate defining the method signature for a function that can create a wrapper
        public delegate SceneWrapper CreateSceneWrapper();

        // Static reference to create delegate
        static CreateSceneWrapper s_createFunction;
        
        // Static constructor, sets the default create function
        static SceneWrapperFactory()
        {
            s_createFunction = () =>
            {
                return new AddressablesSceneWrapper();
            };
        }

        // Optionally sets a different create function
        public static void SetCreateFunction(CreateSceneWrapper method)
        {
            s_createFunction = method;
        }

        // Create a SceneImplementation instance
        public static SceneWrapper Create()
        {
            return s_createFunction.Invoke();
        }
    }
}