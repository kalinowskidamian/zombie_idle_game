using System;
using UnityEngine;

public static class BuildingCatalog
{
    public const string HeadquartersId = "graveyard_chapel";
    public const string GraveId = "grave";
    public const string MorgueId = "morgue";
    public const string MausoleumId = "mausoleum";
    public const string OssuaryId = "ossuary";
    public const string RotVatId = "rot_vat";

    public static long GetCost(string buildingId)
    {
        switch (buildingId)
        {
            case HeadquartersId:
                return 300;
            case GraveId:
                return 25;
            case MorgueId:
                return 100;
            case MausoleumId:
                return 200;
            case OssuaryId:
                return 250;
            case RotVatId:
                return 150;
            default:
                return long.MaxValue;
        }
    }

    public static long GetUpgradeCost(string buildingId, int currentLevel)
    {
        var baseCost = GetCost(buildingId);
        if (baseCost == long.MaxValue)
        {
            return baseCost;
        }

        var safeLevel = Math.Max(1, currentLevel);
        var exponent = safeLevel - 1;
        var result = baseCost * Math.Pow(1.5d, exponent);
        return (long)Math.Round(result, MidpointRounding.AwayFromZero);
    }

    public static long GetTotalSpentForLevel(string buildingId, int level)
    {
        var safeLevel = Math.Max(1, level);
        long total = 0;
        for (var paidLevel = 1; paidLevel <= safeLevel; paidLevel++)
        {
            total += GetUpgradeCost(buildingId, paidLevel);
        }

        return total;
    }

    public static double GetBaseProduction(string buildingId)
    {
        switch (buildingId)
        {
            case HeadquartersId:
                return 2d;
            case GraveId:
                return 0.5d;
            case MorgueId:
                return 3d;
            case MausoleumId:
                return 0d;
            case OssuaryId:
                return 0.02d;
            case RotVatId:
                return 0.2d;
            default:
                return 0d;
        }
    }

    public static ResourceKind GetProducedResource(string buildingId)
    {
        switch (buildingId)
        {
            case HeadquartersId:
            case GraveId:
            case MorgueId:
                return ResourceKind.Ectoplasm;
            case OssuaryId:
                return ResourceKind.Skulls;
            case RotVatId:
                return ResourceKind.Rot;
            default:
                return ResourceKind.None;
        }
    }

    public static double GetProductionAtLevel(string buildingId, int level)
    {
        var baseProduction = GetBaseProduction(buildingId);
        if (baseProduction <= 0d)
        {
            return 0d;
        }

        var safeLevel = Math.Max(1, level);
        return baseProduction * Math.Pow(1.2d, safeLevel - 1);
    }

    public static double GetMausoleumBonusPercent(int level)
    {
        var clampedLevel = Mathf.Clamp(level, 1, 3);
        switch (clampedLevel)
        {
            case 1:
                return 20d;
            case 2:
                return 30d;
            default:
                return 40d;
        }
    }

    public static bool IsKnownBuilding(string buildingId)
    {
        return buildingId == GraveId
            || buildingId == HeadquartersId
            || buildingId == MorgueId
            || buildingId == MausoleumId
            || buildingId == OssuaryId
            || buildingId == RotVatId;
    }

    public static Color GetColor(string buildingId)
    {
        switch (buildingId)
        {
            case HeadquartersId:
                return new Color(0.88f, 0.88f, 1f, 1f);
            case GraveId:
                return new Color(0.2f, 0.65f, 0.3f, 1f);
            case MorgueId:
                return new Color(0.75f, 0.2f, 0.2f, 1f);
            case MausoleumId:
                return new Color(0.5f, 0.2f, 0.7f, 1f);
            case OssuaryId:
                return new Color(0.8f, 0.8f, 0.8f, 1f);
            case RotVatId:
                return new Color(0.6f, 0.45f, 0.2f, 1f);
            default:
                return Color.white;
        }
    }

    public static string GetDisplayName(string buildingId)
    {
        switch (buildingId)
        {
            case HeadquartersId:
                return "Graveyard Chapel";
            case GraveId:
                return "Grave";
            case MorgueId:
                return "Morgue";
            case MausoleumId:
                return "Mausoleum";
            case OssuaryId:
                return "Ossuary";
            case RotVatId:
                return "Rot Vat";
            default:
                return "Unknown";
        }
    }

