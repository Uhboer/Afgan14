using Robust.Shared.Prototypes;

namespace Content.Server.Afgan.ShamanTree;

public enum ShamanTaskType : byte
{
    Kill,
    MassKill,
    Offer,
}

/// <summary>
/// Условие шаманской задачи: вешается на динамически созданный objective
/// и хранит весь контекст задачи (тип, цели, прогресс, награду).
/// Прогресс считается <see cref="ShamanTaskConditionSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(ShamanTaskConditionSystem), typeof(ShamanTreeSystem))]
public sealed partial class ShamanTaskConditionComponent : Component
{
    [DataField]
    public ShamanTaskType TaskType;

    /// <summary>
    /// Mind дерева-выдавшего (нужно, чтобы прогресс-таски находили правильный uid дерева).
    /// </summary>
    [DataField]
    public EntityUid? IssuingTree;

    /// <summary>
    /// Список mind-uid целей для Kill/MassKill.
    /// </summary>
    [DataField]
    public List<EntityUid> TargetMinds = new();

    /// <summary>
    /// Для Offer: разрешённые prototype-id предметов.
    /// </summary>
    [DataField]
    public List<string> OfferPrototypes = new();

    /// <summary>
    /// Для Offer: разрешённые теги предметов (любая материя и т.п.).
    /// </summary>
    [DataField]
    public List<string> OfferTags = new();

    /// <summary>
    /// Для Offer: человекочитаемое название цели (например, «материя»).
    /// </summary>
    [DataField]
    public string Display = string.Empty;

    /// <summary>
    /// Для Offer: 0 или 1.
    /// </summary>
    [DataField]
    public int Progress;

    /// <summary>
    /// Награда в бибках (SpaceCash) за выполнение.
    /// </summary>
    [DataField]
    public int Reward;
}
