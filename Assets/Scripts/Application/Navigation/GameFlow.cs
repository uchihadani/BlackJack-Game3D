using System;
using TwentyThree.Application.Gameplay;

namespace TwentyThree.Application.Navigation
{
    public sealed class GameFlow : IGameFlow
    {
        private readonly SceneCatalog _sceneCatalog;
        private readonly ISceneLoader _sceneLoader;
        private readonly IApplicationLifecycle _applicationLifecycle;
        private readonly IRunSessionController _runSessionController;

        public GameFlow(
            SceneCatalog sceneCatalog,
            ISceneLoader sceneLoader,
            IApplicationLifecycle applicationLifecycle,
            IRunSessionController runSessionController)
        {
            _sceneCatalog = sceneCatalog ?? throw new ArgumentNullException(nameof(sceneCatalog));
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            _applicationLifecycle = applicationLifecycle ?? throw new ArgumentNullException(nameof(applicationLifecycle));
            _runSessionController = runSessionController ?? throw new ArgumentNullException(nameof(runSessionController));
        }

        public bool IsTransitioning => _sceneLoader.IsLoading;

        public bool ShowMainMenu()
        {
            return Load(SceneId.MainMenu);
        }

        public bool StartNewRun()
        {
            if (!Load(SceneId.GameRoom))
            {
                return false;
            }

            _runSessionController.StartNewRun();
            return true;
        }

        public void Quit()
        {
            _applicationLifecycle.Quit();
        }

        private bool Load(SceneId sceneId)
        {
            return _sceneLoader.TryLoad(_sceneCatalog.GetName(sceneId));
        }
    }
}
