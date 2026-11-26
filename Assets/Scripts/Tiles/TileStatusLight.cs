using System.Collections;
using Pulsar.Interactions;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TileStatusLight : MonoBehaviour
{
    private const int k_SortingOffset = 1;

    [SerializeField]
    // this is the status light sprite renderer
    private SpriteRenderer lightRenderer;

    [SerializeField]
    private SpriteRenderer tileRenderer;

    [SerializeField]
    private Health health;

    [SerializeField]
    private Color blinkColor = Color.red;

    [SerializeField]
    [Min(1)]
    private int blinkCount = 3;

    [SerializeField]
    private float blinkInterval = 0.1f;

    private Coroutine _blinkRoutine;
    private WaitForSeconds _blinkWait;

    private void Awake()
    {
        _blinkWait = new WaitForSeconds(blinkInterval);
        lightRenderer.enabled = false;
    }

    private void OnEnable()
    {
        health.HealthStateChanged += OnHealthStateChanged;
    }

    private void OnDisable()
    {
        health.HealthStateChanged -= OnHealthStateChanged;
    }

    // made health state underscore since not using it.
    private void OnHealthStateChanged(HealthState _)
    {
        // if still blinking and health state changes
        if (_blinkRoutine != null)
        {
            StopCoroutine(_blinkRoutine);
        }

        _blinkRoutine = StartCoroutine(Blink());
    }

    private IEnumerator Blink()
    {
        lightRenderer.color = blinkColor;

        lightRenderer.sortingLayerID = tileRenderer.sortingLayerID;
        lightRenderer.sortingOrder
            = tileRenderer.sortingOrder + k_SortingOffset;

        for (int i = 0; i < blinkCount; i++)
        {
            lightRenderer.enabled = true;
            yield return _blinkWait;
            lightRenderer.enabled = false;
            yield return _blinkWait;
        }

        _blinkRoutine = null;
    }
}
