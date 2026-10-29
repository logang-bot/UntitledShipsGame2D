using System.Collections;
using UnityEngine;

/// <summary>
/// Generic, initially-hidden ring on every ship (not role-gated) that
/// Support's party-wide speed boost toggles on/off. Drawn with the Combat FX
/// speed-ring sprite, which is always Support purple whichever ship carries
/// it, at the design's own size. When the boost ends it shows the ring
/// breaking up for a moment before hiding.
/// </summary>
public class PlayerAbilityPartyBuffRing
{
    private const int SortingOrder = -5;
    private const float EndFrameSeconds = 0.25f;

    private readonly PlayerAbility ability;
    private SpriteRenderer ring;
    private Coroutine hideRoutine;

    public PlayerAbilityPartyBuffRing(PlayerAbility ability)
    {
        this.ability = ability;
    }

    public void Create()
    {
        if (ability.Fx == null) return;
        ring = FxSprites.CreateChild(ability.transform, "PartyBuffRing", SortingOrder);
        FxSprites.UndoParentScale(ring.transform);
        ring.gameObject.SetActive(false);
    }

    public void Show(bool active)
    {
        if (ring == null) return;
        if (hideRoutine != null) ability.StopCoroutine(hideRoutine);
        hideRoutine = null;
        if (active) ShowSustain();
        else if (ring.gameObject.activeSelf) hideRoutine = ability.StartCoroutine(ShowEndThenHide());
    }

    private void ShowSustain()
    {
        ring.sprite = ability.Fx.speedRing;
        ring.gameObject.SetActive(true);
    }

    private IEnumerator ShowEndThenHide()
    {
        ring.sprite = ability.Fx.speedRingEnd;
        yield return new WaitForSeconds(EndFrameSeconds);
        ring.gameObject.SetActive(false);
        hideRoutine = null;
    }
}
