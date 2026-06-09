using Content.Shared.Afgan.Fermentation;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Afgan.Fermentation;

public sealed class FermentationVatSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FermentationVatComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<FermentationVatComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, FermentationVatComponent comp, ExaminedEvent args)
    {
        if (comp.ActiveRecipe == null || comp.IngredientsLoaded == 0)
        {
            args.PushText("Внутри ничего нету.");
            return;
        }

        args.PushText($"Бродит {comp.IngredientsLoaded}/{comp.ActiveRecipe.MaxIngredients} ингредиентов → {comp.ActiveRecipe.OutputReagent}.");
    }

    private void OnInteractUsing(EntityUid uid, FermentationVatComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var recipe = FindRecipe(comp, args.Used);
        if (recipe == null)
            return;

        args.Handled = true;

        if (comp.ActiveRecipe != null && comp.ActiveRecipe.ID != recipe.ID)
        {
            var name = comp.ActiveRecipe.Name.Length > 0 ? comp.ActiveRecipe.Name : comp.ActiveRecipe.ID;
            _popup.PopupEntity($"В чане уже идёт другой процесс: {name}.", uid, args.User);
            return;
        }

        if (comp.IngredientsLoaded >= recipe.MaxIngredients)
        {
            _popup.PopupEntity("Чан уже набит ингредиентами под завязку!", uid, args.User);
            return;
        }

        comp.ActiveRecipe = recipe;
        comp.IngredientsLoaded++;

        // Сначала выбрасываем из рук, потом удаляем
        _hands.TryDrop(args.User, args.Used);
        EntityManager.DeleteEntity(args.Used);

        _popup.PopupEntity($"Ингредиент заложен. Бродит: {comp.IngredientsLoaded}/{recipe.MaxIngredients}.", uid, args.User);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FermentationVatComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ActiveRecipe == null || comp.IngredientsLoaded <= 0)
                continue;

            comp.AccumulatedTime += frameTime;

            if (comp.AccumulatedTime < comp.ActiveRecipe.FermentationTime)
                continue;

            comp.AccumulatedTime -= comp.ActiveRecipe.FermentationTime;
            comp.IngredientsLoaded--;

            if (_solution.TryGetSolution(uid, comp.SolutionName, out var solutionEnt, out var solution))
            {
                var space = solution.MaxVolume - solution.Volume;
                if (space > FixedPoint2.Zero)
                {
                    var amount = FixedPoint2.Min(
                        FixedPoint2.New(comp.ActiveRecipe.OutputPerIngredient),
                        space);
                    _solution.TryAddReagent(solutionEnt.Value, comp.ActiveRecipe.OutputReagent, amount, out _);
                }
            }

            if (comp.IngredientsLoaded <= 0)
            {
                comp.ActiveRecipe = null;
                comp.AccumulatedTime = 0f;
            }
        }
    }

    private FermentationRecipePrototype? FindRecipe(FermentationVatComponent comp, EntityUid item)
    {
        foreach (var protoId in comp.Recipes)
        {
            if (!_proto.TryIndex(protoId, out var recipe))
                continue;

            if (_tag.HasTag(item, recipe.IngredientTag))
                return recipe;
        }
        return null;
    }
}
