using UnityEngine;
using Pulsar.Ship;

namespace Pulsar.Building
{
    public class RadialMenu : MonoBehaviour
    {
        private const int Segments = 64;
        private const int ScanZoneSortingOrder = -10;

        // cached oval params 
        private Vector3 _center;
        private float _rx, _ry;
        private Quaternion _rotation;
        
        [Header("Scan zone")]
        [SerializeField] private Transform fillTransform;
        [SerializeField] private Color scanZoneColor = new Color(0f, 1f, 0f, 0.10f);
        
        [Header("Outline")]
        [SerializeField] private LineRenderer outline;
        [SerializeField] private float outlineWidthMultiplier = 0.05f;
        [SerializeField] private Color outlineColor = Color.white;

        private void Awake()
        {
            SpriteRenderer scanZoneSr = fillTransform.GetComponent<SpriteRenderer>();
            scanZoneSr.color = scanZoneColor;
            scanZoneSr.sortingOrder = ScanZoneSortingOrder;
            
            outline.widthMultiplier = outlineWidthMultiplier;
            outline.startColor = outlineColor;
            outline.endColor = outlineColor;
            outline.sortingOrder = ScanZoneSortingOrder + 1;
        }
        
        public void UpdateShape(Vector3 worldCenter, float rx, float ry, Quaternion rotation)
        {
            _center = worldCenter;
            _rx = rx;
            _ry = ry;
            _rotation = rotation;

            transform.SetPositionAndRotation(worldCenter, rotation);

            fillTransform.localScale = new Vector3(rx, ry, 1f);

            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                outline.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * rx,
                    Mathf.Sin(angle) * ry,
                    0f));
            }
        }

        public Vector3 PointOnOutline(float angle)
        {
            return _center + _rotation * new Vector3(
                Mathf.Cos(angle) * _rx, Mathf.Sin(angle) * _ry, 0f);
        }

        // Intersect the ray from the core through the tile with the ellipse
        public float OutlineAngleFromCore(Vector3 core, Vector3 tile)
        {
            //Ray: P(t) = origin + t * direction
            Vector3 origin = Quaternion.Inverse(_rotation) * (core - _center);
            Vector3 direction = Quaternion.Inverse(_rotation) * (tile - core);
            direction.z = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.right;
 
            // Px = ox + t*dx
            // Py = oy + t*dy
            // Px^2/rx^2 + Py^2/ry^2 = 1

            float a = direction.x * direction.x / (_rx * _rx)
                    + direction.y * direction.y / (_ry * _ry);
            float b = 2f * (origin.x * direction.x / (_rx * _rx)
                         + origin.y * direction.y / (_ry * _ry));
            float c = origin.x * origin.x / (_rx * _rx)
                    + origin.y * origin.y / (_ry * _ry) - 1f;
            float distance = (-b + Mathf.Sqrt(Mathf.Max(0f, b * b - 4f * a * c))) / (2f * a);
            Vector3 point = origin + direction * distance;
            return Mathf.Atan2(point.y / _ry, point.x / _rx);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
