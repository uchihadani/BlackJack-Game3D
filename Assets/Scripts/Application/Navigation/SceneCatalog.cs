using System;

namespace TwentyThree.Application.Navigation
{
    public sealed class SceneCatalog
    {
        public SceneCatalog(string mainMenuScene, string gameRoomScene)
        {
            MainMenuScene = RequireName(mainMenuScene, nameof(mainMenuScene));
            GameRoomScene = RequireName(gameRoomScene, nameof(gameRoomScene));
        }

        public string MainMenuScene { get; }

        public string GameRoomScene { get; }

        public string GetName(SceneId sceneId)
        {
            switch (sceneId)
            {
                case SceneId.MainMenu:
                    return MainMenuScene;
                case SceneId.GameRoom:
                    return GameRoomScene;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sceneId), sceneId, "Unknown scene id.");
            }
        }

        private static string RequireName(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A scene name is required.", parameterName);
            }

            return value;
        }
    }
}
