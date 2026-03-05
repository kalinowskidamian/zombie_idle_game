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
        if (state?.buildingInstances == null)
        {
            return;
        }

        var deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
        {
            return;
        }

        var producedAnything = false;
        for (var i = 0; i < state.buildingInstances.Count; i++)
        {
            var building = state.buildingInstances[i];
            var buildingRate = ProductionCalculator.GetBuildingProductionPerSecond(state, building);
            if (buildingRate <= 0d)
            {
                continue;
            }

            building.storedEctoplasm += buildingRate * deltaTime;
            producedAnything = true;
        }

        state.lastSavedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (!producedAnything)
        {
            return;
        }

        saveTimer += deltaTime;
        if (saveTimer >= SaveIntervalSeconds)
        {
            saveTimer = 0f;
            SaveSystem.Save(state);
            GridManager.Instance?.RefreshVisualsFromState();
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
