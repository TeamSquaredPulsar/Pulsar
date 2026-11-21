using System;
using System.Collections;
using Pulsar.DataClasses;
using Pulsar.Player;
using UnityEngine;
using TMPro;

namespace Pulsar
{
public class GamePlayUI : MonoBehaviour
{
    [Header("Gameplay")]
    [SerializeField]
    private TMP_Text scoreText;

    [SerializeField]
    private GameObject scoreObject;

    [Header("Player Loss")]
    [SerializeField]
    private TMP_Text finalScoreText;

    [SerializeField]
    private GameObject playerLossObject;

    private const string k_ScoreString = "Score:";

    private void OnEnable()
    {
        GameManager.OnScoreChange += UpdateScore;
        PlayerController.PlayerLossEvent += OnPlayerLoss;
        SetupScore();
    }

    private void OnDisable()
    {
        GameManager.OnScoreChange -= UpdateScore;
        PlayerController.PlayerLossEvent -= OnPlayerLoss;
    }

    private void UpdateScore(int obj)
    {
        scoreText.text = $"{k_ScoreString} {obj}";
    }

    private void SetupScore()
    {
        scoreObject.SetActive(true);
        playerLossObject.SetActive(false);
        UpdateScore(0);
    }

    private void OnPlayerLoss(PlayerController _)
    {
        playerLossObject.SetActive(true);
        finalScoreText.text = $"Final {scoreText.text}";
        // [CB] We need to hold on to the score object until we get its text.
        scoreObject.SetActive(false);
    }
}
}
