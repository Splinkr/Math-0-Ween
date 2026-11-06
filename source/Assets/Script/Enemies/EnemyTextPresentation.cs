using System.Collections.Generic;
using TMPro;
using UnityEngine;
using SpecialEnemyType = OperationEnemySpawner.SpecialEnemyType;

// Text lifetime is independent of active enemies and delayed projectile impacts.
internal sealed class EnemyTextPresentation
{
    private struct TextMaterialOwnership
    {
        public Material originalMaterial;
        public Material instance;
    }
    private readonly Dictionary<TMP_Text, TextMaterialOwnership> ownedTextMaterials = new Dictionary<TMP_Text, TextMaterialOwnership>();
    private readonly List<TMP_Text> destroyedTextBuffer = new List<TMP_Text>();
    private readonly HashSet<TMP_Text> operationTexts = new HashSet<TMP_Text>();
    private readonly Dictionary<TMP_Text, bool> pausedTextStates = new Dictionary<TMP_Text, bool>();
    private int OutlineColorId;
    private int OutlineWidthId;
    private bool outlineIdsInitialized;
    private struct VisualLayout
    {
        internal Bounds localBounds;
        internal string text;
        internal TMP_FontAsset font;
        internal Material material;
        internal float fontSize;
        internal FontStyles style;
        internal Rect rect;
    }
    private readonly Dictionary<TMP_Text, VisualLayout> visualLayouts = new Dictionary<TMP_Text, VisualLayout>();

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    internal static bool OverlapsWithMargin(Bounds a, Bounds b, float margin) =>
        (double)a.max.x + margin > b.min.x && (double)b.max.x + margin > a.min.x
        && (double)a.max.y + margin > b.min.y && (double)b.max.y + margin > a.min.y;

