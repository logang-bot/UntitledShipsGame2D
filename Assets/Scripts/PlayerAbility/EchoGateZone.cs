using System.Collections;
using UnityEngine;

// The Support's Echo Gate, left in the world above where it was cast: every
// regular player shot that crosses it splits into a fan of splitCount shots,
// each keeping the shooter's damage and credit. Enemy bullets pass through.
// SupportEchoGate drives its frames and closes or shatters it.
public class EchoGateZone : MonoBehaviour
{
    private const int SortingOrder = -4;
    private const float ShatterShowSeconds = 0.3f;
    private const float ShatterSoundTailSeconds = 0.6f;

    private PlayerAbility support;
    private EchoGateSfx sfx;
    private AudioSource voice;
    private BoxCollider2D bar;
    private EchoGateLook look;

    private SupportEchoGateSettings Settings => support.supportPowers;

    public static EchoGateZone Spawn(PlayerAbility support)
    {
        GameObject gateObject = new GameObject("EchoGate");
        gateObject.transform.position = support.transform.position + Vector3.up * support.supportPowers.heightAboveShip;
        EchoGateZone gate = gateObject.AddComponent<EchoGateZone>();
        gate.Build(support);
        return gate;
    }

    private void Build(PlayerAbility caster)
    {
        support = caster;
        sfx = caster.GetComponent<EchoGateSfx>();
        voice = ZoneAudio.CloneOnto(sfx != null ? sfx.source : null, gameObject);
        KinematicTrigger.AddBodyTo(gameObject);
        bar = gameObject.AddComponent<BoxCollider2D>();
        bar.isTrigger = true;
        look = new EchoGateLook(caster, FxSprites.CreateChild(transform, "Look", SortingOrder));
    }

    public void Show(ZoneFrame frame, float scale)
    {
        look.Show(frame, scale);
        bar.size = new Vector2(Settings.width * scale, Settings.thickness);
    }

    public void Close() => Destroy(gameObject);

    public void Shatter() => StartCoroutine(ShatterRoutine());

    private IEnumerator ShatterRoutine()
    {
        bar.enabled = false;
        look.ShowShatter();
        if (sfx != null) SfxPlayback.Play(voice, sfx.shatter);
        yield return new WaitForSeconds(ShatterShowSeconds);
        look.Hide();
        Destroy(gameObject, ShatterSoundTailSeconds);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Bullet shot) && EchoSplit.ShouldSplit(shot)) Split(shot);
    }

    private void Split(Bullet shot)
    {
        Vector2[] directions = EchoSplit.Directions(shot.Direction, Settings.splitCount, Settings.spreadDegrees);
        int middle = EchoSplit.MiddleIndex(directions.Length);
        for (int i = 0; i < directions.Length; i++)
            if (i != middle) SpawnEcho(shot, directions[i]);
        shot.IsEcho = true;
        shot.Redirect(directions[middle]);
        look.PlaySpark(shot.transform.position);
        if (sfx != null) SfxPlayback.Play(voice, sfx.split);
    }

    private static void SpawnEcho(Bullet shot, Vector2 direction)
    {
        Bullet echo = Instantiate(shot, shot.transform.position, shot.transform.rotation);
        echo.damage = shot.damage;
        echo.Kind = shot.Kind;
        echo.IsEcho = true;
        echo.Init(direction, shot.Speed, shot.Owner, shot.OwnerObject);
    }
}
