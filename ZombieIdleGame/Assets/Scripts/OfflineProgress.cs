using System;
using System.Collections.Generic;

public static class OfflineProgress
{
    private const long MaxOfflineSeconds = 8 * 60 * 60;

    public static OfflineProgressResult Apply(GameState state)
    {
        var nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var rawDelta = nowUnixSeconds - state.lastSavedUnixSeconds;
        var offlineSeconds = Math.Max(0, Math.Min(rawDelta, MaxOfflineSeconds));

        var distribution = BuildProductionDistribution(state, nowUnixSeconds, offlineSeconds);
        var producedTotal = 0d;
        for (var i = 0; i < distribution.Count; i++)
        {
            var item = distribution[i];
            if (item.ProducedSeconds <= 0)
            {
                continue;
            }

            var gained = item.Rate * item.ProducedSeconds;
            if (gained <= 0d)
            {
                continue;
            }

            var storageCap = BuildingTiming.GetStorageCap(state, item.Building);
            if (storageCap <= 0d)
            {
                continue;
            }

            var before = item.Building.storedEctoplasm;
            item.Building.storedEctoplasm = Math.Min(storageCap, before + gained);
            producedTotal += item.Building.storedEctoplasm - before;
        }

        var gainedWhole = (long)Math.Floor(producedTotal);
        state.lastSavedUnixSeconds = nowUnixSeconds;

        return new OfflineProgressResult(offlineSeconds, gainedWhole);
    }

    private static List<DistributionItem> BuildProductionDistribution(GameState state, long nowUnixSeconds, long offlineSeconds)
    {
        var result = new List<DistributionItem>();
        if (state?.buildingInstances == null)
        {
            return result;
        }

        for (var i = 0; i < state.buildingInstances.Count; i++)
        {
            var building = state.buildingInstances[i];
            var producedSeconds = GetProducedSeconds(building, state.lastSavedUnixSeconds, nowUnixSeconds, offlineSeconds);

            var rate = ProductionCalculator.GetBuildingProductionPerSecond(state, building);
            if (rate <= 0d)
            {
                continue;
            }

            result.Add(new DistributionItem(building, rate, producedSeconds));
        }

        return result;
    }

    private static long GetProducedSeconds(BuildingInstance building, long fromUnixSeconds, long nowUnixSeconds, long offlineSeconds)
    {
        if (building == null)
        {
            return 0;
        }

        if (BuildingTiming.TryCompleteBuild(building, nowUnixSeconds))
        {
            return offlineSeconds;
        }

        if (!building.isBuilding)
        {
            return offlineSeconds;
        }

        var secondsUntilBuildEnd = building.buildEndUnixSeconds - fromUnixSeconds;
        if (secondsUntilBuildEnd >= offlineSeconds)
        {
            return 0;
        }

        if (secondsUntilBuildEnd <= 0)
        {
            building.isBuilding = false;
            building.buildEndUnixSeconds = 0;
            return offlineSeconds;
        }

        building.isBuilding = false;
        building.buildEndUnixSeconds = 0;
        return Math.Max(0, offlineSeconds - secondsUntilBuildEnd);
    }

    private readonly struct DistributionItem
    {
        public BuildingInstance Building { get; }
        public double Rate { get; }
        public long ProducedSeconds { get; }

        public DistributionItem(BuildingInstance building, double rate, long producedSeconds)
        {
            Building = building;
            Rate = rate;
            ProducedSeconds = producedSeconds;
        }
    }
}

public readonly struct OfflineProgressResult
{
    public long OfflineSeconds { get; }
    public long GainedEctoplasm { get; }

    public OfflineProgressResult(long offlineSeconds, long gainedEctoplasm)
    {
        OfflineSeconds = offlineSeconds;
        GainedEctoplasm = gainedEctoplasm;
    }
}
