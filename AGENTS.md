# AGENTS.md

## Zasady projektu (Unity 2D Android)

1. Kod źródłowy C# umieszczaj wyłącznie w `Assets/Scripts/`.
2. W MVP nie dodawaj zewnętrznych paczek/pluginów (poza tym, co daje czyste Unity).
3. Save/Load realizuj w JSON, zapis w `Application.persistentDataPath`.
4. Offline progress licz jako różnicę czasu od `lastSaved`, z limitem (`cap`) do 8 godzin.
