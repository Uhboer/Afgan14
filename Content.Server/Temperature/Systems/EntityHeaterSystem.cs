using Content.Server.Temperature.Components;
using Content.Shared.Examine;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Temperature;
using Content.Shared.Verbs;

namespace Content.Server.Temperature.Systems;

/// <summary>
/// Handles <see cref="EntityHeaterComponent"/> updating and events.
/// </summary>
public sealed class EntityHeaterSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    private readonly int SettingCount = Enum.GetValues(typeof(EntityHeaterSetting)).Length;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityHeaterComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<EntityHeaterComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    public override void Update(float deltaTime)
    {
        var query = EntityQueryEnumerator<EntityHeaterComponent, ItemPlacerComponent>();
        while (query.MoveNext(out var uid, out var comp, out var placer))
        {

            // don't divide by total entities since its a big grill
            // excess would just be wasted in the air but that's not worth simulating
            // if you want a heater thermomachine just use that...
            foreach (var ent in placer.PlacedEntities)
            {
                var power = 500f;
                _temperature.ChangeHeat(ent, power);
            }
        }
    }

    private void OnExamined(EntityUid uid, EntityHeaterComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("entity-heater-examined", ("setting", comp.Setting)));
    }

    private void OnGetVerbs(EntityUid uid, EntityHeaterComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var setting = (int) comp.Setting;
        setting++;
        setting %= SettingCount;
        var nextSetting = (EntityHeaterSetting) setting;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("entity-heater-switch-setting", ("setting", nextSetting)),
            Act = () =>
            {
                ChangeSetting(uid, nextSetting, comp);
                _popup.PopupEntity(Loc.GetString("entity-heater-switched-setting", ("setting", nextSetting)), uid, args.User);
            }
        });
    }

    private void ChangeSetting(EntityUid uid, EntityHeaterSetting setting, EntityHeaterComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.Setting = setting;
        _appearance.SetData(uid, EntityHeaterVisuals.Setting, setting);
    }

    private float Setting(EntityHeaterSetting setting, float max)
    {
        switch (setting)
        {
            case EntityHeaterSetting.Low:
                return max / 2;
            case EntityHeaterSetting.Medium:
                return max / 2;
            case EntityHeaterSetting.High:
                return max / 2;
            case EntityHeaterSetting.Off:
                return max / 2;
            default:
                return max / 2;
        }
    }
}
