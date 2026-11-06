using UnityEngine;
using UnityEngine.UI;

// Runtime-only presentation. The transparent center has no geometry or input surface.
public sealed class AdrenalineVignette : MaskableGraphic
{
    private float border;

    internal static AdrenalineVignette Create(Transform owner, float border)
    {
        var root = new GameObject("Adrenaline overlay", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(owner, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1; // Existing Gameplay HUD canvases use orders 0 and 2.
        var graphic = new GameObject("Red vignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(AdrenalineVignette));
        graphic.transform.SetParent(root.transform, false);
        var view = graphic.GetComponent<AdrenalineVignette>();
        view.rectTransform.anchorMin = Vector2.zero;
        view.rectTransform.anchorMax = Vector2.one;
        view.rectTransform.offsetMin = view.rectTransform.offsetMax = Vector2.zero;
        view.raycastTarget = false;
        view.maskable = false;
        view.border = border;
        view.SetOpacity(0f);
        return view;
    }

    internal void SetOpacity(float opacity)
    {
        color = new Color(1f, 0f, 0f, opacity);
        transform.parent.gameObject.SetActive(opacity > 0f);
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = rectTransform.rect;
        float dx = r.width * border, dy = r.height * border;
        Vector2[] outer = { new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax),
            new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMin) };
        Vector2[] inner = { new Vector2(r.xMin + dx, r.yMin + dy), new Vector2(r.xMin + dx, r.yMax - dy),
            new Vector2(r.xMax - dx, r.yMax - dy), new Vector2(r.xMax - dx, r.yMin + dy) };
        for (int i = 0; i < 4; i++) mesh.AddVert(outer[i], color, Vector2.zero);
        for (int i = 0; i < 4; i++) mesh.AddVert(inner[i], new Color(1f, 0f, 0f, 0f), Vector2.zero);
        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4;
            mesh.AddTriangle(i, next, next + 4);
            mesh.AddTriangle(i, next + 4, i + 4);
        }
    }
}
