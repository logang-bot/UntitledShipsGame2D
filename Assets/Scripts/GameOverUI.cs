using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
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

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
