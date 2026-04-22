using Content.Shared.Backmen.Eye.NightVision.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.GG.Eye.NightVision;

public sealed class NightVisionSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private NightVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new(Color.Green);

        SubscribeLocalEvent<NightVisionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<NightVisionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<NightVisionComponent, AfterAutoHandleStateEvent>(OnStateChanged);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private bool IsLocalPlayer(EntityUid uid) =>
        uid == _player.LocalSession?.AttachedEntity;

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        if (TryComp<NightVisionComponent>(args.Entity, out var comp))
            Apply(comp);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args) => Disable();

    private void OnStartup(EntityUid uid, NightVisionComponent comp, ComponentStartup args)
    {
        if (IsLocalPlayer(uid))
            Apply(comp);
    }

    private void OnShutdown(EntityUid uid, NightVisionComponent comp, ComponentShutdown args)
    {
        if (IsLocalPlayer(uid))
            Disable();
    }

    private void OnStateChanged(EntityUid uid, NightVisionComponent comp, ref AfterAutoHandleStateEvent args)
    {
        if (IsLocalPlayer(uid))
            Apply(comp);
    }

    private void Apply(NightVisionComponent comp)
    {
        if (comp.IsNightVision)
        {
            _overlay.NightvisionColor = comp.NightVisionColor;
            if (!_overlayMan.HasOverlay<NightVisionOverlay>())
                _overlayMan.AddOverlay(_overlay);
            _lightManager.DrawLighting = false;
        }
        else
        {
            Disable();
        }
    }

    private void Disable()
    {
        _overlayMan.RemoveOverlay(_overlay);
        _lightManager.DrawLighting = true;
    }
}
