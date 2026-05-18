using System.Linq;
using System.Numerics;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared._AFGAN.Prototypes;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Random;

namespace Content.Server._AFGAN.Components;

[RegisterComponent]
public sealed partial class SimpleTransmutationCircleComponent : Component { }

public sealed class SimpleTransmutationCircleSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SimpleTransmutationCircleComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(EntityUid uid, SimpleTransmutationCircleComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        Verb verb = new()
        {
            Act = () => Transmute(uid, args.User),
            Text = "Трансмутировать",
            Priority = 1
        };
        args.Verbs.Add(verb);
    }

    private void Transmute(EntityUid uid, EntityUid user)
    {
        var center = Transform(uid).Coordinates;
        var items = _lookup.GetEntitiesInRange(center, 1.5f).ToList();
        if (items.Count == 0)
        {
            _popup.PopupEntity("Круг пуст.", uid, user);
            return;
        }

        foreach (var recipe in _prototype.EnumeratePrototypes<TransmutationRecipeSimple>())
        {
            var need = new Dictionary<string, int>(recipe.Materials);
            foreach (var item in items)
            {
                if (need.Count == 0) break;
                foreach (var tag in need.Keys.ToList())
                {
                    if (_tag.HasTag(item, tag))
                    {
                        need[tag]--;
                        if (need[tag] == 0)
                            need.Remove(tag);
                        break;
                    }
                }
            }
            if (need.Count != 0) continue;

            // Удаляем предметы
            var toRemove = new List<EntityUid>();
            var need2 = new Dictionary<string, int>(recipe.Materials);
            foreach (var item in items)
            {
                if (need2.Count == 0) break;
                foreach (var tag in need2.Keys.ToList())
                {
                    if (_tag.HasTag(item, tag))
                    {
                        toRemove.Add(item);
                        need2[tag]--;
                        if (need2[tag] == 0)
                            need2.Remove(tag);
                        break;
                    }
                }
            }
            foreach (var item in toRemove)
                Del(item);

            // Дым
            int smokeCount = _random.Next(5, 9);
            for (int i = 0; i < smokeCount; i++)
            {
                float offsetX = _random.NextFloat(-1.2f, 1.2f);
                float offsetY = _random.NextFloat(-1.2f, 1.2f);
                var offset = new Vector2(offsetX, offsetY);
                var smokePos = center.Offset(offset);
                Spawn("Smoke", smokePos);
            }

            // Звук
            var sound = new SoundPathSpecifier("/Audio/Effects/explosion1.ogg");
            _audio.PlayPvs(sound, uid);

            // Результат
            Spawn(recipe.Result, center);
            _popup.PopupEntity($"Вы получили {recipe.Result}.", uid, user);
            return;
        }

        _popup.PopupEntity("Ничего не произошло.", uid, user);
    }
}