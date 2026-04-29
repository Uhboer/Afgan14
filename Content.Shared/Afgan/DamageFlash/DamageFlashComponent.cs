using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Afgan.DamageFlash;

/// <summary>
/// Добавьте этот компонент к существу, чтобы при получении урона:
/// - на экране игрока появлялась мигающая красная вспышка;
/// - персонаж издавал эмоцию "кричит в агонии".
/// В предкритическом состоянии шейдер работает непрерывно.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class DamageFlashComponent : Component
{
    // ── Визуальная вспышка (клиент) ──────────────────────────────────────────

    /// <summary>
    /// Продолжительность одиночной вспышки при получении урона (секунды реального времени).
    /// </summary>
    [DataField]
    public float FlashDuration = 0.8f;

    /// <summary>
    /// Количество пульсаций за время одиночной вспышки.
    /// </summary>
    [DataField]
    public float PulseCount = 2.5f;

    /// <summary>
    /// Порог здоровья (0.0–1.0 от порога крита), при котором включается непрерывное мигание.
    /// Например, 0.9 = шейдер постоянно мигает когда урон >= 90% от порога крита (осталось 10% здоровья до крита).
    /// </summary>
    [DataField]
    public float PreCritThreshold = 0.75f;

    /// <summary>
    /// Скорость пульсации в предкритическом режиме (пульсаций в секунду).
    /// </summary>
    [DataField]
    public float PreCritPulseRate = 1.2f;

    /// <summary>
    /// Максимальная интенсивность в предкритическом режиме (0.0–1.0).
    /// </summary>
    [DataField]
    public float PreCritMaxIntensity = 0.55f;

    // Состояние вспышки — управляется только клиентской системой, не сериализуется в сохранения.
    [NonSerialized]
    public TimeSpan FlashStartTime = TimeSpan.Zero;

    [NonSerialized]
    public bool IsFlashing = false;

    // ── Эмоция крика (сервер) ────────────────────────────────────────────────

    /// <summary>
    /// ID прототипа эмоции, которую персонаж издаёт при получении урона.
    /// По умолчанию — "Agony" (кричит в агонии).
    /// </summary>
    [DataField]
    public string ScreamEmote = "Agony";

    /// <summary>
    /// Кулдаун между криками, чтобы не спамить.
    /// </summary>
    [DataField]
    public TimeSpan ScreamCooldown = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Время последнего крика (симуляционное время, с поддержкой паузы).
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan LastScreamTime = TimeSpan.Zero;
}
