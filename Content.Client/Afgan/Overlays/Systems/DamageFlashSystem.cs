using Content.Shared._Afgan.DamageFlash;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._Afgan.Overlays.Systems;

/// <summary>
/// Управляет полноэкранной красной вспышкой при получении урона.
/// Шейдер работает ПОСТОЯННО при наличии урона, мигает с частотой и интенсивностью,
/// зависящей от процента урона, разбитого на 5 диапазонов:
/// 1. 0-25% - легкая медленная вспышка
/// 2. 25-50% - средняя вспышка
/// 3. 50-75% - сильная быстрая вспышка
/// 4. 75-100% - очень сильная очень быстрая вспышка
/// 5. 100%+ - весь экран красный постоянно
/// </summary>
public sealed class DamageFlashSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;

    private DamageFlashOverlay _overlay = default!;
    private TimeSpan _lastPulseTime = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new DamageFlashOverlay();

        SubscribeLocalEvent<DamageFlashComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<DamageFlashComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<DamageFlashComponent, ComponentShutdown>(OnShutdown);

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _overlayMan.RemoveOverlay(_overlay);
        _overlay.Intensity = 0f;
        _lastPulseTime = TimeSpan.Zero;
    }

    private void OnPlayerAttached(EntityUid uid, DamageFlashComponent component, PlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
        _overlay.Intensity = 0f;
        _lastPulseTime = _timing.RealTime;
    }

    private void OnPlayerDetached(EntityUid uid, DamageFlashComponent component, PlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
        _overlay.Intensity = 0f;
        _lastPulseTime = TimeSpan.Zero;
    }

    private void OnShutdown(EntityUid uid, DamageFlashComponent component, ComponentShutdown args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        _overlayMan.RemoveOverlay(_overlay);
        _overlay.Intensity = 0f;
        _lastPulseTime = TimeSpan.Zero;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_playerMan.LocalEntity is not { Valid: true } localEntity)
        {
            _overlay.Intensity = 0f;
            return;
        }

        if (!TryComp<DamageFlashComponent>(localEntity, out var component))
        {
            _overlay.Intensity = 0f;
            return;
        }

        // Получаем процент урона
        float damagePercentage = GetDamagePercentage(localEntity);
        
        // Если урон 0%, выключаем вспышку
        if (damagePercentage <= 0f)
        {
            _overlay.Intensity = 0f;
            return;
        }

        // Если урон 100%+ (>= 1.0), используем максимальную интенсивность постоянно
        if (damagePercentage >= 1.0f)
        {
            _overlay.Intensity = component.Intensity100Plus;
            return;
        }

        // Не показываем вспышку если игрок мертв или в критическом состоянии
        // (только для урона меньше 100%)
        if (TryComp<MobStateComponent>(localEntity, out var mobState))
        {
            var mobStateSystem = EntityManager.System<MobStateSystem>();
            if (mobStateSystem.IsDead(localEntity, mobState) || mobStateSystem.IsCritical(localEntity, mobState))
            {
                _overlay.Intensity = 0f;
                return;
            }
        }

        // Получаем базовую интенсивность для диапазона урона
        float baseIntensity = GetIntensityForDamagePercentage(damagePercentage, component);
        
        // Получаем частоту пульсации для диапазона урона
        float pulseFrequency = GetPulseFrequencyForDamagePercentage(damagePercentage, component);
        
        // Рассчитываем пульсирующую интенсивность
        float pulseIntensity = CalculatePulsingIntensity(baseIntensity, pulseFrequency);
        
        _overlay.Intensity = pulseIntensity;
    }

    /// <summary>
    /// Возвращает интенсивность вспышки в зависимости от процента урона.
    /// </summary>
    private float GetIntensityForDamagePercentage(float damagePercentage, DamageFlashComponent component)
    {
        if (damagePercentage < 0.25f) // 0-25%
            return component.Intensity0To25;
        else if (damagePercentage < 0.5f) // 25-50%
            return component.Intensity25To50;
        else if (damagePercentage < 0.75f) // 50-75%
            return component.Intensity50To75;
        else // 75-100%
            return component.Intensity75To100;
    }

    /// <summary>
    /// Возвращает частоту пульсации (в Гц) в зависимости от процента урона.
    /// Чем больше урон - тем быстрее мигает экран.
    /// </summary>
    private float GetPulseFrequencyForDamagePercentage(float damagePercentage, DamageFlashComponent component)
    {
        if (damagePercentage < 0.25f) // 0-25% - медленное мигание
            return component.PulseFrequency0To25;
        else if (damagePercentage < 0.5f) // 25-50% - среднее мигание
            return component.PulseFrequency25To50;
        else if (damagePercentage < 0.75f) // 50-75% - быстрое мигание
            return component.PulseFrequency50To75;
        else // 75-100% - очень быстрое мигание
            return component.PulseFrequency75To100;
    }

    /// <summary>
    /// Рассчитывает пульсирующую интенсивность на основе базовой интенсивности и частоты.
    /// Использует синусоидальную функцию для плавного мигания.
    /// </summary>
    private float CalculatePulsingIntensity(float baseIntensity, float frequency)
    {
        // Время с начала игры в секундах
        float time = (float)_timing.RealTime.TotalSeconds;
        
        // Синусоидальная пульсация: от 0 до 1 и обратно
        float pulse = (MathF.Sin(time * 2f * MathF.PI * frequency) + 1f) * 0.5f;
        
        // Применяем базовую интенсивность к пульсации
        // Минимальная интенсивность = 20% от базовой, максимальная = базовая
        float minIntensity = baseIntensity * 0.2f;
        float intensityRange = baseIntensity - minIntensity;
        
        return minIntensity + pulse * intensityRange;
    }

    /// <summary>
    /// Возвращает процент нанесенного урона от максимального здоровья (0.0 - 1.0+).
    /// Может быть больше 1.0 если урон превышает порог крита.
    /// </summary>
    private float GetDamagePercentage(EntityUid uid)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return 0f;

        // Получаем порог крита (урон, при котором персонаж становится критическим)
        if (!_thresholds.TryGetThresholdForState(uid, MobState.Critical, out var critThreshold))
        {
            // Если не можем получить порог крита, используем фиксированное значение 25
            // для отладки (предполагаем, что порог крита для человека = 25)
            float fixedCritThreshold = 25f;
            return damageable.TotalDamage.Float() / fixedCritThreshold;
        }

        // Если порог крита не найден или равен 0, возвращаем 0
        if (critThreshold == null || critThreshold.Value <= 0)
            return 0f;

        // Рассчитываем процент урона от порога крита
        // Может быть больше 1.0 (100%) если урон превышает порог крита
        return damageable.TotalDamage.Float() / critThreshold.Value.Float();
    }
}
