using TwentyThree.Presentation.Player;
using UnityEngine;

namespace TwentyThree.Presentation.Interaction
{
    public readonly struct InteractionContext
    {
        public InteractionContext(GameObject actor, IPlayerModeController playerModes)
        {
            Actor = actor;
            PlayerModes = playerModes;
        }

        public GameObject Actor { get; }

        public IPlayerModeController PlayerModes { get; }
    }
}
