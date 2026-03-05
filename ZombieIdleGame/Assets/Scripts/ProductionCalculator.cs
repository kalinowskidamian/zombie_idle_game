using System;
using System.Collections.Generic;
using UnityEngine;

public static class ProductionCalculator
{
    public static double GetProductionRatePerSecond(GameState state)
    {
        if (state?.buildingInstances == null)
        {
            return 0d;
        }

        var totalRate = 0d;
        for (var i = 0; i < state.buildingInstances.Count; i++)
        {
            totalRate += GetBuildingProductionPerSecond(state, state.buildingInstances[i]);
        }

        return totalRate;
    }

    public static double GetBuildingProductionPerSecond(GameState state, BuildingInstance building)
    {
        if (state?.buildingInstances == null || building == null)
        {
            return 0d;
        }

        var baseProduction = BuildingCatalog.GetProductionAtLevel(building.buildingId, building.level);
        if (baseProduction <= 0d)
        {
            return 0d;
        }

        var bonusPercent = GetTotalBonusPercentForBuilding(state, building);
        return baseProduction * (1d + (bonusPercent / 100d));
    }

    public static double GetTotalBonusPercentForBuilding(GameState state, BuildingInstance building)
    {
        return GetBuffingMausoleumBonusPercent(state, building.x, building.y);
    }

    public static int GetBuffingMausoleumCount(GameState state, BuildingInstance building)
    {
        if (state?.buildingInstances == null || building == null || IsMausoleum(building))
        {
            return 0;
        }

        var count = 0;
        for (var i = 0; i < state.buildingInstances.Count; i++)
        {
            var mausoleum = state.buildingInstances[i];
            if (!IsMausoleum(mausoleum))
            {
                continue;
            }

            if (IsInMausoleumRange(mausoleum.x, mausoleum.y, building.x, building.y))
            {
                count++;
            }
        }

        return count;
    }

    public static int GetMausoleumBuffedBuildingsCount(GameState state, BuildingInstance mausoleum)
    {
        if (state?.buildingInstances == null || mausoleum == null || !IsMausoleum(mausoleum))
        {
            return 0;
        }

        var count = 0;
        for (var i = 0; i < state.buildingInstances.Count; i++)
        {
            var building = state.buildingInstances[i];
            if (building == mausoleum || IsMausoleum(building))
            {
                continue;
            }

            if (IsInMausoleumRange(mausoleum.x, mausoleum.y, building.x, building.y))
            {
                count++;
            }
        }

        return count;
    }

    public static List<BuildingInstance> GetBuildingsBuffedByMausoleum(GameState state, BuildingInstance mausoleum)
    {
        var result = new List<BuildingInstance>();
        if (state?.buildingInstances == null || mausoleum == null || !IsMausoleum(mausoleum))
        {
            return result;
        }

        for (var i = 0; i < state.buildingInstances.Count; i++)
        {
            var building = state.buildingInstances[i];
            if (building == mausoleum || IsMausoleum(building))
            {
                continue;
            }

            if (IsInMausoleumRange(mausoleum.x, mausoleum.y, building.x, building.y))
            {
                result.Add(building);
            }
        }

        return result;
    }

    public static bool IsInMausoleumRange(int mausoleumX, int mausoleumY, int targetX, int targetY)
    {
        return Math.Abs(mausoleumX - targetX) <= 1 && Math.Abs(mausoleumY - targetY) <= 1;
    }

    private static double GetBuffingMausoleumBonusPercent(GameState state, int x, int y)
    {
        if (state?.buildingInstances == null)
        {
            return 0d;
        }

        double totalBonusPercent = 0d;
        for (var i = 0; i < state.buildingInstances.Count; i++)
        {
            var mausoleum = state.buildingInstances[i];
            if (!IsMausoleum(mausoleum))
            {
                continue;
            }

            if (IsInMausoleumRange(mausoleum.x, mausoleum.y, x, y))
            {
                totalBonusPercent += BuildingCatalog.GetMausoleumBonusPercent(mausoleum.level);
            }
        }

        return totalBonusPercent;
    }

    private static bool IsMausoleum(BuildingInstance building)
    {
        return building != null && building.buildingId == BuildingCatalog.MausoleumId;
    }
}
