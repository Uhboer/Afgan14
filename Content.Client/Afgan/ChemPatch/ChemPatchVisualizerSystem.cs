using Content.Shared.Afgan.ChemPatch;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Afgan.ChemPatch;


public sealed class ChemPatchVisualizerSystem : VisualizerSystem<ChemPatchComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ChemPatchComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (args.Sprite.LayerMapTryGet("chem_color", out var oldIdx))
            args.Sprite.RemoveLayer(oldIdx);

        if (!AppearanceSystem.TryGetData<Color>(uid, ChemPatchVisuals.Color, out var color, args.Component))
            return;


        if (!args.Sprite.TryGetLayer(0, out var baseLayer))
            return;

        var rsi = baseLayer.RSI ?? args.Sprite.BaseRSI;
        if (rsi == null)
            return;

        var state = baseLayer.State.Name;
        if (string.IsNullOrEmpty(state))
            return;

        var layerIdx = args.Sprite.AddLayer(
            new SpriteSpecifier.Rsi(new ResPath(rsi.Path.ToString()), state));

        args.Sprite.LayerMapSet("chem_color", layerIdx);
        args.Sprite.LayerSetColor(layerIdx, color.WithAlpha(0.65f));
    }
}
