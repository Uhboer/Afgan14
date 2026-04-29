using Content.Shared._Afgan.DamageFlash;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._Afgan.Overlays.Systems;

/// <summary>
/// Управляет полноэкранной красной вспышкой при получении урона.
/// Режимы:
///   1. Одиночная вспышка — при каждом получении урона.
///   2. Непрерывное мигание — когда здоровье ниже PreCritThreshold от порога крита.
/// </summary>
public sealed class DamageFlashSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;

    private DamageFlashOverlay _overlay = default!;

    /// <summary>
    /// Флаг: игнорировать следующий DamageChangedEvent после прикрепления игрока.
    /// Нужен чтобы не срабатывать на первую синхронизацию состояния с сервера.
    /// </summary>
    private bool _ignoreNextDamageEvent = false;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new DamageFlashOverlay();

        SubscribeLocalEvent<DamageFlashComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<DamageFlashComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<DamageFlashComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DamageFlashComponent, DamageChangedEvent>(OnDamageChanged);

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttached(EntityUid uid, DamageFlashComponent component, PlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
        // Сбрасываем состояние вспышки при подключении к новому персонажу
        component.IsFlashing = false;
        component.FlashStartTime = TimeSpan.Zero;
        // Первый DamageChangedEvent после прикрепления — это синхронизация начального
        // состояния с сервера, а не реальный урон. Игнорируем его.
        _ignoreNextDamageEvent = true;
    }

    private void OnPlayerDetached(EntityUid uid, DamageFlashComponent component, PlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
        _overlay.Intensity = 0f;
        _ignoreNextDamageEvent = false;
    }

    private void OnShutdown(EntityUid uid, DamageFlashComponent component, ComponentShutdown args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        _overlayMan.RemoveOverlay(_overlay);
        _overlay.Intensity = 0f;
        component.IsFlashing = false;
        component.FlashStartTime = TimeSpan.Zero;
    }

    private void OnDamageChanged(EntityUid uid, DamageFlashComponent component, DamageChangedEvent args)
    {
        // Реагируем только на локального игрока
        if (_playerMan.LocalEntity is not { Valid: true } localEntity || uid != localEntity)
            return;

        // Пропускаем первое событие после прикрепления — это начальная синхронизация
        // состояния с сервера, а не реальный урон в игре.
        if (_ignoreNextDamageEvent)
        {
            _ignoreNextDamageEvent = false;
            return;
        }

        // Только реальное увеличение урона (не инициализация)
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        // Игнорируем системные изменения урона (синхронизация, инициализация)
        // Origin == null означает что урон изменен системой, а не другим существом/оружием
        if (args.Origin == null)
            return;

        // Запускаем одиночную вспышку
        component.FlashStartTime = _timing.RealTime;
        component.IsFlashing = true;
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

        // Проверяем предкритическое состояние
        if (IsPreCrit(localEntity, component))
        {
            // Непрерывное синусоидальное мигание
            var time = (float)_timing.RealTime.TotalSeconds;
            var pulse = (MathF.Sin(time * MathF.PI * 2f * component.PreCritPulseRate) + 1f) * 0.5f;
            _overlay.Intensity = pulse * component.PreCritMaxIntensity;

            // Сбрасываем одиночную вспышку — предкрит имеет приоритет
            component.IsFlashing = false;
            return;
        }

        // Одиночная вспышка при получении урона
        if (!component.IsFlashing)
        {
            _overlay.Intensity = 0f;
            return;
        }

        var elapsed = (float)(_timing.RealTime - component.FlashStartTime).TotalSeconds;
        var progress = elapsed / component.FlashDuration;

        if (progress >= 1f)
        {
            component.IsFlashing = false;
            _overlay.Intensity = 0f;
            return;
        }

        // Синусоидальное мигание с затуханием
        var singlePulse = MathF.Sin(progress * MathF.PI * component.PulseCount * 2f);
        var fade = 1f - progress;
        _overlay.Intensity = MathF.Max(0f, singlePulse * fade);
    }

    /// <summary>
    /// Возвращает true, если текущий урон игрока превышает PreCritThreshold от порога крита.
    /// </summary>
    private bool IsPreCrit(EntityUid uid, DamageFlashComponent component)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return false;

        if (!_thresholds.TryGetIncapPercentage(uid, damageable.TotalDamage, out var percentage))
            return false;

        return (float) percentage.Value >= component.PreCritThreshold;
    }
}
