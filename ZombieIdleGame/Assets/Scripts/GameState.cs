using System;

[Serializable]
public class GameState
{
    public long ectoplasm;
    public long bones;
    public long lastSavedUnixSeconds;

    public GameState()
    {
        ectoplasm = 0;
        bones = 0;
        lastSavedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
