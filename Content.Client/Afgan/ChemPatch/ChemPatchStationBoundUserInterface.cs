using System.Numerics;
using Content.Shared.Afgan.ChemPatch;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;

namespace Content.Client.Afgan.ChemPatch;

[UsedImplicitly]
public sealed class ChemPatchStationBoundUserInterface : BoundUserInterface
{
    private ChemPatchStationWindow? _window;

    public ChemPatchStationBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new ChemPatchStationWindow(this);
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is ChemPatchStationBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }

    public void TogglePatchSlot()
    {
        SendMessage(new ItemSlotButtonPressedEvent("patchSlot"));
    }

    public void ToggleBeakerSlot()
    {
        SendMessage(new ItemSlotButtonPressedEvent("beakerSlot"));
    }

    public void Transfer()
    {
        SendMessage(new ChemPatchStationTransferMessage());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }
}

public sealed class ChemPatchStationWindow : DefaultWindow
{
    private readonly Label _patch = new();
    private readonly Label _beaker = new();
    private readonly Button _patchSlot = new();
    private readonly Button _beakerSlot = new();
    private readonly Button _transfer = new() { Text = "Перелить в пластырь" };

    public ChemPatchStationWindow(ChemPatchStationBoundUserInterface bui)
    {
        Title = "Станок химических пластырей";
        MinSize = new Vector2(420, 320);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(8)
        };

        root.AddChild(new Label { Text = "Пластырь" });
        root.AddChild(_patch);
        root.AddChild(_patchSlot);
        root.AddChild(new Control { MinSize = new Vector2(0, 8) });
        root.AddChild(new Label { Text = "Колба" });
        root.AddChild(_beaker);
        root.AddChild(_beakerSlot);
        root.AddChild(new Control { MinSize = new Vector2(0, 8) });
        root.AddChild(_transfer);

        Contents.AddChild(root);

        _patchSlot.OnPressed += _ => bui.TogglePatchSlot();
        _beakerSlot.OnPressed += _ => bui.ToggleBeakerSlot();
        _transfer.OnPressed += _ => bui.Transfer();
    }

    public void UpdateState(ChemPatchStationBoundUserInterfaceState state)
    {
        _patch.Text = state.PatchContents;
        _beaker.Text = state.BeakerContents;
        _patchSlot.Text = state.PatchInserted ? "Вынуть пластырь" : "Вставить пластырь";
        _beakerSlot.Text = state.BeakerInserted ? "Вынуть колбу" : "Вставить колбу";
        _transfer.Disabled = !state.CanTransfer;
    }
}
