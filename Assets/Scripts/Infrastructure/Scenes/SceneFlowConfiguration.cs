using TwentyThree.Application.Navigation;
using UnityEngine;

namespace TwentyThree.Infrastructure.Scenes
{
    [CreateAssetMenu(
        fileName = "SceneFlowConfiguration",
        menuName = "23 al Filo/Configuration/Scene Flow")]
    public sealed class SceneFlowConfiguration : ScriptableObject
    {
        [SerializeField] private string mainMenuScene = "01_MainMenu";
        [SerializeField] private string gameRoomScene = "10_GameRoom";

        public SceneCatalog CreateCatalog()
        {
            return new SceneCatalog(mainMenuScene, gameRoomScene);
        }
    }
}
