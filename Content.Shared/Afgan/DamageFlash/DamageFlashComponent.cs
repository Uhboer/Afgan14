using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Afgan.DamageFlash;

/// <summary>
/// Добавьте этот компонент к существу, чтобы при получении урона:
/// - на экране игрока появлялась мигающая красная вспышка;
/// - персонаж издавал эмоцию "кричит в агонии".
/// Шейдер работает всегда при получении урона, интенсивность зависит от процента урона.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class DamageFlashComponent : Component
{
    // ── Визуальная вспышка (клиент) ──────────────────────────────────────────



    /// <summary>
    /// Интенсивность вспышки для 0-25% урона (0.0–1.0).
    /// </summary>
    [DataField]
    public float Intensity0To25 = 0.2f;

    /// <summary>
    /// Интенсивность вспышки для 25-50% урона (0.0–1.0).
    /// </summary>
    [DataField]
    public float Intensity25To50 = 0.4f;

    /// <summary>
    /// Интенсивность вспышки для 50-75% урона (0.0–1.0).
    /// </summary>
    [DataField]
    public float Intensity50To75 = 0.6f;

    /// <summary>
    /// Интенсивность вспышки для 75-100% урона (0.0–1.0).
    /// </summary>
    [DataField]
    public float Intensity75To100 = 0.8f;

    /// <summary>
    /// Интенсивность вспышки при 100%+ урона (0.0–1.0).
    /// При 1.0 весь экран становится красным постоянно.
    /// </summary>
    [DataField]
    public float Intensity100Plus = 1.0f;

    // Параметры пульсации (клиент)
    /// <summary>
    /// Базовая частота пульсации (Гц) для 0-25% урона.
    /// </summary>
    [DataField]
    public float PulseFrequency0To25 = 0.5f;

    /// <summary>
    /// Базовая частота пульсации (Гц) для 25-50% урона.
    /// </summary>
    [DataField]
    public float PulseFrequency25To50 = 1.0f;

    /// <summary>
    /// Базовая частота пульсации (Гц) для 50-75% урона.
    /// </summary>
    [DataField]
    public float PulseFrequency50To75 = 2.0f;

    /// <summary>
    /// Базовая частота пульсации (Гц) для 75-100% урона.
    /// </summary>
    [DataField]
    public float PulseFrequency75To100 = 3.0f;

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
    /// ��ремя последнего крика (симуляционное время, с поддержкой паузы).
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan LastScreamTime = TimeSpan.Zero;
}
