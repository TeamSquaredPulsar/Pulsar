using UnityEngine;

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
    [SerializeField] private Color scanZoneColor = new(0f, 1f, 0f, 0.10f);
        
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

        // each segment angle
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            outline.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * rx,
                Mathf.Sin(angle) * ry,
                0f));
        }
    }

    // From angle to point on ellipse
    public Vector3 PointOnOutline(float angle) =>
        _center + _rotation * new Vector3(
            Mathf.Cos(angle) * _rx, Mathf.Sin(angle) * _ry, 0f);

    // Intersect the ray from the core through the tile with the ellipse
    public float GetOutlineAngleFromTileToCore(Vector3 core, Vector3 tile)
    {
        const float minMagnitude = 0.0001f;
        //Ray: P(t) = origin + t * direction
        Vector2 origin = Quaternion.Inverse(_rotation) * (core - _center);
        Vector2 direction = Quaternion.Inverse(_rotation) * (tile - core);
        
        if (direction.sqrMagnitude < minMagnitude)
        {
            direction = Vector3.right;
        }

        // Px = ox + t*dx
        // Py = oy + t*dy
        // Px^2/rx^2 + Py^2/ry^2 = 1
        // Solve for t (at^2 + bt + c = 0) 

        float dirXSquared = direction.x * direction.x;
        float dirYSquared = direction.y * direction.y;
        float rxSquared = _rx * _rx;
        float rySquared = _ry * _ry;
        float a = dirXSquared / rxSquared
                  + dirYSquared / rySquared;
        float b = 2f * (origin.x * direction.x / rxSquared
                        + origin.y * direction.y / rySquared);
        float c = origin.x * origin.x / rxSquared
            + origin.y * origin.y / rySquared - 1f;
        float distance = (-b + Mathf.Sqrt(Mathf.Max(0f, b * b - 4f * a * c))) / (2f * a); // get the positive only
        Vector2 point = origin + direction * distance;
        return Mathf.Atan2(point.y / _ry, point.x / _rx);
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}