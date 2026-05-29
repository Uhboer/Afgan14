using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Afgan.Power;

[RegisterComponent]
public sealed partial class BiomassGeneratorComponent : Component
{
    [ViewVariables]
    public int CurrentBiomass;

    [DataField("maxBiomass")]
    public int MaxBiomass = 2000;

    [DataField("consumptionRate")]
    public int ConsumptionRate = 1;

    [DataField("powerOutput")]
    public int PowerOutput = 50000;

    [DataField("updateDelay")]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(1);

    [DataField("nextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField("biomassStackType")]
    public string BiomassStackType = "Biomass";
}
