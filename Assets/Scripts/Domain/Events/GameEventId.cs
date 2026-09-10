namespace TwentyThree.Domain.Events
{
    public enum GameEventId
    {
        VoicesFromBeyond = 1,
        Meow = 2,
        Distracted = 3
    }

    public static class GameEventIdExtensions
    {
        public static string ToTechnicalCode(this GameEventId id)
        {
            return id switch
            {
                GameEventId.VoicesFromBeyond => "EV-01",
                GameEventId.Meow => "EV-02",
                GameEventId.Distracted => "EV-03",
                _ => throw new System.ArgumentOutOfRangeException(nameof(id))
            };
        }
    }
}
