using UnityEngine;

// Banks the ship's 3D model while it moves sideways, a roll around the
// model's nose axis on top of visualRoot's resting orientation. Purely
// visual: the ship itself never rotates (fixed orientation is a core
// design rule) and its collider is untouched.
public class ShipBank : MonoBehaviour
{
    public Transform visualRoot;
    public Vector3 noseAxis = Vector3.forward; // in the model's own space
    public float maxBankAngle = 35f;
    public float fullBankSpeed = 3f; // sideways world units/second that give the full angle
    public float bankSharpness = 10f;

    private Quaternion restingRotation;
    private float lastX;
    private float currentAngle;

    void Start()
    {
        restingRotation = visualRoot.localRotation;
        lastX = transform.position.x;
    }

    void LateUpdate()
    {
        float sidewaysSpeed = Time.deltaTime > 0f ? (transform.position.x - lastX) / Time.deltaTime : 0f;
        lastX = transform.position.x;
        currentAngle = SmoothTowards(currentAngle, BankAngle(sidewaysSpeed, fullBankSpeed, maxBankAngle));
        visualRoot.localRotation = restingRotation * Quaternion.AngleAxis(currentAngle, noseAxis);
    }

    /// <summary>
    /// Bank angle for a sideways speed: linear in speed, clamped to
    /// ±maxAngle at fullBankSpeed. Moving right gives a negative angle.
    /// </summary>
    public static float BankAngle(float sidewaysSpeed, float fullBankSpeed, float maxAngle)
    {
        return -Mathf.Clamp(sidewaysSpeed / fullBankSpeed, -1f, 1f) * maxAngle;
    }

    /// <summary>
    /// Frame-rate independent exponential approach: lerp by 1 - e^(-k·dt),
    /// so the same fraction of the gap closes per second at any frame rate.
    /// </summary>
    private float SmoothTowards(float current, float target)
    {
        return Mathf.Lerp(current, target, 1f - Mathf.Exp(-bankSharpness * Time.deltaTime));
    }
}
