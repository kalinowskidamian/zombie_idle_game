using System;

[Serializable]
public class BuildingInstance
{
    public string buildingId;
    public int x;
    public int y;
    public int level;

    public BuildingInstance(string buildingId, int x, int y, int level)
    {
        this.buildingId = buildingId;
        this.x = x;
        this.y = y;
        this.level = level;
    }
}
