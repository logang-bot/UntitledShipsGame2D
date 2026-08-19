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

    public void Flash()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        sprite.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        sprite.color = roleComponent.Stats.tintColor;
        flashCoroutine = null;
    }
}
