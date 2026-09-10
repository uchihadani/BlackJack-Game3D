using TwentyThree.Domain.SpecialCards;

namespace TwentyThree.Application.Gameplay.Content
{
    internal interface ISpecialCardEffectStrategy
    {
        SpecialCardId Id { get; }

        bool CanResolve(ISpecialCardEffectHost host);

        void Accept(ISpecialCardEffectHost host);

        void Reject(ISpecialCardEffectHost host, bool rejectionCostPaid);
    }
}
