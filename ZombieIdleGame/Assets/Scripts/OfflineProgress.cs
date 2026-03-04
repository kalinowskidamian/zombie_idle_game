using System;

public static class OfflineProgress
{
    private const long EctoplasmPerSecond = 1;
    private const long MaxOfflineSeconds = 8 * 60 * 60;

    public static OfflineProgressResult Apply(GameState state)
    {
        var nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var rawDelta = nowUnixSeconds - state.lastSavedUnixSeconds;
        var offlineSeconds = Math.Max(0, Math.Min(rawDelta, MaxOfflineSeconds));

        var gainedEctoplasm = offlineSeconds * EctoplasmPerSecond;
        state.ectoplasm += gainedEctoplasm;
        state.lastSavedUnixSeconds = nowUnixSeconds;

        return new OfflineProgressResult(offlineSeconds, gainedEctoplasm);
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
