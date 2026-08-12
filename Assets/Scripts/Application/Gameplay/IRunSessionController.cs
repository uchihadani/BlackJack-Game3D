using System;
using System.Collections.Generic;

namespace TwentyThree.Application.Gameplay
{
    public interface IRunSessionController
    {
        IGameSession Current { get; }

        IReadOnlyList<RunResultSnapshot> CompletedRuns { get; }

        IReadOnlyList<Exception> ObserverFailures { get; }

        event Action<IGameSession> SessionChanged;

        event Action<RunResultSnapshot> RunCompleted;

        IGameSession StartNewRun();
    }
}
