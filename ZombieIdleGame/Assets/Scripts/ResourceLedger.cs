using System;
using UnityEngine;

public enum ResourceKind
{
    None,
    Ectoplasm,
    Rot,
    Skulls
}

public static class ResourceLedger
{
    public static double GetStored(BuildingInstance building, ResourceKind resource)
    {
        if (building == null)
        {
            return 0d;
        }

        switch (resource)
        {
            case ResourceKind.Ectoplasm:
                return building.storedEctoplasm;
            case ResourceKind.Rot:
                return building.storedRot;
            case ResourceKind.Skulls:
                return building.storedSkulls;
            default:
                return 0d;
        }
    }

    public static void SetStored(BuildingInstance building, ResourceKind resource, double value)
    {
        if (building == null)
        {
            return;
        }

        var sanitized = Math.Max(0d, value);
        switch (resource)
        {
            case ResourceKind.Ectoplasm:
                building.storedEctoplasm = sanitized;
                break;
            case ResourceKind.Rot:
                building.storedRot = sanitized;
                break;
            case ResourceKind.Skulls:
                building.storedSkulls = sanitized;
                break;
        }
    }

    public static long RemoveWholeStored(BuildingInstance building, ResourceKind resource)
    {
        var whole = (long)Math.Floor(GetStored(building, resource));
        if (whole <= 0)
        {
            return 0;
        }

        SetStored(building, resource, GetStored(building, resource) - whole);
        return whole;
    }

    public static long GetBank(GameState state, ResourceKind resource)
    {
        if (state == null)
        {
            return 0;
        }

        switch (resource)
        {
            case ResourceKind.Ectoplasm:
                return state.ectoplasm;
            case ResourceKind.Rot:
                return state.rot;
            case ResourceKind.Skulls:
                return state.skulls;
            default:
                return 0;
        }
    }

    public static void AddToBank(GameState state, ResourceKind resource, long value)
    {
        if (state == null || value <= 0)
        {
            return;
        }

        switch (resource)
        {
            case ResourceKind.Ectoplasm:
                state.ectoplasm += value;
                break;
            case ResourceKind.Rot:
                state.rot += value;
                break;
            case ResourceKind.Skulls:
                state.skulls += value;
                break;
        }
    }

    public static string GetDisplayName(ResourceKind resource)
    {
        switch (resource)
        {
            case ResourceKind.Ectoplasm:
                return "Ectoplasm";
            case ResourceKind.Rot:
                return "Rot";
            case ResourceKind.Skulls:
                return "Skulls";
            default:
                return "None";
        }
    }

    public static Color GetColor(ResourceKind resource)
    {
        switch (resource)
        {
            case ResourceKind.Ectoplasm:
                return new Color(0.65f, 1f, 0.7f, 1f);
            case ResourceKind.Rot:
                return new Color(0.9f, 0.75f, 0.35f, 1f);
            case ResourceKind.Skulls:
                return new Color(0.92f, 0.92f, 0.92f, 1f);
            default:
                return Color.white;
        }
    }
}
