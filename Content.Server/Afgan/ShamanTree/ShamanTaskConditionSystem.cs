using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Afgan.ShamanTree;

public sealed class ShamanTaskConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShamanTaskConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, ShamanTaskConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = comp.TaskType switch
        {
            ShamanTaskType.Kill => KillProgress(comp),
            ShamanTaskType.MassKill => KillProgress(comp),
            ShamanTaskType.Offer => Math.Clamp(comp.Progress, 0, 1),
            _ => 0f,
        };
    }

    private float KillProgress(ShamanTaskConditionComponent comp)
    {
        if (comp.TargetMinds.Count == 0)
            return 1f;

        var dead = 0;
        foreach (var mindId in comp.TargetMinds)
        {
            if (!TryComp<MindComponent>(mindId, out var mind))
            {
                dead++;
                continue;
            }

            if (mind.OwnedEntity == null || _mind.IsCharacterDeadIc(mind))
                dead++;
        }

        return dead / (float) comp.TargetMinds.Count;
    }
}
