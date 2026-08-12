using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TwentyThree.Application.Gameplay
{
    public sealed class RunSessionController : IRunSessionController
    {
        private readonly IGameSessionFactory _factory;
        private readonly IRunSeedProvider _seedProvider;
        private readonly List<RunResultSnapshot> _completedRuns;
        private readonly ReadOnlyCollection<RunResultSnapshot> _readOnlyCompletedRuns;
        private readonly ObserverDispatcher _observerDispatcher;

        public RunSessionController(
            IGameSessionFactory factory,
            IRunSeedProvider seedProvider)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _seedProvider = seedProvider ?? throw new ArgumentNullException(nameof(seedProvider));
            _completedRuns = new List<RunResultSnapshot>();
            _readOnlyCompletedRuns = _completedRuns.AsReadOnly();
            _observerDispatcher = new ObserverDispatcher();
        }

        public IGameSession Current { get; private set; }

        public IReadOnlyList<RunResultSnapshot> CompletedRuns => _readOnlyCompletedRuns;

        public IReadOnlyList<Exception> ObserverFailures => _observerDispatcher.Failures;

        public event Action<IGameSession> SessionChanged;

        public event Action<RunResultSnapshot> RunCompleted;

        public IGameSession StartNewRun()
        {
            if (Current != null)
            {
                Current.RunCompleted -= HandleRunCompleted;
            }

            Current = _factory.Create(_seedProvider.NextSeed());
            Current.RunCompleted += HandleRunCompleted;
            _observerDispatcher.Publish(SessionChanged, Current);
            return Current;
        }

        private void HandleRunCompleted(RunResultSnapshot result)
        {
            _completedRuns.Add(result);
            _observerDispatcher.Publish(RunCompleted, result);
        }
    }
}
