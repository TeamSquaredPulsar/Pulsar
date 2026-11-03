using System;
using Pulsar.Player;
using Pulsar.UI;
using TMPro;
using UnityEngine;

namespace Pulsar
{
public class UIManager : MonoBehaviour
{
    public static event Action<GameState> GameStateChangeEvent;

    [SerializeField]
    private GameObject mainMenu;

    [SerializeField]
    private GameObject gameplayUI;
    
    [SerializeField]
    private GameObject gameOverMenu;
    
    private TMP_Text _scoreText;

    private void Start()
    {
        GameManager.OnEnemyKill += UpdateScore;
        PlayerController.PlayerLossEvent += OnPlayerLoss;
        _scoreText = gameplayUI.GetComponentInChildren<TMP_Text>();
    }

    private void OnDestroy()
    {
        GameManager.OnEnemyKill -= UpdateScore;
        PlayerController.PlayerLossEvent -= OnPlayerLoss;
    }

    private void UpdateScore(int obj)
    {
        _scoreText.text = "Score: " + obj;
    }

    public void OnStartGameClicked()
    {
        GameStateChangeEvent?.Invoke(GameState.Playing);
        mainMenu.SetActive(false);
        gameplayUI.SetActive(true);
        gameOverMenu.SetActive(false);
        UpdateScore(0);
    }

    public void OnToMainMenuClicked()
    {
        GameStateChangeEvent?.Invoke(GameState.MainMenu);
        mainMenu.SetActive(true);
        gameplayUI.SetActive(false);
        gameOverMenu.SetActive(false);
    }

    private void OnPlayerLoss(PlayerController _)
    {
        GameStateChangeEvent?.Invoke(GameState.GameOver);
        mainMenu.SetActive(false);
        gameplayUI.SetActive(false);
        gameOverMenu.SetActive(true);
    }
}
}
