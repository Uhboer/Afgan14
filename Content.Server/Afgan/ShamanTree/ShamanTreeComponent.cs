using Robust.Shared.Prototypes;

namespace Content.Server.Afgan.ShamanTree;

/// <summary>
/// Дерево-шаман: периодически озвучивает активный «тип задачи», раздаёт его
/// игрокам при активации (E) и уходит в кд. По возвращении игрока с
/// выполненным objective выдаёт награду в бибках.
/// </summary>
[RegisterComponent, Access(typeof(ShamanTreeSystem))]
public sealed partial class ShamanTreeComponent : Component
{
    [DataField]
    public TimeSpan AnnounceInterval = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(5);

    [DataField]
    public EntProtoId RewardPrototype = "Bib";

    [DataField]
    public EntProtoId ObjectivePrototype = "ShamanDynamicObjective";

    // -- runtime state --

    [ViewVariables]
    public ShamanTaskType? CurrentTask;

    [ViewVariables]
    public TimeSpan NextAnnounceTime;

    [ViewVariables]
    public TimeSpan CooldownEnd;

    /// <summary>
    /// Какому mind мы выдали какой objective. Нужно чтобы по возвращении
    /// проверить прогресс и выдать награду.
    /// </summary>
    [ViewVariables]
    public Dictionary<EntityUid, EntityUid> ActiveAssignments = new();
}
