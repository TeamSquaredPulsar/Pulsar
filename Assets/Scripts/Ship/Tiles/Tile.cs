using UnityEngine;

namespace Pulsar.Ship
{
    public class Tile : MonoBehaviour
    {
        public TileInfoSO TileInfo { get; private set; }
        public Vector2Int Cell { get; private set; }
        public float CurrentHp { get; protected set; }
        public int Rotation { get; private set; } 

        private SpriteRenderer _spriteRenderer;
        private BoxCollider _collider;

        private Rigidbody _floatRb;
        private Color _baseColor = Color.white;

        public bool IsAttached { get; private set; }

        protected void Init(TileInfoSO tileInfoSo, Vector2Int gridCell, int rot = 0)
        {
            TileInfo = tileInfoSo;
            CurrentHp = tileInfoSo.hp;
            Rotation = rot;
            Cell = gridCell;

            _spriteRenderer = CreateVisual(transform);

            _spriteRenderer.sprite = tileInfoSo.sprite;
            
            _collider = gameObject.AddComponent<BoxCollider>();
            _collider.size = new Vector3(1f, ShipUtilities.TileColliderHeight, 1f);

            transform.localPosition = ShipUtilities.GridToLocal(gridCell);
            transform.localRotation = ShipUtilities.RotateQuarterOnYAxis(rot);
        }
        
        protected void InitFloating(TileInfoSO tileInfoSo, Vector3 position, Vector3 velocity)
        {
            TileInfo = tileInfoSo;
            CurrentHp = tileInfoSo.hp;
            Rotation = 0;

            _spriteRenderer = CreateVisual(transform);

            _spriteRenderer.sprite = tileInfoSo.sprite;

            _collider = gameObject.AddComponent<BoxCollider>();
            _collider.isTrigger = true;
            _collider.size = new Vector3(1f, ShipUtilities.TileColliderHeight, 1f);

            transform.position = position;
            ReleaseToFloating(velocity, Vector3.up * (Random.Range(-45f, 45f) * Mathf.Deg2Rad));
        }

        public void AttachTo(ShipGrid grid, Vector2Int gridCell, int rot)
        {
            if (_floatRb != null)
            {
                _floatRb.linearVelocity = Vector3.zero;
                _floatRb.angularVelocity = Vector3.zero;
                _floatRb.isKinematic = true;
                _floatRb.detectCollisions = false;
                // Destroy(_floatRb);
                // _floatRb = null;
            }

            Cell = gridCell;
            Rotation = rot;
            transform.SetParent(grid.transform, false);
            transform.localPosition = ShipUtilities.GridToLocal(gridCell);
            transform.localRotation = ShipUtilities.RotateQuarterOnYAxis(rot);
            _collider.isTrigger = false;
            _spriteRenderer.sortingOrder = 0;
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

        private void EnableFloatingPhysics(Vector3 velocity, Vector3 angularVelocity)
        {
            if (_floatRb == null) _floatRb = gameObject.AddComponent<Rigidbody>();
            ShipUtilities.Constrain(_floatRb);
            _floatRb.isKinematic = false;
            _floatRb.detectCollisions = true;
            _floatRb.linearDamping = 0.05f;
            _floatRb.angularDamping = 0.1f;
            _floatRb.linearVelocity = new Vector3(velocity.x, 0f, velocity.z);
            _floatRb.angularVelocity = Vector3.up * angularVelocity.y;
            _floatRb.maxAngularVelocity = Mathf.Max(_floatRb.maxAngularVelocity, Mathf.Abs(angularVelocity.y));
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
            if (_spriteRenderer != null) _spriteRenderer.color = tint;
        }

        public void Unhighlight()
        {
            if (_spriteRenderer != null) _spriteRenderer.color = _baseColor;
        }

        #region FACTORY

        

        public static Tile CreateTile(TileInfoSO tileInfoSo, Vector2Int gridCell, int rot = 0)
        {
            TileType type = tileInfoSo.type;
            GameObject go = new GameObject();
            Tile tile = null;
            switch (type)
            {
                case TileType.Core:     tile = go.AddComponent<CoreTile>(); break;
                case TileType.Chassis:  tile = go.AddComponent<ChassisTile>(); break;
                case TileType.Thruster: tile = go.AddComponent<ThrusterTile>(); break;
                case TileType.Weapon:   tile = go.AddComponent<GunTile>(); break;
                default:                tile = go.AddComponent<Tile>(); break;
            }
            tile.Init(tileInfoSo, gridCell, rot);
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
}
