using UnityEngine;

namespace TwentyThree.Presentation.Input
{
    public sealed class CursorStateController : MonoBehaviour
    {
        public void SetExplorationMode()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void SetPointerMode()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
