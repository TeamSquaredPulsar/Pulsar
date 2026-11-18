using UnityEngine;
using UnityEngine.UI;

namespace Pulsar.UI
{
public class MainMenu : MonoBehaviour
{
    private const string GameScene = "Alpha";
    private const string TutorialScene = "Tutorial";
    private const string SandboxScene = "Sandbox";
    [SerializeField] private Button playButton;
    [SerializeField] private Button sandboxButton;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private SettingsScreen settingsPanel;

    private void OnEnable()
    {
        playButton.onClick.AddListener(Play);
        sandboxButton.onClick.AddListener(SandBox);
        tutorialButton.onClick.AddListener(Tutorial);
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
        sandboxButton.onClick.RemoveListener(SandBox);
        tutorialButton.onClick.RemoveListener(Tutorial);
        settingsButton.onClick.RemoveListener(OpenSettings);
        quitButton.onClick.RemoveListener(Quit);
    }

    public void LoadScene(string sceneName)
    {
        if (SceneTransition.IsTransitioning)
        {
            return;
        }

        SceneTransition.LoadScene(sceneName);
    }

    private void Play()
    {
        LoadScene(GameScene);
    }

    private void Tutorial()
    {
        LoadScene(TutorialScene);
    }

    private void SandBox()
    {
        LoadScene(SandboxScene);
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
