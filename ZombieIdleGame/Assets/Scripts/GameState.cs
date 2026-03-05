using System;
using System.Collections.Generic;

[Serializable]
public class GameState
{
    public long ectoplasm;
    public long bones;
    public long lastSavedUnixSeconds;
    public double ectoplasmRemainder;
    public List<BuildingInstance> buildingInstances;

    public GameState()
    {
        ectoplasm = 0;
        bones = 0;
        lastSavedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        ectoplasmRemainder = 0d;
        buildingInstances = new List<BuildingInstance>();
    }
}
