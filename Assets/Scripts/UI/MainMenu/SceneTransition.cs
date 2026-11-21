using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    
    public static void LoadScene(string sceneName)
    {
        if (IsTransitioning)
        {
            return;
        }

        if (Instance != null)
        {
            Instance.StartCoroutine(Instance.Transition(sceneName));
        }
    }

    private static void Create()
    {
        GameObject prefab = Resources.Load<GameObject>("SceneTransition");
        Instantiate(prefab);
    }

    private IEnumerator Transition(string sceneName)
    {
        _isTransitioning = true;
        _overlayGroup.blocksRaycasts = true;
        AsyncOperation loading = null;
        try
        {
            yield return Fade(_overlayGroup.alpha, 1f);
            yield return null;

            loading = SceneManager.LoadSceneAsync(sceneName);
            if (loading == null)
            {
                Debug.LogError($"Scene '{sceneName}' could not be loaded.");
                yield break;
            }

            loading.allowSceneActivation = false;
            while (loading.progress < 0.9f)
            {
                yield return null;
            }

            loading.allowSceneActivation = true;
            while (!loading.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
            yield return Fade(1f, 0f);
        }
        finally
        {
            if (loading != null && !loading.isDone)
            {
                loading.allowSceneActivation = true;
            }

            if (_overlayGroup != null)
            {
                _overlayGroup.alpha = 0f;
                _overlayGroup.blocksRaycasts = false;
            }

            _isTransitioning = false;
        }
    }

    private IEnumerator Fade(float from, float to)
    {
        _overlayGroup.alpha = from;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            yield return null;
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            _overlayGroup.alpha = Mathf.SmoothStep(from, to, elapsed / fadeDuration);
        }

        _overlayGroup.alpha = to;
    }
}

}
