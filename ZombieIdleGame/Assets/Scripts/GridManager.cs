using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    private const int GridWidth = 8;
    private const int GridHeight = 8;
    private static GridManager instance;
    private static Sprite cachedSquareSprite;
    private static Sprite cachedOutlineSprite;
    private static Sprite cachedCircleSprite;

    private readonly HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, BuildingVisual> buildingVisuals = new Dictionary<Vector2Int, BuildingVisual>();
    private readonly List<SpriteRenderer> mausoleumRangeOverlays = new List<SpriteRenderer>();
    private readonly List<FloatingText> floatingTexts = new List<FloatingText>();

    private Camera mainCamera;
    private Grid grid;
    private Transform tileRoot;
    private Transform buildingRoot;
    private Transform overlayRoot;
    private Transform indicatorRoot;
    private SpriteRenderer ghostRenderer;
    private SpriteRenderer highlightRenderer;
    private SpriteRenderer selectionRenderer;

    private sealed class BuildingVisual
    {
        public BuildingInstance Building;
        public SpriteRenderer Renderer;
        public SpriteRenderer CollectIndicator;
        public TextMesh BuildCountdownText;
        public Color BaseColor;
    }

    private sealed class FloatingText
    {
        public TextMesh TextMesh;
        public float Lifetime;
    }

    public static GridManager Instance => instance;

    public static GridManager EnsureGridExists()
    {
        var existing = FindObjectOfType<GridManager>();
        if (existing != null)
        {
            return existing;
        }

        var gridObject = new GameObject("GridRoot");
        return gridObject.AddComponent<GridManager>();
    }

    public static void ClearBuildingsVisuals()
    {
        if (instance == null)
        {
            return;
        }

        instance.ClearBuildingVisualsInternal();
    }

    public void RefreshVisualsFromState()
    {
        RebuildBuildingsFromState();
    }

    public void ApplySelectionVisuals(BuildingInstance selected)
    {
        for (var i = 0; i < mausoleumRangeOverlays.Count; i++)
        {
            if (mausoleumRangeOverlays[i] != null)
            {
                Destroy(mausoleumRangeOverlays[i].gameObject);
            }
        }

        mausoleumRangeOverlays.Clear();

        foreach (var visual in buildingVisuals.Values)
        {
            if (visual?.Renderer == null)
            {
                continue;
            }

            visual.Renderer.color = visual.BaseColor;
        }

        if (selectionRenderer != null)
        {
            selectionRenderer.gameObject.SetActive(false);
        }

        if (selected == null)
        {
            return;
        }

        var gridPos = new Vector2Int(selected.x, selected.y);
        if (selectionRenderer != null)
        {
            selectionRenderer.transform.position = GridToWorldCenter(gridPos) + new Vector3(0f, 0f, -0.015f);
            selectionRenderer.gameObject.SetActive(true);
        }

        if (!buildingVisuals.TryGetValue(gridPos, out var selectedVisual))
        {
            return;
        }

        selectedVisual.Renderer.color = Color.Lerp(selectedVisual.BaseColor, Color.white, 0.45f);

        if (selected.buildingId != BuildingCatalog.MausoleumId)
        {
            return;
        }

        HighlightMausoleumRangeAndTargets(selected);
    }

    private void Awake()
    {
        instance = this;
        mainCamera = Camera.main;
        grid = GetComponent<Grid>();
        if (grid == null)
        {
            grid = gameObject.AddComponent<Grid>();
        }

        EnsureRoots();
        if (!HasBackgroundTilemap())
        {
            DrawGridTiles();
        }

        CreateHoverVisuals();
        RebuildBuildingsFromState();
    }

    private void Update()
    {
        UpdateHoverVisuals();
        UpdateCollectIndicators();
        UpdateBuildingCountdowns();
        UpdateFloatingTexts();

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        if (!TryGetGridPositionFromMouse(out var gridPos))
        {
            return;
        }

        if (UIHudController.CurrentMode == UIHudController.InteractionMode.Select)
        {
            HandleSelectModeClick(gridPos);
            return;
        }

        TryPlaceBuilding(gridPos);
    }


    private void HandleSelectModeClick(Vector2Int gridPos)
    {
        if (TrySelectExistingBuilding(gridPos))
        {
            return;
        }

        BuildingSelectionManager.EnsureExists().ClearSelection();
    }

    private bool TrySelectExistingBuilding(Vector2Int gridPos)
    {
        var building = GetBuildingAt(gridPos);
        if (building == null)
        {
            return false;
        }

        var state = GameBootstrap.State;
        var shouldCollect = BuildingTiming.CanCollect(state, building);
        var collected = shouldCollect ? TryCollectFromBuilding(building) : 0;
        if (collected > 0)
        {
            return true;
        }

        BuildingSelectionManager.EnsureExists().SelectBuilding(building);
        return true;
    }

    private BuildingInstance GetBuildingAt(Vector2Int gridPos)
    {
        var state = GameBootstrap.State;
        if (state?.buildingInstances == null)
        {
            return null;
        }

        for (var i = 0; i < state.buildingInstances.Count; i++)
        {
            var building = state.buildingInstances[i];
            if (building.x == gridPos.x && building.y == gridPos.y)
            {
                return building;
            }
        }

        return null;
    }

    private long TryCollectFromBuilding(BuildingInstance building)
    {
        if (building == null)
        {
            return 0;
        }

        var state = GameBootstrap.State;
        if (!BuildingTiming.CanCollect(state, building))
        {
            return 0;
        }

        var resource = BuildingCatalog.GetProducedResource(building.buildingId);
        if (resource == ResourceKind.None)
        {
            return 0;
        }

        var whole = ResourceLedger.RemoveWholeStored(building, resource);
        if (whole <= 0)
        {
            return 0;
        }

        if (state == null)
        {
            return 0;
        }

        ResourceLedger.AddToBank(state, resource, whole);
        state.lastSavedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        SaveSystem.Save(state);

        ShowFloatingText(new Vector2Int(building.x, building.y), $"+{whole}", resource);
        RefreshVisualsFromState();

        return whole;
    }

    private void TryPlaceBuilding(Vector2Int gridPos)
    {
        if (!CanPlaceBuilding(gridPos, out _))
        {
            return;
        }

        if (!UIHudController.TryGetBuildBuildingId(out var buildingId))
        {
            return;
        }
        var cost = BuildingCatalog.GetCost(buildingId);

        var state = GameBootstrap.State;
        state.ectoplasm -= cost;
        state.buildingInstances ??= new List<BuildingInstance>();
        var instanceBuilding = new BuildingInstance(buildingId, gridPos.x, gridPos.y, 1);
        BuildingTiming.StartBuild(instanceBuilding, BuildingTiming.InitialBuildSeconds, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        state.buildingInstances.Add(instanceBuilding);
        state.lastSavedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        SpawnBuildingVisual(instanceBuilding, gridPos, BuildingCatalog.GetColor(buildingId));
        occupiedCells.Add(gridPos);

        SaveSystem.Save(state);
    }

    private bool TryGetGridPositionFromMouse(out Vector2Int gridPos)
    {
        var mousePosition = Input.mousePosition;
        mousePosition.z = Mathf.Abs(mainCamera.transform.position.z - 0f);

        var worldPos = mainCamera.ScreenToWorldPoint(mousePosition);
        worldPos.z = 0f;

        var cell = grid.WorldToCell(worldPos);
        gridPos = new Vector2Int(cell.x, cell.y);

        var isInside = cell.x >= 0 && cell.x < GridWidth && cell.y >= 0 && cell.y < GridHeight;
        return isInside;
    }

    private bool CanPlaceBuilding(Vector2Int gridPos, out string buildingId)
    {
        if (!UIHudController.TryGetBuildBuildingId(out buildingId) || occupiedCells.Contains(gridPos))
        {
            return false;
        }

        var state = GameBootstrap.State;
        if (state == null)
        {
            return false;
        }

        var cost = BuildingCatalog.GetCost(buildingId);
        return state.ectoplasm >= cost;
    }

    private Vector3 GridToWorldCenter(Vector2Int gridPos)
    {
        var cell = new Vector3Int(gridPos.x, gridPos.y, 0);
        var center = grid.GetCellCenterWorld(cell);
        center.z = 0f;
        return center;
    }

    private void EnsureRoots()
    {
        var tiles = transform.Find("Tiles");
        if (tiles == null)
        {
            var tileObject = new GameObject("Tiles");
            tileObject.transform.SetParent(transform, false);
            tiles = tileObject.transform;
        }

        var buildings = transform.Find("Buildings");
        if (buildings == null)
        {
            var buildingObject = new GameObject("Buildings");
            buildingObject.transform.SetParent(transform, false);
            buildings = buildingObject.transform;
        }

        var overlays = transform.Find("Overlays");
        if (overlays == null)
        {
            var overlayObject = new GameObject("Overlays");
            overlayObject.transform.SetParent(transform, false);
            overlays = overlayObject.transform;
        }

        tileRoot = tiles;
        buildingRoot = buildings;
        overlayRoot = overlays;

        var indicators = transform.Find("Indicators");
        if (indicators == null)
        {
            var indicatorsObject = new GameObject("Indicators");
            indicatorsObject.transform.SetParent(transform, false);
            indicators = indicatorsObject.transform;
        }

        indicatorRoot = indicators;
    }

    private bool HasBackgroundTilemap()
    {
        var tilemapTransform = transform.Find("BackgroundTilemap");
        return tilemapTransform != null && tilemapTransform.GetComponent<Tilemap>() != null;
    }

    private void DrawGridTiles()
    {
        if (tileRoot.childCount > 0)
        {
            return;
        }

        for (var x = 0; x < GridWidth; x++)
        {
            for (var y = 0; y < GridHeight; y++)
            {
                var cellObject = new GameObject($"Cell_{x}_{y}");
                cellObject.transform.SetParent(tileRoot, false);
                cellObject.transform.position = GridToWorldCenter(new Vector2Int(x, y));
                cellObject.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

                var renderer = cellObject.AddComponent<SpriteRenderer>();
                renderer.sprite = GetSquareSprite();
                renderer.color = new Color(0.12f, 0.16f, 0.12f, 0.55f);
                renderer.sortingOrder = 0;
            }
        }
    }

    private void CreateHoverVisuals()
    {
        if (highlightRenderer == null)
        {
            var highlightObject = new GameObject("HoverHighlight");
            highlightObject.transform.SetParent(transform, false);
            highlightRenderer = highlightObject.AddComponent<SpriteRenderer>();
            highlightRenderer.sprite = GetOutlineSprite();
            highlightRenderer.color = new Color(1f, 1f, 1f, 0.95f);
            highlightRenderer.sortingOrder = 19;
            highlightObject.transform.localScale = new Vector3(0.96f, 0.96f, 1f);
            highlightObject.SetActive(false);
        }

        if (selectionRenderer == null)
        {
            var selectionObject = new GameObject("SelectionHighlight");
            selectionObject.transform.SetParent(transform, false);
            selectionRenderer = selectionObject.AddComponent<SpriteRenderer>();
            selectionRenderer.sprite = GetOutlineSprite();
            selectionRenderer.color = new Color(1f, 0.85f, 0.2f, 1f);
            selectionRenderer.sortingOrder = 21;
            selectionObject.transform.localScale = new Vector3(1.03f, 1.03f, 1f);
            selectionObject.SetActive(false);
        }

        if (ghostRenderer == null)
        {
            var ghostObject = new GameObject("PlacementGhost");
            ghostObject.transform.SetParent(transform, false);
            ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();
            ghostRenderer.color = new Color(0.3f, 1f, 0.35f, 0.45f);
            ghostRenderer.sortingOrder = 20;
            ghostObject.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            ghostObject.SetActive(false);
        }
    }

    private void UpdateHoverVisuals()
    {
        if (ghostRenderer == null || highlightRenderer == null)
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            ghostRenderer.gameObject.SetActive(false);
            highlightRenderer.gameObject.SetActive(false);
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                ghostRenderer.gameObject.SetActive(false);
                highlightRenderer.gameObject.SetActive(false);
                return;
            }
        }

        if (!TryGetGridPositionFromMouse(out var gridPos))
        {
            ghostRenderer.gameObject.SetActive(false);
            highlightRenderer.gameObject.SetActive(false);
            return;
        }

        var center = GridToWorldCenter(gridPos);
        highlightRenderer.transform.position = center + new Vector3(0f, 0f, -0.01f);
        highlightRenderer.gameObject.SetActive(true);

        if (!UIHudController.TryGetBuildBuildingId(out var selectedBuilding))
        {
            ghostRenderer.gameObject.SetActive(false);
            highlightRenderer.gameObject.SetActive(false);
            return;
        }

        ghostRenderer.sprite = BuildingCatalog.GetSprite(selectedBuilding);
        ghostRenderer.transform.position = center;

        var canPlace = CanPlaceBuilding(gridPos, out _);
        ghostRenderer.color = canPlace
            ? new Color(0.3f, 1f, 0.35f, 0.48f)
            : new Color(1f, 0.3f, 0.3f, 0.48f);

        ghostRenderer.gameObject.SetActive(true);
    }

    private void RebuildBuildingsFromState()
    {
        ClearBuildingVisualsInternal();

        var state = GameBootstrap.State;
        if (state == null)
        {
            return;
        }

        state.buildingInstances ??= new List<BuildingInstance>();
        for (var i = 0; i < state.buildingInstances.Count; i++)
        {
            var building = state.buildingInstances[i];
            var gridPos = new Vector2Int(building.x, building.y);
            var isValid = BuildingCatalog.IsKnownBuilding(building.buildingId)
                && building.x >= 0
                && building.x < GridWidth
                && building.y >= 0
                && building.y < GridHeight;

            if (!isValid || occupiedCells.Contains(gridPos))
            {
                continue;
            }

            SpawnBuildingVisual(building, gridPos, BuildingCatalog.GetColor(building.buildingId));
            occupiedCells.Add(gridPos);
        }

        var selector = BuildingSelectionManager.InstanceOrNull;
        if (selector != null)
        {
            if (selector.SelectedBuilding != null)
            {
                selector.SelectBuilding(selector.SelectedBuilding);
            }
            else
            {
                ApplySelectionVisuals(null);
            }
        }
    }

    private void ClearBuildingVisualsInternal()
    {
        occupiedCells.Clear();

        if (indicatorRoot != null)
        {
            for (var i = indicatorRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(indicatorRoot.GetChild(i).gameObject);
            }
        }

        buildingVisuals.Clear();

        if (buildingRoot == null)
        {
            return;
        }

        for (var i = buildingRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(buildingRoot.GetChild(i).gameObject);
        }
    }

    private void SpawnBuildingVisual(BuildingInstance building, Vector2Int gridPos, Color tintColor)
    {
        var buildingObject = new GameObject($"{building.buildingId}_{gridPos.x}_{gridPos.y}");
        buildingObject.transform.SetParent(buildingRoot, false);
        buildingObject.transform.position = GridToWorldCenter(gridPos);
        buildingObject.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

        var renderer = buildingObject.AddComponent<SpriteRenderer>();
        renderer.sprite = BuildingCatalog.GetSprite(building.buildingId);
        renderer.sortingOrder = 5;

        var baseColor = Color.Lerp(Color.white, tintColor, 0.22f);
        renderer.color = baseColor;
        var indicatorObject = new GameObject($"CollectIndicator_{gridPos.x}_{gridPos.y}");
        indicatorObject.transform.SetParent(indicatorRoot, false);
        indicatorObject.transform.position = GridToWorldCenter(gridPos) + new Vector3(0f, 0.33f, 0f);
        indicatorObject.transform.localScale = new Vector3(0.18f, 0.18f, 1f);

        var indicatorRenderer = indicatorObject.AddComponent<SpriteRenderer>();
        indicatorRenderer.sprite = GetCircleSprite();
        indicatorRenderer.color = new Color(0.35f, 1f, 0.45f, 0.9f);
        indicatorRenderer.sortingOrder = 25;

        var countdownObject = new GameObject($"BuildCountdown_{gridPos.x}_{gridPos.y}");
        countdownObject.transform.SetParent(indicatorRoot, false);
        countdownObject.transform.position = GridToWorldCenter(gridPos) + new Vector3(0f, 0.43f, 0f);

        var countdownText = countdownObject.AddComponent<TextMesh>();
        countdownText.text = string.Empty;
        countdownText.characterSize = 0.08f;
        countdownText.fontSize = 64;
        countdownText.color = new Color(1f, 0.9f, 0.2f, 1f);
        countdownText.anchor = TextAnchor.MiddleCenter;
        countdownText.alignment = TextAlignment.Center;

        buildingVisuals[gridPos] = new BuildingVisual
        {
            Building = building,
            Renderer = renderer,
            CollectIndicator = indicatorRenderer,
            BuildCountdownText = countdownText,
            BaseColor = baseColor
        };
    }

    private void HighlightMausoleumRangeAndTargets(BuildingInstance mausoleum)
    {
        var state = GameBootstrap.State;
        if (state?.buildingInstances == null)
        {
            return;
        }

        for (var offsetX = -1; offsetX <= 1; offsetX++)
        {
            for (var offsetY = -1; offsetY <= 1; offsetY++)
            {
                if (offsetX == 0 && offsetY == 0)
                {
                    continue;
                }

                var x = mausoleum.x + offsetX;
                var y = mausoleum.y + offsetY;
                if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
                {
                    continue;
                }

                CreateRangeOverlay(new Vector2Int(x, y));
            }
        }

        for (var i = 0; i < state.buildingInstances.Count; i++)
        {
            var building = state.buildingInstances[i];
            if (building == mausoleum || building.buildingId == BuildingCatalog.MausoleumId)
            {
                continue;
            }

            if (!ProductionCalculator.IsInMausoleumRange(mausoleum.x, mausoleum.y, building.x, building.y))
            {
                continue;
            }

            var pos = new Vector2Int(building.x, building.y);
            if (buildingVisuals.TryGetValue(pos, out var visual))
            {
                visual.Renderer.color = Color.Lerp(visual.BaseColor, Color.green, 0.45f);
            }
        }
    }

    private void CreateRangeOverlay(Vector2Int gridPos)
    {
        var overlayObject = new GameObject($"MausoleumRange_{gridPos.x}_{gridPos.y}");
        overlayObject.transform.SetParent(overlayRoot, false);
        overlayObject.transform.position = GridToWorldCenter(gridPos) + new Vector3(0f, 0f, -0.02f);
        overlayObject.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

        var renderer = overlayObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetSquareSprite();
        renderer.color = new Color(0.35f, 0.85f, 0.35f, 0.16f);
        renderer.sortingOrder = 4;
        mausoleumRangeOverlays.Add(renderer);
    }

    private void UpdateCollectIndicators()
    {
        foreach (var visual in buildingVisuals.Values)
        {
            if (visual?.CollectIndicator == null || visual.Building == null)
            {
                continue;
            }

            var state = GameBootstrap.State;
            var hasCollectable = BuildingTiming.CanCollect(state, visual.Building);
            visual.CollectIndicator.gameObject.SetActive(hasCollectable);
        }
    }


    private void UpdateBuildingCountdowns()
    {
        var nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (var visual in buildingVisuals.Values)
        {
            if (visual?.BuildCountdownText == null || visual.Building == null)
            {
                continue;
            }

            var remaining = BuildingTiming.GetRemainingBuildSeconds(visual.Building, nowUnixSeconds);
            if (remaining <= 0)
            {
                visual.BuildCountdownText.text = string.Empty;
                continue;
            }

            visual.BuildCountdownText.text = BuildingTiming.FormatTimeLeft(remaining);
        }
    }

    private void ShowFloatingText(Vector2Int gridPos, string text, ResourceKind resource)
    {
        var obj = new GameObject("CollectText");
        obj.transform.SetParent(indicatorRoot, false);
        obj.transform.position = GridToWorldCenter(gridPos) + new Vector3(0f, 0.45f, 0f);

        var textMesh = obj.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.characterSize = 0.15f;
        textMesh.fontSize = 64;
        textMesh.color = ResourceLedger.GetColor(resource);
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        floatingTexts.Add(new FloatingText
        {
            TextMesh = textMesh,
            Lifetime = 1f
        });
    }

    private void UpdateFloatingTexts()
    {
        for (var i = floatingTexts.Count - 1; i >= 0; i--)
        {
            var item = floatingTexts[i];
            if (item.TextMesh == null)
            {
                floatingTexts.RemoveAt(i);
                continue;
            }

            item.Lifetime -= Time.deltaTime;
            item.TextMesh.transform.position += new Vector3(0f, Time.deltaTime * 0.5f, 0f);

            var color = item.TextMesh.color;
            color.a = Mathf.Clamp01(item.Lifetime);
            item.TextMesh.color = color;

            if (item.Lifetime <= 0f)
            {
                Destroy(item.TextMesh.gameObject);
                floatingTexts.RemoveAt(i);
            }
        }
    }

    private static Sprite GetCircleSprite()
    {
        if (cachedCircleSprite != null)
        {
            return cachedCircleSprite;
        }

        var size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var center = (size - 1) * 0.5f;
        var radius = size * 0.48f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                var alpha = distance <= radius ? 1f : 0f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        cachedCircleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedCircleSprite;
    }

    private static Sprite GetSquareSprite()
    {
        if (cachedSquareSprite != null)
        {
            return cachedSquareSprite;
        }

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        cachedSquareSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return cachedSquareSprite;
    }

    private static Sprite GetOutlineSprite()
    {
        if (cachedOutlineSprite != null)
        {
            return cachedOutlineSprite;
        }

        var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var clear = new Color32(0, 0, 0, 0);
        var white = new Color32(255, 255, 255, 255);
        for (var y = 0; y < 16; y++)
        {
            for (var x = 0; x < 16; x++)
            {
                var isBorder = x == 0 || x == 15 || y == 0 || y == 15;
                texture.SetPixel(x, y, isBorder ? white : clear);
            }
        }

        texture.Apply();
        cachedOutlineSprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
        return cachedOutlineSprite;
    }
}
