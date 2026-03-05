using System;
using System.Collections.Generic;
using UnityEngine;

public static class ProductionCalculator
{
    private const double MausoleumBonusMultiplier = 1.2d;

    public static double GetProductionRatePerSecond(GameState state)
    {
        if (state?.buildingInstances == null)
        {
            return 0d;
        }

        var mausoleumPositions = new HashSet<Vector2Int>();
        for (var i = 0; i < state.buildingInstances.Count; i++)
        {
            var building = state.buildingInstances[i];
            if (building.buildingId == BuildingCatalog.MausoleumId)
            {
                mausoleumPositions.Add(new Vector2Int(building.x, building.y));
            }
        }

        var totalRate = 0d;
        for (var i = 0; i < state.buildingInstances.Count; i++)
        {
            var building = state.buildingInstances[i];
            var baseProduction = BuildingCatalog.GetBaseProduction(building.buildingId);
            if (baseProduction <= 0d)
            {
                continue;
            }

            var production = baseProduction;
            if (HasMausoleumNeighbor(mausoleumPositions, building.x, building.y))
            {
                production *= MausoleumBonusMultiplier;
            }

            totalRate += production;
        }

        return totalRate;
    }

    private static bool HasMausoleumNeighbor(HashSet<Vector2Int> mausoleumPositions, int x, int y)
    {
        foreach (var pos in mausoleumPositions)
        {
            var distanceX = Math.Abs(pos.x - x);
            var distanceY = Math.Abs(pos.y - y);
            if (distanceX <= 1 && distanceY <= 1)
            {
                return true;
            }
        }

        return false;
    }
}
