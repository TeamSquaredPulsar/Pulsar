using System;
using FMODUnity;
using NaughtyAttributes;
using Pulsar.Audio;
using Pulsar.DataClasses;
using Pulsar.Interactions;
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

    [Header("Basic Info")]
    [SerializeField]
    private Color baseColor = Color.white;

    public Vector2Int Cell { get; private set; }

    // 0 1 2 3 = 0 90 180 270 (counterclockwise)
    public TileRotation Rotation { get; private set; }

    [field: SerializeField]
    public bool IsAttached { get; private set; }

    [field: SerializeField]
    public TileInfo TileInfo { get; private set; }

    [SerializeField]
    protected BoxCollider2D tileCollider;

    [SerializeField]
    private Rigidbody2D rigidBody;

    [Header("Health Info")]
    [SerializeField]
    protected Health health;

    public float CurrentHp => health.CurrentHealth;

    private TileRbInfo _rbInfo;

    [Header("Rendering")]
    [SerializeField]
    private TileSprites spritesOverride;

    [SerializeField]
    protected SpriteRenderer spriteRenderer;

    private TileSprites Sprites =>
        spritesOverride ? spritesOverride : TileInfo.sprites;

    public Sprite Sprite => spriteRenderer.sprite;
    private const int k_FloatingSortingOrder = 10;

    [BoxGroup("Audio")]
    [SerializeField]
    protected EventReference damageStateChangeSound;

    [BoxGroup("Audio")]
    [SerializeField]
    protected EventReference onHitSound;

    protected virtual void Awake()
    {
        //Initializing in awake to stop the chance of it being destroyed before caches
        _rbInfo = new TileRbInfo
        {
            AngularDamping = rigidBody.angularDamping,
            LinearDamping = rigidBody.linearDamping,
            Mass = rigidBody.mass,
        };
    }

    protected virtual void Start()
    {
        health.DestroyEvent += OnDead;
        health.HealthStateChanged += OnHealthStateChanged;
        UpdateSprite(HealthState.FullHealth);
    }

    protected virtual void OnDestroy()
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
        AudioManager.Instance.PlayOneShot(
            FmodEvents.Instance.ShipReduceState,
            transform.position
        );
        UpdateSprite(state);
    }

    private void UpdateSprite(HealthState state)
    {
        Sprite next = Sprites ? Sprites.Get(state) : null;

        if (next)
        {
            spriteRenderer.sprite = next;
        }
    }

    public float Mass => _rbInfo.Mass; //It can return mass even when destroyed

    public void TakeDamage(float damage)
    {
        AudioManager.Instance.PlayOneShot(
            FmodEvents.Instance.ShipHit,
            transform.position);
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
        spriteRenderer.sortingOrder = k_FloatingSortingOrder;
        rigidBody.SetFloatRb(Vector2.zero, 0f, _rbInfo);
        tileCollider.enabled = true;
        transform.SetParent(null, true);
    }

    public virtual void Attach(Vector2Int cell, TileRotation rotation)
    {
        IsAttached = true;

        Cell = cell;
        Rotation = rotation;

        // [CB] Unfortunately we need to destroy the rigidbodies in order for the
        // collider to merge with the parents collider properly.
        if (rigidBody)
        {
            rigidBody.simulated = false;
            Destroy(rigidBody);
            rigidBody = null;
        }
    }

    public virtual void Detatch(
        Vector2? velocity = null,
        float angularVelocity = 0f)
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
        transform.rotation = rotation;
        Rotation = transform.localRotation.eulerAngles.z.ToTileRotation();
    }
}
