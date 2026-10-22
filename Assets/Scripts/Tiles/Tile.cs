using System;
using Nrjwolf.Tools.AttachAttributes;
using Pulsar.Ship.Extensions;
using Pulsar.Utilities;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D),
    typeof(SpriteRenderer),
    typeof(FixedJoint2D))]
[RequireComponent(typeof(Health))]
public abstract class Tile : MonoBehaviour
{
    private const int FloatingSortingOrder = 10;

    [SerializeField]
    private Color baseColor = Color.white;

    public Vector2Int Cell { get; private set; }

    public float CurrentHp { get; protected set; }

    // 0 1 2 3 = 0 90 180 270 (counterclockwise)
    public TileRotationEnum Rotation { get; private set; }

    [field:SerializeField]
    public bool IsAttached { get; private set; }

    [field: SerializeField]
    public TileInfoSO TileInfo { get; private set; }

    [GetComponent]
    [SerializeField]
    private FixedJoint2D joint;

    [GetComponent]
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [GetComponent]
    [SerializeField]
    private BoxCollider2D tileCollider;

    [GetComponent]
    [SerializeField]
    private Rigidbody2D rigidBody;

    [GetComponent]
    [SerializeField]
    private Health health;

    public event Action<Tile> DestroyEvent;

    public void Start()
    {
        health.DestroyEvent += OnDead;
    }

    public void OnDestroy()
    {
        health.DestroyEvent -= OnDead;
    }

    private void OnDead(Health _)
    {
        DestroyEvent?.Invoke(this);
    }

    protected void Init(TileInfoSO tileInfoSo, Vector2Int gridCell, int rot = 0)
    {
        TileInfo = tileInfoSo;
        CurrentHp = tileInfoSo.hp;
        Rotation = rot.ToRotationEnum();
        Cell = gridCell;

        // spriteRenderer = CreateVisual(transform);

        spriteRenderer.sprite = tileInfoSo.sprite;

        tileCollider.size
            = Vector2.one;
        tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

        transform.localPosition = ShipUtilities.GridToLocal(gridCell);
        transform.localRotation = ShipUtilities.RotateQuarterOnZAxis(rot);
    }

    public void TakeDamage(float damage) => health.TakeDamage(damage);

    // protected void InitFloating(
    //     TileInfoSO tileInfoSo,
    //     Vector3 position,
    //     Vector2 velocity)
    // {
    //     TileInfo = tileInfoSo;
    //     CurrentHp = tileInfoSo.hp;
    //     Rotation = 0;
    //
    //     spriteRenderer = CreateVisual(transform);
    //
    //     spriteRenderer.sprite = tileInfoSo.sprite;
    //
    //     // floatRb = GetComponent<Rigidbody2D>();
    //     tileCollider = GetComponent<BoxCollider2D>();
    //     tileCollider.isTrigger = true;
    //     tileCollider.size
    //         = Vector2.one;
    //
    //     transform.position = position;
    //     ReleaseToFloating(velocity,
    //         Random.Range(-45f, 45f));
    // }
    //
    // public void AttachTo(ShipManager manager, Vector2Int gridCell, int rot)
    // {
    //     // floatRb.bodyType = RigidbodyType2D.Static;
    //
    //     Cell = gridCell;
    //     Rotation = rot;
    //     transform.SetParent(manager.transform, false);
    //     transform.localPosition = ShipUtilities.GridToLocal(gridCell);
    //     transform.localRotation = ShipUtilities.RotateQuarterOnZAxis(rot);
    //     tileCollider.isTrigger = false;
    //     spriteRenderer.sortingOrder = 0;
    //     Unhighlight();
    // }
    //
    public void DisableBuildControl(Vector2 velocity, float angularVelocity)
    {
        transform.SetParent(null,
            true);
        tileCollider.isTrigger
            = true;
        spriteRenderer.sortingOrder = FloatingSortingOrder;
        Unhighlight();
        EnableFloatingPhysics(velocity, angularVelocity);
    }

    public void BeginBuildControl() => rigidBody.SetBuildRb();

    private void EnableFloatingPhysics(
        Vector2 velocity,
        float angularVelocity)
    { 
        rigidBody.SetFloatRb(velocity, angularVelocity);
        joint.enableCollision = true;
    }

    //To ask whether the rotated tile can connect from dir
    /*
     * example: tile is true:0 false:1 false:2 false:3 (allow connections from top only)
     * tile.Rotation = 1 -> tile now allows connections from left
     * pass EdgeConnectable(3) -> (3+1)%4==0 -> true
     * => can connect
     */
    public bool EdgeConnectable(TileRotationEnum dir)
    {
        TileRotationEnum localDir = dir.Add(Rotation);
        return TileInfo.connectableEdges[localDir.ToInt()];
    }

    public void Select(Color highlightColor)
    {
        Highlight(highlightColor);
    }

    public void Deselect()
    {
        Unhighlight();
        EnableFloatingPhysics(Vector2.zero, 0f);
    }

    public void Attach(bool isAttached, Rigidbody2D rb)
    {
        IsAttached = isAttached;
        joint.enableCollision = !isAttached;
        if (isAttached)
        {
            joint.connectedBody = rb;
        }
    }

    private void Highlight(Color tint)
    {
        spriteRenderer.color = tint;
    }

    private void Unhighlight()
    {
        spriteRenderer.color = baseColor;
    }

    #region FACTORY

    // public static Tile CreateTile<T>(
    //     TileInfoSO info,
    //     Vector2Int gridCell,
    //     int rot = 0
    // ) where T : Tile
    // {
    //     GameObject go = new($"Tile_{info.tileName}");
    //     Tile tile = go.AddComponent<T>();
    //     tile.Init(info, gridCell, rot);
    //     return tile;
    // }

    // public static Tile SpawnFloating<T>(
    //     TileInfoSO info,
    //     Vector3 position,
    //     Vector2 velocity
    // ) where T : Tile
    // {
    //     GameObject go = new($"Tile_{info.tileName}_floating");
    //     Tile tile = go.AddComponent<T>();
    //     tile.InitFloating(info, position, velocity);
    //     return tile;
    // }

    // public static SpriteRenderer CreateVisual(
    //     Transform parent,
    //     int sortingOrder = 0)
    // {
    //     GameObject visual = new("Visual");
    //     visual.transform.SetParent(parent, false);
    //     visual.transform.localRotation = Quaternion.identity;
    //     SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
    //     renderer.sortingOrder = sortingOrder;
    //     return renderer;
    // }

    #endregion
}