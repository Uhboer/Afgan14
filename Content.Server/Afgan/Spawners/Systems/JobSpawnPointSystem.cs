using Content.Server.Afgan.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.Spawners.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Afgan.Spawners.Systems;

/// <summary>
/// Обрабатывает спавн игроков на замапленных <see cref="JobSpawnPointComponent"/> спавн-поинтах.
/// Работает как при раундстарте, так и при лейтджойне: если у игрока джоб из списка поинта — спавнит его там.
/// Имеет приоритет выше обычного SpawnPointSystem (подписывается раньше через порядок событий).
/// </summary>
public sealed class JobSpawnPointSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning, before: new[] { typeof(SpawnPointSystem) });
    }

    private void OnPlayerSpawning(PlayerSpawningEvent args)
    {
        // Уже кто-то обработал спавн — не вмешиваемся
        if (args.SpawnResult != null)
            return;

        // Нет джоба — нечего фильтровать
        if (args.Job?.Prototype == null)
            return;

        var jobProto = args.Job.Prototype.Value;

        var possiblePositions = new List<EntityCoordinates>();
        var query = EntityQueryEnumerator<JobSpawnPointComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var spawnPoint, out var xform))
        {
            // Проверяем принадлежность к станции
            if (args.Station != null && _stationSystem.GetOwningStation(uid, xform) != args.Station)
                continue;

            // Если список джобов пустой — поинт универсальный, подходит всем
            // Если список задан — проверяем, есть ли джоб игрока в нём
            if (spawnPoint.Jobs.Count > 0 && !spawnPoint.Jobs.Contains(jobProto))
                continue;

            possiblePositions.Add(xform.Coordinates);
        }

        if (possiblePositions.Count == 0)
            return;

        var spawnLoc = _random.Pick(possiblePositions);

        args.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station);
    }
}
