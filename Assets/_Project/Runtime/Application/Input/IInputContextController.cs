using System;

namespace TwentyThree.Application.Input
{
    public interface IInputContextController
    {
        ControlContext CurrentContext { get; }

        event Action<ControlContext> ContextChanged;

        void SetContext(ControlContext context);
    }
}
