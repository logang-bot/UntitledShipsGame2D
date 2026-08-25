using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseUI : MonoBehaviour
{
    public GameObject panelRoot;
    // Mutual-exclusion guard against GameOverUI/VictoryUI - mirrors their
    // own guard against each other (see GameOverUI.cs/VictoryUI.cs). Escape
    // should never pop Pause on top of an end screen.
    public GameObject gameOverPanelRoot;
    public GameObject victoryPanelRoot;

    private InputAction pauseAction;

    void Awake()
    {
        panelRoot.SetActive(false);
        pauseAction = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
        pauseAction.performed += OnPausePerformed;
    }

    void OnEnable()
    {
        pauseAction.Enable();
    }

    void OnDisable()
    {
        pauseAction.Disable();
    }

    void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (panelRoot.activeSelf)
        {
            Resume();
            return;
        }

        // Pause is only meaningful offline - once Online mode is real,
        // pausing a networked/authoritative match won't make sense. Unset
        // (e.g. Gameplay opened directly, bypassing Lobby) is treated as
        // allowed, matching PartySetupBootstrap's same direct-open fallback.
        if (GameModeSelection.Mode == GameMode.Online) return;
        if (gameOverPanelRoot != null && gameOverPanelRoot.activeSelf) return;
        if (victoryPanelRoot != null && victoryPanelRoot.activeSelf) return;

        Show();
    }

    public void Show()
    {
        panelRoot.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        panelRoot.SetActive(false);
        Time.timeScale = 1f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ChangeRoles()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("RoleSelect");
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
