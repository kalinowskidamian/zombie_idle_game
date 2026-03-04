using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIHudController : MonoBehaviour
{
    [SerializeField] private Text ectoplasmText;
    [SerializeField] private Button addTenButton;

    public static UIHudController EnsureHudExists()
    {
        var existing = FindObjectOfType<UIHudController>();
        if (existing != null)
        {
            existing.AutoAssignReferencesIfMissing();
            return existing;
        }

        var canvasObject = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        CreateEventSystemIfMissing();

        var hudController = canvasObject.AddComponent<UIHudController>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var labelObject = new GameObject("EctoplasmLabel", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(canvasObject.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = new Vector2(20f, -20f);
        labelRect.sizeDelta = new Vector2(320f, 50f);

        var label = labelObject.GetComponent<Text>();
        label.font = font;
        label.fontSize = 32;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleLeft;

        var buttonObject = new GameObject("AddTenButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvasObject.transform, false);
        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.anchoredPosition = new Vector2(20f, -80f);
        buttonRect.sizeDelta = new Vector2(140f, 50f);

        var buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 0.2f, 0.9f);
        var button = buttonObject.GetComponent<Button>();

        var buttonTextObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        buttonTextObject.transform.SetParent(buttonObject.transform, false);
        var buttonTextRect = buttonTextObject.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        var buttonText = buttonTextObject.GetComponent<Text>();
        buttonText.font = font;
        buttonText.fontSize = 28;
        buttonText.text = "+10";
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;

        hudController.ectoplasmText = label;
        hudController.addTenButton = button;
        return hudController;
    }

    private static void CreateEventSystemIfMissing()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void Awake()
    {
        AutoAssignReferencesIfMissing();
    }

    private void OnEnable()
    {
        if (addTenButton != null)
        {
            addTenButton.onClick.AddListener(HandleAddTenClicked);
        }
    }

    private void Start()
    {
        RefreshEctoplasmLabel();
    }

    private void Update()
    {
        RefreshEctoplasmLabel();
    }

    private void OnDisable()
    {
        if (addTenButton != null)
        {
            addTenButton.onClick.RemoveListener(HandleAddTenClicked);
        }
    }

    private void HandleAddTenClicked()
    {
        if (GameBootstrap.State == null)
        {
            return;
        }

        GameBootstrap.State.ectoplasm += 10;
        GameBootstrap.State.lastSavedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        SaveSystem.Save(GameBootstrap.State);
        RefreshEctoplasmLabel();
    }

    private void RefreshEctoplasmLabel()
    {
        if (ectoplasmText == null)
        {
            return;
        }

        var ectoplasm = GameBootstrap.State?.ectoplasm ?? 0;
        ectoplasmText.text = $"Ectoplasm: {ectoplasm}";
    }

    private void AutoAssignReferencesIfMissing()
    {
        if (ectoplasmText == null)
        {
            ectoplasmText = FindObjectOfType<Text>();
        }

        if (addTenButton == null)
        {
            addTenButton = FindObjectOfType<Button>();
        }
    }
}
