namespace TwentyThree.Presentation.Interaction
{
    public interface IInteractable
    {
        bool CanInteract(InteractionContext context);

        string GetPrompt(InteractionContext context);

        void Interact(InteractionContext context);
    }
}
