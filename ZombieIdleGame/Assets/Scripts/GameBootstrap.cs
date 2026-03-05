using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    public static GameState State { get; private set; }

    public static void ResetState()
    {
        State = new GameState();
        SaveSystem.Save(State);
    }

    private void Awake()
    {
        State = SaveSystem.LoadOrDefault();

        var result = OfflineProgress.Apply(State);
        SaveSystem.Save(State);

        Debug.Log($"Offline progress: {result.OfflineSeconds}s, added {result.GainedEctoplasm} ectoplasm to uncollected storage.");

        UIHudController.EnsureHudExists();
        GridManager.EnsureGridExists();
        BuildingSelectionManager.EnsureExists();
        ProductionSystem.EnsureExists();
    }

    private void OnApplicationQuit()
    {
        if (State == null)
        {
            return;
        }

        State.lastSavedUnixSeconds = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        SaveSystem.Save(State);
    }
}
