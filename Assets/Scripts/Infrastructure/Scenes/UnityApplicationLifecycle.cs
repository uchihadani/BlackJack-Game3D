using TwentyThree.Application.Navigation;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TwentyThree.Infrastructure.Scenes
{
    public sealed class UnityApplicationLifecycle : IApplicationLifecycle
    {
        public void Quit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}
