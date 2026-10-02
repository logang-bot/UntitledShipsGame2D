using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryUI : MonoBehaviour
{
    public GameObject panelRoot;
    // Mutual-exclusion guard against GameOverUI - see GameOverUI.cs's
    // matching victoryPanelRoot field. Prevents a boss defeat that happens
    // after the human Player has already died from popping Victory on top
    // of (or instead of) an already-showing Game Over.
    public GameObject gameOverPanelRoot;

    void Awake()
    {
        panelRoot.SetActive(false);
    }

public void Show()
    {
        if (gameOverPanelRoot != null && gameOverPanelRoot.activeSelf) return;
        panelRoot.SetActive(true);
    }

    public void PlayAgain()
    {
        // Reload whichever level scene is actually active, not a hardcoded
        // name - that would silently send a player who just beat Level 2/3
        // back to Level 1 instead of replaying the level they were on. See
        // GameOverUI.Restart()/PauseUI.Restart(), which already use this
        // pattern.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ChangeRoles()
    {
        SceneManager.LoadScene("RoleSelect");
    }
}
