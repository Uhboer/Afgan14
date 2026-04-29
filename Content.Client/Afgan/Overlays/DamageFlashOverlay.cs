using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Afgan.Overlays;

/// <summary>
/// Полноэкранный красный оверлей, который вспыхивает при получении урона.
/// </summary>
public sealed class DamageFlashOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _shader;

    /// <summary>
    /// Текущая интенсивность вспышки (0.0 — нет, 1.0 — максимум).
    /// Управляется системой DamageFlashSystem.
    /// </summary>
    public float Intensity = 0f;

    public DamageFlashOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index<ShaderPrototype>("DamageFlash").InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (Intensity <= 0f)
            return false;

        return base.BeforeDraw(in args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("intensity", Intensity);

        var handle = args.WorldHandle;
        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
