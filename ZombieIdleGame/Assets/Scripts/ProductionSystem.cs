using System;
using UnityEngine;

public class ProductionSystem : MonoBehaviour
{
    private const float SaveIntervalSeconds = 5f;

    private float saveTimer;

    public static ProductionSystem EnsureExists()
    {
        var existing = FindObjectOfType<ProductionSystem>();
        if (existing != null)
        {
            return existing;
        }

        var systemObject = new GameObject("ProductionSystem");
        return systemObject.AddComponent<ProductionSystem>();
    }

    private void Update()
    {
        var state = GameBootstrap.State;
        if (state == null)
        {
            return;
        }

        var deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
        {
            return;
        }

        var rate = ProductionCalculator.GetProductionRatePerSecond(state);
        if (rate <= 0d)
        {
            return;
        }

        state.ectoplasmRemainder += rate * deltaTime;

        var gainedWhole = (long)Math.Floor(state.ectoplasmRemainder);
        if (gainedWhole > 0)
        {
            state.ectoplasm += gainedWhole;
            state.ectoplasmRemainder -= gainedWhole;
        }

        state.lastSavedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        saveTimer += deltaTime;
        if (saveTimer >= SaveIntervalSeconds)
        {
            saveTimer = 0f;
            SaveSystem.Save(state);
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus || GameBootstrap.State == null)
        {
            return;
        }

        GameBootstrap.State.lastSavedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        SaveSystem.Save(GameBootstrap.State);
    }
}
