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

    public static Sprite GetSprite(string buildingId)
    {
        switch (buildingId)
        {
            case GraveId:
                return SpriteCache.GraveSprite;
            case MorgueId:
                return SpriteCache.MorgueSprite;
            case MausoleumId:
                return SpriteCache.MausoleumSprite;
            default:
                return SpriteCache.FallbackSprite;
        }
    }

    private static class SpriteCache
    {
        public static readonly Sprite GraveSprite = CreateSpriteFor(GraveId);
        public static readonly Sprite MorgueSprite = CreateSpriteFor(MorgueId);
        public static readonly Sprite MausoleumSprite = CreateSpriteFor(MausoleumId);
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

            if (buildingId == GraveId)
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
