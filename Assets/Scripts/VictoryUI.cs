using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryUI : MonoBehaviour
{
    public GameObject panelRoot;

    void Awake()
    {
        panelRoot.SetActive(false);
    }

    public void Show()
    {
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
