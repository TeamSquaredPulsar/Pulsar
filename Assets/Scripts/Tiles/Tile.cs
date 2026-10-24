using System;
using Nrjwolf.Tools.AttachAttributes;
using Pulsar.Ship.Extensions;
using UnityEngine;

public struct TileRbInfo
{
    public float LinearDamping;
    public float AngularDamping;
}

[RequireComponent(typeof(BoxCollider2D),
    typeof(SpriteRenderer),
    typeof(Health))]
[RequireComponent(typeof(Rigidbody2D))]
public abstract class Tile : MonoBehaviour
{
    public event Action<Tile> DestroyEvent;

    [SerializeField]
    private Color baseColor = Color.white;

    public Vector2Int Cell { get; set; }

    public float CurrentHp { get; protected set; }

    // 0 1 2 3 = 0 90 180 270 (counterclockwise)
    public TileRotation Rotation { get; private set; }

    [field: SerializeField]
    public bool IsAttached { get; private set; }

    [field: SerializeField]
    public TileInfoSO TileInfo { get; private set; }

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

    private const int FloatingSortingOrder = 10;

    private TileRbInfo _rbInfo;

    public Sprite Sprite => spriteRenderer.sprite;

    public void Start()
    {
        _rbInfo = new TileRbInfo
        {
            AngularDamping = rigidBody.angularDamping,
            LinearDamping = rigidBody.linearDamping,
        };

        health.DestroyEvent += OnDead;
    }

    public void OnDestroy()
    {
        health.DestroyEvent -= OnDead;
    }

    private void OnDead(Health _)
    {
        DestroyEvent?.Invoke(this);
        Destroy(gameObject);
    }
    
    public float Mass => rigidBody.mass;

    public void TakeDamage(float damage) => health.TakeDamage(damage);

    //To ask whether the rotated tile can connect from dir
    /*
     * example: tile is true:0 false:1 false:2 false:3 (allow connections from top only)
     * tile.Rotation = 1 -> tile now allows connections from left
     * pass EdgeConnectable(3) -> (3+1)%4==0 -> true => can connect
     */
    public bool EdgeConnectable(TileRotation dir)
    {
        TileRotation localDir = dir.Add(Rotation);
        return TileInfo.connectableEdges[localDir.ToInt()];
    }

    public void Select(Color highlightColor)
    {
        Highlight(highlightColor);
        rigidBody.SetBuildRb();
        tileCollider.enabled = false;
    }

    public void Deselect()
    {
        Unhighlight();
        spriteRenderer.sortingOrder = FloatingSortingOrder;
        rigidBody.SetFloatRb(Vector2.zero, 0f, _rbInfo);
        tileCollider.enabled = true;
    }

    public void Attach(bool isAttached, Rigidbody2D rb, Vector2? velocity = null, float angularVelocity = 0f)
    {
        IsAttached = isAttached;
        if (isAttached)
        {
            Debug.Log($"Attached {name}");
            rigidBody = rigidBody.SetBuildRb();
        }
        else
        {
            transform.SetParent(null);
            Vector2 selectedVel = velocity ?? new Vector2(0, 0);
            rigidBody.SetFloatRb(selectedVel, angularVelocity, _rbInfo);
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
}