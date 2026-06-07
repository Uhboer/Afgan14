using Content.Shared.Afgan.HolyStamp;
using Content.Shared.Hands;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Afgan.HolyStamp;

public sealed class BlessedMeleeVisualsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlessedMeleeComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BlessedMeleeComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BlessedMeleeComponent, GetInhandVisualsEvent>(OnGetInhandVisuals);
    }

    private void OnStartup(Entity<BlessedMeleeComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        if (!sprite.TryGetLayer(0, out var baseLayer))
            return;

        var rsi = baseLayer.RSI ?? sprite.BaseRSI;
        if (rsi == null)
            return;

        var state = baseLayer.State.Name;
        if (string.IsNullOrEmpty(state))
            return;

        var layerIdx = sprite.AddLayer(
            new SpriteSpecifier.Rsi(new ResPath(rsi.Path.ToString()), state));

        sprite.LayerMapSet("blessed_glow", layerIdx);
        sprite.LayerSetShader(layerIdx, "unshaded");
        sprite.LayerSetColor(layerIdx, new Color(1.0f, 0.84f, 0.0f, 0.6f));
    }

    private void OnShutdown(Entity<BlessedMeleeComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        if (sprite.LayerMapTryGet("blessed_glow", out var layerIdx))
            sprite.RemoveLayer(layerIdx);
    }

    private void OnGetInhandVisuals(Entity<BlessedMeleeComponent> ent, ref GetInhandVisualsEvent args)
    {
        if (!TryComp<ItemComponent>(ent.Owner, out var item))
            return;

        var locationStr = args.Location.ToString().ToLowerInvariant();
        var baseState = item.HeldPrefix == null
            ? $"inhand-{locationStr}"
            : $"{item.HeldPrefix}-inhand-{locationStr}";

        string? rsiPath = item.RsiPath;
        if (rsiPath == null && TryComp<SpriteComponent>(ent.Owner, out var sprite))
            rsiPath = sprite.BaseRSI?.Path.ToString();

        if (rsiPath == null)
            return;

        args.Layers.Add(("blessed_glow_inhand", new PrototypeLayerData
        {
            RsiPath = rsiPath,
            State = baseState,
            Shader = "unshaded",
            Color = new Color(1.0f, 0.84f, 0.0f, 0.6f),
            MapKeys = new HashSet<string> { "blessed_glow_inhand" },
        }));
    }
}
