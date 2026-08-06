using System;
using System.Linq;
using TMPro;
using TwentyThree.Bootstrap;
using TwentyThree.Infrastructure.Scenes;
using TwentyThree.Presentation.Camera;
using TwentyThree.Presentation.Input;
using TwentyThree.Presentation.Interaction;
using TwentyThree.Presentation.Movement;
using TwentyThree.Presentation.Player;
using TwentyThree.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TwentyThree.Editor
{
    public static class PhaseOneProjectBuilder
    {
        public const string InputActionsPath = "Assets/GameContent/Input/TwentyThreeInput.inputactions";
        public const string SceneConfigurationPath = "Assets/GameContent/Configuration/SceneFlowConfiguration.asset";
        public const string PlayerPrefabPath = "Assets/Prefabs/PlayerRig.prefab";
        public const string BootstrapScenePath = "Assets/Scenes/00_Bootstrap.unity";
        public const string MainMenuScenePath = "Assets/Scenes/01_MainMenu.unity";
        public const string GameRoomScenePath = "Assets/Scenes/10_GameRoom.unity";

        private const string PixelRenderTexturePath =
            "Assets/GameContent/Environment/PixelRenderTexture.renderTexture";
        private const string InteractableLayerName = "Interactable";
        private const string SeatPrompt = "Interactuar — Sentarse en la mesa";
        private const float PlayerHeight = 2.7f;
        private const float PlayerRadius = 0.45f;
        private const float StandingEyeHeight = 2.4f;
        private const float SeatedEyeHeight = 1.75f;
        private const float InteractionDistance = 3f;

        private static readonly Color BackgroundColor = new Color(0.025f, 0.018f, 0.04f, 1f);
        private static readonly Color PanelColor = new Color(0.08f, 0.045f, 0.12f, 0.96f);
        private static readonly Color ButtonColor = new Color(0.25f, 0.10f, 0.34f, 1f);
        private static readonly Color AccentColor = new Color(0.72f, 0.36f, 0.95f, 1f);

        [MenuItem("23 al Filo/Phase 1/Build Foundation")]
        public static void BuildFromMenu()
        {
            SceneSetup[] previousSceneSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                BuildFoundation();
                EditorUtility.DisplayDialog(
                    "23 al Filo",
                    "Phase 1 foundation validated and added to Build Settings.",
                    "OK");
            }
            finally
            {
                if (previousSceneSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSceneSetup);
                }
            }
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                BuildFoundation();
                Debug.Log("PHASE_ONE_BUILD_SUCCEEDED");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static void BuildFoundation()
        {
            EnsureFolder("Assets/GameContent/Configuration");
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Scenes");

            AssetDatabase.ImportAsset(InputActionsPath, ImportAssetOptions.ForceSynchronousImport);
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
            {
                throw new InvalidOperationException($"Input actions could not be loaded at {InputActionsPath}.");
            }

            EditorBuildSettings.AddConfigObject(
                "com.unity.input.settings.actions",
                inputActions,
                true);

            int interactableLayer = EnsureLayer(InteractableLayerName);
            SceneFlowConfiguration sceneConfiguration = GetOrCreateSceneConfiguration();
            GameObject playerPrefab = CreatePlayerPrefab(inputActions, interactableLayer);

            CreateBootstrapScene(sceneConfiguration);
            CreateMainMenuScene(inputActions);
            CreateGameRoomScene(inputActions, playerPrefab, interactableLayer);
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static SceneFlowConfiguration GetOrCreateSceneConfiguration()
        {
            SceneFlowConfiguration configuration =
                AssetDatabase.LoadAssetAtPath<SceneFlowConfiguration>(SceneConfigurationPath);
            if (configuration != null)
            {
                return configuration;
            }

            configuration = ScriptableObject.CreateInstance<SceneFlowConfiguration>();
            AssetDatabase.CreateAsset(configuration, SceneConfigurationPath);
            return configuration;
        }

        private static GameObject CreatePlayerPrefab(InputActionAsset inputActions, int interactableLayer)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (existingPrefab != null)
            {
                UpdatePlayerPrefab(inputActions, interactableLayer);
                return AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            }

            GameObject root = new GameObject("PlayerRig");
            try
            {
                CharacterController characterController = root.AddComponent<CharacterController>();
                ConfigureCharacterController(characterController);
                characterController.slopeLimit = 50f;

                InputContextController input = root.AddComponent<InputContextController>();
                CursorStateController cursor = root.AddComponent<CursorStateController>();
                FirstPersonMovementController movement = root.AddComponent<FirstPersonMovementController>();
                FirstPersonLookController look = root.AddComponent<FirstPersonLookController>();
                PlayerModeController playerModes = root.AddComponent<PlayerModeController>();
                InteractionScanner scanner = root.AddComponent<InteractionScanner>();
                InteractionController interaction = root.AddComponent<InteractionController>();

                Transform pitchPivot = new GameObject("CameraPivot").transform;
                pitchPivot.SetParent(root.transform, false);
                pitchPivot.localPosition = new Vector3(0f, StandingEyeHeight, 0f);

                GameObject cameraObject = new GameObject("MainCamera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(pitchPivot, false);
                UnityEngine.Camera playerCamera = cameraObject.AddComponent<UnityEngine.Camera>();
                playerCamera.nearClipPlane = 0.05f;
                playerCamera.farClipPlane = 200f;
                playerCamera.targetTexture = LoadPixelRenderTexture();
                cameraObject.AddComponent<AudioListener>();

                SetObjectReference(input, "inputActions", inputActions);

                SetObjectReference(movement, "input", input);

                SetObjectReference(look, "input", input);
                SetObjectReference(look, "body", root.transform);
                SetObjectReference(look, "pitchPivot", pitchPivot);

                SetObjectReference(playerModes, "input", input);
                SetObjectReference(playerModes, "cursorState", cursor);
                SetObjectReference(playerModes, "movement", movement);
                SetObjectReference(playerModes, "look", look);
                SetObjectReference(playerModes, "viewPivot", pitchPivot);
                SetFloat(playerModes, "standingEyeHeight", StandingEyeHeight);
                SetFloat(playerModes, "seatedEyeHeight", SeatedEyeHeight);

                SetObjectReference(scanner, "viewCamera", playerCamera);
                SetObjectReference(scanner, "playerModes", playerModes);
                SetLayerMask(scanner, "raycastMask", Physics.DefaultRaycastLayers);
                SetLayerMask(scanner, "interactableLayers", 1 << interactableLayer);
                SetFloat(scanner, "maximumDistance", InteractionDistance);

                SetObjectReference(interaction, "input", input);
                SetObjectReference(interaction, "scanner", scanner);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException("PlayerRig prefab could not be saved.");
                }

                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void UpdatePlayerPrefab(InputActionAsset inputActions, int interactableLayer)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                InputContextController input = RequireComponent<InputContextController>(root);
                CharacterController characterController = RequireComponent<CharacterController>(root);
                CursorStateController cursor = RequireComponent<CursorStateController>(root);
                FirstPersonMovementController movement = RequireComponent<FirstPersonMovementController>(root);
                FirstPersonLookController look = RequireComponent<FirstPersonLookController>(root);
                PlayerModeController playerModes = RequireComponent<PlayerModeController>(root);
                InteractionScanner scanner = RequireComponent<InteractionScanner>(root);
                InteractionController interaction = RequireComponent<InteractionController>(root);

                Transform pitchPivot = root.transform.Find("CameraPivot");
                if (pitchPivot == null)
                {
                    throw new InvalidOperationException("PlayerRig prefab has no CameraPivot.");
                }

                UnityEngine.Camera playerCamera = pitchPivot.GetComponentInChildren<UnityEngine.Camera>(true);
                if (playerCamera == null)
                {
                    throw new InvalidOperationException("PlayerRig prefab has no player camera.");
                }

                playerCamera.targetTexture = LoadPixelRenderTexture();
                EditorUtility.SetDirty(playerCamera);
                ConfigureCharacterController(characterController);
                pitchPivot.localPosition = new Vector3(0f, StandingEyeHeight, 0f);
                EditorUtility.SetDirty(pitchPivot);

                SetObjectReference(input, "inputActions", inputActions);
                SetObjectReference(movement, "input", input);
                SetObjectReference(look, "input", input);
                SetObjectReference(look, "body", root.transform);
                SetObjectReference(look, "pitchPivot", pitchPivot);
                SetObjectReference(playerModes, "input", input);
                SetObjectReference(playerModes, "cursorState", cursor);
                SetObjectReference(playerModes, "movement", movement);
                SetObjectReference(playerModes, "look", look);
                SetObjectReference(playerModes, "viewPivot", pitchPivot);
                SetFloat(playerModes, "standingEyeHeight", StandingEyeHeight);
                SetFloat(playerModes, "seatedEyeHeight", SeatedEyeHeight);
                SetObjectReference(scanner, "viewCamera", playerCamera);
                SetObjectReference(scanner, "playerModes", playerModes);
                SetLayerMask(scanner, "raycastMask", Physics.DefaultRaycastLayers);
                SetLayerMask(scanner, "interactableLayers", 1 << interactableLayer);
                SetFloat(scanner, "maximumDistance", InteractionDistance);
                SetObjectReference(interaction, "input", input);
                SetObjectReference(interaction, "scanner", scanner);

                if (PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath) == null)
                {
                    throw new InvalidOperationException("PlayerRig prefab could not be updated.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CreateBootstrapScene(SceneFlowConfiguration sceneConfiguration)
        {
            sceneConfiguration = AssetDatabase.LoadAssetAtPath<SceneFlowConfiguration>(
                SceneConfigurationPath);
            if (sceneConfiguration == null)
            {
                throw new InvalidOperationException("SceneFlowConfiguration could not be reloaded.");
            }

            Scene scene;
            ApplicationBootstrap bootstrap;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath) != null)
            {
                scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
                bootstrap = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<ApplicationBootstrap>(true))
                    .FirstOrDefault();
                if (bootstrap == null)
                {
                    bootstrap = new GameObject("ApplicationBootstrap").AddComponent<ApplicationBootstrap>();
                }
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                bootstrap = new GameObject("ApplicationBootstrap").AddComponent<ApplicationBootstrap>();
            }

            bootstrap.SetSceneFlowConfiguration(sceneConfiguration);
            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.MarkSceneDirty(scene);
            SaveScene(scene, BootstrapScenePath);
        }

        private static void CreateMainMenuScene(InputActionAsset inputActions)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) != null)
            {
                Scene existingScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
                ConfigureEventSystems(existingScene, inputActions);
                SaveScene(existingScene, MainMenuScenePath);
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateMenuCamera();
            GameObject eventSystem = CreateEventSystem("EventSystem", inputActions);
            eventSystem.SetActive(true);

            Canvas canvas = CreateCanvas("MainMenuUI");
            CreateImage("Background", canvas.transform, BackgroundColor, Vector2.zero, Vector2.zero, true);
            CreateText("Title", canvas.transform, "23 AL FILO", 72f, AccentColor, new Vector2(0f, 310f), new Vector2(900f, 120f));
            CreateText("Subtitle", canvas.transform, "La deuda siempre cobra algo.", 26f, Color.white, new Vector2(0f, 240f), new Vector2(900f, 60f));

            Button newRun = CreateButton("NewRunButton", canvas.transform, "Nueva partida", new Vector2(0f, 105f), new Vector2(380f, 64f));
            Button continueButton = CreateButton("ContinueButton", canvas.transform, "Continuar", new Vector2(0f, 25f), new Vector2(380f, 64f));
            Button options = CreateButton("OptionsButton", canvas.transform, "Opciones", new Vector2(0f, -55f), new Vector2(380f, 64f));
            Button quit = CreateButton("QuitButton", canvas.transform, "Salir", new Vector2(0f, -135f), new Vector2(380f, 64f));

            GameObject optionsPanel = CreatePanel("OptionsPanel", canvas.transform, PanelColor, new Vector2(720f, 570f));
            CreateText("OptionsTitle", optionsPanel.transform, "Opciones de cámara", 38f, Color.white, new Vector2(0f, 225f), new Vector2(620f, 60f));

            CreateText("MouseLabel", optionsPanel.transform, "Sensibilidad del mouse", 24f, Color.white, new Vector2(-100f, 125f), new Vector2(380f, 45f));
            Slider mouseSlider = CreateSlider("MouseSensitivity", optionsPanel.transform, new Vector2(-65f, 75f), new Vector2(430f, 34f), 0.01f, 0.5f, 0.08f);
            TMP_Text mouseValue = CreateText("MouseValue", optionsPanel.transform, "0.08", 22f, Color.white, new Vector2(245f, 75f), new Vector2(100f, 40f));

            CreateText("GamepadLabel", optionsPanel.transform, "Velocidad del gamepad", 24f, Color.white, new Vector2(-100f, 5f), new Vector2(380f, 45f));
            Slider gamepadSlider = CreateSlider("GamepadLookSpeed", optionsPanel.transform, new Vector2(-65f, -45f), new Vector2(430f, 34f), 30f, 360f, 160f);
            gamepadSlider.wholeNumbers = true;
            TMP_Text gamepadValue = CreateText("GamepadValue", optionsPanel.transform, "160", 22f, Color.white, new Vector2(245f, -45f), new Vector2(100f, 40f));

            Toggle invertToggle = CreateToggle("InvertVertical", optionsPanel.transform, "Invertir eje vertical", new Vector2(-95f, -125f), new Vector2(420f, 44f));
            Button closeOptions = CreateButton("CloseOptionsButton", optionsPanel.transform, "Volver", new Vector2(0f, -220f), new Vector2(260f, 58f));

            MainMenuPresenter presenter = canvas.gameObject.AddComponent<MainMenuPresenter>();
            SetObjectReference(presenter, "newRunButton", newRun);
            SetObjectReference(presenter, "continueButton", continueButton);
            SetObjectReference(presenter, "optionsButton", options);
            SetObjectReference(presenter, "quitButton", quit);
            SetObjectReference(presenter, "optionsPanel", optionsPanel);
            SetObjectReference(presenter, "closeOptionsButton", closeOptions);
            SetObjectReference(presenter, "mouseSensitivitySlider", mouseSlider);
            SetObjectReference(presenter, "gamepadLookSpeedSlider", gamepadSlider);
            SetObjectReference(presenter, "invertVerticalToggle", invertToggle);
            SetObjectReference(presenter, "mouseSensitivityValue", mouseValue);
            SetObjectReference(presenter, "gamepadLookSpeedValue", gamepadValue);

            SaveScene(scene, MainMenuScenePath);
        }

        private static void CreateGameRoomScene(
            InputActionAsset inputActions,
            GameObject playerPrefab,
            int interactableLayer)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameRoomScenePath) == null)
            {
                throw new InvalidOperationException(
                    $"The authoritative GameRoom scene is missing at {GameRoomScenePath}. " +
                    "It is no longer generated from a legacy scene.");
            }

            AssetDatabase.ImportAsset(GameRoomScenePath, ImportAssetOptions.ForceSynchronousImport);
            Scene scene = EditorSceneManager.OpenScene(GameRoomScenePath, OpenSceneMode.Single);
            if (scene.GetRootGameObjects().Any(root => root.name == "Gameplay"))
            {
                UpdateGameRoomScene(scene, inputActions, interactableLayer);
                SaveScene(scene, GameRoomScenePath);
                return;
            }

            RemoveExistingSceneInfrastructure(scene);
            GroupEnvironmentRoots(scene);

            GameObject gameplayRoot = new GameObject("Gameplay");
            GameObject collisionRoot = new GameObject("Collision");
            GameObject uiRoot = new GameObject("UI");

            GameObject playerSpawn = new GameObject("PlayerSpawn");
            playerSpawn.transform.SetParent(gameplayRoot.transform, false);
            playerSpawn.transform.SetPositionAndRotation(
                new Vector3(-0.3f, 3.02f, 36f),
                Quaternion.Euler(0f, 180f, 0f));

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "PlayerRig";
            player.transform.SetParent(gameplayRoot.transform, true);
            player.transform.SetPositionAndRotation(playerSpawn.transform.position, playerSpawn.transform.rotation);

            CreateCollisionProxies(collisionRoot.transform);
            CreateTableSeat(gameplayRoot.transform, interactableLayer);

            InputContextController input = player.GetComponent<InputContextController>();
            PlayerModeController playerModes = player.GetComponent<PlayerModeController>();
            InteractionScanner scanner = player.GetComponent<InteractionScanner>();

            Canvas canvas = CreateCanvas("GameRoomUI");
            canvas.sortingOrder = 100;
            canvas.transform.SetParent(uiRoot.transform, false);
            TMP_Text crosshair = CreateText("Crosshair", canvas.transform, "+", 24f, Color.white, Vector2.zero, new Vector2(40f, 40f));
            crosshair.raycastTarget = false;
            TMP_Text prompt = CreateText("InteractionPrompt", canvas.transform, string.Empty, 26f, Color.white, new Vector2(0f, -260f), new Vector2(900f, 60f));
            prompt.raycastTarget = false;

            InteractionPromptPresenter promptPresenter = canvas.gameObject.AddComponent<InteractionPromptPresenter>();
            SetObjectReference(promptPresenter, "scanner", scanner);
            SetObjectReference(promptPresenter, "promptLabel", prompt);

            GameObject pausePanel = CreatePanel("PausePanel", canvas.transform, PanelColor, new Vector2(620f, 430f));
            CreateText("PauseTitle", pausePanel.transform, "Pausa", 46f, Color.white, new Vector2(0f, 135f), new Vector2(500f, 70f));
            Button resume = CreateButton("ResumeButton", pausePanel.transform, "Continuar", new Vector2(0f, 25f), new Vector2(340f, 64f));
            Button mainMenu = CreateButton("MainMenuButton", pausePanel.transform, "Volver al menú", new Vector2(0f, -65f), new Vector2(340f, 64f));

            GameObject eventSystem = CreateEventSystem("EventSystem", inputActions);
            eventSystem.transform.SetParent(uiRoot.transform, false);
            eventSystem.SetActive(false);

            GameRoomMenuPresenter pausePresenter = canvas.gameObject.AddComponent<GameRoomMenuPresenter>();
            SetObjectReference(pausePresenter, "input", input);
            SetObjectReference(pausePresenter, "playerModes", playerModes);
            SetObjectReference(pausePresenter, "menuPanel", pausePanel);
            SetObjectReference(pausePresenter, "eventSystemRoot", eventSystem);
            SetObjectReference(pausePresenter, "resumeButton", resume);
            SetObjectReference(pausePresenter, "mainMenuButton", mainMenu);

            ConfigurePixelPresentation(scene, player.GetComponentInChildren<UnityEngine.Camera>(true));
            SaveScene(scene, GameRoomScenePath);
        }

        private static void UpdateGameRoomScene(
            Scene scene,
            InputActionAsset inputActions,
            int interactableLayer)
        {
            PlayerModeController playerModes = FindSceneComponent<PlayerModeController>(scene);
            if (playerModes == null)
            {
                throw new InvalidOperationException("GameRoom has no PlayerModeController.");
            }

            UnityEngine.Camera playerCamera = playerModes.GetComponentInChildren<UnityEngine.Camera>(true);
            if (playerCamera == null)
            {
                throw new InvalidOperationException("GameRoom has no player camera.");
            }

            Transform viewPivot = playerModes.transform.Find("CameraPivot");
            SetObjectReference(playerModes, "viewPivot", viewPivot);

            InteractionScanner scanner = playerModes.GetComponent<InteractionScanner>();
            if (scanner != null)
            {
                SetLayerMask(scanner, "interactableLayers", 1 << interactableLayer);
                SetFloat(scanner, "maximumDistance", InteractionDistance);
            }

            SeatInteractable seat = FindSceneComponent<SeatInteractable>(scene);
            if (seat != null)
            {
                seat.gameObject.layer = interactableLayer;
                SetString(seat, "prompt", SeatPrompt);
                ConfigureSeatInteractionCollider(seat.GetComponent<BoxCollider>());
            }

            Canvas gameRoomUi = FindSceneComponents<Canvas>(scene)
                .FirstOrDefault(candidate => candidate.name == "GameRoomUI");
            if (gameRoomUi != null)
            {
                gameRoomUi.sortingOrder = 100;
                EditorUtility.SetDirty(gameRoomUi);
            }

            ConfigureEventSystems(scene, inputActions);
            ConfigurePixelPresentation(scene, playerCamera);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void RemoveExistingSceneInfrastructure(Scene scene)
        {
            UnityEngine.Camera[] cameras = Object.FindObjectsByType<UnityEngine.Camera>(
                FindObjectsInactive.Include);
            foreach (UnityEngine.Camera sceneCamera in cameras)
            {
                if (sceneCamera.gameObject.scene == scene)
                {
                    Object.DestroyImmediate(sceneCamera.gameObject);
                }
            }

            EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include);
            foreach (EventSystem eventSystem in eventSystems)
            {
                if (eventSystem.gameObject.scene == scene)
                {
                    Object.DestroyImmediate(eventSystem.gameObject);
                }
            }
        }

        private static void GroupEnvironmentRoots(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            GameObject environment = roots.FirstOrDefault(root => root.name == "Environment") ?? new GameObject("Environment");
            foreach (GameObject root in roots)
            {
                if (root != environment)
                {
                    root.transform.SetParent(environment.transform, true);
                }
            }
        }

        private static void CreateCollisionProxies(Transform parent)
        {
            CreateBoxCollider("Floor", parent, new Vector3(-0.3f, 2.77f, 31f), new Vector3(18f, 0.5f, 24f));
            CreateBoxCollider("WallNorth", parent, new Vector3(-0.3f, 5.5f, 19f), new Vector3(18f, 5.5f, 0.5f));
            CreateBoxCollider("WallSouth", parent, new Vector3(-0.3f, 5.5f, 43f), new Vector3(18f, 5.5f, 0.5f));
            CreateBoxCollider("WallWest", parent, new Vector3(-9.3f, 5.5f, 31f), new Vector3(0.5f, 5.5f, 24f));
            CreateBoxCollider("WallEast", parent, new Vector3(8.7f, 5.5f, 31f), new Vector3(0.5f, 5.5f, 24f));
            CreateBoxCollider("TableProxy", parent, new Vector3(-0.37f, 3.85f, 24.51f), new Vector3(6.2f, 1.7f, 4.4f));
        }

        private static void CreateTableSeat(Transform parent, int interactableLayer)
        {
            GameObject root = new GameObject("TableInteraction");
            root.layer = interactableLayer;
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(-0.287f, 3.02f, 30.8f);

            BoxCollider collider = root.AddComponent<BoxCollider>();
            ConfigureSeatInteractionCollider(collider);

            Transform seatAnchor = new GameObject("SeatAnchor").transform;
            seatAnchor.SetParent(root.transform, false);
            seatAnchor.SetPositionAndRotation(
                new Vector3(-0.287f, 3.02f, 29.8f),
                Quaternion.Euler(0f, 180f, 0f));

            Transform exitAnchor = new GameObject("ExitAnchor").transform;
            exitAnchor.SetParent(root.transform, false);
            exitAnchor.SetPositionAndRotation(
                new Vector3(-0.287f, 3.02f, 32.4f),
                Quaternion.Euler(0f, 180f, 0f));

            SeatInteractable seat = root.AddComponent<SeatInteractable>();
            SetObjectReference(seat, "seatPose", seatAnchor);
            SetObjectReference(seat, "exitPose", exitAnchor);
            SetString(seat, "prompt", SeatPrompt);
        }

        private static void CreateBoxCollider(string name, Transform parent, Vector3 position, Vector3 size)
        {
            GameObject colliderObject = new GameObject(name);
            colliderObject.transform.SetParent(parent, false);
            colliderObject.transform.position = position;
            BoxCollider collider = colliderObject.AddComponent<BoxCollider>();
            collider.size = size;
        }

        private static void ConfigureCharacterController(CharacterController controller)
        {
            controller.height = PlayerHeight;
            controller.radius = PlayerRadius;
            controller.center = new Vector3(0f, PlayerHeight * 0.5f, 0f);
            controller.stepOffset = 0.4f;
            controller.slopeLimit = 50f;
            EditorUtility.SetDirty(controller);
        }

        private static void ConfigureSeatInteractionCollider(BoxCollider collider)
        {
            if (collider == null)
            {
                throw new InvalidOperationException("TableInteraction requires a BoxCollider.");
            }

            collider.isTrigger = true;
            collider.center = new Vector3(0f, 1.6f, 0f);
            collider.size = new Vector3(2f, 3.6f, 1.5f);
            EditorUtility.SetDirty(collider);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true),
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameRoomScenePath, true)
            };
        }

        private static UnityEngine.Camera CreateMenuCamera()
        {
            GameObject cameraObject = new GameObject("MainCamera");
            cameraObject.tag = "MainCamera";
            UnityEngine.Camera camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject canvasObject = new GameObject(name, typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static GameObject CreateEventSystem(string name, InputActionAsset inputActions)
        {
            GameObject eventSystemObject = new GameObject(name);
            eventSystemObject.AddComponent<EventSystem>();
            InputSystemUIInputModule module = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            ConfigureUiInputModule(module, inputActions);
            return eventSystemObject;
        }

        private static void ConfigureEventSystems(Scene scene, InputActionAsset inputActions)
        {
            foreach (InputSystemUIInputModule module in FindSceneComponents<InputSystemUIInputModule>(scene))
            {
                ConfigureUiInputModule(module, inputActions);
                EditorUtility.SetDirty(module);
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void ConfigureUiInputModule(
            InputSystemUIInputModule module,
            InputActionAsset inputActions)
        {
            module.actionsAsset = inputActions;
            module.point = FindActionReference(inputActions, "UI/Point");
            module.leftClick = FindActionReference(inputActions, "UI/Click");
            module.scrollWheel = FindActionReference(inputActions, "UI/ScrollWheel");
            module.move = FindActionReference(inputActions, "UI/Navigate");
            module.submit = FindActionReference(inputActions, "UI/Submit");
            module.cancel = FindActionReference(inputActions, "UI/Cancel");
        }

        private static void ConfigurePixelPresentation(Scene scene, UnityEngine.Camera playerCamera)
        {
            RenderTexture renderTexture = LoadPixelRenderTexture();
            playerCamera.targetTexture = renderTexture;
            EditorUtility.SetDirty(playerCamera);

            Transform pixelRoot = FindSceneComponents<Transform>(scene)
                .FirstOrDefault(candidate => candidate.name == "PixelArr");
            if (pixelRoot == null)
            {
                throw new InvalidOperationException("GameRoom environment has no PixelArr presentation root.");
            }

            Canvas pixelCanvas = pixelRoot.GetComponentInChildren<Canvas>(true);
            RawImage pixelImage = pixelRoot.GetComponentInChildren<RawImage>(true);
            if (pixelCanvas == null || pixelImage == null)
            {
                throw new InvalidOperationException("PixelArr requires an overlay Canvas and RawImage.");
            }

            pixelCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            pixelCanvas.sortingOrder = 0;
            pixelImage.texture = renderTexture;
            pixelImage.raycastTarget = false;
            EditorUtility.SetDirty(pixelCanvas);
            EditorUtility.SetDirty(pixelImage);

            foreach (Canvas worldCanvas in FindSceneComponents<Canvas>(scene)
                         .Where(candidate => candidate.renderMode == RenderMode.WorldSpace))
            {
                worldCanvas.worldCamera = playerCamera;
                EditorUtility.SetDirty(worldCanvas);
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static RenderTexture LoadPixelRenderTexture()
        {
            RenderTexture renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(PixelRenderTexturePath);
            if (renderTexture == null)
            {
                throw new InvalidOperationException($"Pixel render texture not found at {PixelRenderTexturePath}.");
            }

            return renderTexture;
        }

        private static InputActionReference FindActionReference(InputActionAsset inputActions, string actionPath)
        {
            InputAction action = inputActions.FindAction(actionPath, true);
            InputActionReference reference = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath)
                .OfType<InputActionReference>()
                .FirstOrDefault(candidate => candidate.action != null && candidate.action.id == action.id);
            if (reference == null)
            {
                throw new InvalidOperationException($"No imported InputActionReference exists for '{actionPath}'.");
            }

            return reference;
        }

        private static GameObject CreateImage(
            string name,
            Transform parent,
            Color color,
            Vector2 anchoredPosition,
            Vector2 size,
            bool stretch)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            if (stretch)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = size;
            }

            imageObject.GetComponent<Image>().color = color;
            return imageObject;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color, Vector2 size)
        {
            return CreateImage(name, parent, color, Vector2.zero, size, false);
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            string value,
            float fontSize,
            Color color,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string label,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject buttonObject = CreateImage(name, parent, ButtonColor, anchoredPosition, size, false);
            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.7f);
            button.colors = colors;

            TMP_Text text = CreateText("Label", buttonObject.transform, label, 28f, Color.white, Vector2.zero, size);
            text.raycastTarget = false;
            return button;
        }

        private static Slider CreateSlider(
            string name,
            Transform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            float minimum,
            float maximum,
            float value)
        {
            GameObject sliderObject = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderObject.name = name;
            sliderObject.transform.SetParent(parent, false);
            RectTransform rect = sliderObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.value = value;
            return slider;
        }

        private static Toggle CreateToggle(
            string name,
            Transform parent,
            string label,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject toggleObject = DefaultControls.CreateToggle(new DefaultControls.Resources());
            toggleObject.name = name;
            toggleObject.transform.SetParent(parent, false);
            RectTransform rect = toggleObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text legacyLabel = toggleObject.GetComponentInChildren<Text>();
            if (legacyLabel != null)
            {
                Object.DestroyImmediate(legacyLabel.gameObject);
            }

            TMP_Text text = CreateText("Label", toggleObject.transform, label, 23f, Color.white, new Vector2(100f, 0f), new Vector2(300f, 44f));
            text.alignment = TextAlignmentOptions.Left;
            text.raycastTarget = false;
            return toggleObject.GetComponent<Toggle>();
        }

        private static void SaveScene(Scene scene, string path)
        {
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new InvalidOperationException($"Scene could not be saved at {path}.");
            }
        }

        private static int EnsureLayer(string layerName)
        {
            int existingLayer = LayerMask.NameToLayer(layerName);
            if (existingLayer >= 0)
            {
                return existingLayer;
            }

            Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets.Length == 0)
            {
                throw new InvalidOperationException("TagManager.asset could not be loaded.");
            }

            SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int index = 8; index < 32; index++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(index);
                if (!string.IsNullOrEmpty(layer.stringValue))
                {
                    continue;
                }

                layer.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return index;
            }

            throw new InvalidOperationException("No free user layer is available for interactables.");
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().Name}.");
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLayerMask(Object target, string propertyName, int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().Name}.");
            }

            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().Name}.");
            }

            property.stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().Name}.");
            }

            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T RequireComponent<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException($"{root.name} requires {typeof(T).Name}.");
            }

            return component;
        }

        private static T FindSceneComponent<T>(Scene scene) where T : Component
        {
            return FindSceneComponents<T>(scene).FirstOrDefault();
        }

        private static T[] FindSceneComponents<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
        }
    }
}
