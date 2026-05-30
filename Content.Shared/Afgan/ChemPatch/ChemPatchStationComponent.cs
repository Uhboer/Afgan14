using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Afgan.ChemPatch;

[RegisterComponent]
public sealed partial class ChemPatchStationComponent : Component
{
    [DataField]
    public string PatchSlot = "patchSlot";

    [DataField]
    public string BeakerSlot = "beakerSlot";

    [DataField]
    public FixedPoint2 TransferAmount = FixedPoint2.New(5);
}

[Serializable, NetSerializable]
public enum ChemPatchStationUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class ChemPatchStationBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly string PatchContents;
    public readonly string BeakerContents;
    public readonly bool PatchInserted;
    public readonly bool BeakerInserted;
    public readonly bool CanTransfer;

    public ChemPatchStationBoundUserInterfaceState(
        string patchContents,
        string beakerContents,
        bool patchInserted,
        bool beakerInserted,
        bool canTransfer)
    {
        PatchContents = patchContents;
        BeakerContents = beakerContents;
        PatchInserted = patchInserted;
        BeakerInserted = beakerInserted;
        CanTransfer = canTransfer;
    }
}

[Serializable, NetSerializable]
public sealed class ChemPatchStationTransferMessage : BoundUserInterfaceMessage
{
}
