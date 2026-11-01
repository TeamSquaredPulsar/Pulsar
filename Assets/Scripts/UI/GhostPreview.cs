using UnityEngine;

namespace Pulsar.UI
{

[RequireComponent(typeof(SpriteRenderer))]
public class GhostPreview : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer sr;

    [SerializeField]
    private int sortingOrder;

    [SerializeField]
    private Color ghostColor = new(0f, 1f, 0f, 0.4f);

    public bool IsShowing => sr.sprite != null;

    private void Awake()
    {
        sr.sortingOrder = sortingOrder;
    }

    public void Show(Sprite sprite, Vector3 worldPos, Quaternion rotation)
    {
        sr.sprite = sprite;
        sr.color = ghostColor;
        transform.SetPositionAndRotation(worldPos, rotation);
        sr.enabled = true;
    }

    public void Hide()
    {
        sr.sprite = null;
    }
}
}