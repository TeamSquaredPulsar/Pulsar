using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pulsar.DataClasses;
using Pulsar.Stations.Guards;
using Pulsar.Utilities;
using UnityEngine;

namespace Pulsar.Stations.Generation
{
/// <summary>
/// Builds a new guard ship at runtime out of a station's tiles.
/// It lays the tiles out on a grid (core, body, guns on top, thrusters
/// underneath, mirrored left/right so ships look designed), then puts them
/// on an empty guard shell as children. The shell's ShipManager attaches them
/// in its Start, exactly like it does for a hand-built enemy prefab.
/// </summary>
public static class EnemyGenerator
{
    private const int MaxLayoutAttempts = 8;

    private const int
        SmallShipTiles = 5; // below this, one thruster instead of two

    private static readonly TileRotation[] AllDirections =
    {
        TileRotation.Up, TileRotation.Left, TileRotation.Down,
        TileRotation.Right,
    };

    [CanBeNull]
    public static EnemyGuardAI GenerateEnemy(
        StationProfile profile,
        int waveIndex,
        Vector2 position,
        float rotationDegrees)
    {
        if (!IsUsable(profile, waveIndex))
        {
            return null;
        }

        WaveConfig wave = profile.Waves[waveIndex];
        // StationProfile.OnValidate keeps MaxTiles >= MinTiles.
        int totalTiles = Random.Range(wave.MinTiles, wave.MaxTiles + 1);

        // The first try always runs, so layout is never null below.
        bool hasGun = TryPlanLayout(profile, totalTiles,
            out Dictionary<Vector2Int, PlannedTile> layout);
        for (int attempt = 1; !hasGun && attempt < MaxLayoutAttempts; attempt++)
        {
            hasGun = TryPlanLayout(profile, totalTiles, out layout);
        }

        if (!hasGun)
        {
            Debug.LogWarning($"EnemyGenerator couldn't fit a gun on a " +
                             $"{profile.name} guard after {MaxLayoutAttempts} tries; " +
                             "it spawns without one.");
        }

        GameObject shell = GameObject.Instantiate(profile.GuardShellPrefab,
            position, Quaternion.Euler(0f, 0f, rotationDegrees));
        shell.name = $"{profile.name} Guard (wave {waveIndex + 1})";

        foreach ((Vector2Int cell, PlannedTile planned) in layout)
        {
            GameObject tile
                = GameObject.Instantiate(planned.Prefab, shell.transform);
            tile.transform.SetLocalPositionAndRotation(
                ShipUtilities.GridToLocal(cell),
                ShipUtilities.RotateQuarterOnZAxis(planned.Rotation.ToInt()));
        }

        EnemyGuardAI guard = shell.GetComponent<EnemyGuardAI>();
        guard.PickRandomBehavior();
        return guard;
    }

    private static bool IsUsable(StationProfile profile, int waveIndex)
    {
        string problem = null;
        if (profile == null)
        {
            problem = "no StationProfile";
        }
        else if (profile.Waves == null || waveIndex < 0 ||
                 waveIndex >= profile.Waves.Length)
        {
            problem
                = $"wave {waveIndex + 1} isn't in {profile.name}'s wave list";
        }
        else if (!profile.GuardShellPrefab ||
                 !profile.GuardShellPrefab.GetComponent<EnemyGuardAI>())
        {
            problem
                = $"{profile.name} needs a guard shell prefab with an EnemyGuardAI";
        }
        else if (!profile.CoreTilePrefab)
        {
            problem = $"{profile.name} has no core tile";
        }
        else if (profile.GetAvailableWeapons().Count == 0 ||
                 profile.OwnWeaponPrefabs.All(w => !w))
        {
            problem = $"{profile.name} has no weapon of its own";
        }

        if (problem != null)
        {
            Debug.LogError($"EnemyGenerator can't build a guard: {problem}.");
        }

        return problem == null;
    }

    // layout

