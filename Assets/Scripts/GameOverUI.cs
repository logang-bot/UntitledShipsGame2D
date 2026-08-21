using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public GameObject panelRoot;
    // Mutual-exclusion guard against VictoryUI - see VictoryUI.cs's
    // matching gameOverPanelRoot field. Whichever end screen fires first
    // wins; the other's Show() becomes a true no-op.
    public GameObject victoryPanelRoot;

    void Awake()
    {
        panelRoot.SetActive(false);
    }

public void Show()
    {
        if (victoryPanelRoot != null && victoryPanelRoot.activeSelf) return;
        panelRoot.SetActive(true);
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ChangeRoles()
    {
        SceneManager.LoadScene("RoleSelect");
    }
}
