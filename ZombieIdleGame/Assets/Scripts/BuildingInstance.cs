using System;

[Serializable]
public class BuildingInstance
{
    public string buildingId;
    public int x;
    public int y;
    public int level;
    public double storedEctoplasm;
    public double storedRot;
    public double storedSkulls;
    public bool isBuilding;
    public long buildEndUnixSeconds;

    public BuildingInstance(string buildingId, int x, int y, int level)
    {
        this.buildingId = buildingId;
        this.x = x;
        this.y = y;
        this.level = level;
        storedEctoplasm = 0d;
        storedRot = 0d;
        storedSkulls = 0d;
        isBuilding = false;
        buildEndUnixSeconds = 0;
    }
}
