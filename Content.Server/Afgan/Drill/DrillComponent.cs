using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Afgan.Drill;

/// <summary>
/// Базовый компонент для бура, который генерирует ресурсы
/// </summary>
[RegisterComponent]
public sealed partial class DrillComponent : Component
{
    /// <summary>
    /// Включен ли бур
    /// </summary>
    [DataField("enabled")]
    public bool Enabled = false;

    /// <summary>
    /// Время следующего обновления
    /// </summary>
    [DataField("nextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// Задержка между генерацией ресурсов
    /// </summary>
    [DataField("updateDelay")]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Список ресурсов для генерации: прототип -> количество
    /// </summary>
    [DataField("resources")]
    public Dictionary<string, int> Resources = new()
    {
        { "SteelOre1", 2 },
        { "SpaceQuartz1", 1 },
        { "Coal1", 2 }
    };
}
