using System;
using System.Collections;
using System.Collections.Generic;
using Pulsar.DataClasses;
using Pulsar.Interfaces;
using Pulsar.Player;
using Pulsar.Ship;
using Pulsar.UI;
using Pulsar.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Pulsar
{
public class TutorialController : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private PlayerController player;
    [SerializeField] private ShipManager enemy;
    [SerializeField] private CameraData cameraData;
    [SerializeField] private Image[] gateOutlines;
    [SerializeField] private GameObject[] steps;
    [SerializeField] private GameObject enemyCoreMarker;
    [SerializeField] private RectTransform completionPanel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;
    [SerializeField, Min(0f)] private float childFadeDuration = 0.5f;
    [SerializeField] private Color gateCompleteColor = new Color(0.2f, 1f, 0.4f);

    // private Coroutine _stepFade;
    private readonly List<Tile> _tilesWillBeAttached = new();
    public int _step;
    private bool _finished;
    private Camera _mainCam;

    private void Awake()
    {
        cameraData.MainCamera = _mainCam = Camera.main;
        completionPanel.gameObject.SetActive(false);
    }

    private void Start()
    {
        enemy.ShipDestroyedEvent += OnEnemyCoreDestroyed;
        retryButton.onClick.AddListener(Retry);
        menuButton.onClick.AddListener(BackToMenu);
        PlayerCursor.PlayerSelectTile += OnPlayerSelectTile;
        player.GetComponent<ShipManager>().OnDetachTile += OnPlayerDetachTile;
        ShowStep();
    }

    private void OnPlayerDetachTile()
    {
        Debug.Log("tile detached");
        if (_step >= 5)
        {

        Debug.Log("finsih");
            Finish();
        }
    }

    private void Update()
    {
        if (_finished) return;
        if (_step == 0 || _step == 1)
        {
            if (_step >= gateOutlines.Length) return;
            Image gate = gateOutlines[_step];
            if (Vector2.Distance(player.transform.position, gate.transform.position) >= 3.5f)
                return;

            gate.color = gateCompleteColor;
            AdvanceStep();
        }
        else if (_step == 4)
        {
            foreach (Tile tile in _tilesWillBeAttached)
            {
                if (!player.OwnsTile(tile)) continue;
                AdvanceStep();
                break;
            }
        }
    }

    private void OnPlayerSelectTile(Result<Tile> result)
    {
        if(result.IsSuccess && _step>=3)
            AdvanceStep();
    }

    private void AdvanceStep()
    {
        _step++;
        if (_step == 2) ActivateEnemy();
        ShowStep();
    }

    private void ActivateEnemy()
    {
        enemy.gameObject.SetActive(true);
        foreach (Tile tile in enemy.GetComponentsInChildren<Tile>())
        {
            if (tile is not IShipCore)
            {
                _tilesWillBeAttached.Add(tile);
            }
        }
    }

    private void OnEnemyCoreDestroyed(ShipManager defeated)
    {
        defeated.ShipDestroyedEvent -= OnEnemyCoreDestroyed;
        enemyCoreMarker.SetActive(false);
        foreach (Tile tile in _tilesWillBeAttached)
        {
            if(tile == null) continue;
            Rigidbody2D rb = tile.GetComponent<Rigidbody2D>();
            rb.linearVelocity *= 0.2f;
            rb.angularVelocity = 0f;
            rb.linearDamping = 3f;
        }
        if (_step == 2)
        {
            AdvanceStep();
        }
    }

    private void LateUpdate()
    {
        if (_finished) return;
        Vector3 position = player.transform.position;
        _mainCam.transform.position = new Vector3(position.x, position.y, -10f);
    }

    private void ShowStep()
    {
        // if (_stepFade != null) StopCoroutine(_stepFade);
        for (int i = 0; i < steps.Length; i++)
        {
            if (i == _step)
            {
                steps[i].SetActive(true);
            }
        }

        enemyCoreMarker.SetActive(_step == 2);
        if (_step < steps.Length)
        {
            // _stepFade = StartCoroutine(FadeStepChildren(steps[_step]));
            StartCoroutine(FadeStepChildren(steps[_step]));
        }
    }

    private IEnumerator FadeStepChildren(GameObject step)
    {
        Graphic[] children = step.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic child in children)
        {
            if (child.gameObject == step) continue;
            Color color = child.color;
            color.a = 0f;
            child.color = color;
        }

        foreach (Graphic child in children)
        {
            if (child.gameObject == step)
            {
                continue;
            }
            yield return UIUtilities.FadeIn(child, childFadeDuration);
        }
        // _stepFade = null;
    }

    private void Finish()
    {
        _finished = true;
        // if (_stepFade != null) StopCoroutine(_stepFade);
        completionPanel.gameObject.SetActive(true);
        completionPanel.transform.position = _mainCam.transform.position + new Vector3(0, 2f, 8f);
        retryButton.Select();
    }

    private void Retry() => Load("Tutorial");
    private void BackToMenu() => Load("MainMenu");

    private void Load(string scene)
    {
        if (SceneTransition.IsTransitioning) return;
        SceneTransition.LoadScene(scene);
    }

    private void OnDestroy()
    {
        PlayerCursor.PlayerSelectTile -= OnPlayerSelectTile;
        enemy.ShipDestroyedEvent -= OnEnemyCoreDestroyed;
        retryButton.onClick.RemoveListener(Retry);
        menuButton.onClick.RemoveListener(BackToMenu);
    }
}
}
