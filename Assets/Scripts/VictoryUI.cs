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
        SceneManager.LoadScene("Gameplay");
    }

    public void ChangeRoles()
    {
        SceneManager.LoadScene("RoleSelect");
    }
}
