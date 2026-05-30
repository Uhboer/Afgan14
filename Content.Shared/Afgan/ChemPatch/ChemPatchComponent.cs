using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Afgan.ChemPatch;

[RegisterComponent]
public sealed partial class ChemPatchComponent : Component
{
    [DataField]
    public string SolutionName = "patch";

    [DataField]
    public FixedPoint2 TransferAmount = FixedPoint2.New(0.5);

    [DataField]
    public TimeSpan TransferDelay = TimeSpan.FromSeconds(2);
}

[RegisterComponent]
public sealed partial class ActiveChemPatchComponent : Component
{
    [DataField]
    public Solution Solution = new();

    [DataField]
    public FixedPoint2 TransferAmount = FixedPoint2.New(0.5);

    [DataField]
    public TimeSpan TransferDelay = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextTransfer;
}
