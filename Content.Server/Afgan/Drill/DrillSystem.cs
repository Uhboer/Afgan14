using Content.Server.Popups;
using Content.Shared.Afgan.Drill;
using Content.Shared.Jittering;
using Robust.Shared.Player;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Player;

namespace Content.Server.Afgan.Drill;

/// <summary>
/// Система управления бурами
/// </summary>
public sealed class DrillSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrillComponent, ComponentInit>(OnDrillInit);
        SubscribeLocalEvent<DrillComponent, ComponentShutdown>(OnDrillShutdown);
        SubscribeLocalEvent<DrillComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<DrillComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DrillComponent, DrillStartupDoAfterEvent>(OnStartupComplete);
    }

    private void OnDrillInit(EntityUid uid, DrillComponent component, ComponentInit args)
    {
        component.NextUpdate = _timing.CurTime + component.UpdateDelay;
    }

    private void OnDrillShutdown(EntityUid uid, DrillComponent component, ComponentShutdown args)
    {
        StopAllAudio(uid, component);
        RemCompDeferred<JitteringComponent>(uid);
    }

    private void OnExamined(EntityUid uid, DrillComponent component, ExaminedEvent args)
    {
        if (component.Enabled)
            args.PushMarkup(Loc.GetString("drill-examined-on"));
        else
            args.PushMarkup(Loc.GetString("drill-examined-off"));
    }

    private void OnGetVerbs(EntityUid uid, DrillComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!component.Enabled && !component.IsStartingUp)
        {
            AlternativeVerb verbOn = new()
            {
                Act = () =>
                {
                    var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(10),
                        new DrillStartupDoAfterEvent(), uid, target: uid)
                    {
                        BreakOnDamage = true,
                        BreakOnMove = true,
                        NeedHand = false
                    };

                    if (_doAfter.TryStartDoAfter(doAfterArgs))
                    {
                        component.IsStartingUp = true;
                        BeginStartupAudio(uid, component);
                    }

                    _popup.PopupEntity(Loc.GetString("drill-starting"), uid, args.User);
                },
                Text = Loc.GetString("drill-turn-on"),
                Priority = 1
            };
            args.Verbs.Add(verbOn);
        }
        else if (component.Enabled || component.IsStartingUp)
        {
            AlternativeVerb verbOff = new()
            {
                Act = () =>
                {
                    TurnOff(uid, component);
                    _popup.PopupEntity(Loc.GetString("drill-turned-off"), uid, args.User);
                },
                Text = Loc.GetString("drill-turn-off"),
                Priority = 1
            };
            args.Verbs.Add(verbOff);
        }
    }

    private void OnStartupComplete(EntityUid uid, DrillComponent component, DrillStartupDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
        {
            // DoAfter отменён — останавливаем заводку
            if (args.Cancelled)
                TurnOff(uid, component);
            return;
        }

        component.IsStartingUp = false;
        component.Enabled = true;
        component.NextUpdate = _timing.CurTime + component.UpdateDelay;

        // Переключаем звук: buron → bur
        StopAllAudio(uid, component);
        RemComp<JitteringComponent>(uid);
        component.AudioStream = _audio.PlayPvs(component.WorkingSound, uid,
            AudioParams.Default.WithLoop(true).WithMaxDistance(7f)).Value.Entity;

        // Постоянная лёгкая тряска во время работы
        _jitter.AddJitter(uid, -10, 40);

        _popup.PopupEntity(Loc.GetString("drill-turned-on"), uid, args.User);

        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<DrillComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var drill, out var xform))
        {
            // ── Обработка цикла заводки (джиттер) ──────────────────────────
            if (drill.IsStartingUp)
            {
                // Конец джиттера — убираем
                if (HasComp<JitteringComponent>(uid) && curTime >= drill.JitterEndTime)
                    RemComp<JitteringComponent>(uid);

                // Начало нового цикла — добавляем джиттер
                if (curTime >= drill.NextJitterCycle)
                {
                    _jitter.AddJitter(uid, -5, 30);
                    drill.JitterEndTime = drill.NextJitterCycle + TimeSpan.FromSeconds(drill.StartupJitterDuration);
                    // Следующий цикл считаем от предыдущего NextJitterCycle, не от curTime —
                    // иначе накапливается дрейф при пропуске тиков.
                    drill.NextJitterCycle += TimeSpan.FromSeconds(drill.StartupLoopDuration);
                }

                continue; // пока заводится — ресурсы не генерируем
            }

            if (!drill.Enabled)
                continue;

            if (curTime < drill.NextUpdate)
                continue;

            if (!CanStoreResources(uid, drill))
                continue;

            // Проверка близости игрока для ProximityDrill
            if (TryComp<ProximityDrillComponent>(uid, out var proximityDrill))
            {
                var playerInRange = CheckPlayerInRange(uid, xform, proximityDrill.RequiredRange);

                if (!playerInRange)
                {
                    if (proximityDrill.PlayerInRange)
                    {
                        proximityDrill.PlayerInRange = false;
                        TurnOff(uid, drill);
                    }
                    continue;
                }

                proximityDrill.PlayerInRange = true;
            }

            GenerateResources(uid, drill, xform);
            drill.NextUpdate = curTime + drill.UpdateDelay;
        }
    }

    // ── Вспомогательные методы ─────────────────────────────────────────────

    private void BeginStartupAudio(EntityUid uid, DrillComponent component)
    {
        StopAllAudio(uid, component);
        component.AudioStream = _audio.PlayPvs(component.StartupSound, uid,
            AudioParams.Default.WithLoop(true).WithMaxDistance(7f)).Value.Entity;

        // Первый джиттер — сразу, без задержки (звук тоже стартует сейчас).
        // NextJitterCycle выставляем точно через длину цикла от текущего момента,
        // чтобы следующие джиттеры не накапливали дрейф.
        var curTime = _timing.CurTime;
        var loopSpan = TimeSpan.FromSeconds(component.StartupLoopDuration);
        _jitter.AddJitter(uid, -5, 30);
        component.JitterEndTime = curTime + TimeSpan.FromSeconds(component.StartupJitterDuration);
        component.NextJitterCycle = curTime + loopSpan;
    }

    private void TurnOff(EntityUid uid, DrillComponent component)
    {
        component.Enabled = false;
        component.IsStartingUp = false;
        StopAllAudio(uid, component);
        RemComp<JitteringComponent>(uid);
    }

    private void StopAllAudio(EntityUid uid, DrillComponent component)
    {
        component.AudioStream = _audio.Stop(component.AudioStream);
    }

    private bool CheckPlayerInRange(EntityUid uid, TransformComponent xform, float range)
    {
        var coords = _transform.GetMapCoordinates(uid, xform);
        var entities = _lookup.GetEntitiesInRange(coords, range);

        foreach (var entity in entities)
        {
            if (HasComp<ActorComponent>(entity))
                return true;
        }

        return false;
    }

    private bool CanStoreResources(EntityUid uid, DrillComponent drill)
    {
        if (!TryComp<StorageComponent>(uid, out var storage))
            return false;

        var totalItemsToAdd = 0;
        foreach (var (_, count) in drill.Resources)
            totalItemsToAdd += count;

        var currentItems = storage.Container.ContainedEntities.Count;
        const int maxSlots = 72;

        return (currentItems + totalItemsToAdd) <= maxSlots;
    }

    private void GenerateResources(EntityUid uid, DrillComponent drill, TransformComponent xform)
    {
        if (!TryComp<StorageComponent>(uid, out var storage))
            return;

        var coords = xform.Coordinates;

        foreach (var (prototype, count) in drill.Resources)
        {
            for (int i = 0; i < count; i++)
            {
                var item = Spawn(prototype, coords);

                // Пытаемся вставить в хранилище
                if (!_storage.Insert(uid, item, out _, storageComp: storage, playSound: false))
                    Del(item);
            }
        }
    }
}
