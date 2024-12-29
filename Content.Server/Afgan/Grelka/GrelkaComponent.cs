using Robust.Shared.GameObjects;

namespace Content.Server.Afgan.Grelka;

[RegisterComponent]
public sealed partial class GrelkaComponent : Component
{
    /// <summary>
    /// Скорость нагрева в кельвинах за тик
    /// </summary>
    [DataField("heatingRate")]
    public float HeatingRate = 50f;

    /// <summary>
    /// Максимальная температура до которой можно нагреть
    /// </summary>
    [DataField("maxTemperature")]
    public float MaxTemperature = 373.15f; // 100 градусов Цельсия
}
