using System.Collections.Generic;
using UnityEngine;

public sealed class FloatingTextSpawner : MonoBehaviour
{
    private const float DurationSeconds = 1f;
    private const float RiseDistance = 0.3f;

    private static FloatingTextSpawner instance;
    private readonly List<FloatingTextEntry> activeEntries = new List<FloatingTextEntry>();

    private sealed class FloatingTextEntry
    {
        public TextMesh TextMesh;
        public float Remaining;
        public Color BaseColor;
    }

    public static void Spawn(Vector3 worldPos, string text, Color color)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        EnsureExists().SpawnInternal(worldPos, text, color);
    }

    private static FloatingTextSpawner EnsureExists()
    {
        if (instance != null)
        {
            return instance;
        }

        var existing = FindObjectOfType<FloatingTextSpawner>();
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        var root = new GameObject("FloatingTextSpawner");
        instance = root.AddComponent<FloatingTextSpawner>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void SpawnInternal(Vector3 worldPos, string text, Color color)
    {
        var textObject = new GameObject("FloatingCollectText");
        textObject.transform.SetParent(transform, false);
        textObject.transform.position = worldPos;

        var textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.characterSize = 0.15f;
        textMesh.fontSize = 64;
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        var meshRenderer = textObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = 900;
        }

        activeEntries.Add(new FloatingTextEntry
        {
            TextMesh = textMesh,
            Remaining = DurationSeconds,
            BaseColor = color
        });
    }

    private void Update()
    {
        for (var i = activeEntries.Count - 1; i >= 0; i--)
        {
            var entry = activeEntries[i];
            if (entry.TextMesh == null)
            {
                activeEntries.RemoveAt(i);
                continue;
            }

            entry.Remaining -= Time.deltaTime;
            entry.TextMesh.transform.position += new Vector3(0f, (RiseDistance / DurationSeconds) * Time.deltaTime, 0f);

            var alpha = Mathf.Clamp01(entry.Remaining / DurationSeconds);
            var color = entry.BaseColor;
            color.a *= alpha;
            entry.TextMesh.color = color;

            if (entry.Remaining <= 0f)
            {
                Destroy(entry.TextMesh.gameObject);
                activeEntries.RemoveAt(i);
            }
        }
    }
}
