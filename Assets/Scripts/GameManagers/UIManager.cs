using System;
using Pulsar.Player;
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
    
    private TMP_Text scoreText;

    private void Start()
    {
        GameManager.OnEnemyKill += UpdateScore;
        scoreText = gameplayUI.GetComponentInChildren<TMP_Text>();
    }

    private void OnDestroy()
    {
        GameManager.OnEnemyKill -= UpdateScore;
    }

    private void UpdateScore(int obj)
    {
        scoreText.text = "Score: " + obj;
    }

    public void OnStartGameClicked()
    {
        GameStateChangeEvent?.Invoke(GameState.Playing);
        PlayerController.PlayerLossEvent += OnPlayerLoss;
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
        PlayerController.PlayerLossEvent -= OnPlayerLoss;
        mainMenu.SetActive(false);
        gameplayUI.SetActive(false);
        gameOverMenu.SetActive(true);
    }
}
}
