using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class BackgroundTilemapPainter
{
    private const int Width = 8;
    private const int Height = 8;

    [MenuItem("Tools/NecroIdle/Paint Background 8x8")]
    public static void PaintBackground8x8()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            Debug.LogError("No active loaded scene found.");
            return;
        }

        Tilemap groundTilemap = FindTilemap(activeScene, "GroundTilemap");
        Tilemap backgroundTilemap = FindTilemap(activeScene, "BackgroundTilemap");
        if (groundTilemap == null || backgroundTilemap == null)
        {
            Debug.LogError("Could not find Tilemaps named 'GroundTilemap' and 'BackgroundTilemap' in the active scene.");
            return;
        }

        TileBase groundFill = LoadTile("Assets/Art/Tiles/TilesetTiles/ground_fill.asset");
        TileBase path = LoadTile("Assets/Art/Tiles/TilesetTiles/path.asset");
        TileBase dirt = LoadTile("Assets/Art/Tiles/TilesetTiles/dirt.asset");
        TileBase darkDirt = LoadTile("Assets/Art/Tiles/TilesetTiles/dark_dirt.asset");

        if (groundFill == null || path == null || dirt == null || darkDirt == null)
        {
            return;
        }

        Undo.RecordObjects(new UnityEngine.Object[] { groundTilemap, backgroundTilemap }, "Paint Background 8x8");
        groundTilemap.ClearAllTiles();
        backgroundTilemap.ClearAllTiles();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                groundTilemap.SetTile(new Vector3Int(x, y, 0), groundFill);
            }
        }

        for (int y = 0; y < Height; y++)
        {
            backgroundTilemap.SetTile(new Vector3Int(3, y, 0), path);
            backgroundTilemap.SetTile(new Vector3Int(4, y, 0), path);
        }

        var rng = new System.Random(123);
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                bool isPath = x == 3 || x == 4;
                if (isPath)
                {
                    continue;
                }

                int roll = rng.Next(100);
                if (roll < 14)
                {
                    backgroundTilemap.SetTile(new Vector3Int(x, y, 0), dirt);
                }
                else if (roll < 22)
                {
                    backgroundTilemap.SetTile(new Vector3Int(x, y, 0), darkDirt);
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        Debug.Log("Painted GroundTilemap and BackgroundTilemap 8x8 (ground fill + decorations).");
    }

    private static Tilemap FindTilemap(Scene scene, string tilemapName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            Tilemap[] tilemaps = root.GetComponentsInChildren<Tilemap>(true);
            foreach (Tilemap tilemap in tilemaps)
            {
                if (tilemap.name == tilemapName)
                {
                    return tilemap;
                }
            }
        }

        return null;
    }

    private static TileBase LoadTile(string path)
    {
        TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
        if (tile == null)
        {
            Debug.LogError($"Tile asset not found at path: {path}");
        }

        return tile;
    }
}
