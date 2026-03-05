using UnityEngine;

public class MapCameraController : MonoBehaviour
{
    private Camera targetCamera;

    public static MapCameraController EnsureExists()
    {
        var existing = FindObjectOfType<MapCameraController>();
        if (existing != null)
        {
            existing.ApplyViewport();
            return existing;
        }

        var camera = Camera.main;
        if (camera == null)
        {
            return null;
        }

        var controller = camera.gameObject.GetComponent<MapCameraController>();
        if (controller == null)
        {
            controller = camera.gameObject.AddComponent<MapCameraController>();
        }

        controller.ApplyViewport();
        return controller;
    }

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        ApplyViewport();
    }

    private void Update()
    {
        ApplyViewport();
    }

    public void ApplyViewport()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                return;
            }
        }

        var topBarScreenHeight = UIHudController.GetTopBarScreenHeight();
        var screenHeight = Mathf.Max(1f, Screen.height);
        var yMin = Mathf.Clamp01(topBarScreenHeight / screenHeight);
        var cameraHeight = Mathf.Clamp01(1f - yMin);

        targetCamera.rect = new Rect(0f, 0f, 1f, cameraHeight);
    }
}
