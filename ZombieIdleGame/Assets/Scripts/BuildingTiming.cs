using System;
using UnityEngine;

public static class BuildingTiming
{
    public const double CollectDelaySeconds = 15d;
    public const double StorageCapSeconds = 6d * 60d * 60d;
    public const long InitialBuildSeconds = 5;

    private const double UpgradeDurationBaseSeconds = 5d;
    private const double UpgradeDurationGrowth = 1.35d;
    private const long UpgradeDurationCapSeconds = 2 * 60 * 60;

    public static bool TryCompleteBuild(BuildingInstance building, long nowUnixSeconds)
    {
        if (building == null || !building.isBuilding)
        {
            return false;
        }

        if (nowUnixSeconds < building.buildEndUnixSeconds)
        {
            return false;
        }

        building.isBuilding = false;
        building.buildEndUnixSeconds = 0;
        return true;
    }

    public static long GetRemainingBuildSeconds(BuildingInstance building, long nowUnixSeconds)
    {
        if (building == null || !building.isBuilding)
        {
            return 0;
        }

        return Math.Max(0, building.buildEndUnixSeconds - nowUnixSeconds);
    }

    public static void StartBuild(BuildingInstance building, long durationSeconds, long nowUnixSeconds)
    {
        if (building == null)
        {
            return;
        }

        building.isBuilding = true;
        building.buildEndUnixSeconds = nowUnixSeconds + Math.Max(1, durationSeconds);
    }

    public static long GetUpgradeDurationSeconds(int currentLevel)
    {
        var safeLevel = Math.Max(1, currentLevel);
        var raw = UpgradeDurationBaseSeconds * Math.Pow(UpgradeDurationGrowth, safeLevel - 1);
        var rounded = (long)Math.Round(raw, MidpointRounding.AwayFromZero);
        return Math.Min(UpgradeDurationCapSeconds, Math.Max(1, rounded));
    }

    public static double GetCollectThreshold(GameState state, BuildingInstance building)
    {
        var rate = ProductionCalculator.GetBuildingProductionPerSecond(state, building);
        return Math.Max(0d, rate * CollectDelaySeconds);
    }

    public static bool CanCollect(GameState state, BuildingInstance building)
    {
        if (building == null || building.isBuilding)
        {
            return false;
        }

        return building.storedEctoplasm >= GetCollectThreshold(state, building);
    }

    public static double GetStorageCap(GameState state, BuildingInstance building)
    {
        var rate = ProductionCalculator.GetBuildingProductionPerSecond(state, building);
        return Math.Max(0d, rate * StorageCapSeconds);
    }

    public static string FormatTimeLeft(long seconds)
    {
        var clamped = Math.Max(0, seconds);
        if (clamped >= 60)
        {
            var minutes = clamped / 60;
            var rem = clamped % 60;
            return $"{minutes:00}:{rem:00}";
        }

        return $"{clamped}s";
    }
}
