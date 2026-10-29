using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public GameObject panelRoot;
    // Mutual-exclusion guard against VictoryUI - see VictoryUI.cs's
    // matching gameOverPanelRoot field. Whichever end screen fires first
    // wins; the other's Show() becomes a true no-op.
    public GameObject victoryPanelRoot;
    // Source of the live party roster (PartySetupBootstrap overwrites
    // PartyFrameManager.players with the spawned ships before any Awake()
    // here runs). Game Over shows once every human-controlled ship is dead -
    // AI teammates still alive don't keep the run going.
    public PartyFrameManager partyFrameManager;

    private readonly List<GameObject> humanShips = new List<GameObject>();

    void Awake()
    {
        panelRoot.SetActive(false);
        WatchHumanShips();
    }

    /// <summary>
    /// Listeners are added at runtime, not in the Inspector: the co-op
    /// spawner creates ships from Ship.prefab, and a prefab's persistent
    /// listener can't target a scene object like this one - the same
    /// runtime-bound exception PartyFrameUI's ability button makes.
    /// </summary>
    private void WatchHumanShips()
    {
        if (partyFrameManager == null || partyFrameManager.players == null) return;
        foreach (GameObject ship in partyFrameManager.players)
            if (PartyFrameManager.IsHuman(ship)) Watch(ship);
    }

    private void Watch(GameObject ship)
    {
        humanShips.Add(ship);
        ship.GetComponent<PlayerHealth>().OnDeath.AddListener(OnHumanShipDied);
    }

    private void OnHumanShipDied()
    {
        foreach (GameObject ship in humanShips)
            if (ship != null && ship.activeInHierarchy) return;
        Show();
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
