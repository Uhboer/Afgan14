using Content.Server.Popups;
using Content.Shared.Afgan.Drill;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrillComponent, ComponentInit>(OnDrillInit);
        SubscribeLocalEvent<DrillComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<DrillComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DrillComponent, DrillStartupDoAfterEvent>(OnStartupComplete);
    }

    private void OnDrillInit(EntityUid uid, DrillComponent component, ComponentInit args)
    {
        component.NextUpdate = _timing.CurTime + component.UpdateDelay;
    }

    private void OnExamined(EntityUid uid, DrillComponent component, ExaminedEvent args)
    {
        // Показываем только статус работы
        if (component.Enabled)
        {
            args.PushMarkup(Loc.GetString("drill-examined-on"));
        }
        else
        {
            args.PushMarkup(Loc.GetString("drill-examined-off"));
        }
    }

    private void OnGetVerbs(EntityUid uid, DrillComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        // Verb для включения
        if (!component.Enabled)
        {
            AlternativeVerb verbOn = new()
            {
                Act = () =>
                {
                    // Запускаем DoAfter на 10 секунд
                    var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(10), new DrillStartupDoAfterEvent(), uid, target: uid)
                    {
                        BreakOnDamage = true,
                        BreakOnMove = true,
                        NeedHand = false
                    };

                    _doAfter.TryStartDoAfter(doAfterArgs);
                    _popup.PopupEntity(Loc.GetString("drill-starting"), uid, args.User);
                },
                Text = Loc.GetString("drill-turn-on"),
                Priority = 1
            };
            args.Verbs.Add(verbOn);
        }
        // Verb для выключения (моментально)
        else
        {
            AlternativeVerb verbOff = new()
            {
                Act = () =>
                {
                    component.Enabled = false;
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
            return;

        component.Enabled = true;
        component.NextUpdate = _timing.CurTime + component.UpdateDelay;
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
            if (!drill.Enabled)
                continue;

            if (curTime < drill.NextUpdate)
                continue;

            // Проверяем, есть ли место в хранилище
            if (!CanStoreResources(uid, drill))
            {
                // Хранилище заполнено, останавливаем генерацию
                continue;
            }

            // Проверка для бура с требованием близости игрока
            if (TryComp<ProximityDrillComponent>(uid, out var proximityDrill))
            {
                var playerInRange = CheckPlayerInRange(uid, xform, proximityDrill.RequiredRange);

                if (!playerInRange)
                {
                    if (proximityDrill.PlayerInRange)
                    {
                        // Игрок только что вышел из радиуса
                        drill.Enabled = false;
                        proximityDrill.PlayerInRange = false;
                    }
                    continue;
                }

                proximityDrill.PlayerInRange = true;
            }

            // Генерация ресурсов
            GenerateResources(uid, drill, xform);
            drill.NextUpdate = curTime + drill.UpdateDelay;
        }
    }

    private bool CheckPlayerInRange(EntityUid uid, TransformComponent xform, float range)
    {
        var coords = _transform.GetMapCoordinates(uid, xform);
        var entities = _lookup.GetEntitiesInRange(coords, range);

        foreach (var entity in entities)
        {
            // Проверяем, является ли энтити игроком (имеет ActorComponent)
            if (HasComp<ActorComponent>(entity))
                return true;
        }

        return false;
    }

    private bool CanStoreResources(EntityUid uid, DrillComponent drill)
    {
        if (!TryComp<StorageComponent>(uid, out var storage))
            return false;

        // Подсчитываем сколько предметов нужно добавить
        var totalItemsToAdd = 0;
        foreach (var (_, count) in drill.Resources)
        {
            totalItemsToAdd += count;
        }

        // Проверяем есть ли место (12x12 = 144 слота)
        var currentItems = storage.Container.ContainedEntities.Count;
        var maxSlots = 144;

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
                // Спавним предмет
                var item = Spawn(prototype, coords);

                // Пытаемся вставить в хранилище
                if (!_storage.Insert(uid, item, out _, storageComp: storage, playSound: false))
                {
                    // Если не удалось вставить, удаляем предмет
                    Del(item);
                }
            }
        }
    }
}
