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
        // Two bindings, not restricted to a specific device/player - a
        // shared pause matches local co-op convention, and a gamepad-only
        // human (co-op players 2-4, or player 1 choosing a controller) has
        // no keyboard to reach Escape with otherwise.
        pauseAction = new InputAction("Pause", InputActionType.Button);
        pauseAction.AddBinding("<Keyboard>/escape");
        pauseAction.AddBinding("<Gamepad>/start");
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
        // (e.g. a level scene opened directly, bypassing Lobby) is treated
        // as allowed, matching PartySetupBootstrap's same direct-open
        // fallback.
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
