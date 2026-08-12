using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TwentyThree.Application.Gameplay
{
    internal sealed class ObserverDispatcher
    {
        private readonly List<Exception> _failures;
        private readonly ReadOnlyCollection<Exception> _readOnlyFailures;

        public ObserverDispatcher()
        {
            _failures = new List<Exception>();
            _readOnlyFailures = _failures.AsReadOnly();
        }

        public IReadOnlyList<Exception> Failures => _readOnlyFailures;

        public void Publish(Action observers)
        {
            if (observers == null)
            {
                return;
            }

            foreach (Action observer in observers.GetInvocationList())
            {
                try
                {
                    observer();
                }
                catch (Exception exception)
                {
                    _failures.Add(exception);
                }
            }
        }

        public void Publish<T>(Action<T> observers, T value)
        {
            if (observers == null)
            {
                return;
            }

            foreach (Action<T> observer in observers.GetInvocationList())
            {
                try
                {
                    observer(value);
                }
                catch (Exception exception)
                {
                    _failures.Add(exception);
                }
            }
        }
    }
}
