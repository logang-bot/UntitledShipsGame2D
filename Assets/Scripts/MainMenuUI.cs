using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public MenuScreen screen;

    public void Play()
    {
        screen.GoTo("Lobby");
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
