using Robust.Shared.GameObjects;

namespace Content.Server.Afgan.Bura;

[RegisterComponent]
public sealed partial class BuraComponent : Component
{
    /// <summary>
    /// Скорость охлаждения в кельвинах за тик
    /// </summary>
    [DataField("coolingRate")]
    public float CoolingRate = 10f;

    /// <summary>
    /// Минимальная температура до которой можно охладить
    /// </summary>
    [DataField("minTemperature")]
    public float MinTemperature = 273.15f; // 0 градусов Цельсия
}
