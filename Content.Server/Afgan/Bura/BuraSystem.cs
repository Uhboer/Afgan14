using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Afgan.Bura;

public sealed class BuraSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BuraComponent, AtmosDeviceUpdateEvent>(OnDeviceUpdate);
    }

    private void OnDeviceUpdate(EntityUid uid, BuraComponent component, ref AtmosDeviceUpdateEvent args)
    {
        var xform = Transform(uid);

        if (!xform.GridUid.HasValue)
            return;

        var tile = _transformSystem.GetGridOrMapTilePosition(uid, xform);
        var environment = _atmosphereSystem.GetTileMixture(xform.GridUid.Value, null, tile);

        if (environment == null)
            return;

        // Охлаждение атмосферы до безопасной температуры
        environment.Temperature = MathF.Max(environment.Temperature - component.CoolingRate, component.MinTemperature);

        // Обновляем атмосферу тайла
        _atmosphereSystem.InvalidateTile(xform.GridUid.Value, tile);
    }
}
