using Content.Server.Afgan.Power;
using Content.Server.Power.Components;
using Content.Server.Stack;
using Content.Shared.Examine;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Robust.Shared.Timing;

namespace Content.Server.EntitySystems;

public sealed class BiomassGeneratorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StackSystem _stack = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BiomassGeneratorComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, BiomassGeneratorComponent component, ExaminedEvent args)
    {
        args.PushText($"Biomass: {component.CurrentBiomass}/{component.MaxBiomass}");
        args.PushText($"Output: {component.PowerOutput} W");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<BiomassGeneratorComponent, PowerSupplierComponent>();
        while (query.MoveNext(out var uid, out var gen, out var supplier))
        {
            if (curTime < gen.NextUpdate)
                continue;

            gen.NextUpdate = curTime + gen.UpdateDelay;
            LoadBiomassFromStorage(uid, gen);

            if (gen.CurrentBiomass < gen.ConsumptionRate)
            {
                supplier.Enabled = false;
                supplier.MaxSupply = 0;
                continue;
            }

            gen.CurrentBiomass -= gen.ConsumptionRate;
            supplier.Enabled = true;
            supplier.MaxSupply = gen.PowerOutput;
        }
    }

    private void LoadBiomassFromStorage(EntityUid uid, BiomassGeneratorComponent component)
    {
        if (component.CurrentBiomass >= component.MaxBiomass)
            return;

        if (!TryComp<StorageComponent>(uid, out var storage))
            return;

        var need = component.MaxBiomass - component.CurrentBiomass;
        foreach (var stored in storage.Container.ContainedEntities)
        {
            if (need <= 0)
                break;

            if (!TryComp<StackComponent>(stored, out var stack) || stack.StackTypeId != component.BiomassStackType)
                continue;

            var taken = Math.Min(need, stack.Count);
            if (taken <= 0 || !_stack.Use(stored, taken, stack))
                continue;

            component.CurrentBiomass += taken;
            need -= taken;
        }
    }
}
