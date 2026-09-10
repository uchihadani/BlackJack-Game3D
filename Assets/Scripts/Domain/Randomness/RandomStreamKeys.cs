namespace TwentyThree.Domain.Randomness
{
    public static class RandomStreamKeys
    {
        public static readonly RandomStreamKey RoundDeck = new RandomStreamKey("ROUND_DECK");
        public static readonly RandomStreamKey CardPerception = new RandomStreamKey("CARD_PERCEPTION");
        public static readonly RandomStreamKey It05 = new RandomStreamKey("IT-05");
        public static readonly RandomStreamKey Sc01Reshuffle = new RandomStreamKey("SC01_RESHUFFLE");
        public static readonly RandomStreamKey Ev01Trigger = new RandomStreamKey("EV01_TRIGGER");
        public static readonly RandomStreamKey Ev01Truth = new RandomStreamKey("EV01_TRUTH");
        public static readonly RandomStreamKey Ev01Signal = new RandomStreamKey("EV01_SIGNAL");
        public static readonly RandomStreamKey Ev02Trigger = new RandomStreamKey("EV02_TRIGGER");
        public static readonly RandomStreamKey Ev02Target = new RandomStreamKey("EV02_TARGET");
        public static readonly RandomStreamKey Ev02Value = new RandomStreamKey("EV02_VALUE");
        public static readonly RandomStreamKey Ev03Trigger = new RandomStreamKey("EV03_TRIGGER");
    }
}
