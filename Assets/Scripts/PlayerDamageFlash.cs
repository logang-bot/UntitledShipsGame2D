using System.Collections;
using UnityEngine;

public class PlayerDamageFlash : MonoBehaviour
{
    public Color flashColor = Color.white;
    public float flashDuration = 0.12f;

    private SpriteRenderer sprite;
    private PlayerRoleComponent roleComponent;
    private Coroutine flashCoroutine;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        roleComponent = GetComponent<PlayerRoleComponent>();
    }

    public void Flash() => Flash(flashColor);

    // Overload lets other systems (e.g. Medic's heal aura) flash a
    // different color than the default damage-flash color, while still
    // sharing the same coroutine/revert-to-role-tint mechanics.
    public void Flash(Color color)
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine(color));
    }

    IEnumerator FlashRoutine(Color color)
    {
        sprite.color = color;
        yield return new WaitForSeconds(flashDuration);
        sprite.color = roleComponent.Stats.tintColor;
        flashCoroutine = null;
    }
}
