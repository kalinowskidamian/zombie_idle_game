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

    private readonly HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();

    private Camera mainCamera;
    private Grid grid;
    private Transform tileRoot;
    private Transform buildingRoot;
    private SpriteRenderer ghostRenderer;
    private SpriteRenderer highlightRenderer;

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

        TryPlaceBuilding(gridPos);
    }

    private void TryPlaceBuilding(Vector2Int gridPos)
    {
        if (!CanPlaceBuilding(gridPos, out _))
        {
            return;
        }

        var buildingId = UIHudController.SelectedBuildingId;
        var cost = BuildingCatalog.GetCost(buildingId);

        var state = GameBootstrap.State;
        state.ectoplasm -= cost;
        state.buildingInstances ??= new List<BuildingInstance>();
        state.buildingInstances.Add(new BuildingInstance(buildingId, gridPos.x, gridPos.y, 1));
        state.lastSavedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        SpawnBuildingVisual(buildingId, gridPos, BuildingCatalog.GetColor(buildingId));
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
        buildingId = UIHudController.SelectedBuildingId;
        if (!BuildingCatalog.IsKnownBuilding(buildingId) || occupiedCells.Contains(gridPos))
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

        tileRoot = tiles;
        buildingRoot = buildings;
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

        var selectedBuilding = UIHudController.SelectedBuildingId;
        if (!BuildingCatalog.IsKnownBuilding(selectedBuilding))
        {
            ghostRenderer.gameObject.SetActive(false);
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

            SpawnBuildingVisual(building.buildingId, gridPos, BuildingCatalog.GetColor(building.buildingId));
            occupiedCells.Add(gridPos);
        }
    }

    private void ClearBuildingVisualsInternal()
    {
        occupiedCells.Clear();

        if (buildingRoot == null)
        {
            return;
        }

        for (var i = buildingRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(buildingRoot.GetChild(i).gameObject);
        }
    }

    private void SpawnBuildingVisual(string buildingId, Vector2Int gridPos, Color tintColor)
    {
        var buildingObject = new GameObject($"{buildingId}_{gridPos.x}_{gridPos.y}");
        buildingObject.transform.SetParent(buildingRoot, false);
        buildingObject.transform.position = GridToWorldCenter(gridPos);
        buildingObject.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

        var renderer = buildingObject.AddComponent<SpriteRenderer>();
        renderer.sprite = BuildingCatalog.GetSprite(buildingId);
        renderer.color = Color.Lerp(Color.white, tintColor, 0.22f);
        renderer.sortingOrder = 5;
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
