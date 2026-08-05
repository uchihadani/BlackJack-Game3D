using System.Linq;
using NUnit.Framework;
using TwentyThree.Editor;
using TwentyThree.Presentation.Input;
using UnityEditor;
using UnityEngine.InputSystem;

namespace TwentyThree.Tests.EditMode.Foundation
{
    public sealed class InputActionsContractTests
    {
        [Test]
        public void AssetContainsTheRequiredMapsAndActions()
        {
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                PhaseOneProjectBuilder.InputActionsPath);
            Assert.That(asset, Is.Not.Null);

            Assert.That(asset.FindActionMap(InputActionPaths.GlobalMap), Is.Not.Null);
            Assert.That(asset.FindActionMap(InputActionPaths.ExplorationMap), Is.Not.Null);
            Assert.That(asset.FindActionMap(InputActionPaths.TableMap), Is.Not.Null);
            Assert.That(asset.FindActionMap(InputActionPaths.UiMap), Is.Not.Null);

            Assert.That(asset.FindAction(InputActionPaths.Pause), Is.Not.Null);
            Assert.That(asset.FindAction(InputActionPaths.Move), Is.Not.Null);
            Assert.That(asset.FindAction(InputActionPaths.Look), Is.Not.Null);
            Assert.That(asset.FindAction(InputActionPaths.Interact), Is.Not.Null);
            Assert.That(asset.FindAction(InputActionPaths.LeaveTable), Is.Not.Null);
        }

        [Test]
        public void InteractIsAPressAndNeverAHold()
        {
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                PhaseOneProjectBuilder.InputActionsPath);
            InputAction interact = asset.FindAction(InputActionPaths.Interact, true);

            string[] interactionDeclarations = interact.bindings
                .Select(binding => binding.interactions)
                .Append(interact.interactions)
                .Where(interactions => !string.IsNullOrWhiteSpace(interactions))
                .ToArray();

            Assert.That(
                interactionDeclarations.Any(value =>
                    value.IndexOf("Press", System.StringComparison.OrdinalIgnoreCase) >= 0),
                Is.True);
            Assert.That(
                interactionDeclarations.Any(value =>
                    value.IndexOf("Hold", System.StringComparison.OrdinalIgnoreCase) >= 0),
                Is.False);
            Assert.That(interact.bindings.Any(binding => binding.path == "<Keyboard>/e"), Is.True);
        }
    }
}
