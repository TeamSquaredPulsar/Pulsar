using UnityEngine;
using Pulsar.Ship;

public class GhostPreview : MonoBehaviour
{
    private SpriteRenderer _sr;
    private static readonly Color ValidColor = new(0f, 1f, 0f, 0.4f);
    private static readonly Color InvalidColor = new(1f, 0f, 0f, 0.4f);
    private void Awake()
    {
        _sr = Tile.CreateVisual(transform);
        _sr.sortingOrder = 100;
        gameObject.SetActive(false);
    }
    public void Show(Sprite sprite, Vector3 worldPos, bool valid, Quaternion rotation)
    {
        gameObject.SetActive(true);
        _sr.sprite = sprite;
        _sr.color = valid ? ValidColor : InvalidColor;
        transform.SetPositionAndRotation(worldPos, rotation);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}