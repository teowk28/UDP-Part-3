using UnityEngine;
using System.Collections;

public class CameraShaker : MonoBehaviour
{
    // Singleton instance
    public static CameraShaker Instance { get; private set; }

    // The camera transform to shake
    private Transform cameraTransform;

    // Original position of the camera
    private Vector3 originalPosition;

    private void Awake()
    {
        // Set up singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Get the camera transform
        cameraTransform = Camera.main.transform;
        originalPosition = cameraTransform.localPosition;
    }

    public void ShakeCamera(float duration = 0.25f, float magnitude = 0.1f)
    {
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // Calculate a random offset
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            // Apply the shake
            cameraTransform.localPosition = new Vector3(originalPosition.x + x, originalPosition.y + y, originalPosition.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset the camera position
        cameraTransform.localPosition = originalPosition;
    }
}