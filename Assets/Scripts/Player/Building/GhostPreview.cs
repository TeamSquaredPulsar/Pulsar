using Nrjwolf.Tools.AttachAttributes;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GhostPreview : MonoBehaviour
{
    [GetComponent][SerializeField]
    private SpriteRenderer sr;
    
    [SerializeField]
    private int sortingOrder;
    
    [SerializeField] 
    private Color ghostColor = new(0f, 1f, 0f, 0.4f);
    
    private void Awake()
    {
        sr.sortingOrder = sortingOrder;
        gameObject.SetActive(false);
    }
    
    public void Show(Sprite sprite, Vector3 worldPos, Quaternion rotation)
    {
        sr.sprite = sprite;
        sr.color = ghostColor;
        transform.SetPositionAndRotation(worldPos, rotation);
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}