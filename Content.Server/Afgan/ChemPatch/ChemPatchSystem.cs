using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Afgan.ChemPatch;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Afgan.ChemPatch;

public sealed class ChemPatchSystem : EntitySystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChemPatchComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<ChemPatchComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (!TryComp<BloodstreamComponent>(args.Target.Value, out _))
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out var patchSoln, out var patchSolution) ||
            patchSolution.Volume <= FixedPoint2.Zero)
            return;

        var active = EnsureComp<ActiveChemPatchComponent>(args.Target.Value);
        active.Solution.AddSolution(_solution.SplitSolution(patchSoln.Value, patchSolution.Volume), _prototype);
        active.TransferAmount = ent.Comp.TransferAmount;
        active.TransferDelay = ent.Comp.TransferDelay;
        active.NextTransfer = _timing.CurTime + active.TransferDelay;

        QueueDel(ent.Owner);
        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveChemPatchComponent, BloodstreamComponent>();
        while (query.MoveNext(out var uid, out var patch, out var bloodstream))
        {
            if (_timing.CurTime < patch.NextTransfer)
                continue;

            patch.NextTransfer = _timing.CurTime + patch.TransferDelay;

            if (patch.Solution.Volume <= FixedPoint2.Zero)
            {
                RemCompDeferred<ActiveChemPatchComponent>(uid);
                continue;
            }

            var transfer = patch.Solution.SplitSolution(FixedPoint2.Min(patch.TransferAmount, patch.Solution.Volume));
            _bloodstream.TryAddToChemicals(uid, transfer, bloodstream);
            _reactive.DoEntityReaction(uid, transfer, ReactionMethod.Injection);

            if (patch.Solution.Volume <= FixedPoint2.Zero)
                RemCompDeferred<ActiveChemPatchComponent>(uid);
        }
    }
}
