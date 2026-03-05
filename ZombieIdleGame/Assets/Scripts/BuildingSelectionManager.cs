using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildingSelectionManager : MonoBehaviour
{
    private static BuildingSelectionManager instance;

    private BuildingInstance selectedBuilding;
    private GameObject panelRoot;
    private Text titleText;
    private Text levelText;
    private Text productionText;
    private Text bonusText;
    private Text detailsText;
    private Button upgradeButton;
    private Text upgradeButtonText;
    private Button sellButton;

    public static BuildingSelectionManager EnsureExists()
    {
        if (instance != null)
        {
            return instance;
        }

        var existing = FindObjectOfType<BuildingSelectionManager>();
        if (existing != null)
        {
            instance = existing;
            existing.EnsurePanelExists();
            return existing;
        }

        var managerObject = new GameObject("BuildingSelectionManager");
        instance = managerObject.AddComponent<BuildingSelectionManager>();
        instance.EnsurePanelExists();
        return instance;
    }

    public static BuildingSelectionManager InstanceOrNull => instance;

    public BuildingInstance SelectedBuilding => selectedBuilding;

    public void SelectBuilding(BuildingInstance building)
    {
        if (building == null)
        {
            ClearSelection();
            return;
        }

        selectedBuilding = building;
        EnsurePanelExists();
        panelRoot.SetActive(true);
        RefreshPanel();

        var gridManager = GridManager.Instance;
        if (gridManager != null)
        {
            gridManager.ApplySelectionVisuals(selectedBuilding);
        }
    }

    public void ClearSelection()
    {
        selectedBuilding = null;
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        var gridManager = GridManager.Instance;
        if (gridManager != null)
        {
            gridManager.ApplySelectionVisuals(null);
        }
    }

    public void RefreshPanel()
    {
        if (selectedBuilding == null || panelRoot == null)
        {
            return;
        }

        var state = GameBootstrap.State;
        if (state?.buildingInstances == null || !state.buildingInstances.Contains(selectedBuilding))
        {
            ClearSelection();
            return;
        }

        var level = Mathf.Max(1, selectedBuilding.level);
        titleText.text = BuildingCatalog.GetDisplayName(selectedBuilding.buildingId);
        levelText.text = $"Level: {level}";

        var production = ProductionCalculator.GetBuildingProductionPerSecond(state, selectedBuilding);
        productionText.text = BuildingCatalog.GetBaseProduction(selectedBuilding.buildingId) > 0d
            ? $"Production/s: {production:F2}"
            : "Production/s: N/A";

        var bonusPercent = ProductionCalculator.GetTotalBonusPercentForBuilding(state, selectedBuilding);
        var buffCount = ProductionCalculator.GetBuffingMausoleumCount(state, selectedBuilding);
        bonusText.text = $"Bonus: +{bonusPercent:F0}% ({buffCount} mausoleum)";

        if (selectedBuilding.buildingId == BuildingCatalog.MausoleumId)
        {
            var buffedBuildings = ProductionCalculator.GetBuildingsBuffedByMausoleum(state, selectedBuilding);
            detailsText.text = BuildMausoleumDetails(buffedBuildings, level);
        }
        else
        {
            detailsText.text = buffCount > 0
                ? $"Buff sources: {buffCount} mausoleum nearby"
                : "Buff sources: none";
        }

        var upgradeCost = BuildingCatalog.GetUpgradeCost(selectedBuilding.buildingId, level);
        var canUpgrade = state.ectoplasm >= upgradeCost;
        upgradeButton.interactable = canUpgrade;
        upgradeButtonText.text = $"Upgrade ({upgradeCost})";
    }

    private string BuildMausoleumDetails(List<BuildingInstance> buffedBuildings, int level)
    {
        var bonus = BuildingCatalog.GetMausoleumBonusPercent(level);
        if (buffedBuildings.Count == 0)
        {
            return $"Buffuje 0 budynków\nBonus aura: +{bonus:F0}%";
        }

        var lines = new List<string>();
        lines.Add($"Buffuje {buffedBuildings.Count} budynków");
        lines.Add($"Bonus aura: +{bonus:F0}%");

        var maxLines = Math.Min(3, buffedBuildings.Count);
        for (var i = 0; i < maxLines; i++)
        {
            var building = buffedBuildings[i];
            lines.Add($"- {BuildingCatalog.GetDisplayName(building.buildingId)} ({building.x},{building.y})");
        }

        if (buffedBuildings.Count > maxLines)
        {
            lines.Add("...");
        }

        return string.Join("\n", lines);
    }

    private void HandleUpgradeClicked()
    {
        var state = GameBootstrap.State;
        if (state == null || selectedBuilding == null)
        {
            return;
        }

        var level = Mathf.Max(1, selectedBuilding.level);
        var upgradeCost = BuildingCatalog.GetUpgradeCost(selectedBuilding.buildingId, level);
        if (state.ectoplasm < upgradeCost)
        {
            RefreshPanel();
            return;
        }

        state.ectoplasm -= upgradeCost;
        selectedBuilding.level = level + 1;
        state.lastSavedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        SaveSystem.Save(state);

        GridManager.Instance?.RefreshVisualsFromState();
        SelectBuilding(selectedBuilding);
    }

    private void HandleSellClicked()
    {
        var state = GameBootstrap.State;
        if (state?.buildingInstances == null || selectedBuilding == null)
        {
            return;
        }

        var totalSpent = BuildingCatalog.GetTotalSpentForLevel(selectedBuilding.buildingId, selectedBuilding.level);
        var refund = (long)Math.Round(totalSpent * 0.5d, MidpointRounding.AwayFromZero);

        state.ectoplasm += refund;
        state.buildingInstances.Remove(selectedBuilding);
        state.lastSavedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        SaveSystem.Save(state);

        GridManager.Instance?.RefreshVisualsFromState();
        ClearSelection();
    }

    private void EnsurePanelExists()
    {
        if (panelRoot != null)
        {
            return;
        }

        var hud = UIHudController.EnsureHudExists();
        var canvas = hud.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        panelRoot = new GameObject("BuildingInfoPanel", typeof(RectTransform), typeof(Image));
        panelRoot.transform.SetParent(canvas.transform, false);

        var rect = panelRoot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-20f, -20f);
        rect.sizeDelta = new Vector2(430f, 300f);

        var bg = panelRoot.GetComponent<Image>();
        bg.color = new Color(0.07f, 0.08f, 0.11f, 0.92f);

        titleText = CreateText("Name", new Vector2(16f, -14f), new Vector2(260f, 34f), 28, TextAnchor.MiddleLeft, font);
        levelText = CreateText("Level", new Vector2(16f, -56f), new Vector2(300f, 28f), 22, TextAnchor.MiddleLeft, font);
        productionText = CreateText("Production", new Vector2(16f, -90f), new Vector2(390f, 26f), 20, TextAnchor.MiddleLeft, font);
        bonusText = CreateText("Bonus", new Vector2(16f, -120f), new Vector2(390f, 26f), 20, TextAnchor.MiddleLeft, font);
        detailsText = CreateText("Details", new Vector2(16f, -150f), new Vector2(390f, 90f), 18, TextAnchor.UpperLeft, font);

        upgradeButton = CreateButton("UpgradeButton", "Upgrade", new Vector2(16f, -250f), new Color(0.2f, 0.45f, 0.25f, 0.95f), font);
        upgradeButtonText = upgradeButton.GetComponentInChildren<Text>();
        sellButton = CreateButton("SellButton", "Sell", new Vector2(162f, -250f), new Color(0.55f, 0.28f, 0.18f, 0.95f), font);
        var closeButton = CreateButton("CloseButton", "X", new Vector2(374f, -10f), new Color(0.45f, 0.18f, 0.18f, 0.95f), font, new Vector2(40f, 34f));

        upgradeButton.onClick.AddListener(HandleUpgradeClicked);
        sellButton.onClick.AddListener(HandleSellClicked);
        closeButton.onClick.AddListener(ClearSelection);

        panelRoot.SetActive(false);

        Text CreateText(string name, Vector2 anchoredPos, Vector2 size, int fontSize, TextAnchor anchor, Font usedFont)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(panelRoot.transform, false);

            var objRect = obj.GetComponent<RectTransform>();
            objRect.anchorMin = new Vector2(0f, 1f);
            objRect.anchorMax = new Vector2(0f, 1f);
            objRect.pivot = new Vector2(0f, 1f);
            objRect.anchoredPosition = anchoredPos;
            objRect.sizeDelta = size;

            var txt = obj.GetComponent<Text>();
            txt.font = usedFont;
            txt.fontSize = fontSize;
            txt.alignment = anchor;
            txt.color = Color.white;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            return txt;
        }

        Button CreateButton(string name, string label, Vector2 anchoredPos, Color color, Font usedFont, Vector2? customSize = null)
        {
            var btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(panelRoot.transform, false);

            var btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0f, 1f);
            btnRect.anchorMax = new Vector2(0f, 1f);
            btnRect.pivot = new Vector2(0f, 1f);
            btnRect.anchoredPosition = anchoredPos;
            btnRect.sizeDelta = customSize ?? new Vector2(136f, 44f);

            var image = btnObj.GetComponent<Image>();
            image.color = color;

            var labelObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            labelObj.transform.SetParent(btnObj.transform, false);
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var txt = labelObj.GetComponent<Text>();
            txt.font = usedFont;
            txt.fontSize = 20;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = label;

            return btnObj.GetComponent<Button>();
        }
    }
}
