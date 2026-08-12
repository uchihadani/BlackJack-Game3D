using System;
using System.Linq;
using TwentyThree.Bootstrap;
using TwentyThree.Infrastructure.Configuration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwentyThree.Editor
{
    public static class PhaseTwoConfigurationBuilder
    {
        public const string GameRulesConfigurationPath =
            "Assets/GameContent/Configuration/GameRulesConfiguration.asset";

        [MenuItem("23 al Filo/Phase 2/Configure Rules")]
        public static void ConfigureFromMenu()
        {
            SceneSetup[] previousSceneSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Configure();
                EditorUtility.DisplayDialog(
                    "23 al Filo",
                    "Phase 2 rules configuration validated and connected.",
                    "OK");
            }
            finally
            {
                if (previousSceneSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSceneSetup);
                }
            }
        }

        public static void Configure()
        {
            GameRulesConfiguration configuration =
                AssetDatabase.LoadAssetAtPath<GameRulesConfiguration>(GameRulesConfigurationPath);
            if (configuration == null)
            {
                configuration = ScriptableObject.CreateInstance<GameRulesConfiguration>();
                AssetDatabase.CreateAsset(configuration, GameRulesConfigurationPath);
            }

            configuration.CreateRules();

            Scene scene = EditorSceneManager.OpenScene(
                PhaseOneProjectBuilder.BootstrapScenePath,
                OpenSceneMode.Single);
            ApplicationBootstrap bootstrap = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ApplicationBootstrap>(true))
                .Single();
            bootstrap.SetGameRulesConfiguration(configuration);
            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(scene, PhaseOneProjectBuilder.BootstrapScenePath))
            {
                throw new InvalidOperationException("Bootstrap scene could not be saved.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
