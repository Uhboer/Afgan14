using Content.Shared.Afgan.HolyStamp;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Afgan.HolyStamp;

/// <summary>
/// Добавляет визуальный эффект зачарования на благословлённое оружие.
/// Дублирует базовый слой спрайта с unshaded + золотой цвет — эффект ложится точно по силуэту меча.
/// </summary>
public sealed class BlessedMeleeVisualsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlessedMeleeComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BlessedMeleeComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<BlessedMeleeComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        // Берём RSI и state первого слоя оружия
        if (!sprite.TryGetLayer(0, out var baseLayer))
            return;

        var rsi = baseLayer.RSI ?? sprite.BaseRSI;
        if (rsi == null)
            return;

        var state = baseLayer.State.Name;
        if (string.IsNullOrEmpty(state))
            return;

        // Добавляем дублирующий слой с тем же спрайтом, но unshaded + золотой цвет
        // Это даёт эффект зачарования точно по контуру оружия
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
}
