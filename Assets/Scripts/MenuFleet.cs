using UnityEngine;
using UnityEngine.SceneManagement;

// The four drifting ships in MenuBackground. They show on the Main Menu and
// glide off to the side on every other screen, keyed off the active scene,
// which MenuScreen sets to whichever screen is current - so the fleet needs
// no reference to the screen scenes.
public class MenuFleet : MonoBehaviour
{
    public string showOnScene = "MainMenu";
    public Vector3 parkedOffset = new Vector3(10f, 0f, 0f);
    public float smoothTime = 0.35f;

    private Vector3 home;
    private Vector3 velocity;

    void Awake()
    {
        home = transform.localPosition;
        if (!IsShowing()) transform.localPosition = home + parkedOffset;
    }

    void Update()
    {
        Vector3 target = IsShowing() ? home : home + parkedOffset;
        transform.localPosition = Vector3.SmoothDamp(transform.localPosition, target, ref velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
    }

    private bool IsShowing() => SceneManager.GetActiveScene().name == showOnScene;
}
