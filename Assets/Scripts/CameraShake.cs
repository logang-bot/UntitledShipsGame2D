using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.15f;

    private Vector3 basePosition;
    private Coroutine shakeCoroutine;

    void Awake()
    {
        basePosition = transform.localPosition;
    }

    public void Shake()
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shakeDuration;
            float magnitude = shakeMagnitude * (1f - t);
            transform.localPosition = basePosition + (Vector3)(Random.insideUnitCircle * magnitude);
            yield return null;
        }
        transform.localPosition = basePosition;
        shakeCoroutine = null;
    }
}
