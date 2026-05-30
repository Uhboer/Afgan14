using Content.Shared.Audio;
using Content.Shared.Chemistry;
using Content.Shared.EntityEffects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;

namespace Content.Server.EntityEffects.Effects
{
    /// <summary>
    /// Эффект реагента, который проигрывает музыку во время метаболизма.
    /// </summary>
    public sealed partial class ReagentMusicEffect : EntityEffect
    {
        [DataField(required: true)]
        public SoundSpecifier Sound = default!;

        [DataField]
        public AudioParams SoundParams = AudioParams.Default.WithVolume(-2f);

        [DataField]
        public float RequiredAmount = 1.0f;

        [DataField]
        public float DurationPerUnit = 1.0f; // Длительность в секундах на единицу реагента

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-reagent-music-effect", 
                ("chance", Probability));

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (args is not EntityEffectReagentArgs reagentArgs)
            {
                Logger.Warning("ReagentMusicEffect: args is not EntityEffectReagentArgs");
                return;
            }

            if (reagentArgs.Source == null || reagentArgs.Reagent == null)
            {
                Logger.Warning($"ReagentMusicEffect: Source or Reagent is null. Source: {reagentArgs.Source}, Reagent: {reagentArgs.Reagent}");
                return;
            }

            var reagentId = reagentArgs.Reagent.ID;
            var amount = reagentArgs.Quantity.Float();
            
            Logger.Info($"ReagentMusicEffect: Processing reagent {reagentId} with amount {amount}, required {RequiredAmount}");
            
            if (amount < RequiredAmount)
            {
                Logger.Info($"ReagentMusicEffect: Amount {amount} less than required {RequiredAmount}");
                return;
            }

            var duration = TimeSpan.FromSeconds(amount * DurationPerUnit);
            
            Logger.Info($"ReagentMusicEffect: Activating music for reagent {reagentId} on entity {reagentArgs.TargetEntity} for duration {duration}");
            
            var reagentMusicSystem = reagentArgs.EntityManager.System<SharedReagentMusicSystem>();
            reagentMusicSystem.ActivateMusic(reagentArgs.TargetEntity, reagentId, Sound, duration, RequiredAmount, SoundParams);
        }
    }
}