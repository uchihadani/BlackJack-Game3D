using System.IO;
using System.Linq;
using NUnit.Framework;

namespace TwentyThree.Tests.EditMode.Foundation
{
    public sealed class NoLegacyInputApiTests
    {
        private static readonly string[] ForbiddenTokens =
        {
            "Input.GetAxis",
            "Input.GetKey",
            "Input.GetMouseButton",
            "Input.mousePosition"
        };

        [Test]
        public void NewRuntimeNeverUsesTheLegacyInputApi()
        {
            string[] runtimeRoots =
            {
                "Assets/Scripts/Domain",
                "Assets/Scripts/Application",
                "Assets/Scripts/Infrastructure",
                "Assets/Scripts/Presentation",
                "Assets/Scripts/Bootstrap"
            };

            foreach (string runtimeRoot in runtimeRoots.Select(Path.GetFullPath))
            {
                foreach (string scriptPath in Directory.GetFiles(runtimeRoot, "*.cs", SearchOption.AllDirectories))
                {
                    string source = File.ReadAllText(scriptPath);
                    foreach (string forbiddenToken in ForbiddenTokens)
                    {
                        Assert.That(
                            source,
                            Does.Not.Contain(forbiddenToken),
                            $"{scriptPath} contains forbidden legacy input token '{forbiddenToken}'.");
                    }
                }
            }
        }
    }
}
