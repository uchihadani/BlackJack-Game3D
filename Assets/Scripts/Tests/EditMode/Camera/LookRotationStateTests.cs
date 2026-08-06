using NUnit.Framework;
using TwentyThree.Presentation.Camera;
using UnityEngine;

namespace TwentyThree.Tests.EditMode.Camera
{
    public sealed class LookRotationStateTests
    {
        [Test]
        public void ZeroInputNeverProducesResidualYaw()
        {
            LookRotationState state = new LookRotationState(0f, -85f, 85f);

            for (int frame = 0; frame < 300; frame++)
            {
                LookRotationStep step = state.Evaluate(Vector2.zero, 0.08f, 0.08f, false);
                Assert.That(step.YawDelta, Is.Zero);
                Assert.That(step.Pitch, Is.Zero);
            }
        }

        [Test]
        public void OneMouseDeltaRotatesOnceAndThenStops()
        {
            LookRotationState state = new LookRotationState(0f, -85f, 85f);

            LookRotationStep inputFrame = state.Evaluate(new Vector2(10f, 0f), 0.08f, 0.08f, false);
            Assert.That(inputFrame.YawDelta, Is.EqualTo(0.8f).Within(0.0001f));

            for (int frame = 0; frame < 300; frame++)
            {
                LookRotationStep idleFrame = state.Evaluate(Vector2.zero, 0.08f, 0.08f, false);
                Assert.That(idleFrame.YawDelta, Is.Zero);
            }
        }

        [Test]
        public void PitchIsClampedBeforeItIsReturned()
        {
            LookRotationState state = new LookRotationState(0f, -70f, 80f);

            LookRotationStep up = state.Evaluate(new Vector2(0f, -1000f), 1f, 1f, false);
            Assert.That(up.Pitch, Is.EqualTo(80f));

            LookRotationStep down = state.Evaluate(new Vector2(0f, 1000f), 1f, 1f, false);
            Assert.That(down.Pitch, Is.EqualTo(-70f));
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void GamepadYawIsFrameRateIndependent(int framesPerSecond)
        {
            LookRotationState state = new LookRotationState(0f, -85f, 85f);
            float accumulatedYaw = 0f;
            float secondsPerFrame = 1f / framesPerSecond;

            for (int frame = 0; frame < framesPerSecond; frame++)
            {
                LookRotationStep step = state.Evaluate(
                    Vector2.right,
                    160f * secondsPerFrame,
                    160f * secondsPerFrame,
                    false);
                accumulatedYaw += step.YawDelta;
            }

            Assert.That(accumulatedYaw, Is.EqualTo(160f).Within(0.001f));
        }
    }
}
