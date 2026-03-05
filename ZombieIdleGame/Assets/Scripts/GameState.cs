using System;
using System.Collections.Generic;

[Serializable]
public class GameState
{
    public long ectoplasm;
    public long skulls;
    public long rot;
    public long bones;
    public long lastSavedUnixSeconds;
    public double ectoplasmRemainder;
    public List<BuildingInstance> buildingInstances;

    public const int HeadquartersStartX = 3;
    public const int HeadquartersStartY = 3;

    public void EnsureHeadquarters()
    {
        buildingInstances ??= new List<BuildingInstance>();

        for (var i = 0; i < buildingInstances.Count; i++)
        {
            var building = buildingInstances[i];
            if (building != null && BuildingCatalog.IsHeadquarters(building.buildingId))
            {
                building.level = Math.Max(1, building.level);
                return;
            }
        }

        buildingInstances.Add(new BuildingInstance(BuildingCatalog.HeadquartersId, HeadquartersStartX, HeadquartersStartY, 1));
    }

    public GameState()
    {
        ectoplasm = 0;
        skulls = 0;
        rot = 0;
        bones = 0;
        lastSavedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        ectoplasmRemainder = 0d;
        buildingInstances = new List<BuildingInstance>();
        EnsureHeadquarters();
    }
}
