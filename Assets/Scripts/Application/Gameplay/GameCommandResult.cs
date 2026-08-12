namespace TwentyThree.Application.Gameplay
{
    public readonly struct GameCommandResult
    {
        private GameCommandResult(bool succeeded, GameCommandFailure failure)
        {
            Succeeded = succeeded;
            Failure = failure;
        }

        public bool Succeeded { get; }

        public GameCommandFailure Failure { get; }

        public static GameCommandResult Success()
        {
            return new GameCommandResult(true, GameCommandFailure.None);
        }

        public static GameCommandResult Reject(GameCommandFailure failure)
        {
            return new GameCommandResult(false, failure);
        }
    }
}
