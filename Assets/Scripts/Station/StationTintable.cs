using UnityEngine;

namespace Pulsar.Stations
{
/// <summary>
/// Marks a station colour tile (hull, armour, window, heat slit, triangle,
/// core) that takes its station's colour. Only tiles with this component are
/// tinted, so solar panels, guns and thrusters never are.
/// The colour goes to the "Pulsar/Station Tint Lit" shader through a
/// MaterialPropertyBlock, and only lands on the pixels the sprite's _TintMask
/// marks. The SpriteRenderer's own Color is left alone, because Tile uses it
/// for the selection highlight.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class StationTintable : MonoBehaviour
{
    private static readonly int StationTintId = Shader.PropertyToID("_StationTint");

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    private MaterialPropertyBlock _block;

    public Color Tint { get; private set; } = Color.white;

    private void Awake()
    {
        if (!spriteRenderer)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void SetTint(Color tint)
    {
        if (!spriteRenderer)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        Tint = tint;
        _block ??= new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(_block);
        _block.SetColor(StationTintId, tint);
        spriteRenderer.SetPropertyBlock(_block);
    }

    // Tints every station colour tile under a ship (or a station).
    public static void TintAll(GameObject ship, Color tint)
    {
        foreach (StationTintable tile in ship.GetComponentsInChildren<StationTintable>(true))
        {
            tile.SetTint(tint);
        }
    }
}
}
