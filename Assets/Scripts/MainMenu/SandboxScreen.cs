using System;
using Pulsar.UI;
using UnityEngine;
using UnityEngine.UI;

public class SandboxScreen : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;

    private void Start()
    {
        mainMenuButton.onClick.AddListener(() =>
        {
            SceneTransition.LoadScene("MainMenu");
        });
    }
}
