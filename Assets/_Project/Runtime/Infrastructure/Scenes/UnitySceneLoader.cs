using TwentyThree.Application.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwentyThree.Infrastructure.Scenes
{
    public sealed class UnitySceneLoader : ISceneLoader
    {
        public bool IsLoading { get; private set; }

        public bool TryLoad(string sceneName)
        {
            if (IsLoading)
            {
                return false;
            }

            if (!UnityEngine.Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Scene '{sceneName}' is not enabled in Build Settings.");
                return false;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                Debug.LogError($"Unity could not start loading scene '{sceneName}'.");
                return false;
            }

            IsLoading = true;
            operation.completed += OnLoadCompleted;
            return true;
        }

        private void OnLoadCompleted(AsyncOperation _)
        {
            IsLoading = false;
        }
    }
}
