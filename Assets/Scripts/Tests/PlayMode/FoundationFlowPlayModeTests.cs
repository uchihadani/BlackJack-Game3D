using System.Collections;
using NUnit.Framework;
using TwentyThree.Bootstrap;
using TwentyThree.Domain.Economy;
using TwentyThree.Presentation.Camera;
using TwentyThree.Presentation.Input;
using TwentyThree.Presentation.Movement;
using TwentyThree.Presentation.Player;
using TwentyThree.Presentation.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TwentyThree.Tests.PlayMode
{
    public sealed class FoundationFlowPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            ApplicationBootstrap bootstrap = Object.FindAnyObjectByType<ApplicationBootstrap>(FindObjectsInactive.Include);
            if (bootstrap != null)
            {
                Object.Destroy(bootstrap.gameObject);
                yield return null;
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            ApplicationBootstrap bootstrap = Object.FindAnyObjectByType<ApplicationBootstrap>(FindObjectsInactive.Include);
            if (bootstrap != null)
            {
                Object.Destroy(bootstrap.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator BootstrapLoadsMainMenu()
        {
            yield return SceneManager.LoadSceneAsync("00_Bootstrap", LoadSceneMode.Single);
            yield return WaitForScene("01_MainMenu");

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("01_MainMenu"));
            Assert.That(GameObject.Find("NewRunButton"), Is.Not.Null);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("NewRunButton"));
        }

        [UnityTest]
        public IEnumerator NewRunButtonLoadsGameRoom()
        {
            yield return SceneManager.LoadSceneAsync("00_Bootstrap", LoadSceneMode.Single);
            yield return WaitForScene("01_MainMenu");

            Button newRunButton = GameObject.Find("NewRunButton").GetComponent<Button>();
            newRunButton.onClick.Invoke();
            yield return WaitForScene("10_GameRoom");

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("10_GameRoom"));
            Assert.That(Object.FindObjectsByType<UnityEngine.Camera>(FindObjectsInactive.Include), Has.Length.EqualTo(1));
            ApplicationBootstrap bootstrap = Object.FindAnyObjectByType<ApplicationBootstrap>(FindObjectsInactive.Include);
            Assert.That(bootstrap.RunSessionController.Current, Is.Not.Null);
            Assert.That(bootstrap.RunSessionController.Current.AvailableMoney, Is.EqualTo(Money.FromCoins(50)));
            Assert.That(bootstrap.RunSessionController.Current.RemainingDebt, Is.EqualTo(Money.FromCoins(200)));
            Assert.That(bootstrap.RunSessionController.Current.CurrentCycleNumber, Is.EqualTo(1));
            Assert.That(bootstrap.RunSessionController.Current.CurrentRoundNumber, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CameraDoesNotRotateWithoutLookInput()
        {
            yield return SceneManager.LoadSceneAsync("10_GameRoom", LoadSceneMode.Single);
            yield return null;

            FirstPersonLookController look = Object.FindAnyObjectByType<FirstPersonLookController>();
            Transform player = look.transform;
            Transform pivot = player.Find("CameraPivot");
            Quaternion initialBodyRotation = player.rotation;
            Quaternion initialPivotRotation = pivot.localRotation;

            for (int frame = 0; frame < 30; frame++)
            {
                yield return null;
            }

            Assert.That(Quaternion.Angle(initialBodyRotation, player.rotation), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(initialPivotRotation, pivot.localRotation), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator PlayerUsesAnIsolatedRuntimeInputAsset()
        {
            yield return SceneManager.LoadSceneAsync("10_GameRoom", LoadSceneMode.Single);
            yield return null;

            InputContextController input = Object.FindAnyObjectByType<InputContextController>();
            Assert.That(input.Actions, Is.Not.Null);
            Assert.That(input.Actions, Is.Not.SameAs(InputSystem.actions));
            Assert.That(input.Actions.FindActionMap("Exploration").enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator SeatTransitionDisablesControlAndRestoresExplorationAtExitAnchor()
        {
            yield return SceneManager.LoadSceneAsync("10_GameRoom", LoadSceneMode.Single);
            yield return null;

            PlayerModeController modes = Object.FindAnyObjectByType<PlayerModeController>();
            InputContextController input = modes.GetComponent<InputContextController>();
            FirstPersonMovementController movement = modes.GetComponent<FirstPersonMovementController>();
            FirstPersonLookController look = modes.GetComponent<FirstPersonLookController>();
            Transform seat = GameObject.Find("SeatAnchor").transform;
            Transform exit = GameObject.Find("ExitAnchor").transform;

            Assert.That(modes.TryEnterSeat(seat, exit), Is.True);
            Assert.That(modes.CurrentMode, Is.EqualTo(PlayerControlMode.SeatedAtTable));
            Assert.That(input.CurrentContext, Is.EqualTo(TwentyThree.Application.Input.ControlContext.Table));
            Assert.That(movement.MovementEnabled, Is.False);
            Assert.That(look.LookEnabled, Is.False);
            Assert.That(modes.transform.Find("CameraPivot").localPosition.y, Is.EqualTo(1.75f).Within(0.001f));

            modes.ExitSeat();
            Assert.That(modes.CurrentMode, Is.EqualTo(PlayerControlMode.Exploration));
            Assert.That(input.CurrentContext, Is.EqualTo(TwentyThree.Application.Input.ControlContext.Exploration));
            Assert.That(Vector3.Distance(modes.transform.position, exit.position), Is.LessThan(0.001f));
            Assert.That(movement.MovementEnabled, Is.True);
            Assert.That(look.LookEnabled, Is.True);
            Assert.That(modes.transform.Find("CameraPivot").localPosition.y, Is.EqualTo(2.4f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator ScannerFindsAndExecutesTheSeatInteraction()
        {
            yield return SceneManager.LoadSceneAsync("10_GameRoom", LoadSceneMode.Single);
            yield return null;

            PlayerModeController modes = Object.FindAnyObjectByType<PlayerModeController>();
            TwentyThree.Presentation.Interaction.InteractionScanner scanner =
                modes.GetComponent<TwentyThree.Presentation.Interaction.InteractionScanner>();
            CharacterController controller = modes.GetComponent<CharacterController>();

            controller.enabled = false;
            modes.transform.SetPositionAndRotation(
                new Vector3(-0.287f, 3.02f, 33f),
                Quaternion.Euler(0f, 180f, 0f));
            controller.enabled = true;
            Physics.SyncTransforms();

            UnityEngine.Camera playerCamera = Object.FindAnyObjectByType<UnityEngine.Camera>();
            BoxCollider interactionCollider = GameObject.Find("TableInteraction").GetComponent<BoxCollider>();
            Assert.That(playerCamera.transform.position.y, Is.EqualTo(5.42f).Within(0.001f));
            Assert.That(interactionCollider.isTrigger, Is.True);
            Assert.That(interactionCollider.bounds.min.y, Is.LessThan(playerCamera.transform.position.y));
            Assert.That(interactionCollider.bounds.max.y, Is.GreaterThan(playerCamera.transform.position.y));

            scanner.RefreshFocus();
            Assert.That(scanner.HasFocus, Is.True);
            Assert.That(scanner.CurrentPrompt, Does.Contain("Sentarse"));
            Assert.That(scanner.TryInteract(), Is.True);
            Assert.That(modes.CurrentMode, Is.EqualTo(PlayerControlMode.SeatedAtTable));
        }

        [UnityTest]
        public IEnumerator PauseSwitchesToUiAndRestoresThePreviousMode()
        {
            yield return SceneManager.LoadSceneAsync("10_GameRoom", LoadSceneMode.Single);
            yield return null;

            GameRoomMenuPresenter pause = Object.FindAnyObjectByType<GameRoomMenuPresenter>();
            PlayerModeController modes = Object.FindAnyObjectByType<PlayerModeController>();

            pause.Open();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(modes.CurrentMode, Is.EqualTo(PlayerControlMode.UserInterface));
            Assert.That(Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include).gameObject.activeSelf, Is.True);

            pause.Close();
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(modes.CurrentMode, Is.EqualTo(PlayerControlMode.Exploration));
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            const int maximumFrames = 300;
            for (int frame = 0; frame < maximumFrames; frame++)
            {
                if (SceneManager.GetActiveScene().name == sceneName)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"Scene '{sceneName}' did not load within {maximumFrames} frames.");
        }
    }
}
