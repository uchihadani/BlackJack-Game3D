namespace TwentyThree.Application.Settings
{
    public interface IPlayerPreferencesConsumer
    {
        void Configure(IPlayerPreferences preferences);
    }
}
