using System;

namespace TwentyThree.Application.Gameplay
{
    public interface IRunCompletionSource
    {
        RunResultSnapshot FinalResult { get; }

        event Action<RunResultSnapshot> RunCompleted;
    }
}
