using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One of Join Lobby's four player slots. Empty: a dim player number and a
// pulsing "press any button" prompt. Joined: a white frame with the player
// number, device icon and label, and how to leave; it fills in with a short
// top-down wipe the moment the player joins.
public class JoinSlotView : MonoBehaviour
{
    private const float WipeSeconds = 0.15f;
    private const float PulseSpeed = 2.4f;

    public GameObject joinedRoot;
    public GameObject emptyRoot;
    public TMP_Text joinedNumber;
    public TMP_Text emptyNumber;
    public TMP_Text playerLabel;
    public TMP_Text deviceLabel;
    public TMP_Text leaveHint;
    public Image deviceIcon;
    public Sprite keyboardIcon;
    public Sprite gamepadIcon;
    public CanvasGroup emptyPrompt;

    void Update()
    {
        if (emptyRoot.activeSelf && emptyPrompt != null)
            emptyPrompt.alpha = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * PulseSpeed);
    }

    public void ShowEmpty(int number)
    {
        StopAllCoroutines();
        joinedRoot.transform.localScale = Vector3.one;
        joinedRoot.SetActive(false);
        emptyRoot.SetActive(true);
        emptyNumber.text = $"P{number}";
    }

    public void ShowJoined(JoinSlotInfo info)
    {
        bool newlyJoined = !joinedRoot.activeSelf;
        emptyRoot.SetActive(false);
        joinedRoot.SetActive(true);
        FillJoined(info);
        if (newlyJoined) StartCoroutine(Wipe((RectTransform)joinedRoot.transform));
    }

    private void FillJoined(JoinSlotInfo info)
    {
        joinedNumber.text = $"P{info.number}";
        playerLabel.text = $"PLAYER {info.number}";
        deviceLabel.text = info.DeviceLabel;
        leaveHint.text = info.LeaveHint;
        deviceIcon.sprite = info.isKeyboard ? keyboardIcon : gamepadIcon;
    }

    private static IEnumerator Wipe(RectTransform target)
    {
        for (float t = 0f; t < WipeSeconds; t += Time.unscaledDeltaTime)
        {
            target.localScale = new Vector3(1f, MenuEase.Out(t / WipeSeconds), 1f);
            yield return null;
        }
        target.localScale = Vector3.one;
    }
}
