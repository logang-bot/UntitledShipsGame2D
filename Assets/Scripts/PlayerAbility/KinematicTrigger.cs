using UnityEngine;

// Bullets carry no Rigidbody2D, so a trigger that must notice them needs a
// kinematic body of its own for Unity to send the trigger messages.
public static class KinematicTrigger
{
    public static void AddBodyTo(GameObject zone)
    {
        Rigidbody2D body = zone.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.simulated = true;
    }
}
