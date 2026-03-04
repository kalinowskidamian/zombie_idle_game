using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private const string FileName = "gamestate.json";

    private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public static void Save(GameState state)
    {
        var json = JsonUtility.ToJson(state, true);
        File.WriteAllText(SavePath, json);
    }

    public static GameState LoadOrDefault()
    {
        if (!File.Exists(SavePath))
        {
            return new GameState();
        }

        var json = File.ReadAllText(SavePath);
        var state = JsonUtility.FromJson<GameState>(json);
        return state ?? new GameState();
    }
}
