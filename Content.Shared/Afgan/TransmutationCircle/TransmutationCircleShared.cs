using Robust.Shared.Serialization;

namespace Content.Shared._AFGAN.TransmutationCircle;

[Serializable, NetSerializable]
public enum TransmutationCircleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class TransmutationCircleSelectMessage : BoundUserInterfaceMessage
{
    public string RecipeId;
    public TransmutationCircleSelectMessage(string recipeId) => RecipeId = recipeId;
}

[Serializable, NetSerializable]
public sealed class TransmutationCircleState : BoundUserInterfaceState
{
    /// <summary>
    /// Список рецептов которые можно скрафтить прямо сейчас (материалы на круге есть)
    /// </summary>
    public List<string> AvailableRecipes;

    /// <summary>
    /// Все рецепты — чтобы показать список даже если материалов не хватает
    /// </summary>
    public List<string> AllRecipes;

    public TransmutationCircleState(List<string> available, List<string> all)
    {
        AvailableRecipes = available;
        AllRecipes = all;
    }
}
