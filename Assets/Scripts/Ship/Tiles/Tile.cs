using UnityEngine;

namespace Pulsar.Ship
{
[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public class Tile : MonoBehaviour
{
    [field: SerializeField]
    public TileInfoSO TileInfo { get; private set; }

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private BoxCollider2D tileCollider;

    [SerializeField]
    private Rigidbody2D floatRb;

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

        floatRb = GetComponent<Rigidbody2D>();
        tileCollider = GetComponent<BoxCollider2D>();
        tileCollider.size
            = Vector2.one;

        transform.localPosition = ShipUtilities.GridToLocal(gridCell);
        transform.localRotation = ShipUtilities.RotateQuarterOnZAxis(rot);
    }

    protected void InitFloating(
        TileInfoSO tileInfoSo,
        Vector3 position,
        Vector2 velocity)
    {
        TileInfo = tileInfoSo;
        CurrentHp = tileInfoSo.hp;
        Rotation = 0;

        spriteRenderer = CreateVisual(transform);

        spriteRenderer.sprite = tileInfoSo.sprite;
        
        floatRb = GetComponent<Rigidbody2D>();
        tileCollider = GetComponent<BoxCollider2D>();
        tileCollider.isTrigger = true;
        tileCollider.size
            = Vector2.one;

        transform.position = position;
        ReleaseToFloating(velocity,
            Random.Range(-45f, 45f));
    }

    public void AttachTo(ShipManager manager, Vector2Int gridCell, int rot)
    {
        floatRb.bodyType = RigidbodyType2D.Static;

        Cell = gridCell;
        Rotation = rot;
        transform.SetParent(manager.transform, false);
        transform.localPosition = ShipUtilities.GridToLocal(gridCell);
        transform.localRotation = ShipUtilities.RotateQuarterOnZAxis(rot);
        tileCollider.isTrigger = false;
        spriteRenderer.sortingOrder = 0;
        Unhighlight();
    }

    public void ReleaseToFloating(Vector2 velocity, float angularVelocity)
    {
        transform.SetParent(null,
            true); // preserve world position and orientation
        IsAttached = false;
        tileCollider.isTrigger
            = true; // pass through ship, no physical collision
        spriteRenderer.sortingOrder = 10;
        Unhighlight();
        EnableFloatingPhysics(velocity, angularVelocity);
    }

    private void EnableFloatingPhysics(
        Vector2 velocity,
        float angularVelocity)
    {
        ShipUtilities.Constrain(floatRb);
        floatRb.bodyType = RigidbodyType2D.Dynamic;
        floatRb.linearVelocity = velocity;
        floatRb.angularVelocity = angularVelocity;
    }

    public bool EdgeConnectable(int gridDir)
    {
        int localDir = (gridDir + Rotation) % 4;
        return TileInfo != null && TileInfo.connectableEdges[localDir];
    }

    public virtual void SetAttached(ShipManager manager)
    {
        IsAttached = true;
    }

    public virtual void SetDetached(ShipManager manager)
    {
        IsAttached = false;
    }

    public virtual void TakeDamage(float amount)
    {
        CurrentHp -= amount;
        if (CurrentHp <= 0f)
        {
            ShipManager manager = GetComponentInParent<ShipManager>();
            if (manager != null)
            {
                manager.DestroyTile(Cell);
            }
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

    public static Tile SpawnFloating<T>(
        TileInfoSO info,
        Vector3 position,
        Vector2 velocity
    ) where T : Tile
    {
        GameObject go = new($"Tile_{info.tileName}_floating");
        Tile tile = go.AddComponent<T>();
        tile.InitFloating(info, position, velocity);
        return tile;
    }

    public static SpriteRenderer CreateVisual(
        Transform parent,
        int sortingOrder = 0)
    {
        GameObject visual = new("Visual");
        visual.transform.SetParent(parent, false);
        visual.transform.localRotation = Quaternion.identity;
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    #endregion
}
}
