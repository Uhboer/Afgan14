using System.Linq;
using Content.Shared.Afgan.HolyStamp;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;

namespace Content.Server.Afgan.HolyStamp;

public sealed class HolyStampSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HolyStampComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<BlessedMeleeComponent, ExaminedEvent>(OnExamined);
    }

    private void OnAfterInteract(Entity<HolyStampComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        var target = args.Target.Value;

        if (!TryComp<MeleeWeaponComponent>(target, out var melee))
        {
            _popup.PopupEntity("Это не оружие ближнего боя!", args.User, args.User, PopupType.SmallCaution);
            return;
        }

        if (HasComp<BlessedMeleeComponent>(target))
        {
            _popup.PopupEntity("Это оружие уже благословлено!", args.User, args.User, PopupType.SmallCaution);
            return;
        }

        var multiplier = FixedPoint2.New(ent.Comp.DamageMultiplier);
        var keys = melee.Damage.DamageDict.Keys.ToList();
        foreach (var key in keys)
        {
            melee.Damage.DamageDict[key] = FixedPoint2.New((melee.Damage.DamageDict[key] * multiplier).Float());
        }
        Dirty(target, melee);

        EnsureComp<BlessedMeleeComponent>(target);
        QueueDel(ent.Owner);

        _popup.PopupEntity("Оружие благословлено.", args.User, args.User, PopupType.Medium);

        args.Handled = true;
    }

    private void OnExamined(Entity<BlessedMeleeComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup("[color=#ffd700]✝✝ Это оружие усилено.✝✝ [/color]");
    }
}
