using Robust.Shared.Audio;
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

    // ── Аудио ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Звук при заводке (циклично). Папка намеренно "Structues" — опечатка в проекте.
    /// </summary>
    [DataField("startupSound")]
    public SoundSpecifier StartupSound = new SoundPathSpecifier("/Audio/Afgan/Structues/buron.ogg");

    /// <summary>
    /// Звук при работе (циклично).
    /// </summary>
    [DataField("workingSound")]
    public SoundSpecifier WorkingSound = new SoundPathSpecifier("/Audio/Afgan/Structues/bur.ogg");

    /// <summary>
    /// Длина одного цикла buron.mp3 в секундах — нужна для таймера джиттера
    /// </summary>
    [DataField("startupLoopDuration")]
    public float StartupLoopDuration = 1.254f;

    /// <summary>
    /// Длительность джиттера в начале каждого цикла заводки (секунды)
    /// </summary>
    [DataField("startupJitterDuration")]
    public float StartupJitterDuration = 0.5f;

    // ── Runtime-поля (не сериализуются) ───────────────────────────────────

    /// <summary>
    /// Идёт ли сейчас заводка (DoAfter запущен)
    /// </summary>
    public bool IsStartingUp = false;

    /// <summary>
    /// Текущий аудиопоток (заводка или работа)
    /// </summary>
    public EntityUid? AudioStream;

    /// <summary>
    /// Время следующего цикла джиттера при заводке
    /// </summary>
    public TimeSpan NextJitterCycle = TimeSpan.Zero;

    /// <summary>
    /// Время окончания текущего джиттера
    /// </summary>
    public TimeSpan JitterEndTime = TimeSpan.Zero;
}
