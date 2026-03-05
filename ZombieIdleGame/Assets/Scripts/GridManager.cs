using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridManager : MonoBehaviour
{
    private const int GridWidth = 8;
    private const int GridHeight = 8;
    private const float CellSize = 1f;
    private const long GraveCost = 25;

    private static Sprite cachedSquareSprite;
    private static GridManager instance;

    private readonly HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();

    private Camera mainCamera;
    private Transform tileRoot;
    private Transform buildingRoot;

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
        EnsureRoots();
        DrawGridTiles();
        RebuildBuildingsFromState();
    }

    private void Update()
    {
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

        TryPlaceGrave(gridPos);
    }

    private void TryPlaceGrave(Vector2Int gridPos)
    {
        if (occupiedCells.Contains(gridPos))
        {
            return;
        }

        var state = GameBootstrap.State;
        if (state == null || state.ectoplasm < GraveCost)
        {
            return;
        }

        state.ectoplasm -= GraveCost;
        state.buildingInstances ??= new List<BuildingInstance>();
        state.buildingInstances.Add(new BuildingInstance("grave", gridPos.x, gridPos.y, 1));
        state.lastSavedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        SpawnBuildingVisual(gridPos, Color.magenta);
        occupiedCells.Add(gridPos);

        SaveSystem.Save(state);
    }

    private bool TryGetGridPositionFromMouse(out Vector2Int gridPos)
    {
        var mousePosition = Input.mousePosition;
        mousePosition.z = Mathf.Abs(mainCamera.transform.position.z);

        var world = mainCamera.ScreenToWorldPoint(mousePosition);
        return TryWorldToGrid(world, out gridPos);
    }

    private static bool TryWorldToGrid(Vector3 world, out Vector2Int gridPos)
    {
        var x = Mathf.FloorToInt(world.x / CellSize);
        var y = Mathf.FloorToInt(world.y / CellSize);

        var isInside = x >= 0 && x < GridWidth && y >= 0 && y < GridHeight;
        gridPos = new Vector2Int(x, y);
        return isInside;
    }

    private static Vector3 GridToWorldCenter(Vector2Int gridPos)
    {
        return new Vector3((gridPos.x + 0.5f) * CellSize, (gridPos.y + 0.5f) * CellSize, 0f);
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
            var isValid = building.buildingId == "grave"
                && building.x >= 0
                && building.x < GridWidth
                && building.y >= 0
                && building.y < GridHeight;

            if (!isValid || occupiedCells.Contains(gridPos))
            {
                continue;
            }

            SpawnBuildingVisual(gridPos, new Color(0.45f, 0.1f, 0.55f, 1f));
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

    private void SpawnBuildingVisual(Vector2Int gridPos, Color color)
    {
        var buildingObject = new GameObject($"Grave_{gridPos.x}_{gridPos.y}");
        buildingObject.transform.SetParent(buildingRoot, false);
        buildingObject.transform.position = GridToWorldCenter(gridPos);
        buildingObject.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

        var renderer = buildingObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetSquareSprite();
        renderer.color = color;
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
}
