using System.IO;
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
            string runtimeRoot = Path.GetFullPath("Assets/_Project/Runtime");
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
