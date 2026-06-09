using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Afgan.Fermentation;

/// <summary>
/// Компонент чана брожения. Поддерживает несколько рецептов из прототипов
/// <see cref="FermentationRecipePrototype"/>.
/// </summary>
[RegisterComponent]
public sealed partial class FermentationVatComponent : Component
{
    /// <summary>
    /// Список рецептов, которые этот чан умеет обрабатывать.
    /// Каждый рецепт — это ProtoId, указывающий на FermentationRecipePrototype.
    /// </summary>
    [DataField("recipes")]
    public List<ProtoId<FermentationRecipePrototype>> Recipes = new();

    /// <summary>
    /// Название раствора внутри чана (должно совпадать с SolutionContainerManager).
    /// </summary>
    [DataField("solutionName")]
    public string SolutionName = "vat";

    // ─── Рантайм-состояние (не сериализуется) ───────────────────────────────

    /// <summary>
    /// Текущий активный рецепт (null — ничего не бродит).
    /// Устанавливается при загрузке первого предмета-ингредиента.
    /// </summary>
    public FermentationRecipePrototype? ActiveRecipe = null;

    /// <summary>
    /// Количество загруженных ингредиентов текущего рецепта.
    /// </summary>
    public int IngredientsLoaded = 0;

    /// <summary>
    /// Накопленное время брожения (секунды).
    /// </summary>
    public float AccumulatedTime = 0f;
}
