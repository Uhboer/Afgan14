using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Popups;
using Content.Shared.Roles.Jobs;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Afgan.ShamanTree;

public sealed class ShamanTreeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    private static readonly HashSet<string> AfghanJobs = new()
    {
        "Bartender", "Botanist", "Chaplain", "Passenger",
    };

    private static readonly HashSet<string> RebelJobs = new()
    {
        "Reporter", "Boxer",
    };

    private static readonly HashSet<string> AlchemistJobs = new()
    {
        "TechnicalAssistant",
    };

    private enum Faction { Afghan, Rebel, Alchemist, Other }

    private static readonly (ShamanTaskType Type, int Weight)[] TaskWeights =
    {
        (ShamanTaskType.Kill, 55),
        (ShamanTaskType.Offer, 40),
        (ShamanTaskType.MassKill, 5),
    };

    private static int RewardFor(ShamanTaskType t) => t switch
    {
        ShamanTaskType.Kill => 15,
        ShamanTaskType.Offer => 20,
        ShamanTaskType.MassKill => 50,
        _ => 0,
    };

    private sealed record OfferEntry(string Display, string? Prototype, string[]? Tags, int Weight);

    private static readonly OfferEntry[] OfferTable =
    {
        new("самодельный АК", "WeaponRifleAKSamodel", null, 30),
        new("сверхукреплённый бронежилет", "ClothingOuterVestGiga", null, 20),
        new("золотой меч", "AfganSwordGold", null, 15),
        new("материя", null, new[] { "Materia", "MateriaM", "MateriaB", "MateriaG", "MateriaL" }, 15),
        new("дерьмо", "FoodKakashka", null, 5),
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShamanTreeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ShamanTreeComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<MindComponent, MindGotRemovedEvent>(OnMindLeftBody);
    }

    /// <summary>
    /// Когда mind отделяется от тела (смерть/смена тела/нового сознания) — сбрасываем
    /// все шаманские objective этого mind: удаляем сущности, чистим записи в каждом дереве.
    /// </summary>
    private void OnMindLeftBody(EntityUid mindId, MindComponent mind, MindGotRemovedEvent args)
    {

        // Снимаем шаманские objective с mind и удаляем сущности
        for (var i = mind.Objectives.Count - 1; i >= 0; i--)
        {
            if (HasComp<ShamanTaskConditionComponent>(mind.Objectives[i]))
                _mind.TryRemoveObjective(mindId, mind, i);
        }

        // Чистим назначения во всех деревьях
        var treeQuery = EntityQueryEnumerator<ShamanTreeComponent>();
        while (treeQuery.MoveNext(out _, out var tree))
        {
            tree.ActiveAssignments.Remove(mindId);
        }
    }

    private void OnMapInit(EntityUid uid, ShamanTreeComponent comp, MapInitEvent args)
    {
        comp.NextAnnounceTime = _timing.CurTime + comp.AnnounceInterval;
        comp.CooldownEnd = TimeSpan.Zero;
        PickNewTask(comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ShamanTreeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.CurrentTask == null && curTime >= comp.CooldownEnd)
                PickNewTask(comp);

            if (curTime < comp.NextAnnounceTime)
                continue;

            comp.NextAnnounceTime = curTime + comp.AnnounceInterval;

            if (comp.CurrentTask is { } t)
                Announce(uid, t);
        }
    }

    private void Announce(EntityUid uid, ShamanTaskType type)
    {
        var line = Loc.GetString($"shaman-tree-announce-{type.ToString().ToLowerInvariant()}");
        SayRaw(uid, line);
    }

    private void Say(EntityUid uid, string locId)
    {
        SayRaw(uid, Loc.GetString(locId));
    }

    private void SayRaw(EntityUid uid, string line)
    {
        _chat.TrySendInGameICMessage(uid, line, InGameICChatType.Speak, ChatTransmitRange.Normal);
        _popup.PopupEntity(line, uid, PopupType.Medium);
    }

    private void PickNewTask(ShamanTreeComponent comp)
    {
        comp.CurrentTask = WeightedPick(TaskWeights, t => t.Weight).Type;
    }

    private T WeightedPick<T>(IReadOnlyList<T> table, Func<T, int> weightOf)
    {
        var total = 0;
        for (var i = 0; i < table.Count; i++) total += weightOf(table[i]);
        var r = _random.Next(total);
        var acc = 0;
        for (var i = 0; i < table.Count; i++)
        {
            acc += weightOf(table[i]);
            if (r < acc) return table[i];
        }
        return table[^1];
    }

    private void OnInteract(EntityUid uid, ShamanTreeComponent comp, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!_mind.TryGetMind(args.User, out var mindId, out var mind))
            return;

        args.Handled = true;

        if (comp.ActiveAssignments.TryGetValue(mindId, out var objectiveUid))
        {
            if (!EntityManager.EntityExists(objectiveUid) || !TryComp<ShamanTaskConditionComponent>(objectiveUid, out var task))
            {
                comp.ActiveAssignments.Remove(mindId);
            }
            else
            {
                if (task.TaskType == ShamanTaskType.Offer && task.Progress < 1)
                    TryAcceptOffer(args.User, task);

                var ev = new ObjectiveGetProgressEvent(mindId, mind);
                RaiseLocalEvent(objectiveUid, ref ev);

                if (ev.Progress is >= 1f)
                {
                    GiveReward(uid, comp, task.Reward);
                    comp.ActiveAssignments.Remove(mindId);
                    RemoveObjectiveFromMind(mindId, mind, objectiveUid);
                    Say(uid, "shaman-tree-task-complete");
                }
                else
                {
                    Say(uid, "shaman-tree-task-incomplete");
                }
                return;
            }
        }

        if (comp.CurrentTask is not { } type)
        {
            Say(uid, "shaman-tree-resting");
            return;
        }

        var faction = GetFaction(mindId);

        var objective = Spawn(comp.ObjectivePrototype);
        var cond = EnsureComp<ShamanTaskConditionComponent>(objective);
        cond.TaskType = type;
        cond.IssuingTree = uid;
        cond.Reward = RewardFor(type);

        string description;
        switch (type)
        {
            case ShamanTaskType.Kill:
                if (!FillKill(cond, mindId, faction, out description))
                {
                    Del(objective);
                    PickNewTask(comp);
                    Say(uid, "shaman-tree-refuses");
                    return;
                }
                break;
            case ShamanTaskType.MassKill:
                if (!FillMassKill(cond, mindId, out description))
                {
                    Del(objective);
                    PickNewTask(comp);
                    Say(uid, "shaman-tree-refuses");
                    return;
                }
                break;
            case ShamanTaskType.Offer:
                FillOffer(cond, out description);
                break;
            default:
                Del(objective);
                return;
        }

        var meta = MetaData(objective);
        _metaData.SetEntityName(objective, Loc.GetString($"shaman-tree-objective-title-{type.ToString().ToLowerInvariant()}"), meta);
        _metaData.SetEntityDescription(objective, description, meta);

        _mind.AddObjective(mindId, mind, objective);
        comp.ActiveAssignments[mindId] = objective;

        comp.CurrentTask = null;
        comp.CooldownEnd = _timing.CurTime + comp.Cooldown;

        Say(uid, "shaman-tree-task-given");
    }

    private bool FillKill(ShamanTaskConditionComponent cond, EntityUid recipientMind, Faction recipient, out string description)
    {
        var pool = PickKillPool(recipientMind, recipient);
        if (pool.Count == 0)
            pool = _mind.GetAliveHumansExcept(recipientMind); // фолбэк — хоть кто-то живой

        if (pool.Count == 0)
        {
            description = "";
            return false;
        }
        var target = _random.Pick(pool);
        cond.TargetMinds.Add(target);
        description = Loc.GetString("shaman-tree-objective-kill", ("target", GetMindName(target)));
        return true;
    }

    /// <summary>
    /// Возвращает список разрешённых mind-uid жертв с учётом фракции получателя задачи.
    /// </summary>
    private List<EntityUid> PickKillPool(EntityUid recipientMind, Faction recipient)
    {
        var all = _mind.GetAliveHumansExcept(recipientMind);
        if (recipient == Faction.Other)
            return all;

        HashSet<Faction> allowed;
        switch (recipient)
        {
            case Faction.Rebel:
                // 95% не-повстанец+не-алхимик, 5% алхимик
                allowed = _random.Prob(0.05f)
                    ? new HashSet<Faction> { Faction.Alchemist }
                    : new HashSet<Faction> { Faction.Afghan, Faction.Other };
                break;
            case Faction.Afghan:
                var r = _random.NextFloat();
                if (r < 0.05f) allowed = new HashSet<Faction> { Faction.Alchemist };
                else if (r < 0.25f) allowed = new HashSet<Faction> { Faction.Afghan };
                else allowed = new HashSet<Faction> { Faction.Rebel, Faction.Other };
                break;
            case Faction.Alchemist:
                allowed = _random.Prob(0.5f)
                    ? new HashSet<Faction> { Faction.Rebel }
                    : new HashSet<Faction> { Faction.Afghan };
                break;
            default:
                return all;
        }

        return all.Where(m => allowed.Contains(GetFaction(m))).ToList();
    }

    private bool FillMassKill(ShamanTaskConditionComponent cond, EntityUid recipientMind, out string description)
    {
        var alive = _mind.GetAliveHumansExcept(recipientMind);
        var aliveCount = alive.Count + 1;
        if (aliveCount < 7)
        {
            description = "";
            return false;
        }

        int n;
        if (aliveCount >= 15) n = 4;
        else if (aliveCount >= 12) n = 3;
        else n = 2;

        _random.Shuffle(alive);
        n = Math.Min(n, alive.Count);
        var names = new List<string>();
        for (var i = 0; i < n; i++)
        {
            cond.TargetMinds.Add(alive[i]);
            names.Add(GetMindName(alive[i]));
        }
        description = Loc.GetString("shaman-tree-objective-masskill",
            ("targets", string.Join(", ", names)),
            ("count", n));
        return true;
    }

    private void FillOffer(ShamanTaskConditionComponent cond, out string description)
    {
        var entry = WeightedPick(OfferTable, e => e.Weight);
        if (entry.Prototype != null) cond.OfferPrototypes.Add(entry.Prototype);
        if (entry.Tags != null) cond.OfferTags.AddRange(entry.Tags);
        cond.Display = entry.Display;
        description = Loc.GetString("shaman-tree-objective-offer", ("item", entry.Display));
    }

    private void RemoveObjectiveFromMind(EntityUid mindId, MindComponent mind, EntityUid objectiveUid)
    {
        var idx = mind.Objectives.IndexOf(objectiveUid);
        if (idx >= 0)
            _mind.TryRemoveObjective(mindId, mind, idx);
    }

    private void GiveReward(EntityUid tree, ShamanTreeComponent comp, int amount)
    {
        if (amount <= 0 || string.IsNullOrEmpty(comp.RewardPrototype.Id))
            return;

        var pos = Transform(tree).Coordinates;
        var reward = Spawn(comp.RewardPrototype, pos);
        if (TryComp<StackComponent>(reward, out var stack))
            _stack.SetCount(reward, amount, stack);
    }

    private void TryAcceptOffer(EntityUid user, ShamanTaskConditionComponent task)
    {
        foreach (var held in _hands.EnumerateHeld(user).ToList())
        {
            if (!MatchesOffer(held, task))
                continue;

            QueueDel(held);
            task.Progress = 1;
            return;
        }
    }

    private bool MatchesOffer(EntityUid item, ShamanTaskConditionComponent task)
    {
        var protoId = MetaData(item).EntityPrototype?.ID;
        if (protoId != null && task.OfferPrototypes.Contains(protoId))
            return true;

        foreach (var tag in task.OfferTags)
        {
            if (_tag.HasTag(item, tag))
                return true;
        }
        return false;
    }

    private string GetMindName(EntityUid mindId)
    {
        if (TryComp<MindComponent>(mindId, out var mind))
        {
            if (mind.OwnedEntity is { } ent)
                return MetaData(ent).EntityName;
            return mind.CharacterName ?? "???";
        }
        return "???";
    }

    private Faction GetFaction(EntityUid mindId)
    {
        if (!_jobs.MindTryGetJob(mindId, out _, out var jobProto))
            return Faction.Other;

        var id = jobProto.ID;
        if (AfghanJobs.Contains(id)) return Faction.Afghan;
        if (RebelJobs.Contains(id)) return Faction.Rebel;
        if (AlchemistJobs.Contains(id)) return Faction.Alchemist;
        return Faction.Other;
    }

}
