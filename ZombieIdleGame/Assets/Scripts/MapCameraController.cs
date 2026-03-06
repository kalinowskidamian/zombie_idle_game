using UnityEngine;

public class MapCameraController : MonoBehaviour
{
    private static readonly Vector3 ObliqueEuler = new Vector3(40f, 0f, 45f);
    private static readonly Vector3 ObliquePosition = new Vector3(0f, 8.4f, -15.5f);
    private const float ObliqueOrthoSize = 6.6f;

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

        targetCamera.orthographic = true;
        targetCamera.orthographicSize = ObliqueOrthoSize;
        targetCamera.transform.position = ObliquePosition;
        targetCamera.transform.rotation = Quaternion.Euler(ObliqueEuler);

        var topBarScreenHeight = UIHudController.GetTopBarScreenHeight();
        var screenHeight = Mathf.Max(1f, Screen.height);
        var yMin = Mathf.Clamp01(topBarScreenHeight / screenHeight);
        var cameraHeight = Mathf.Clamp01(1f - yMin);

        targetCamera.rect = new Rect(0f, 0f, 1f, cameraHeight);
    }
}
