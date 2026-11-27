using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Pulsar.UI
{
public class SceneTransition : MonoBehaviour
{
    [SerializeField, Min(0f)] private float fadeDuration = 0.35f;

    public static SceneTransition Instance { get; private set; }

    private CanvasGroup _overlayGroup;
    private bool _isTransitioning;

    public static bool IsTransitioning => Instance != null && Instance._isTransitioning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (Instance == null)
        {
            Create();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _overlayGroup = GetComponent<CanvasGroup>();
        _overlayGroup.alpha = 0f;
        _overlayGroup.blocksRaycasts = false;
        GetComponent<Canvas>().sortingOrder = short.MaxValue;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    public static async Task Run(Func<Task> loadScene)
    {
        if (IsTransitioning)
        {
            return;
        }

        if (Instance == null)
        {
            Create();
        }

        if (Instance == null)
        {
            await loadScene();
            return;
        }

        await Instance.Transition(loadScene);
    }

    private static void Create()
    {
        GameObject prefab = Resources.Load<GameObject>("SceneTransition");
        if (prefab == null)
        {
            Debug.LogWarning("SceneTransition prefab could not be loaded.");
            return;
        }

        Instantiate(prefab);
    }

    private async Task Transition(Func<Task> loadScene)
    {
        _isTransitioning = true;
        _overlayGroup.blocksRaycasts = true;
        try
        {
            await Fade(_overlayGroup.alpha, 1f);
            await Awaitable.NextFrameAsync();
            await loadScene();
            await Awaitable.NextFrameAsync();
            await Awaitable.NextFrameAsync();
            await Fade(1f, 0f);
        }
        finally
        {
            if (_overlayGroup != null)
            {
                _overlayGroup.alpha = 0f;
                _overlayGroup.blocksRaycasts = false;
            }

            _isTransitioning = false;
        }
    }

    private async Awaitable Fade(float from, float to)
    {
        _overlayGroup.alpha = from;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            await Awaitable.NextFrameAsync();
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            _overlayGroup.alpha = Mathf.SmoothStep(from, to, elapsed / fadeDuration);
        }

        _overlayGroup.alpha = to;
    }
}

}
