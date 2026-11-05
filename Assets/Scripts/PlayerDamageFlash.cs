using System.Collections;
using UnityEngine;

public class PlayerDamageFlash : MonoBehaviour
{
    public PaletteSwatch flashColor = new PaletteSwatch(PaletteColor.White);
    public float flashDuration = 0.12f;

    private ShipVisual visual;
    private Coroutine flashCoroutine;

    void Awake()
    {
        visual = GetComponent<ShipVisual>();
    }

    public void Flash() => Flash(flashColor);

    // Overload lets other systems (e.g. Medic's heal aura) flash a
    // different color than the default damage-flash color, while still
    // sharing the same coroutine and revert mechanics.
    public void Flash(Color color)
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine(color));
    }

    IEnumerator FlashRoutine(Color color)
    {
        visual.SetFlash(color);
        yield return new WaitForSeconds(flashDuration);
        visual.ClearFlash();
        flashCoroutine = null;
    }
}
