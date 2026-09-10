using System;
using TwentyThree.Application.Gameplay;
using TwentyThree.Application.Navigation;
using TwentyThree.Application.Settings;
using TwentyThree.Infrastructure.Configuration;
using TwentyThree.Infrastructure.Random;
using TwentyThree.Infrastructure.Scenes;
using TwentyThree.Infrastructure.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwentyThree.Bootstrap
{
    [DefaultExecutionOrder(-1000)]
    public sealed class ApplicationBootstrap : MonoBehaviour
    {
        [SerializeField] private SceneFlowConfiguration sceneFlowConfiguration;
        [SerializeField] private GameRulesConfiguration gameRulesConfiguration;

        private static ApplicationBootstrap activeBootstrap;
        private IGameFlow _gameFlow;
        private IPlayerPreferences _playerPreferences;
        private IRunSessionController _runSessionController;

        public IRunSessionController RunSessionController => _runSessionController;

        public void SetSceneFlowConfiguration(SceneFlowConfiguration configuration)
        {
            sceneFlowConfiguration = configuration;
        }

        public void SetGameRulesConfiguration(GameRulesConfiguration configuration)
        {
            gameRulesConfiguration = configuration;
        }

        private void Awake()
        {
            if (activeBootstrap != null && activeBootstrap != this)
            {
                Destroy(gameObject);
                return;
            }

            if (sceneFlowConfiguration == null || gameRulesConfiguration == null)
            {
                Debug.LogError(
                    "ApplicationBootstrap requires scene flow and game rules configurations.",
                    this);
                enabled = false;
                return;
            }

            activeBootstrap = this;
            DontDestroyOnLoad(gameObject);

            try
            {
                GameSessionFactory sessionFactory = new GameSessionFactory(
                    gameRulesConfiguration.CreateRules(),
                    new DeterministicCardShuffler(),
                    new DeterministicRandomStreamFactory());
                _runSessionController = new RunSessionController(
                    sessionFactory,
                    new SystemRunSeedProvider());
                _gameFlow = new GameFlow(
                    sceneFlowConfiguration.CreateCatalog(),
                    new UnitySceneLoader(),
                    new UnityApplicationLifecycle(),
                    _runSessionController);
                _playerPreferences = new PlayerPreferences();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                enabled = false;
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            InjectInto(SceneManager.GetActiveScene());
        }

        private void Start()
        {
            if (enabled && SceneManager.GetActiveScene().name == "00_Bootstrap")
            {
                _gameFlow.ShowMainMenu();
            }
        }

        private void OnDestroy()
        {
            if (activeBootstrap != this)
            {
                return;
            }

            _playerPreferences?.Save();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            activeBootstrap = null;
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                _playerPreferences?.Save();
            }
        }

        private void OnApplicationQuit()
        {
            _playerPreferences?.Save();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            InjectInto(scene);
        }

        private void InjectInto(Scene scene)
        {
            if (_gameFlow == null ||
                _playerPreferences == null ||
                _runSessionController == null ||
                !scene.IsValid())
            {
                return;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (behaviour is IGameFlowConsumer consumer)
                    {
                        consumer.Configure(_gameFlow);
                    }

                    if (behaviour is IPlayerPreferencesConsumer preferencesConsumer)
                    {
                        preferencesConsumer.Configure(_playerPreferences);
                    }

                    if (behaviour is IRunSessionConsumer runSessionConsumer)
                    {
                        runSessionConsumer.Configure(_runSessionController);
                    }
                }
            }
        }
    }
}
