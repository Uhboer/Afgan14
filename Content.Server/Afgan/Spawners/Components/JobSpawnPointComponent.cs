using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Afgan.Spawners.Components;

/// <summary>
/// Спавн-поинт, который используется при лейтджойне для определённых джобов.
/// Если у игрока джоб из списка <see cref="Jobs"/>, он будет заспавнен здесь
/// вместо обычного LateJoin спавн-поинта.
/// </summary>
[RegisterComponent]
public sealed partial class JobSpawnPointComponent : Component
{
    /// <summary>
    /// Список джобов, которые могут спавниться на этом поинте.
    /// Если список пустой — поинт доступен для всех джобов.
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> Jobs { get; set; } = new();
}
