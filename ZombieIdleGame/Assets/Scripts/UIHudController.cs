using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIHudController : MonoBehaviour
{
    [SerializeField] private Text ectoplasmText;
    [SerializeField] private Button addTenButton;
    [SerializeField] private Button resetButton;

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

        var resetButtonObject = new GameObject("ResetButton", typeof(RectTransform), typeof(Image), typeof(Button));
        resetButtonObject.transform.SetParent(canvasObject.transform, false);
        var resetButtonRect = resetButtonObject.GetComponent<RectTransform>();
        resetButtonRect.anchorMin = new Vector2(0f, 1f);
        resetButtonRect.anchorMax = new Vector2(0f, 1f);
        resetButtonRect.pivot = new Vector2(0f, 1f);
        resetButtonRect.anchoredPosition = new Vector2(180f, -80f);
        resetButtonRect.sizeDelta = new Vector2(140f, 50f);

        var resetButtonImage = resetButtonObject.GetComponent<Image>();
        resetButtonImage.color = new Color(0.7f, 0.2f, 0.2f, 0.9f);
        var reset = resetButtonObject.GetComponent<Button>();

        var resetButtonTextObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        resetButtonTextObject.transform.SetParent(resetButtonObject.transform, false);
        var resetButtonTextRect = resetButtonTextObject.GetComponent<RectTransform>();
        resetButtonTextRect.anchorMin = Vector2.zero;
        resetButtonTextRect.anchorMax = Vector2.one;
        resetButtonTextRect.offsetMin = Vector2.zero;
        resetButtonTextRect.offsetMax = Vector2.zero;

        var resetButtonText = resetButtonTextObject.GetComponent<Text>();
        resetButtonText.font = font;
        resetButtonText.fontSize = 28;
        resetButtonText.text = "RESET";
        resetButtonText.alignment = TextAnchor.MiddleCenter;
        resetButtonText.color = Color.white;

        hudController.ectoplasmText = label;
        hudController.addTenButton = button;
        hudController.resetButton = reset;
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
        AutoAssignReferencesIfMissing();
    }

    private void Start()
    {
        AutoAssignReferencesIfMissing();

        if (addTenButton != null)
        {
            addTenButton.onClick.RemoveListener(HandleAddTenClicked);
            addTenButton.onClick.AddListener(HandleAddTenClicked);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(HandleResetClicked);
            resetButton.onClick.AddListener(HandleResetClicked);
        }

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

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(HandleResetClicked);
        }
    }

    private void HandleAddTenClicked()
    {
        Debug.Log("Clicked +10");

        if (GameBootstrap.State == null)
        {
            return;
        }

        GameBootstrap.State.ectoplasm += 10;
        GameBootstrap.State.lastSavedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        SaveSystem.Save(GameBootstrap.State);
        RefreshEctoplasmLabel();
    }

    private void HandleResetClicked()
    {
        SaveSystem.Delete();
        GameBootstrap.ResetState();
        GameBootstrap.State.buildingInstances?.Clear();
        SaveSystem.Save(GameBootstrap.State);
        GridManager.ClearBuildingsVisuals();

        Debug.Log("Save reset");
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
            var buttons = FindObjectsOfType<Button>();
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == "AddTenButton")
                {
                    addTenButton = buttons[i];
                    break;
                }
            }
        }

        if (resetButton == null)
        {
            var buttons = FindObjectsOfType<Button>();
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == "ResetButton")
                {
                    resetButton = buttons[i];
                    break;
                }
            }
        }
    }
}
