using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TwentyThree.Bootstrap;
using TwentyThree.Editor;
using TwentyThree.Presentation.Input;
using TwentyThree.Presentation.Interaction;
using TwentyThree.Presentation.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TwentyThree.Tests.EditMode.Foundation
{
    public sealed class BuildAndSceneIntegrityTests
    {
        private static readonly string[] ExpectedScenePaths =
        {
            PhaseOneProjectBuilder.BootstrapScenePath,
            PhaseOneProjectBuilder.MainMenuScenePath,
            PhaseOneProjectBuilder.GameRoomScenePath
        };

        [Test]
        public void BuildSettingsContainOnlyTheThreeFoundationScenesInOrder()
        {
            string[] enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            Assert.That(enabledScenes, Is.EqualTo(ExpectedScenePaths));
        }

        [Test]
        public void ProjectUsesTheNewInputBackendAndTheFoundationActionsAsset()
        {
            Object[] playerSettingsAssets = AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/ProjectSettings.asset");
            Assert.That(playerSettingsAssets, Is.Not.Empty);
            SerializedObject playerSettings = new SerializedObject(playerSettingsAssets[0]);
            Assert.That(playerSettings.FindProperty("activeInputHandler").intValue, Is.EqualTo(1));

            bool found = EditorBuildSettings.TryGetConfigObject(
                "com.unity.input.settings.actions",
                out InputActionAsset configuredActions);
            Assert.That(found, Is.True);
            Assert.That(
                AssetDatabase.GetAssetPath(configuredActions),
                Is.EqualTo(PhaseOneProjectBuilder.InputActionsPath));
        }

        [Test]
        public void EveryFoundationSceneHasNoMissingScripts()
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in ExpectedScenePaths)
                {
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    int missingScriptCount = CountMissingScripts(scene);
                    Assert.That(missingScriptCount, Is.Zero, $"Missing scripts in {path}.");
                }
            }
            finally
            {
                RestorePreviousSetup(previousSetup);
            }
        }

        [Test]
        public void FoundationScenesContainExpectedInfrastructure()
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene bootstrapScene = EditorSceneManager.OpenScene(
                    PhaseOneProjectBuilder.BootstrapScenePath,
                    OpenSceneMode.Single);
                Assert.That(FindComponents<ApplicationBootstrap>(bootstrapScene), Has.Count.EqualTo(1));

                Scene menuScene = EditorSceneManager.OpenScene(
                    PhaseOneProjectBuilder.MainMenuScenePath,
                    OpenSceneMode.Single);
                Assert.That(FindComponents<EventSystem>(menuScene), Has.Count.EqualTo(1));
                List<InputSystemUIInputModule> menuInputModules = FindComponents<InputSystemUIInputModule>(menuScene);
                Assert.That(menuInputModules, Has.Count.EqualTo(1));
                AssertUiActionsAreAssigned(menuInputModules[0]);
                Assert.That(FindComponents<StandaloneInputModule>(menuScene), Is.Empty);

                Scene gameRoomScene = EditorSceneManager.OpenScene(
                    PhaseOneProjectBuilder.GameRoomScenePath,
                    OpenSceneMode.Single);
                UnityEngine.Camera playerCamera = FindComponents<UnityEngine.Camera>(gameRoomScene).Single();
                Assert.That(playerCamera.targetTexture, Is.Not.Null);
                Assert.That(FindComponents<AudioListener>(gameRoomScene), Has.Count.EqualTo(1));
                Assert.That(FindComponents<EventSystem>(gameRoomScene), Has.Count.EqualTo(1));
                AssertUiActionsAreAssigned(FindComponents<InputSystemUIInputModule>(gameRoomScene).Single());
                Assert.That(FindComponents<InputContextController>(gameRoomScene), Has.Count.EqualTo(1));
                PlayerModeController playerModes = FindComponents<PlayerModeController>(gameRoomScene).Single();
                CharacterController characterController = playerModes.GetComponent<CharacterController>();
                Assert.That(characterController.height, Is.EqualTo(2.7f).Within(0.001f));
                Assert.That(characterController.radius, Is.EqualTo(0.45f).Within(0.001f));
                Assert.That(characterController.center.y, Is.EqualTo(1.35f).Within(0.001f));
                Assert.That(characterController.stepOffset, Is.EqualTo(0.4f).Within(0.001f));
                Assert.That(
                    playerModes.transform.Find("CameraPivot").localPosition.y,
                    Is.EqualTo(2.4f).Within(0.001f));

                SerializedObject serializedPlayerModes = new SerializedObject(playerModes);
                Assert.That(
                    serializedPlayerModes.FindProperty("standingEyeHeight").floatValue,
                    Is.EqualTo(2.4f).Within(0.001f));
                Assert.That(
                    serializedPlayerModes.FindProperty("seatedEyeHeight").floatValue,
                    Is.EqualTo(1.75f).Within(0.001f));

                InteractionScanner scanner = playerModes.GetComponent<InteractionScanner>();
                Assert.That(scanner, Is.Not.Null);
                Assert.That(
                    new SerializedObject(scanner).FindProperty("maximumDistance").floatValue,
                    Is.EqualTo(3f).Within(0.001f));

                BoxCollider floor = FindComponents<BoxCollider>(gameRoomScene)
                    .Single(collider => collider.name == "Floor");
                Assert.That(floor.bounds.max.y, Is.EqualTo(3.02f).Within(0.001f));
                Assert.That(GameObject.Find("PlayerSpawn").transform.position.y, Is.EqualTo(3.02f).Within(0.001f));
                Assert.That(GameObject.Find("SeatAnchor").transform.position.y, Is.EqualTo(3.02f).Within(0.001f));
                Assert.That(GameObject.Find("ExitAnchor").transform.position.y, Is.EqualTo(3.02f).Within(0.001f));

                SeatInteractable seat = FindComponents<SeatInteractable>(gameRoomScene).Single();
                BoxCollider interactionCollider = seat.GetComponent<BoxCollider>();
                Assert.That(interactionCollider.isTrigger, Is.True);
                Assert.That(interactionCollider.bounds.max.y, Is.GreaterThan(playerCamera.transform.position.y));
                SerializedProperty prompt = new SerializedObject(seat).FindProperty("prompt");
                Assert.That(prompt.stringValue, Is.EqualTo("Interactuar — Sentarse en la mesa"));

                RawImage pixelImage = FindComponents<RawImage>(gameRoomScene)
                    .Single(image => image.texture == playerCamera.targetTexture);
                Canvas pixelCanvas = pixelImage.GetComponentInParent<Canvas>();
                Canvas gameRoomUi = FindComponents<Canvas>(gameRoomScene)
                    .Single(canvas => canvas.name == "GameRoomUI");
                Assert.That(pixelImage.raycastTarget, Is.False);
                Assert.That(pixelCanvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(gameRoomUi.sortingOrder, Is.GreaterThan(pixelCanvas.sortingOrder));
                Assert.That(FindComponents<StandaloneInputModule>(gameRoomScene), Is.Empty);
            }
            finally
            {
                RestorePreviousSetup(previousSetup);
            }
        }

        [Test]
        public void BuildScenesContainOnlyModularGameplayComponents()
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in ExpectedScenePaths)
                {
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    Component[] presentComponents = FindComponents<Component>(scene)
                        .Where(component => component != null)
                        .ToArray();
                    string[] assemblyCSharpTypes = presentComponents
                        .OfType<MonoBehaviour>()
                        .Where(component => component.GetType().Assembly.GetName().Name == "Assembly-CSharp")
                        .Select(component => component.GetType().FullName)
                        .Distinct()
                        .ToArray();
                    Assert.That(
                        assemblyCSharpTypes,
                        Is.Empty,
                        $"Assembly-CSharp components found in build scene {path}.");
                }
            }
            finally
            {
                RestorePreviousSetup(previousSetup);
            }
        }

        [Test]
        public void LegacyProjectAssetsWereRemovedAndGameRoomHasNoLegacySceneDependencies()
        {
            Assert.That(AssetDatabase.IsValidFolder("Assets/Scrip"), Is.False);
            Assert.That(AssetDatabase.IsValidFolder("Assets/Scenes"), Is.False);
            Assert.That(AssetDatabase.IsValidFolder("Assets/Prefabs"), Is.False);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<Object>("Assets/InputSystem_Actions.inputactions"),
                Is.Null);

            string[] dependencies = AssetDatabase.GetDependencies(
                PhaseOneProjectBuilder.GameRoomScenePath,
                true);
            Assert.That(
                dependencies.Any(path => path.StartsWith(
                    "Assets/Scenes/",
                    StringComparison.OrdinalIgnoreCase)),
                Is.False);
            Assert.That(
                dependencies,
                Does.Contain("Assets/_Project/Content/Environment/Global Volume Profile.asset"));
            Assert.That(
                dependencies,
                Does.Contain("Assets/_Project/Content/Environment/PixelRenderTexture.renderTexture"));
        }

        private static int CountMissingScripts(Scene scene)
        {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                }
            }

            return count;
        }

        private static List<T> FindComponents<T>(Scene scene) where T : Component
        {
            List<T> components = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                components.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return components;
        }

        private static void RestorePreviousSetup(SceneSetup[] previousSetup)
        {
            if (previousSetup != null && previousSetup.Any(scene => scene.isActive))
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void AssertUiActionsAreAssigned(InputSystemUIInputModule module)
        {
            Assert.That(module.actionsAsset, Is.Not.Null);
            Assert.That(module.point?.action, Is.Not.Null);
            Assert.That(module.leftClick?.action, Is.Not.Null);
            Assert.That(module.scrollWheel?.action, Is.Not.Null);
            Assert.That(module.move?.action, Is.Not.Null);
            Assert.That(module.submit?.action, Is.Not.Null);
            Assert.That(module.cancel?.action, Is.Not.Null);
        }
    }
}
