using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIHudController : MonoBehaviour
{
    public enum InteractionMode
    {
        Select,
        Build
    }

    [SerializeField] private Text ectoplasmText;
    [SerializeField] private Text selectedBuildingText;
    [SerializeField] private Text uncollectedText;
    [SerializeField] private Button addTenButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button selectModeButton;
    [SerializeField] private Button graveSelectButton;
    [SerializeField] private Button morgueSelectButton;
    [SerializeField] private Button mausoleumSelectButton;

    public static InteractionMode CurrentMode { get; private set; } = InteractionMode.Select;
    public static string SelectedBuildingId { get; private set; }

    public static bool TryGetBuildBuildingId(out string buildingId)
    {
        buildingId = SelectedBuildingId;
        return CurrentMode == InteractionMode.Build && BuildingCatalog.IsKnownBuilding(buildingId);
    }

    public static UIHudController EnsureHudExists()
    {
        var existing = FindObjectOfType<UIHudController>();
        if (existing != null)
        {
            existing.AutoAssignReferencesIfMissing();
            existing.RefreshSelectedBuildingLabel();
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
        labelRect.sizeDelta = new Vector2(420f, 50f);

        var label = labelObject.GetComponent<Text>();
        label.font = font;
        label.fontSize = 32;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleLeft;

        var selectedLabelObject = new GameObject("SelectedBuildingLabel", typeof(RectTransform), typeof(Text));
        selectedLabelObject.transform.SetParent(canvasObject.transform, false);
        var selectedLabelRect = selectedLabelObject.GetComponent<RectTransform>();
        selectedLabelRect.anchorMin = new Vector2(0f, 1f);
        selectedLabelRect.anchorMax = new Vector2(0f, 1f);
        selectedLabelRect.pivot = new Vector2(0f, 1f);
        selectedLabelRect.anchoredPosition = new Vector2(20f, -130f);
        selectedLabelRect.sizeDelta = new Vector2(520f, 40f);

        var selectedLabel = selectedLabelObject.GetComponent<Text>();
        selectedLabel.font = font;
        selectedLabel.fontSize = 24;
        selectedLabel.color = Color.white;
        selectedLabel.alignment = TextAnchor.MiddleLeft;

        var uncollectedLabelObject = new GameObject("UncollectedLabel", typeof(RectTransform), typeof(Text));
        uncollectedLabelObject.transform.SetParent(canvasObject.transform, false);
        var uncollectedLabelRect = uncollectedLabelObject.GetComponent<RectTransform>();
        uncollectedLabelRect.anchorMin = new Vector2(0f, 1f);
        uncollectedLabelRect.anchorMax = new Vector2(0f, 1f);
        uncollectedLabelRect.pivot = new Vector2(0f, 1f);
        uncollectedLabelRect.anchoredPosition = new Vector2(20f, -165f);
        uncollectedLabelRect.sizeDelta = new Vector2(520f, 36f);

        var uncollectedLabel = uncollectedLabelObject.GetComponent<Text>();
        uncollectedLabel.font = font;
        uncollectedLabel.fontSize = 22;
        uncollectedLabel.color = new Color(0.75f, 1f, 0.8f, 1f);
        uncollectedLabel.alignment = TextAnchor.MiddleLeft;

        var button = CreateButton(canvasObject.transform, font, "AddTenButton", "+10", new Vector2(20f, -80f), new Color(0.2f, 0.6f, 0.2f, 0.9f));
        var reset = CreateButton(canvasObject.transform, font, "ResetButton", "RESET", new Vector2(180f, -80f), new Color(0.7f, 0.2f, 0.2f, 0.9f));

        var selectButton = CreateButton(canvasObject.transform, font, "SelectModeButton", "Select", new Vector2(20f, -180f), new Color(0.2f, 0.35f, 0.6f, 0.9f));
        var graveButton = CreateButton(canvasObject.transform, font, "GraveSelectButton", "Grave", new Vector2(180f, -180f), new Color(0.2f, 0.45f, 0.25f, 0.9f));
        var morgueButton = CreateButton(canvasObject.transform, font, "MorgueSelectButton", "Morgue", new Vector2(340f, -180f), new Color(0.5f, 0.2f, 0.2f, 0.9f));
        var mausoleumButton = CreateButton(canvasObject.transform, font, "MausoleumSelectButton", "Mausoleum", new Vector2(500f, -180f), new Color(0.4f, 0.2f, 0.55f, 0.9f));

        hudController.ectoplasmText = label;
        hudController.selectedBuildingText = selectedLabel;
        hudController.uncollectedText = uncollectedLabel;
        hudController.addTenButton = button;
        hudController.resetButton = reset;
        hudController.selectModeButton = selectButton;
        hudController.graveSelectButton = graveButton;
        hudController.morgueSelectButton = morgueButton;
        hudController.mausoleumSelectButton = mausoleumButton;
        return hudController;
    }

    private static Button CreateButton(Transform parent, Font font, string name, string text, Vector2 anchoredPos, Color backgroundColor)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.anchoredPosition = anchoredPos;
        buttonRect.sizeDelta = new Vector2(140f, 50f);

        var buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = backgroundColor;

        var buttonTextObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        buttonTextObject.transform.SetParent(buttonObject.transform, false);
        var buttonTextRect = buttonTextObject.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        var buttonText = buttonTextObject.GetComponent<Text>();
        buttonText.font = font;
        buttonText.fontSize = 24;
        buttonText.text = text;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;

        return buttonObject.GetComponent<Button>();
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

        if (graveSelectButton != null)
        {
            graveSelectButton.onClick.RemoveListener(HandleGraveSelected);
            graveSelectButton.onClick.AddListener(HandleGraveSelected);
        }

        if (selectModeButton != null)
        {
            selectModeButton.onClick.RemoveListener(HandleSelectModeSelected);
            selectModeButton.onClick.AddListener(HandleSelectModeSelected);
        }

        if (morgueSelectButton != null)
        {
            morgueSelectButton.onClick.RemoveListener(HandleMorgueSelected);
            morgueSelectButton.onClick.AddListener(HandleMorgueSelected);
        }

        if (mausoleumSelectButton != null)
        {
            mausoleumSelectButton.onClick.RemoveListener(HandleMausoleumSelected);
            mausoleumSelectButton.onClick.AddListener(HandleMausoleumSelected);
        }

        RefreshEctoplasmLabel();
        RefreshUncollectedLabel();
        RefreshSelectedBuildingLabel();
    }

    private void Update()
    {
        RefreshEctoplasmLabel();
        RefreshUncollectedLabel();
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

        if (graveSelectButton != null)
        {
            graveSelectButton.onClick.RemoveListener(HandleGraveSelected);
        }

        if (selectModeButton != null)
        {
            selectModeButton.onClick.RemoveListener(HandleSelectModeSelected);
        }

        if (morgueSelectButton != null)
        {
            morgueSelectButton.onClick.RemoveListener(HandleMorgueSelected);
        }

        if (mausoleumSelectButton != null)
        {
            mausoleumSelectButton.onClick.RemoveListener(HandleMausoleumSelected);
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
        RefreshUncollectedLabel();
    }

    private void HandleResetClicked()
    {
        SaveSystem.Delete();
        GameBootstrap.ResetState();
        GameBootstrap.State.buildingInstances?.Clear();
        GameBootstrap.State.ectoplasm = 5000;
        GameBootstrap.State.bones = 0;
        SaveSystem.Save(GameBootstrap.State);
        GridManager.ClearBuildingsVisuals();
        GridManager.Instance?.RefreshVisualsFromState();
        BuildingSelectionManager.InstanceOrNull?.ClearSelection();

        EnterSelectMode();
        RefreshEctoplasmLabel();
        RefreshUncollectedLabel();
    }

    private void HandleSelectModeSelected()
    {
        EnterSelectMode();
    }

    private void HandleGraveSelected()
    {
        EnterBuildMode(BuildingCatalog.GraveId);
    }

    private void HandleMorgueSelected()
    {
        EnterBuildMode(BuildingCatalog.MorgueId);
    }

    private void HandleMausoleumSelected()
    {
        EnterBuildMode(BuildingCatalog.MausoleumId);
    }

    private void EnterSelectMode()
    {
        CurrentMode = InteractionMode.Select;
        SelectedBuildingId = null;
        BuildingSelectionManager.InstanceOrNull?.ClearSelection();
        RefreshSelectedBuildingLabel();
    }

    private void EnterBuildMode(string buildingId)
    {
        if (!BuildingCatalog.IsKnownBuilding(buildingId))
        {
            return;
        }

        CurrentMode = InteractionMode.Build;
        SelectedBuildingId = buildingId;
        BuildingSelectionManager.InstanceOrNull?.ClearSelection();
        RefreshSelectedBuildingLabel();
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

    private void RefreshUncollectedLabel()
    {
        if (uncollectedText == null)
        {
            return;
        }

        var total = 0d;
        var state = GameBootstrap.State;
        if (state?.buildingInstances != null)
        {
            for (var i = 0; i < state.buildingInstances.Count; i++)
            {
                total += state.buildingInstances[i].storedEctoplasm;
            }
        }

        uncollectedText.text = $"Uncollected: {(long)Math.Floor(total)}";
    }

    private void RefreshSelectedBuildingLabel()
    {
        if (selectedBuildingText == null)
        {
            return;
        }

        if (CurrentMode == InteractionMode.Select)
        {
            selectedBuildingText.text = "Mode: Select";
            return;
        }

        var selectedName = BuildingCatalog.IsKnownBuilding(SelectedBuildingId)
            ? BuildingCatalog.GetDisplayName(SelectedBuildingId)
            : "Unknown";
        selectedBuildingText.text = $"Mode: Build ({selectedName})";
    }

    private void AutoAssignReferencesIfMissing()
    {
        if (ectoplasmText == null)
        {
            var text = GameObject.Find("EctoplasmLabel");
            if (text != null)
            {
                ectoplasmText = text.GetComponent<Text>();
            }
        }

        if (selectedBuildingText == null)
        {
            var selectedLabel = GameObject.Find("SelectedBuildingLabel");
            if (selectedLabel != null)
            {
                selectedBuildingText = selectedLabel.GetComponent<Text>();
            }
        }

        if (uncollectedText == null)
        {
            var uncollectedLabel = GameObject.Find("UncollectedLabel");
            if (uncollectedLabel != null)
            {
                uncollectedText = uncollectedLabel.GetComponent<Text>();
            }
        }

        if (addTenButton == null)
        {
            var addTen = GameObject.Find("AddTenButton");
            if (addTen != null)
            {
                addTenButton = addTen.GetComponent<Button>();
            }
        }

        if (resetButton == null)
        {
            var reset = GameObject.Find("ResetButton");
            if (reset != null)
            {
                resetButton = reset.GetComponent<Button>();
            }
        }

        if (graveSelectButton == null)
        {
            var button = GameObject.Find("GraveSelectButton");
            if (button != null)
            {
                graveSelectButton = button.GetComponent<Button>();
            }
        }

        if (selectModeButton == null)
        {
            var button = GameObject.Find("SelectModeButton");
            if (button != null)
            {
                selectModeButton = button.GetComponent<Button>();
            }
        }

        if (morgueSelectButton == null)
        {
            var button = GameObject.Find("MorgueSelectButton");
            if (button != null)
            {
                morgueSelectButton = button.GetComponent<Button>();
            }
        }

        if (mausoleumSelectButton == null)
        {
            var button = GameObject.Find("MausoleumSelectButton");
            if (button != null)
            {
                mausoleumSelectButton = button.GetComponent<Button>();
            }
        }
    }
}
