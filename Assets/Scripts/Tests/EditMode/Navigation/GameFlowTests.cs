using NUnit.Framework;
using System;
using System.Collections.Generic;
using TwentyThree.Application.Gameplay;
using TwentyThree.Application.Navigation;

namespace TwentyThree.Tests.EditMode.Navigation
{
    public sealed class GameFlowTests
    {
        [Test]
        public void RoutesUseCatalogNamesAndRejectConcurrentLoads()
        {
            FakeSceneLoader loader = new FakeSceneLoader();
            FakeLifecycle lifecycle = new FakeLifecycle();
            FakeRunSessionController runSessions = new FakeRunSessionController();
            GameFlow flow = new GameFlow(
                new SceneCatalog("01_MainMenu", "10_GameRoom"),
                loader,
                lifecycle,
                runSessions);

            Assert.That(flow.ShowMainMenu(), Is.True);
            Assert.That(loader.LastScene, Is.EqualTo("01_MainMenu"));

            loader.IsLoading = true;
            Assert.That(flow.StartNewRun(), Is.False);
            Assert.That(loader.LastScene, Is.EqualTo("01_MainMenu"));
            Assert.That(runSessions.StartCount, Is.Zero);

            loader.IsLoading = false;
            Assert.That(flow.StartNewRun(), Is.True);
            Assert.That(loader.LastScene, Is.EqualTo("10_GameRoom"));
            Assert.That(runSessions.StartCount, Is.EqualTo(1));

            flow.Quit();
            Assert.That(lifecycle.QuitRequested, Is.True);
        }

        private sealed class FakeRunSessionController : IRunSessionController
        {
            public IGameSession Current => null;

            public IReadOnlyList<RunResultSnapshot> CompletedRuns => Array.Empty<RunResultSnapshot>();

            public IReadOnlyList<Exception> ObserverFailures => Array.Empty<Exception>();

            public int StartCount { get; private set; }

            public event Action<IGameSession> SessionChanged;

            public event Action<RunResultSnapshot> RunCompleted
            {
                add { }
                remove { }
            }

            public IGameSession StartNewRun()
            {
                StartCount++;
                SessionChanged?.Invoke(null);
                return null;
            }
        }

        private sealed class FakeSceneLoader : ISceneLoader
        {
            public bool IsLoading { get; set; }

            public string LastScene { get; private set; }

            public bool TryLoad(string sceneName)
            {
                if (IsLoading)
                {
                    return false;
                }

                LastScene = sceneName;
                return true;
            }
        }

        private sealed class FakeLifecycle : IApplicationLifecycle
        {
            public bool QuitRequested { get; private set; }

            public void Quit()
            {
                QuitRequested = true;
            }
        }
    }
}
