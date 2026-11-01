using System;
using Pulsar.Interactions;
using Pulsar.Ship.Extensions;
using UnityEngine;

public struct TileRbInfo
{
    public float LinearDamping;
    public float AngularDamping;
    public float Mass;
}

[RequireComponent(typeof(BoxCollider2D),
     typeof(SpriteRenderer),
     typeof(Health)),]
public abstract class Tile : MonoBehaviour
{
    public event Action<Tile> DestroyEvent;

    [SerializeField]
    private Color baseColor = Color.white;

    public Vector2Int Cell { get; private set; }

    public float CurrentHp => health.CurrentHealth;

    // 0 1 2 3 = 0 90 180 270 (counterclockwise)
    public TileRotation Rotation { get; private set; }

    [field: SerializeField]
    public bool IsAttached { get; private set; }

    [field: SerializeField]
    public TileInfoSO TileInfo { get; private set; }

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private BoxCollider2D tileCollider;

    [SerializeField]
    private Rigidbody2D rigidBody;

    [SerializeField]
    private Health health;

    private const int FloatingSortingOrder = 10;

    private TileRbInfo _rbInfo;

    public Sprite Sprite => spriteRenderer.sprite;

    private void Awake()
    {
        //Initializing in awake to stop the chance of it being destroyed before caches
        _rbInfo = new TileRbInfo
        {
            AngularDamping = rigidBody.angularDamping,
            LinearDamping = rigidBody.linearDamping,
            Mass = rigidBody.mass,
        };
    }
    
    public void Start()
    {
        health.DestroyEvent += OnDead;
        health.HealthStateChanged += OnHealthStateChanged;
    }

    public void OnDestroy()
    {
        health.DestroyEvent -= OnDead;
        health.HealthStateChanged -= OnHealthStateChanged;
    }

    private void OnDead(Health _)
    {
        DestroyEvent?.Invoke(this);
        Destroy(gameObject);
    }
    
    private void OnHealthStateChanged(HealthState state)
    {
        UpdateSprite(state);
    }
    
    private void UpdateSprite(HealthState state)
    {
        spriteRenderer.sprite = state switch
        {
            HealthState.FullHealth => TileInfo.fullHealthSprite,
            HealthState.LessOrEqualTo75Percent => TileInfo.seventyFivePercentSprite,
            HealthState.LessOrEqualTo50Percent => TileInfo.fiftyPercentSprite,
            HealthState.LessOrEqualTo25Percent => TileInfo.twentyFivePercentSprite,
            HealthState.LessOrEqualTo10Percent => TileInfo.tenPercentSprite,
            _ => spriteRenderer.sprite
        };
    }

    public float Mass => _rbInfo.Mass; //It can return mass even when destroyed

    public void TakeDamage(float damage)
    {
        health.TakeDamage(damage);
    }

    /// <summary>
    /// To ask whether the rotated tile can connect from dir
    ///  example: tile is true:0 false:1 false:2 false:3 (allow connections from top only)
    ///  tile.Rotation = 1 -> tile now allows connections from left
    ///  pass EdgeConnectable(3) -> (3+1)%4==0 -> true => can connect
    /// </summary>
    /// <param name="dir"></param>
    /// <returns></returns>
    public bool EdgeConnectable(TileRotation dir)
    {
        TileRotation localDir = dir.Subtract(Rotation);
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
        transform.SetParent(null, true);
    }

    public void Attach(Vector2Int cell, TileRotation rotation)
    {
        IsAttached = true;
        
        Cell = cell;
        Rotation = rotation;
        
        // [CB] Unfortunately we need to destroy the rigidbodies in order for the
        // collider to merge with the parents collider properly.
        if (rigidBody)
        {
            Destroy(rigidBody);
            rigidBody = null;
        }
    }

    public void Detatch(Vector2? velocity = null, float angularVelocity = 0f)
    {
        IsAttached = false;
        transform.SetParent(null);
        
        Vector2 selectedVel = velocity ?? Vector2.zero;
        if (!rigidBody)
        {
            rigidBody = gameObject.AddComponent<Rigidbody2D>();
        }

        rigidBody.SetFloatRb(selectedVel, angularVelocity, _rbInfo);
    }

    private void Highlight(Color tint)
    {
        spriteRenderer.color = tint;
    }

    private void Unhighlight()
    {
        spriteRenderer.color = baseColor;
    }

    public void Rotate(Quaternion rotation)
    {
        /*
         transform.SetLocalPositionAndRotation(transform.localPosition, rotation);
        // transform.SetPositionAndRotation(this.transform.position, rotation);
        Rotation = transform.localRotation.eulerAngles.z.ToTileRotation();
        */
        
        transform.rotation = rotation;
        Rotation = transform.localRotation.eulerAngles.z.ToTileRotation();
    }
}