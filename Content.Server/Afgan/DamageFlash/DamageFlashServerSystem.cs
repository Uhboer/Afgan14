using Content.Server.Chat.Systems;
using Content.Shared._Afgan.DamageFlash;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Afgan.DamageFlash;

/// <summary>
/// Серверная часть системы вспышки урона.
/// Отвечает за воспроизведение эмоции "кричит в агонии" при получении урона.
/// </summary>
public sealed class DamageFlashServerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageFlashComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(EntityUid uid, DamageFlashComponent component, DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        // Проверяем кулдаун эмоции
        if (component.LastScreamTime + component.ScreamCooldown > _gameTiming.CurTime)
            return;

        // Не проигрываем эмоцию если существо уже мертво или в критическом состоянии
        if (_mobStateSystem.IsDead(uid) || _mobStateSystem.IsCritical(uid))
            return;

        // Воспроизводим эмоцию крика в агонии со звуком, анимацией и текстом в чате
        _chatSystem.TryEmoteWithChat(uid, component.ScreamEmote, ignoreActionBlocker: true, forceEmote: true);

        component.LastScreamTime = _gameTiming.CurTime;
    }
}
