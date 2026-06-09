using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Afgan.Fermentation;

/// <summary>
/// Прототип рецепта для чана брожения.
/// Определяет: какой тег принимается, какой реагент производится,
/// сколько его производится за цикл, и сколько длится цикл.
/// </summary>
[NetSerializable, Serializable, Prototype("fermentationRecipe")]
public sealed partial class FermentationRecipePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Отображаемое название рецепта (необязательно).
    /// </summary>
    [DataField("name")]
    public string Name = string.Empty;

    /// <summary>
    /// Тег, который должен быть у предмета-сырья (например, "Mushroom").
    /// </summary>
    [DataField("ingredientTag", required: true)]
    public string IngredientTag = string.Empty;

    /// <summary>
    /// ID реагента, который производится (например, "Ethanol").
    /// </summary>
    [DataField("outputReagent", required: true)]
    public string OutputReagent = string.Empty;

    /// <summary>
    /// Сколько единиц реагента производит ОДИН предмет за ОДИН цикл.
    /// </summary>
    [DataField("outputPerIngredient")]
    public float OutputPerIngredient = 10f;

    /// <summary>
    /// Длина одного цикла брожения в секундах.
    /// </summary>
    [DataField("fermentationTime")]
    public float FermentationTime = 60f;

    /// <summary>
    /// Максимум предметов-сырья для этого рецепта в одном чане.
    /// </summary>
    [DataField("maxIngredients")]
    public int MaxIngredients = 10;
}
