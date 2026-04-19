using Robust.Shared.GameObjects;

namespace Content.Server.Afgan.Drill;

/// <summary>
/// Компонент для бура, который требует присутствия игрока в радиусе
/// </summary>
[RegisterComponent]
public sealed partial class ProximityDrillComponent : Component
{
    /// <summary>
    /// Радиус в тайлах, в котором должен находиться игрок
    /// </summary>
    [DataField("requiredRange")]
    public float RequiredRange = 5f;

    /// <summary>
    /// Был ли игрок в радиусе при последней проверке
    /// </summary>
    [ViewVariables]
    public bool PlayerInRange = false;
}
