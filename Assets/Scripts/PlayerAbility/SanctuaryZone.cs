using System.Collections.Generic;
using UnityEngine;

// The Medic's Sanctuary dome, left in the world where it was cast: heals
// every ally inside (the Medic included) each tickInterval, and slows enemy
// bullets while they're inside. Drawn under the bullets, which stay at full
// brightness. MedicSanctuary drives its frames and closes it.
public class SanctuaryZone : MonoBehaviour
{
    private const int SortingOrder = -6;

    private readonly HashSet<Bullet> slowed = new HashSet<Bullet>();
    private PlayerAbility medic;
    private MedicSfx sfx;
    private SpriteRenderer look;
    private float nextTickTime;

    private MedicSanctuarySettings Settings => medic.medicPowers;

    public static SanctuaryZone Spawn(PlayerAbility medic)
    {
        GameObject zoneObject = new GameObject("Sanctuary");
        zoneObject.transform.position = medic.transform.position;
        SanctuaryZone zone = zoneObject.AddComponent<SanctuaryZone>();
        zone.Build(medic);
        return zone;
    }

    private void Build(PlayerAbility caster)
    {
        medic = caster;
        sfx = caster.GetComponent<MedicSfx>();
        KinematicTrigger.AddBodyTo(gameObject);
        CircleCollider2D area = gameObject.AddComponent<CircleCollider2D>();
        area.isTrigger = true;
        area.radius = Settings.radius;
        look = FxSprites.CreateChild(transform, "Look", SortingOrder);
    }

    public void Show(ZoneFrame frame, float scale)
    {
        if (medic.Fx == null) return;
        look.sprite = medic.Fx.SanctuarySprite(frame);
        FxSprites.FitRingRadius(look.transform, medic.Fx.SanctuaryRadiusPx, Settings.radius * scale);
    }

    public void Close()
    {
        foreach (Bullet bullet in slowed)
            if (bullet != null) bullet.SetSpeedFactor(1f);
        Destroy(gameObject);
    }

    void Update()
    {
        if (Time.time < nextTickTime) return;
        nextTickTime = Time.time + Settings.tickInterval;
        bool healed = MedicHeal.Pulse(medic, new HealArea(transform.position, Settings.radius, true));
        if (healed && sfx != null) sfx.PlayHealTick();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out Bullet bullet) || bullet.Owner != "Enemy") return;
        bullet.SetSpeedFactor(Settings.bulletSpeedFactor);
        slowed.Add(bullet);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out Bullet bullet) && slowed.Remove(bullet)) bullet.SetSpeedFactor(1f);
    }
}