    // True when the layout got at least one gun. Only trust layout when true.
    private static bool TryPlanLayout(
        StationProfile profile,
        int totalTiles,
        out Dictionary<Vector2Int, PlannedTile> layout)
    {
        int guns = Mathf.Max(1, totalTiles / profile.TilesPerGun);
        int thrusters = profile.ThrusterPrefab
            ? (totalTiles >= SmallShipTiles ? 2 : 1)
            : 0;
        int bodyTiles = Mathf.Max(0, totalTiles - 1 - guns - thrusters);

        layout = new Dictionary<Vector2Int, PlannedTile>();
        Place(layout, Vector2Int.zero, profile.CoreTilePrefab, TileRotation.Up);

        GrowBody(profile, layout, bodyTiles);
        bool hasGun = PlaceGuns(profile, layout, guns);
        PlaceThrusters(profile, layout, thrusters);
        return hasGun;
    }

    private static void GrowBody(
        StationProfile profile,
        Dictionary<Vector2Int, PlannedTile> layout,
        int count)
    {
        List<GameObject> squares = Usable(profile.BodyTilePrefabs);
        List<GameObject> corners = Usable(profile.CornerTilePrefabs);
        if (squares.Count == 0 && corners.Count == 0)
        {
            return;
        }

        int placed = 0;
        int failures = 0;
        while (placed < count && failures < 50)
        {
            bool useCorner = corners.Count > 0 &&
                             (squares.Count == 0 ||
                              Random.value < profile.CornerChance);
            GameObject prefab = useCorner ? Pick(corners) : Pick(squares);
            TileInfo info = InfoOf(prefab);

            // Build the right half (x >= 0) and mirror it to the left.
            // With one tile left, prefer the middle column so the ship stays even.
            // Square tiles stay upright so their art lines up; triangles turn to fit.
            bool onlyOneLeft = count - placed == 1;
            List<(Vector2Int cell, TileRotation rotation)> fits =
                FreeSpots(layout, info)
                    .Where(o => o.cell.x >= 0 &&
                                (useCorner || o.rotation == TileRotation.Up))
                    .ToList();
            List<(Vector2Int cell, TileRotation rotation)> options = onlyOneLeft
                ? fits.Where(o => o.cell.x == 0).ToList()
                : fits;
            if (options.Count == 0)
            {
                options = fits;
            }

            if (options.Count == 0)
            {
                failures++;
                continue;
            }

            (Vector2Int cell, TileRotation rotation) = Pick(options);
            Place(layout, cell, prefab, rotation);
            placed++;

            Vector2Int mirror = new(-cell.x, cell.y);
            TileRotation mirrorRotation
                = useCorner ? MirrorX(rotation) : rotation;
            if (cell.x != 0 && placed < count &&
                CanPlace(layout, mirror, info, mirrorRotation))
            {
                Place(layout, mirror, prefab, mirrorRotation);
                placed++;
            }
        }
    }

    // Guns point forward (up) and sit on top of a tile whose top edge is open.
    private static bool PlaceGuns(
        StationProfile profile,
        Dictionary<Vector2Int, PlannedTile> layout,
        int count)
    {
        List<GameObject> ownWeapons = Usable(profile.OwnWeaponPrefabs);
        List<GameObject> pool = profile.GetAvailableWeapons();
        int placed = 0;

        while (placed < count)
        {
            // The first gun is always this station's own new weapon.
            GameObject prefab = placed == 0 ? Pick(ownWeapons) : Pick(pool);
            TileInfo info = InfoOf(prefab);
            List<Vector2Int> spots = FreeSpots(layout, info)
                .Where(o => o.rotation == TileRotation.Up && o.cell.x >= 0)
                .Select(o => o.cell)
                .ToList();
            if (spots.Count == 0)
            {
                break;
            }

            // Prefer the front of the ship.
            int front = spots.Max(c => c.y);
            Vector2Int cell = Pick(spots.Where(c => c.y == front).ToList());
            Place(layout, cell, prefab, TileRotation.Up);
            placed++;

            Vector2Int mirror = new(-cell.x, cell.y);
            if (cell.x != 0 && placed < count &&
                CanPlace(layout, mirror, info, TileRotation.Up))
            {
                Place(layout, mirror, prefab, TileRotation.Up);
                placed++;
            }
        }

        return placed > 0;
    }

