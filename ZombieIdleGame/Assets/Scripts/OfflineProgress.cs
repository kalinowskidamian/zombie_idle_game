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

        var distribution = BuildProductionDistribution(state);
        var producedTotal = 0d;
        for (var i = 0; i < distribution.Count; i++)
        {
            var item = distribution[i];
            var gained = item.Rate * offlineSeconds;
            if (gained <= 0d)
            {
                continue;
            }

            item.Building.storedEctoplasm += gained;
            producedTotal += gained;
        }

        var gainedWhole = (long)Math.Floor(producedTotal);
        state.lastSavedUnixSeconds = nowUnixSeconds;

        return new OfflineProgressResult(offlineSeconds, gainedWhole);
    }

    private static List<DistributionItem> BuildProductionDistribution(GameState state)
    {
        var result = new List<DistributionItem>();
        if (state?.buildingInstances == null)
        {
            return result;
        }

        for (var i = 0; i < state.buildingInstances.Count; i++)
        {
            var building = state.buildingInstances[i];
            var rate = ProductionCalculator.GetBuildingProductionPerSecond(state, building);
            if (rate <= 0d)
            {
                continue;
            }

            result.Add(new DistributionItem(building, rate));
        }

        return result;
    }

    private readonly struct DistributionItem
    {
        public BuildingInstance Building { get; }
        public double Rate { get; }

        public DistributionItem(BuildingInstance building, double rate)
        {
            Building = building;
            Rate = rate;
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
