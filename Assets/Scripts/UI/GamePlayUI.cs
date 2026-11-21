using NaughtyAttributes;
using Pulsar.Player;
using Pulsar.Stations.Guards;
using UnityEngine;
using TMPro;

namespace Pulsar.UI
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

    [SerializeField]
    [ReadOnly]
    private int score;

    private const string k_ScoreString = "Score:";

    private void OnEnable()
    {
        EnemyController.EnemyDefeatedEvent += UpdateScore;
        EnemyGuardAI.GuardDefeatedEvent += OnGuardDefeated;
        PlayerController.PlayerLossEvent += OnPlayerLoss;
        SetupScore();
    }

    private void OnDisable()
    {
        EnemyController.EnemyDefeatedEvent -= UpdateScore;
        EnemyGuardAI.GuardDefeatedEvent -= OnGuardDefeated;
        PlayerController.PlayerLossEvent -= OnPlayerLoss;
    }

    private void UpdateScore(EnemyController _, int killPoints)
    {
        score += killPoints;
        scoreText.text = $"{k_ScoreString} {score}";
    }

    // Station guards score the same way as the old enemies.
    private void OnGuardDefeated(EnemyGuardAI _, int killPoints) =>
        UpdateScore(null, killPoints);

    private void SetupScore()
    {
        scoreObject.SetActive(true);
        playerLossObject.SetActive(false);
        score = 0;
        UpdateScore(null, 0);
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
