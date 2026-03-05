using System;

public static class OfflineProgress
{
    private const double GraveProductionPerSecond = 0.5d;
    private const long MaxOfflineSeconds = 8 * 60 * 60;

    public static OfflineProgressResult Apply(GameState state)
    {
        var nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var rawDelta = nowUnixSeconds - state.lastSavedUnixSeconds;
        var offlineSeconds = Math.Max(0, Math.Min(rawDelta, MaxOfflineSeconds));

        var productionRate = GetProductionRatePerSecond(state);
        var produced = productionRate * offlineSeconds;

        state.ectoplasmRemainder += produced;
        var gainedWhole = (long)Math.Floor(state.ectoplasmRemainder);
        if (gainedWhole > 0)
        {
            state.ectoplasm += gainedWhole;
            state.ectoplasmRemainder -= gainedWhole;
        }

        state.lastSavedUnixSeconds = nowUnixSeconds;

        return new OfflineProgressResult(offlineSeconds, gainedWhole);
    }

    private static double GetProductionRatePerSecond(GameState state)
    {
        if (state?.buildingInstances == null)
        {
            return 0d;
        }

        var graveCount = 0;
        for (var i = 0; i < state.buildingInstances.Count; i++)
        {
            if (state.buildingInstances[i].buildingId == "grave")
            {
                graveCount++;
            }
        }

        return graveCount * GraveProductionPerSecond;
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
