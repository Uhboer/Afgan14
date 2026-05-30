using Content.Server.UserInterface;
using Content.Shared.Afgan.ChemPatch;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Afgan.ChemPatch;

public sealed class ChemPatchStationSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChemPatchStationComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ChemPatchStationComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
        SubscribeLocalEvent<ChemPatchStationComponent, EntInsertedIntoContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<ChemPatchStationComponent, EntRemovedFromContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<ChemPatchStationComponent, ChemPatchStationTransferMessage>(OnTransfer);
    }

    private void OnUiOpened(EntityUid uid, ChemPatchStationComponent component, BoundUIOpenedEvent args)
    {
        UpdateUi(uid, component);
    }

    private void OnSolutionChanged(EntityUid uid, ChemPatchStationComponent component, ref SolutionContainerChangedEvent args)
    {
        UpdateUi(uid, component);
    }

    private void OnContainerChanged(EntityUid uid, ChemPatchStationComponent component, ContainerModifiedMessage args)
    {
        UpdateUi(uid, component);
    }

    private void OnTransfer(EntityUid uid, ChemPatchStationComponent component, ChemPatchStationTransferMessage args)
    {
        if (!TryGetSolutions(uid, component, out var patchSoln, out var patchSolution, out var beakerSoln, out var beakerSolution))
            return;

        var amount = FixedPoint2.Min(component.TransferAmount, beakerSolution!.Volume);
        amount = FixedPoint2.Min(amount, patchSolution!.AvailableVolume);
        if (amount <= FixedPoint2.Zero)
            return;

        var split = _solution.SplitSolution(beakerSoln!.Value, amount);
        _solution.TryTransferSolution(patchSoln!.Value, split, split.Volume);
        UpdateUi(uid, component);
    }

    private bool TryGetSolutions(
        EntityUid uid,
        ChemPatchStationComponent component,
        out Entity<SolutionComponent>? patchSoln,
        out Solution? patchSolution,
        out Entity<SolutionComponent>? beakerSoln,
        out Solution? beakerSolution)
    {
        patchSoln = null;
        patchSolution = null;
        beakerSoln = null;
        beakerSolution = null;

        var patch = _slots.GetItemOrNull(uid, component.PatchSlot);
        var beaker = _slots.GetItemOrNull(uid, component.BeakerSlot);
        if (patch == null || beaker == null)
            return false;

        if (!TryComp<ChemPatchComponent>(patch.Value, out var patchComp))
            return false;

        return _solution.TryGetSolution(patch.Value, patchComp.SolutionName, out patchSoln, out patchSolution) &&
               _solution.TryGetFitsInDispenser(beaker.Value, out beakerSoln, out beakerSolution);
    }

    private void UpdateUi(EntityUid uid, ChemPatchStationComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var patchText = "Нет пластыря";
        var beakerText = "Нет колбы";
        var patchInserted = false;
        var beakerInserted = false;
        var canTransfer = false;

        if (TryGetSolutions(uid, component, out _, out var patchSolution, out _, out var beakerSolution))
        {
            patchInserted = true;
            beakerInserted = true;
            patchText = FormatSolution(patchSolution!);
            beakerText = FormatSolution(beakerSolution!);
            canTransfer = beakerSolution!.Volume > FixedPoint2.Zero && patchSolution!.AvailableVolume > FixedPoint2.Zero;
        }
        else
        {
            var patch = _slots.GetItemOrNull(uid, component.PatchSlot);
            var beaker = _slots.GetItemOrNull(uid, component.BeakerSlot);

            patchInserted = patch != null;
            beakerInserted = beaker != null;

            if (patch != null &&
                TryComp<ChemPatchComponent>(patch.Value, out var patchComp) &&
                _solution.TryGetSolution(patch.Value, patchComp.SolutionName, out _, out var onlyPatchSolution))
            {
                patchText = FormatSolution(onlyPatchSolution);
            }

            if (beaker != null &&
                _solution.TryGetFitsInDispenser(beaker.Value, out _, out var onlyBeakerSolution))
            {
                beakerText = FormatSolution(onlyBeakerSolution);
            }
        }

        _ui.SetUiState(uid, ChemPatchStationUiKey.Key,
            new ChemPatchStationBoundUserInterfaceState(
                patchText,
                beakerText,
                patchInserted,
                beakerInserted,
                canTransfer));
    }

    private string FormatSolution(Solution solution)
    {
        if (solution.Volume <= FixedPoint2.Zero)
            return $"Пусто / {solution.MaxVolume}u";

        var lines = new List<string> { $"{solution.Volume}/{solution.MaxVolume}u" };
        foreach (var reagent in solution.Contents)
        {
            var name = _prototype.TryIndex<ReagentPrototype>(reagent.Reagent.Prototype, out var reagentPrototype)
                ? reagentPrototype.LocalizedName
                : reagent.Reagent.Prototype;

            lines.Add($"{name}: {reagent.Quantity}u");
        }

        return string.Join("\n", lines);
    }
}
