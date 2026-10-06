using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class Tile : MonoBehaviour
{
    private static Color _errorColor = Color.rebeccaPurple;

    [SerializeField]
    protected SpriteRenderer spriteRenderer;

    [SerializeField]
    protected BoxCollider2D boxCollider;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.color = _errorColor;
        }

        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider2D>();
        }

        // Ensure that all Tiles use a Composite Collider. This makes it
        // so any new tiles are automatically added to the Ship collider.        
        boxCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
    }
}