    // Measure TMP glyph quads, including the material padding used for the outline.
    // Cache only local geometry: moving/rotating/scaling an enemy never leaves a stale world rectangle.
    internal bool TryGetVisualBounds(TMP_Text text, out Bounds worldBounds)
    {
        worldBounds = default;
        if (text == null) return false;
        if (!visualLayouts.TryGetValue(text, out VisualLayout layout) || text.havePropertiesChanged
            || layout.text != text.text || layout.font != text.font || layout.material != text.fontSharedMaterial
            || layout.fontSize != text.fontSize || layout.style != text.fontStyle || layout.rect != text.rectTransform.rect)
        {
            text.ForceMeshUpdate(true);
            TMP_TextInfo info = text.textInfo;
            if (info == null || info.characterCount == 0 || info.lineCount != 1 || text.isTextOverflowing) return false;
            Bounds local = default;
            bool found = false;
            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible) continue;
                Vector3[] vertices = info.meshInfo[character.materialReferenceIndex].vertices;
                for (int v = 0; v < 4; v++)
                {
                    Vector3 point = vertices[character.vertexIndex + v];
                    if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z)) return false;
                    if (!found) { local = new Bounds(point, Vector3.zero); found = true; }
                    else local.Encapsulate(point);
                }
            }
            if (!found || local.size.x <= 0 || local.size.y <= 0) return false;
            layout = new VisualLayout { localBounds = local, text = text.text, font = text.font,
                material = text.fontSharedMaterial, fontSize = text.fontSize, style = text.fontStyle, rect = text.rectTransform.rect };
            visualLayouts[text] = layout;
        }
        Bounds bounds = layout.localBounds;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = new Vector3((i & 1) == 0 ? bounds.min.x : bounds.max.x,
                (i & 2) == 0 ? bounds.min.y : bounds.max.y, (i & 4) == 0 ? bounds.min.z : bounds.max.z);
            Vector3 point = text.transform.TransformPoint(corner);
            if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z)) return false;
            if (i == 0) worldBounds = new Bounds(point, Vector3.zero); else worldBounds.Encapsulate(point);
        }
        return worldBounds.size.x > 0 && worldBounds.size.y > 0;
    }

    internal bool CanPlaceAtSpawn(TMP_Text candidate, Camera camera, float margin)
    {
        if (camera == null || !TryGetVisualBounds(candidate, out Bounds bounds)) return false;
        // Only lateral containment. Entry above the visible top is intentional.
        for (int i = 0; i < 8; i++)
        {
            Vector3 point = camera.WorldToViewportPoint(new Vector3((i & 1) == 0 ? bounds.min.x : bounds.max.x,
                (i & 2) == 0 ? bounds.min.y : bounds.max.y, (i & 4) == 0 ? bounds.min.z : bounds.max.z));
            if (!Finite(point.x) || !Finite(point.z) || point.z <= 0 || point.x < 0 || point.x > 1) return false;
        }
        foreach (TMP_Text existing in operationTexts)
        {
            if (existing == null || existing == candidate || !existing.isActiveAndEnabled || existing.color.a <= 0) continue;
            if (!TryGetVisualBounds(existing, out Bounds occupied) || OverlapsWithMargin(bounds, occupied, margin)) return false;
        }
        return true;
    }

    internal bool TryPlaceAtSpawn(GameObject enemy, TMP_Text text, float preferredX, float spawnY,
        float[] columns, Camera camera, float margin)
    {
        if (enemy == null || text == null || columns == null || camera == null) return false;
        var examined = new HashSet<float>();
        // preferredX is chosen by the director (or the existing explicit-position wrapper).
        for (int i = -1; i < columns.Length; i++)
        {
            float x = i < 0 ? preferredX : columns[i];
            if (!Finite(x) || !examined.Add(x)) continue;
            enemy.transform.position = new Vector3(x, spawnY, enemy.transform.position.z);
            if (CanPlaceAtSpawn(text, camera, margin)) return true;
        }
        return false;
    }

    internal void Forget(TMP_Text text)
    {
        if (ReferenceEquals(text, null)) return;
        if (ownedTextMaterials.TryGetValue(text, out TextMaterialOwnership owned))
        {
            if (text != null && text.fontSharedMaterial == owned.instance) text.fontSharedMaterial = owned.originalMaterial;
            Object.Destroy(owned.instance);
            ownedTextMaterials.Remove(text);
        }
        operationTexts.Remove(text);
        pausedTextStates.Remove(text);
        visualLayouts.Remove(text);
    }

    internal Color GetColorForSpecialType(SpecialEnemyType specialType)
    {
        switch (specialType)
        {
            case SpecialEnemyType.DarkRed:
                return new Color(0.5f, 0f, 0f, 1f); // dark red
            case SpecialEnemyType.Golden:
                return new Color(1f, 0.84f, 0f, 1f); // golden
            case SpecialEnemyType.Blue:
                return Color.cyan;
            case SpecialEnemyType.Silver:
                return new Color(0.75f, 0.75f, 0.75f, 1f); // silver
            default:
                return Color.white; // normal enemy
        }
    }

    internal void ApplyBorderToText(TMP_Text tmp, Color borderColor, float borderWidth, bool isGamePaused)
    {
        if (tmp == null)
            return;

        operationTexts.Add(tmp);
        visualLayouts.Remove(tmp);
        if (isGamePaused && !pausedTextStates.ContainsKey(tmp))
        {
            pausedTextStates.Add(tmp, tmp.enabled);
            tmp.enabled = false;
        }

        Material originalMaterial = tmp.fontSharedMaterial;
        Material material = tmp.fontMaterial;
        if (material == null)
            return;

        if (ownedTextMaterials.TryGetValue(tmp, out TextMaterialOwnership previous) && previous.instance != material)
        {
            Object.Destroy(previous.instance);
            ownedTextMaterials.Remove(tmp);
        }

        // The getter clones on first access, but reuses an existing instance.
        // Do not take ownership of materials created by another caller.
        if (material != originalMaterial)
        {
            ownedTextMaterials[tmp] = new TextMaterialOwnership
            {
                originalMaterial = originalMaterial,
                instance = material
            };
        }

        if (!outlineIdsInitialized)
        {
            OutlineColorId = Shader.PropertyToID("_OutlineColor");
            OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
            outlineIdsInitialized = true;
        }
        material.SetColor(OutlineColorId, borderColor);
        material.SetFloat(OutlineWidthId, borderWidth);
        material.EnableKeyword("OUTLINE_ON");
        tmp.UpdateMeshPadding();
    }

    internal void PruneDestroyed()
    {
        foreach (TMP_Text text in operationTexts)
        {
            if (text == null)
                destroyedTextBuffer.Add(text);
        }
        foreach (TMP_Text text in destroyedTextBuffer)
        {
            Forget(text);
        }
        destroyedTextBuffer.Clear();
    }

    internal void SetOperationVisibility(bool hide)
    {
        if (hide)
        {
            foreach (TMP_Text text in operationTexts)
            {
                if (text == null)
                    continue;
                if (!pausedTextStates.ContainsKey(text))
                    pausedTextStates.Add(text, text.enabled);
                text.enabled = false;
            }
        }
        else
        {
            foreach (var entry in pausedTextStates)
            {
                if (entry.Key != null)
                    entry.Key.enabled = entry.Value;
            }
            pausedTextStates.Clear();
        }
    }

    internal void Dispose()
    {
        foreach (var entry in ownedTextMaterials)
        {
            if (entry.Key != null && entry.Key.fontSharedMaterial == entry.Value.instance)
                entry.Key.fontSharedMaterial = entry.Value.originalMaterial;

            Object.Destroy(entry.Value.instance);
        }
        ownedTextMaterials.Clear();
        operationTexts.Clear();
        visualLayouts.Clear();
        destroyedTextBuffer.Clear();
    }

    internal void MarkHit(GameObject enemy)
    {
        TMP_Text text = enemy.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.color = Color.red;
    }

    internal void ApplyFreezeColor(TMP_Text text)
    {
        Color frozen = Color.cyan;
        frozen.a = text.color.a;
        text.color = frozen;
    }

    internal void RestoreColor(TMP_Text text, Color original)
    {
        original.a = text.color.a;
        text.color = original;
    }

    internal Color FadeColor(Color originalColor, bool frozen, float elapsed, float duration)
    {
        Color displayColor = frozen ? Color.cyan : originalColor;
        displayColor.a = Mathf.Lerp(originalColor.a, 0f, elapsed / duration);
        return displayColor;
    }
}
