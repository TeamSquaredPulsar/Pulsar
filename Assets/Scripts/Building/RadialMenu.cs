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
        private float _rx, _rz;
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
        
        public void UpdateShape(Vector3 worldCenter, float rx, float rz, Quaternion rotation)
        {
            _center = worldCenter;
            _rx = rx;
            _rz = rz;
            _rotation = rotation;

            transform.SetPositionAndRotation(worldCenter, rotation);

            fillTransform.localScale = new Vector3(rx, rz, 1f);

            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                outline.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * rx,
                    0f,
                    Mathf.Sin(angle) * rz));
            }
        }

        public bool IsInside(Vector3 worldPoint)
        {
            if (_rx <= 0f || _rz <= 0f) return false;
            Vector3 local = Quaternion.Inverse(_rotation) * (worldPoint - _center);
            float normalizedX = local.x / _rx;
            float normalizedZ = local.z / _rz;
            return (normalizedX * normalizedX + normalizedZ * normalizedZ) <= 1f;
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
