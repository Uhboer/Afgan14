using Robust.Shared.GameStates;

namespace Content.Shared.Afgan.HolyStamp;

/// <summary>
/// Компонент благословенной печати ортодоксов.
/// При применении на оружие ближнего боя умножает его урон на DamageMultiplier.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HolyStampComponent : Component
{
    /// <summary>
    /// Множитель урона. 2.0 = +100% урона.
    /// </summary>
    [DataField]
    public float DamageMultiplier = 5.0f;
}
