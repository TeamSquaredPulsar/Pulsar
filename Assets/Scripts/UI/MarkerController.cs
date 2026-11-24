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

        Vector2 playerDir =
            _playerObj.transform.position - markerTransform.position;

        //Provides values in 0 -> 1 range
        Vector2 viewPortPos
            = _mainCam.WorldToViewportPoint(_playerObj.transform.position);

        Vector2 viewPortCenter =
            new Vector2(viewPortPos.x - .5f, viewPortPos.y - .5f);

        //Stop division by 0
        float maxAxis = Mathf.Max(Mathf.Abs(viewPortCenter.x), Mathf.Abs(viewPortCenter.y));
        if (maxAxis < 0.0001f) return;

        float maxExtent = 0.5f - _halfMarker;
        float scale = maxExtent / Mathf.Max(Mathf.Abs(viewPortCenter.x), Mathf.Abs(viewPortCenter.y));
        viewPortCenter *= scale;

        float distanceFromCamera = Mathf.Abs(_mainCam.transform.position.z -
                                             _playerObj.transform.position.z);

        Vector3 clampedViewport = new Vector3(
            viewPortCenter.x + 0.5f,
            viewPortCenter.y + 0.5f,
            distanceFromCamera
        );

        markerTransform.position = _mainCam.ViewportToWorldPoint(clampedViewport);
    }

    private Color GetMarkerColor()
    {
        //TODO Read from player preferences for color of markers
        switch (markerType)
        {
            case MarkerType.Objective:
                return Color.lightBlue;
            case MarkerType.Enemy:
                return Color.red;
            case MarkerType.Guide:
                return Color.limeGreen;
            default:
                return Color.white;
        }

    }
}
}
