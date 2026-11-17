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
    [SerializeField] private string gameplayScene = "Alpha";

    private void OnEnable()
    {
        playButton.onClick.AddListener(Play);
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
        playButton.onClick.RemoveListener(Play);
        settingsButton.onClick.RemoveListener(OpenSettings);
        quitButton.onClick.RemoveListener(Quit);
    }

    public void Play()
    {
        if (SceneTransition.IsTransitioning)
        {
            return;
        }

        Time.timeScale = 1f;
        SceneTransition.LoadScene(gameplayScene);
    }

    public void OpenSettings()
    {
        menuPanel.SetActive(false);
        settingsPanel.gameObject.SetActive(true);
    }

    public void ShowMenu()
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

    public void Quit()
    {
        Application.Quit();
    }
}
}
