using UnityEngine;

public static class BuildingCatalog
{
    public const string GraveId = "grave";
    public const string MorgueId = "morgue";
    public const string MausoleumId = "mausoleum";

    public static long GetCost(string buildingId)
    {
        switch (buildingId)
        {
            case GraveId:
                return 25;
            case MorgueId:
                return 100;
            case MausoleumId:
                return 200;
            default:
                return long.MaxValue;
        }
    }

    public static double GetBaseProduction(string buildingId)
    {
        switch (buildingId)
        {
            case GraveId:
                return 0.5d;
            case MorgueId:
                return 3d;
            case MausoleumId:
                return 0d;
            default:
                return 0d;
        }
    }

    public static bool IsKnownBuilding(string buildingId)
    {
        return buildingId == GraveId || buildingId == MorgueId || buildingId == MausoleumId;
    }

    public static Color GetColor(string buildingId)
    {
        switch (buildingId)
        {
            case GraveId:
                return new Color(0.2f, 0.65f, 0.3f, 1f);
            case MorgueId:
                return new Color(0.75f, 0.2f, 0.2f, 1f);
            case MausoleumId:
                return new Color(0.5f, 0.2f, 0.7f, 1f);
            default:
                return Color.white;
        }
    }

    public static string GetDisplayName(string buildingId)
    {
        switch (buildingId)
        {
            case GraveId:
                return "Grave";
            case MorgueId:
                return "Morgue";
            case MausoleumId:
                return "Mausoleum";
            default:
                return "Unknown";
        }
    }
}
