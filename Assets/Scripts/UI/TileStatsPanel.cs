using System.Text;
using TMPro;
using UnityEngine;

namespace Pulsar.UI
{
[RequireComponent(typeof(CanvasGroup))]
public class TileStatsPanel : MonoBehaviour
{
    private const float k_HiddenAlpha = 0f;

    [SerializeField]
    private CanvasGroup _canvasGroup;

    [SerializeField]
    private TMP_Text _tileNameText;

    [SerializeField]
    private TMP_Text _statsText;

    [SerializeField]
    [Range(0f, 1f)]
    private float visibleAlpha = 0.85f;

    private readonly StringBuilder _builder = new();
    private Tile _shownTile;

    private void Awake()
    {
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        Hide();
    }

    private void Update()
    {
        // the !_shownTile.IsAttached is so that the panel hides when u attach it
        if (_shownTile && !_shownTile.IsAttached)
        {
            Refresh();
        } // If not shownTile, and panel visible, hide
        else if (_canvasGroup.alpha > k_HiddenAlpha)
        {
            Hide();
        }
    }

    private void OnEnable()
    {
        PlayerCursor.LooseTileHoverChanged += OnLooseTileHoverChanged;
    }

    private void OnDisable()
    {
        PlayerCursor.LooseTileHoverChanged -= OnLooseTileHoverChanged;
    }

    private void Show(Tile tile)
    {
        _shownTile = tile;
        Refresh();
        _canvasGroup.alpha = visibleAlpha;
    }

    private void Hide()
    {
        _shownTile = null;
        _canvasGroup.alpha = k_HiddenAlpha;
    }

    private void Refresh()
    {
        _tileNameText.text = _shownTile.DisplayName;

        _builder.Clear();
        foreach ((string label, string value) in _shownTile.GetStats())
        {
            _builder.AppendLine($"{label}: {value}");
        }

        _statsText.text = _builder.ToString();
    }

    private void OnLooseTileHoverChanged(Tile tile)
    {
        if (tile)
        {
            Show(tile);
        }
        else
        {
            Hide();
        }
    }
}
}
