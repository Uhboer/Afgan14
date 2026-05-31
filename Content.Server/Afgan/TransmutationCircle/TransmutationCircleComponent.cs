using System.Linq;
using System.Numerics;
using Content.Server.Popups;
using Content.Server.UserInterface;
using Content.Shared._AFGAN.Prototypes;
using Content.Shared._AFGAN.TransmutationCircle;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._AFGAN.Components;

[RegisterComponent]
[Access(typeof(SimpleTransmutationCircleSystem))]
public sealed partial class SimpleTransmutationCircleComponent : Component { }

public sealed class SimpleTransmutationCircleSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SimpleTransmutationCircleComponent, BeforeActivatableUIOpenEvent>(OnUIOpen);
        SubscribeLocalEvent<SimpleTransmutationCircleComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
        SubscribeLocalEvent<SimpleTransmutationCircleComponent, TransmutationCircleSelectMessage>(OnRecipeSelected);
    }

    private void OnUIOpen(EntityUid uid, SimpleTransmutationCircleComponent component, BeforeActivatableUIOpenEvent args)
    {
        UpdateUI(uid);
    }

    private void OnBoundUIOpened(EntityUid uid, SimpleTransmutationCircleComponent component, BoundUIOpenedEvent args)
    {
        UpdateUI(uid);
    }

    private void UpdateUI(EntityUid uid)
    {
        var center = Transform(uid).Coordinates;
        var items = _lookup.GetEntitiesInRange(center, 1.5f).ToList();

        var allRecipes = _prototype.EnumeratePrototypes<TransmutationRecipeSimple>()
            .Select(r => r.ID)
            .ToList();

        var available = new List<string>();
        foreach (var recipe in _prototype.EnumeratePrototypes<TransmutationRecipeSimple>())
        {
            if (CanCraft(recipe, items))
                available.Add(recipe.ID);
        }

        _ui.SetUiState(uid, TransmutationCircleUiKey.Key,
            new TransmutationCircleState(available, allRecipes));
    }

    private void OnRecipeSelected(EntityUid uid, SimpleTransmutationCircleComponent component, TransmutationCircleSelectMessage args)
    {
        if (!_prototype.TryIndex<TransmutationRecipeSimple>(args.RecipeId, out var recipe))
            return;

        var user = args.Actor;
        var center = Transform(uid).Coordinates;
        var items = _lookup.GetEntitiesInRange(center, 1.5f).ToList();

        if (!CanCraft(recipe, items))
        {
            _popup.PopupEntity("Недостаточно материалов!", uid, user);
            UpdateUI(uid);
            return;
        }

        // Удаляем материалы
        var need = new Dictionary<string, int>(recipe.Materials);
        foreach (var item in items)
        {
            if (need.Count == 0) break;
            foreach (var tag in need.Keys.ToList())
            {
                if (_tag.HasTag(item, tag))
                {
                    Del(item);
                    need[tag]--;
                    if (need[tag] == 0)
                        need.Remove(tag);
                    break;
                }
            }
        }

        // Дым
        int smokeCount = _random.Next(5, 9);
        for (int i = 0; i < smokeCount; i++)
        {
            float offsetX = _random.NextFloat(-1.2f, 1.2f);
            float offsetY = _random.NextFloat(-1.2f, 1.2f);
            Spawn("Smoke", center.Offset(new Vector2(offsetX, offsetY)));
        }

        // Звук
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/explosion1.ogg"), uid);

        // Результат
        Spawn(recipe.Result, center);
        _popup.PopupEntity($"Вы получили {recipe.Result}.", uid, user);

        // Обновляем меню
        UpdateUI(uid);
    }

    private bool CanCraft(TransmutationRecipeSimple recipe, List<EntityUid> items)
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
        return need.Count == 0;
    }
}
