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

        var nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var changedState = false;
        var producedAnything = false;

        for (var i = 0; i < state.buildingInstances.Count; i++)
        {
            var building = state.buildingInstances[i];
            if (BuildingTiming.TryCompleteBuild(building, nowUnixSeconds))
            {
                changedState = true;
            }

            if (building.isBuilding)
            {
                continue;
            }

            var buildingRate = ProductionCalculator.GetBuildingProductionPerSecond(state, building);
            if (buildingRate <= 0d)
            {
                continue;
            }

            var resource = BuildingCatalog.GetProducedResource(building.buildingId);
            if (resource == ResourceKind.None)
            {
                continue;
            }

            var storageCap = BuildingTiming.GetStorageCap(state, building);
            if (storageCap <= 0d)
            {
                continue;
            }

            var stored = ResourceLedger.GetStored(building, resource);
            if (stored >= storageCap)
            {
                ResourceLedger.SetStored(building, resource, storageCap);
                continue;
            }

            var before = stored;
            var after = Math.Min(storageCap, before + (buildingRate * deltaTime));
            ResourceLedger.SetStored(building, resource, after);
            if (after > before)
            {
                producedAnything = true;
            }
        }

        state.lastSavedUnixSeconds = nowUnixSeconds;

        if (!producedAnything && !changedState)
        {
            return;
        }

        if (changedState)
        {
            saveTimer = 0f;
            SaveSystem.Save(state);
            GridManager.Instance?.RefreshVisualsFromState();
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
