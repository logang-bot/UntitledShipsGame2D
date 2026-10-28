using UnityEngine;

// Spins one part of a ship model in place (Lumen's halo ring, Echo's gyro
// rings), as in the design's animated preview.
public class ShipPartSpinner : MonoBehaviour
{
    public Vector3 axis = Vector3.up;
    public float degreesPerSecond = 23f;

    void Update()
    {
        transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
