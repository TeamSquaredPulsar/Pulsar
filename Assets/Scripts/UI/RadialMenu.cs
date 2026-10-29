using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(SpriteRenderer))]
public class RadialMenu : MonoBehaviour
{
    private const int Segments = 64;
    private const int ScanZoneSortingOrder = -10;

    // cached oval params 
    private Vector3 _center;
    private Vector2 _radius;
    private Quaternion _rotation;
        
    [Header("Scan zone"),SerializeField,]
    private SpriteRenderer scanZoneSr;
    [SerializeField] private Color scanZoneColor = new(0f, 1f, 0f, 0.10f);
        
    [Header("Outline"),SerializeField,]
        private LineRenderer outline;
    [SerializeField] private float outlineWidthMultiplier = 0.05f;
    [SerializeField] private Color outlineColor = Color.white;

    private void Awake()
    {
        scanZoneSr.color = scanZoneColor;
        scanZoneSr.sortingOrder = ScanZoneSortingOrder;
            
        outline.widthMultiplier = outlineWidthMultiplier;
        outline.startColor = outlineColor;
        outline.endColor = outlineColor;
        outline.sortingOrder = ScanZoneSortingOrder + 1;
        outline.positionCount = Segments;
    }

    public void UpdateShape(Vector3 worldCenter, Vector2 radius, Quaternion rotation)
    {
        _center = worldCenter;
        _radius = radius;
        _rotation = rotation;

        transform.SetPositionAndRotation(worldCenter, rotation);

        scanZoneSr.transform.localScale = new Vector3(_radius.x, _radius.y, 1f);
        
        // each segment angle
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            outline.SetPosition(i, new Vector2(
                Mathf.Cos(angle) * _radius.x,
                Mathf.Sin(angle) * _radius.y
            ));
        }
    }

    // From angle to point on ellipse
    public Vector3 PointOnOutline(float angle) =>
        _center + _rotation * new Vector3(
            Mathf.Cos(angle) * _radius.x, Mathf.Sin(angle) * _radius.y, 0f);

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

        Vector2 directionSquared = direction * direction;
        Vector2 radiusSquared = _radius * _radius;
        Vector2 originSquared = origin * origin;
        
        float a = directionSquared.x / radiusSquared.x
                  + directionSquared.y / radiusSquared.y;
        float b = 2f * (
            origin.x * direction.x / radiusSquared.x
            + origin.y * direction.y / radiusSquared.y
        );
        float c = originSquared.x / radiusSquared.x
            + originSquared.y / radiusSquared.y - 1f;
        float distance = PositiveQuadraticFormula(a, b, c); // get the positive only
        Vector2 point = origin + direction * distance;
        return Mathf.Atan2(point.y / _radius.y, point.x / _radius.x);
    }

    private static float PositiveQuadraticFormula(float a, float b, float c)
    {
        float bSquared = b * b;
        float discriminant = bSquared - 4f * a * c;
        float numerator = -b + Mathf.Sqrt(Mathf.Max(0f, discriminant));
        float denominator = (2f * a);
        return numerator / denominator;
    }

    public void Show() => scanZoneSr.enabled = true;
    public void Hide() => scanZoneSr.enabled = false;
}