using Robust.Shared.GameStates;

namespace Content.Shared.Afgan.HolyStamp;

/// <summary>
/// Маркерный компонент. Добавляется оружию после наложения благословенной печати.
/// Предотвращает повторное применение печати на то же оружие.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BlessedMeleeComponent : Component
{
}
