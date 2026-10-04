using UnityEngine;

namespace Pulsar.Ship
{
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider),
    typeof(Rigidbody))]
public class Tile : MonoBehaviour
{
    [field: SerializeField]
    public TileInfoSO TileInfo { get; private set; }

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private BoxCollider tileCollider;

    [SerializeField]
    private Rigidbody floatRb;

    [SerializeField]
    private Color baseColor = Color.white;

    public Vector2Int Cell { get; private set; }
    public float CurrentHp { get; protected set; }
    public int Rotation { get; private set; }

        public bool IsAttached { get; private set; }

        protected void Init(TileInfoSO tileInfoSo, Vector2Int gridCell, int rot = 0)
        {
            TileInfo = tileInfoSo;
            CurrentHp = tileInfoSo.hp;
            Rotation = rot;
            Cell = gridCell;

        spriteRenderer = CreateVisual(transform);

        spriteRenderer.sprite = tileInfoSo.sprite;

        tileCollider = gameObject.AddComponent<BoxCollider>();
        tileCollider.size
            = new Vector3(1f, ShipUtilities.TileColliderHeight, 1f);

            transform.localPosition = ShipUtilities.GridToLocal(gridCell);
            transform.localRotation = ShipUtilities.RotateQuarterOnYAxis(rot);
        }
        
        protected void InitFloating(TileInfoSO tileInfoSo, Vector3 position, Vector3 velocity)
        {
            TileInfo = tileInfoSo;
            CurrentHp = tileInfoSo.hp;
            Rotation = 0;

        spriteRenderer = CreateVisual(transform);

        spriteRenderer.sprite = tileInfoSo.sprite;

        tileCollider = gameObject.AddComponent<BoxCollider>();
        tileCollider.isTrigger = true;
        tileCollider.size
            = new Vector3(1f, ShipUtilities.TileColliderHeight, 1f);

            transform.position = position;
            ReleaseToFloating(velocity, Vector3.up * (Random.Range(-45f, 45f) * Mathf.Deg2Rad));
        }

    public void AttachTo(ShipGrid grid, Vector2Int gridCell, int rot)
    {
        floatRb.linearVelocity = Vector3.zero;
        floatRb.angularVelocity = Vector3.zero;
        floatRb.isKinematic = true;
        floatRb.detectCollisions = false;
        // Destroy(_floatRb);
        // _floatRb = null;


        Cell = gridCell;
        Rotation = rot;
        transform.SetParent(grid.transform, false);
        transform.localPosition = ShipUtilities.GridToLocal(gridCell);
        transform.localRotation = ShipUtilities.RotateQuarterOnYAxis(rot);
        tileCollider.isTrigger = false;
        spriteRenderer.sortingOrder = 0;
        Unhighlight();
    }

        public void ReleaseToFloating(Vector3 velocity, Vector3 angularVelocity)
        {
            transform.SetParent(null, true); // preserve world position and orientation
            IsAttached = false;
            _collider.isTrigger = true; // pass through ship, no physical collision
            _spriteRenderer.sortingOrder = 10;
            Unhighlight();
            EnableFloatingPhysics(velocity, angularVelocity);
        }

    private void EnableFloatingPhysics(
        Vector3 velocity,
        Vector3 angularVelocity)
    {
        ShipUtilities.Constrain(floatRb);
        floatRb.isKinematic = false;
        floatRb.detectCollisions = true;
        floatRb.linearDamping = 0.05f;
        floatRb.angularDamping = 0.1f;
        floatRb.linearVelocity = new Vector3(velocity.x, 0f, velocity.z);
        floatRb.angularVelocity = Vector3.up * angularVelocity.y;
        floatRb.maxAngularVelocity = Mathf.Max(floatRb.maxAngularVelocity,
            Mathf.Abs(angularVelocity.y));
    }

        public bool EdgeConnectable(int gridDir)
        {
            int localDir = (gridDir + Rotation) % 4;
            return TileInfo != null && TileInfo.connectableEdges[localDir];
        }
        
        public virtual void SetAttached(ShipGrid grid)
        {
            IsAttached = true;
        }

        public virtual void SetDetached(ShipGrid grid)
        {
            IsAttached = false;
        }

        public virtual void TakeDamage(float amount)
        {
            CurrentHp -= amount;
            if (CurrentHp <= 0f)
            {
                ShipGrid grid = GetComponentInParent<ShipGrid>();
                if (grid != null) grid.DestroyTile(Cell);
            }
        }

    // Highlight on radial menu hover 
    public void Highlight(Color tint)
    {
        spriteRenderer.color = tint;
    }

    public void Unhighlight()
    {
        spriteRenderer.color = baseColor;
    }

        #region FACTORY

    public static Tile CreateTile<T>(
        TileInfoSO info,
        Vector2Int gridCell,
        int rot = 0
    ) where T : Tile
    {
        GameObject go = new($"Tile_{info.tileName}");
        Tile tile = go.AddComponent<T>();
        tile.Init(info, gridCell, rot);
        return tile;
    }

        public static Tile SpawnFloating(TileInfoSO info, Vector3 position, Vector3 velocity)
        {
            GameObject go = new GameObject($"Tile_{info.tileName}_floating");
            Tile tile = null;
            switch (info.type)
            {
                case TileType.Core:     tile = go.AddComponent<CoreTile>(); break;
                case TileType.Chassis:  tile = go.AddComponent<ChassisTile>(); break;
                case TileType.Thruster: tile = go.AddComponent<ThrusterTile>(); break;
                case TileType.Weapon:   tile = go.AddComponent<GunTile>(); break;
                default:                tile = go.AddComponent<Tile>(); break;
            }
            tile.InitFloating(info, position, velocity);
            return tile;
        }
        
        public static SpriteRenderer CreateVisual(Transform parent, int sortingOrder = 0)
        {
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(parent, false);
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
        #endregion
    }
    public static Tile SpawnFloating<T>(
        TileInfoSO info,
        Vector3 position,
        Vector3 velocity
    ) where T : Tile
    {
        GameObject go = new($"Tile_{info.tileName}_floating");
        Tile tile = go.AddComponent<T>();
        tile.InitFloating(info, position, velocity);
        return tile;
    }

}
