namespace TwentyThree.Application.Gameplay
{
    public readonly struct VoicesMessage
    {
        public VoicesMessage(bool dealerHasTwentyThreeMessage, bool suspicionSignal)
        {
            DealerHasTwentyThreeMessage = dealerHasTwentyThreeMessage;
            SuspicionSignal = suspicionSignal;
        }

        public bool DealerHasTwentyThreeMessage { get; }

        public bool SuspicionSignal { get; }
    }
}