    public static Sprite GetSprite(string buildingId)
    {
        switch (buildingId)
        {
            case HeadquartersId:
                return SpriteCache.HeadquartersSprite;
            case GraveId:
                return SpriteCache.GraveSprite;
            case MorgueId:
                return SpriteCache.MorgueSprite;
            case MausoleumId:
                return SpriteCache.MausoleumSprite;
            case OssuaryId:
                return SpriteCache.OssuarySprite;
            case RotVatId:
                return SpriteCache.RotVatSprite;
            default:
                return SpriteCache.FallbackSprite;
        }
    }

    public static bool IsHeadquarters(string buildingId)
    {
        return buildingId == HeadquartersId;
    }

    private static class SpriteCache
    {
        public static readonly Sprite HeadquartersSprite = CreateSpriteFor(HeadquartersId);
        public static readonly Sprite GraveSprite = CreateSpriteFor(GraveId);
        public static readonly Sprite MorgueSprite = CreateSpriteFor(MorgueId);
        public static readonly Sprite MausoleumSprite = CreateSpriteFor(MausoleumId);
        public static readonly Sprite OssuarySprite = CreateSpriteFor(OssuaryId);
        public static readonly Sprite RotVatSprite = CreateSpriteFor(RotVatId);
        public static readonly Sprite FallbackSprite = CreateSpriteFor(string.Empty);

        private static Sprite CreateSpriteFor(string buildingId)
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var clear = new Color32(0, 0, 0, 0);
            var pixels = new Color32[64 * 64];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            var fill = new Color32(230, 230, 230, 255);
            DrawRect(pixels, 64, 14, 6, 36, 6, fill);

            if (buildingId == HeadquartersId)
            {
                DrawRect(pixels, 64, 14, 14, 36, 24, fill);
                for (var y = 0; y < 10; y++)
                {
                    DrawRect(pixels, 64, 14 + y, 38 + y, 36 - (2 * y), 1, fill);
                }

                DrawRect(pixels, 64, 28, 14, 8, 16, new Color32(65, 65, 65, 255));
                DrawRect(pixels, 64, 30, 40, 4, 12, fill);
                DrawRect(pixels, 64, 26, 46, 12, 2, fill);
            }
            else if (buildingId == GraveId)
            {
                DrawRect(pixels, 64, 28, 16, 8, 28, fill);
                DrawRect(pixels, 64, 20, 28, 24, 8, fill);
            }
            else if (buildingId == MorgueId)
            {
                DrawRect(pixels, 64, 14, 16, 36, 24, fill);
                DrawRect(pixels, 64, 28, 18, 8, 20, new Color32(60, 60, 60, 255));
            }
            else if (buildingId == MausoleumId)
            {
                DrawRect(pixels, 64, 14, 16, 36, 20, fill);
                for (var y = 0; y < 16; y++)
                {
                    DrawRect(pixels, 64, 14 + y, 36 + y, 36 - (2 * y), 1, fill);
                }
                DrawRect(pixels, 64, 27, 16, 10, 12, new Color32(50, 50, 50, 255));
            }
            else if (buildingId == OssuaryId)
            {
                DrawRect(pixels, 64, 14, 14, 36, 28, fill);
                DrawRect(pixels, 64, 18, 18, 28, 20, new Color32(45, 45, 45, 255));
                DrawRect(pixels, 64, 24, 22, 4, 4, fill);
                DrawRect(pixels, 64, 36, 22, 4, 4, fill);
                DrawRect(pixels, 64, 28, 30, 8, 2, fill);
            }
            else if (buildingId == RotVatId)
            {
                DrawRect(pixels, 64, 16, 14, 32, 30, fill);
                DrawRect(pixels, 64, 20, 18, 24, 22, new Color32(55, 70, 35, 255));
                DrawRect(pixels, 64, 12, 44, 40, 4, fill);
            }
            else
            {
                DrawRect(pixels, 64, 12, 12, 40, 40, fill);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 64f);
        }

        private static void DrawRect(Color32[] pixels, int textureWidth, int x, int y, int width, int height, Color32 color)
        {
            var xMin = Mathf.Clamp(x, 0, textureWidth);
            var xMax = Mathf.Clamp(x + width, 0, textureWidth);
            var yMin = Mathf.Clamp(y, 0, textureWidth);
            var yMax = Mathf.Clamp(y + height, 0, textureWidth);

            for (var py = yMin; py < yMax; py++)
            {
                for (var px = xMin; px < xMax; px++)
                {
                    pixels[(py * textureWidth) + px] = color;
                }
            }
        }
    }
}
