using System;
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

    private void Start()
    {
        sprite.color = GetMarkerColor();
        _halfMarker = sprite.bounds.size.x / 2;
        _playerObj = GameObject.FindWithTag("Player");
    }

    private void FixedUpdate()
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

        float markerAngle =
            Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg;

        float halfScreenWidth = (float)Screen.width / 2;
        float halfScreenHeight = (float)Screen.height / 2;

        Vector2 nextPos = markerAngle switch
        {
            < -45 => new Vector2(-halfScreenWidth,
                halfScreenHeight - _halfMarker),

            < 45 => new Vector2(halfScreenWidth,
                halfScreenHeight - _halfMarker),

            > 45 => new Vector2(-halfScreenWidth,
                -halfScreenHeight - _halfMarker),

            > -135 => new Vector2(halfScreenWidth,
                halfScreenHeight - _halfMarker),
            _ => Vector2.zero,
        };

        markerTransform.transform.position = nextPos;
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
