using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class TilemapSetupAuthoring : MonoBehaviour
{
    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private TileBase grassTile;
    [SerializeField] private TileBase dirtTile;
    [SerializeField] private TileBase pathTile;
    [SerializeField] private TileBase darkDirtTile;
    [SerializeField] private bool repaintInEditor = true;

    private const int GridSize = 8;

    private void OnEnable()
    {
        if (!Application.isPlaying && repaintInEditor)
        {
            Paint();
        }
    }

    [ContextMenu("Paint Cemetery Placeholder")]
    public void Paint()
    {
        if (targetTilemap == null || grassTile == null)
        {
            return;
        }

        targetTilemap.ClearAllTiles();

        for (var x = 0; x < GridSize; x++)
        {
            for (var y = 0; y < GridSize; y++)
            {
                var position = new Vector3Int(x, y, 0);
                var tile = SelectTileFor(x, y);
                targetTilemap.SetTile(position, tile);
            }
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(targetTilemap);
        if (targetTilemap.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(targetTilemap.gameObject.scene);
        }
#endif
    }

    private TileBase SelectTileFor(int x, int y)
    {
        if (pathTile != null && (x == 3 || x == 4))
        {
            return pathTile;
        }

        var pattern = ((x * 17) + (y * 31)) % 7;
        if (darkDirtTile != null && (pattern == 0 || pattern == 3))
        {
            return darkDirtTile;
        }

        if (dirtTile != null && pattern == 5)
        {
            return dirtTile;
        }

        return grassTile;
    }
}
