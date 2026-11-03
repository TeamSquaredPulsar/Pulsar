using NaughtyAttributes;
using Nrjwolf.Tools.AttachAttributes;
using Pulsar.DataClasses;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Parallax : MonoBehaviour
{
    [Header("References"),GetComponent,SerializeField,]
    private SpriteRenderer spriteRenderer;
    
    [SerializeField]
    private CameraData cam;
    
    [Header("Settings"),OnValueChanged("SetupTiling"),SerializeField,]
    private Sprite sprite;
    
    [OnValueChanged("SetupTiling"),SortingLayer,SerializeField,]
    private int sortingLayerID;
    
    [SerializeField,Range(0f, 1f),
     Tooltip(@"Lower values move more closely to main camera.
0 = moves constant with camera.
1 = won't move at all.
0.5 = moves half the amount the camera does."),]
    private float parallaxEffect;

    
    private Vector2 _startPos;
    private Vector2 _size;
    
    private void Start()
    {
        _startPos = transform.position;
        _size = spriteRenderer.bounds.size;
    }
    
    private void FixedUpdate()
    {
        Vector2 distance = cam.MainCamera.transform.position * parallaxEffect;
        transform.position = _startPos + distance;
        HandleInfiniteScroll();
    }
    
    public void ChangeSprite(Sprite newSprite)
    {
        spriteRenderer.sprite = newSprite;
        SetupTiling();
    }

    private void SetupTiling()
    {
        RemoveChildren();
        
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingLayerID = sortingLayerID;
        _size = spriteRenderer.bounds.size;
        
        int[] values = {-1, 0, 1,};
        foreach (int x in values)
        {
            foreach (int y in values)
            {
                // 0, 0 is the root object; don't generate another one.
                if (x == 0 && y == 0)
                {
                    continue;
                }

                SetupTile(x, y);
            }
        }
    }

    private void RemoveChildren()
    {
        //-1 because we don't want to overflow the end of the array
        int numChildren = transform.childCount - 1;
        
        // go from the end of the list to the front;
        // otherwise we'd overflow half way
        for (int i = numChildren; i >= 0 ; i--)
        {
            Transform child = transform.GetChild(i);
#if UNITY_EDITOR
            DestroyImmediate(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }
    }

    private void SetupTile(int x, int y)
    {
        GameObject go = new ($"{gameObject.name}[{x},{y}]");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteRenderer.sprite;
        sr.sortingLayerID = spriteRenderer.sortingLayerID;
        go.transform.position = new Vector3(
            transform.position.x + (_size.x * x), 
            transform.position.y + (_size.y * y), 
            0
        );
        go.transform.parent = transform;
    }

    private void HandleInfiniteScroll()
    {
        //Check how much the background has moved
        Vector2 movement
            = cam.MainCamera.transform.position * (1 - parallaxEffect);

        //If the parallax caused the edge of the layer to show,
        //adjust the position so it doesn't
        if (movement.x > _startPos.x + _size.x)
        {
            _startPos.x += _size.x;
        }
        else if (movement.x < _startPos.x - _size.x)
        {
            _startPos.x -= _size.x;
        }

        if (movement.y > _startPos.y + _size.y)
        {
            _startPos.y += _size.y;
        }
        else if (movement.y < _startPos.y - _size.y)
        {
            _startPos.y -= _size.y;
        }
    }
}