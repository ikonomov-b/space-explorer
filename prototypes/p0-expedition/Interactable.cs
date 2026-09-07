namespace P0Expedition;

/// <summary>Something the player can approach and press E on. See decision 0015.</summary>
public interface IInteractable
{
    string PromptText { get; }

    void Activate();
}
