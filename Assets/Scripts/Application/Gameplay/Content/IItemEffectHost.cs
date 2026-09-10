namespace TwentyThree.Application.Gameplay.Content
{
    internal interface IItemEffectHost
    {
        GamePhase Phase { get; }

        bool IsPreparationWindowOpen { get; }

        bool ItemsBlockedForCurrentRound { get; }

        bool DealerHoleCardRevealed { get; }

        bool BlackoutActive { get; }

        int Pressure { get; }

        void RevealDealerHoleCardWithMagnifyingGlass();

        void ApplyCigaretteBox();
    }
}
