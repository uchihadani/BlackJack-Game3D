using System;
using System.Threading;
using TwentyThree.Application.Gameplay;

namespace TwentyThree.Infrastructure.Random
{
    public sealed class SystemRunSeedProvider : IRunSeedProvider
    {
        private int _current = Environment.TickCount;

        public int NextSeed()
        {
            return Interlocked.Increment(ref _current);
        }
    }
}
