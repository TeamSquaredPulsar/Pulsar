using UnityEngine;
using UnityEngine.UI;

namespace Pulsar.UI
{
public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private SettingsScreen settingsPanel;

    private void OnEnable()
    {
        settingsButton.onClick.AddListener(OpenSettings);
        quitButton.onClick.AddListener(Quit);
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ShowMenu();
    }

    private void OnDisable()
    {
        settingsButton.onClick.RemoveListener(OpenSettings);
        quitButton.onClick.RemoveListener(Quit);
    }

    private void OpenSettings()
    {
        menuPanel.SetActive(false);
        settingsPanel.gameObject.SetActive(true);
    }

    private void ShowMenu()
    {
        settingsPanel.gameObject.SetActive(false);
        menuPanel.SetActive(true);
        playButton.Select();
    }

    public void CloseSettings()
    {
        ShowMenu();
        settingsButton.Select();
    }

    private void Quit()
    {
        Application.Quit();
    }
}
}
