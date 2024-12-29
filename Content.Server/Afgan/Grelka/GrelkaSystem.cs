using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Content.Server.Atmos;

namespace Content.Server.Afgan.Grelka;

public sealed class GrelkaSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GrelkaComponent, AtmosDeviceUpdateEvent>(OnDeviceUpdate);
    }

    private void OnDeviceUpdate(EntityUid uid, GrelkaComponent component, ref AtmosDeviceUpdateEvent args)
    {
        var xform = Transform(uid);

        if (!xform.GridUid.HasValue)
            return;

        var tile = _transformSystem.GetGridOrMapTilePosition(uid, xform);

        // Получаем смесь газов в тайле
        var mixture = _atmosphereSystem.GetTileMixture(xform.GridUid.Value, null, tile, true);

        if (mixture == null || mixture.Temperature >= component.MaxTemperature)
            return;

        // Прямой нагрев температуры
        var oldTemp = mixture.Temperature;
        mixture.Temperature = MathF.Min(mixture.Temperature + component.HeatingRate, component.MaxTemperature);

        // Обновляем тайл
        _atmosphereSystem.InvalidateTile(xform.GridUid.Value, tile);

        // Добавим логирование для отладки
        Log.Debug($"Grelka heating: Old temp = {oldTemp}, New temp = {mixture.Temperature}, Target = {component.MaxTemperature}");
    }
}
