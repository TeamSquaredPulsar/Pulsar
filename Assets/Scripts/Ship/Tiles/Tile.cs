using System;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D),
    typeof(SpriteRenderer))]
public abstract class Tile : MonoBehaviour
{
    public static TileRotationEnum GetRotationInt(int rot) =>
        (TileRotationEnum)(rot % Enum.GetNames(typeof(TileRotationEnum))
            .Length);

    private const int FloatingSortingOrder = 10;

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

    // 0 1 2 3 = 0 90 180 270 (counterclockwise)
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
        tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

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
            true);
        IsAttached = false;
        tileCollider.isTrigger
            = true;
        spriteRenderer.sortingOrder = FloatingSortingOrder;
        Unhighlight();
        EnableFloatingPhysics(velocity, angularVelocity);
    }

    public void BeginBuildControl()
    {
        floatRb.linearVelocity = Vector2.zero;
        floatRb.angularVelocity = 0f;
        floatRb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void EnableFloatingPhysics(
        Vector2 velocity,
        float angularVelocity)
    {
        ShipUtilities.FloatingTileRbConstrain(floatRb);
        floatRb.bodyType = RigidbodyType2D.Dynamic;
        floatRb.linearVelocity = velocity;
        floatRb.angularVelocity = angularVelocity;
    }

    //To ask whether the rotated tile can connect from dir
    /*
     * example: tile is true:0 false:1 false:2 false:3 (allow connect from top only)
     * tile.Rotation = 1 -> tile now allows connect from left
     * pass EdgeConnectable(3) -> (3+1)%4==0 -> true
     * => can connect
     */
    public bool EdgeConnectable(int dir)
    {
        int localDir = (dir + Rotation) % 4;
        return TileInfo.connectableEdges[localDir];
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
                manager.DestroyTileAt(Cell);
            }
        }
    }

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