    // Thrusters face backward (down) and hang under a tile whose bottom edge is open.
    private static void PlaceThrusters(
        StationProfile profile,
        Dictionary<Vector2Int, PlannedTile> layout,
        int count)
    {
        if (count == 0)
        {
            return;
        }

        TileInfo info = InfoOf(profile.ThrusterPrefab);
        int placed = 0;
        while (placed < count)
        {
            List<Vector2Int> spots = FreeSpots(layout, info)
                .Where(o => o.rotation == TileRotation.Down && o.cell.x >= 0)
                .Select(o => o.cell)
                .ToList();
            if (spots.Count == 0)
            {
                return;
            }

            // Prefer the back of the ship; a pair beats a single middle thruster.
            int back = spots.Min(c => c.y);
            List<Vector2Int> backRow = spots.Where(c => c.y == back).ToList();
            List<Vector2Int> sides = backRow.Where(c => c.x > 0).ToList();
            Vector2Int cell = count - placed >= 2 && sides.Count > 0
                ? Pick(sides)
                : Pick(backRow);

            Place(layout, cell, profile.ThrusterPrefab, TileRotation.Down);
            placed++;

            Vector2Int mirror = new(-cell.x, cell.y);
            if (cell.x != 0 && placed < count &&
                CanPlace(layout, mirror, info, TileRotation.Down))
            {
                Place(layout, mirror, profile.ThrusterPrefab,
                    TileRotation.Down);
                placed++;
            }
        }
    }

    // the same edge rules as ShipManager.CanAttach

    private static IEnumerable<(Vector2Int cell, TileRotation rotation)>
        FreeSpots(Dictionary<Vector2Int, PlannedTile> layout, TileInfo info)
    {
        HashSet<Vector2Int> seen = new();
        foreach (Vector2Int occupied in layout.Keys.ToList())
        {
            foreach (TileRotation direction in AllDirections)
            {
                Vector2Int cell = occupied + direction.ToVector2Int();
                if (layout.ContainsKey(cell) || !seen.Add(cell))
                {
                    continue;
                }

                foreach (TileRotation rotation in AllDirections)
                {
                    if (CanPlace(layout, cell, info, rotation))
                    {
                        yield return (cell, rotation);
                    }
                }
            }
        }
    }

    private static bool CanPlace(
        Dictionary<Vector2Int, PlannedTile> layout,
        Vector2Int cell,
        TileInfo info,
        TileRotation rotation)
    {
        if (layout.ContainsKey(cell))
        {
            return false;
        }

        foreach (TileRotation direction in AllDirections)
        {
            if (EdgeOpen(info, rotation, direction) &&
                layout.TryGetValue(cell + direction.ToVector2Int(),
                    out PlannedTile neighbour) &&
                EdgeOpen(neighbour.Info, neighbour.Rotation,
                    direction.Opposite()))
            {
                return true;
            }
        }

        return false;
    }

    // Same maths as Tile.EdgeConnectable: turn the world direction back into the tile's own frame.
    private static bool EdgeOpen(
        TileInfo info,
        TileRotation rotation,
        TileRotation worldDirection) =>
        info.connectableEdges[worldDirection.Subtract(rotation).ToInt()];

    // Mirroring a triangle left/right: Up <-> Left and Down <-> Right.
    // (Square tiles keep their rotation when mirrored.)
    private static TileRotation MirrorX(TileRotation rotation) =>
        rotation switch
        {
            TileRotation.Up => TileRotation.Left,
            TileRotation.Left => TileRotation.Up,
            TileRotation.Down => TileRotation.Right,
            _ => TileRotation.Down,
        };

    // helpers

    private static void Place(
        Dictionary<Vector2Int, PlannedTile> layout,
        Vector2Int cell,
        GameObject prefab,
        TileRotation rotation)
    {
        layout[cell] = new PlannedTile
        {
            Prefab = prefab, Info = InfoOf(prefab), Rotation = rotation,
        };
    }

    private static TileInfo InfoOf(GameObject prefab) =>
        prefab.GetComponent<Tile>().TileInfo;

    private static List<GameObject> Usable(IEnumerable<GameObject> prefabs) =>
        prefabs == null
            ? new List<GameObject>()
            : prefabs.Where(p => p && p.GetComponent<Tile>()).ToList();

    private static T Pick<T>(IReadOnlyList<T> list) =>
        list[Random.Range(0, list.Count)];

    private sealed class PlannedTile
    {
        public TileInfo Info;
        public GameObject Prefab;
        public TileRotation Rotation;
    }
}
}
