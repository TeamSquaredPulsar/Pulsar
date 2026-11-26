using UnityEngine;

public enum MarkerType
{
    Blank,
    Objective,
    Enemy,
    Guide,
}

namespace Pulsar.UI
{
public class MarkerController : MonoBehaviour
{
    [SerializeField]
    private Transform markerTransform;

    [SerializeField]
    private SpriteRenderer sprite;

    [SerializeField]
    private MarkerType markerType = MarkerType.Blank;

    private float _halfMarker;
    private GameObject _playerObj;
    private Camera _mainCam;

    private void Start()
    {
        _mainCam = Camera.main;
        sprite.color = GetMarkerColor();
        _halfMarker = sprite.bounds.size.x / 2;
        _playerObj = GameObject.FindWithTag("Player");
    }

    private void LateUpdate()
    {
        MoveMarker();
    }

    private void MoveMarker()
    {
        if (!_playerObj)
        {
            _playerObj = GameObject.FindWithTag("Player");
            return;
        }

        //Provides values in 0 -> 1 range
        Vector2 viewPortPos
            = _mainCam.WorldToViewportPoint(transform.position);

        if (viewPortPos is { x: <= 1f, y: <= 1f, } and { x: >= 0f, y: >= 0f, })
        {
            markerTransform.position = transform.position;
            sprite.enabled = false;
            return;
        }

        sprite.enabled = true;

        Vector2 viewPortCenter =
            new Vector2(viewPortPos.x - .5f, viewPortPos.y - .5f);

        //Stop division by 0
        float maxAxis = Mathf.Max(Mathf.Abs(viewPortCenter.x),
            Mathf.Abs(viewPortCenter.y));
        if (maxAxis < 0.0001f)
        {
            return;
        }

        float distanceFromCamera = Mathf.Abs(_mainCam.transform.position.z -
                                             _playerObj.transform.position.z);

        //Converts the marker size to be in viewport coordinates
        Vector3 viewportOrigin =
            _mainCam.WorldToViewportPoint(new Vector3(0, 0,
                _playerObj.transform.position.z));
        Vector3 viewportOffset =
            _mainCam.WorldToViewportPoint(new Vector3(_halfMarker, _halfMarker,
                _playerObj.transform.position.z));
        Vector2 halfMarkerViewport = new Vector2(
            Mathf.Abs(viewportOffset.x - viewportOrigin.x),
            Mathf.Abs(viewportOffset.y - viewportOrigin.y)
        );

        float maxExtentX = Mathf.Max(0.01f, 0.5f - halfMarkerViewport.x);
        float maxExtentY = Mathf.Max(0.01f, 0.5f - halfMarkerViewport.y);

        float scale = Mathf.Min(
            maxExtentX / Mathf.Abs(viewPortCenter.x),
            maxExtentY / Mathf.Abs(viewPortCenter.y));

        //Only if its off-screen clamp itz
        if (scale < 1f)
        {
            viewPortCenter *= scale;
        }

        Vector3 clampedViewport = new Vector3(
            viewPortCenter.x + 0.5f,
            viewPortCenter.y + 0.5f,
            distanceFromCamera
        );

        markerTransform.position
            = _mainCam.ViewportToWorldPoint(clampedViewport);

        markerTransform.rotation = clampedViewport.x switch
        {
            < .01f => Quaternion.Euler(0f, 0f, 270f),
            > .99f => Quaternion.Euler(0f, 0f, 90f),
            _ => Quaternion.Euler(0f, 0f, clampedViewport.y > .9f ? 180f : 0f),
        };
    }

    private Color GetMarkerColor()
    {
        //TODO Read from player preferences for color of markers
        return markerType switch
        {
            MarkerType.Objective => Color.lightBlue,
            MarkerType.Enemy => Color.red,
            MarkerType.Guide => Color.limeGreen,
            _ => Color.white,
        };
    }
}
}
