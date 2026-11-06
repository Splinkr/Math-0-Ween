using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ScrollItemScroller : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("Setup")]
    public ScrollRect scrollRect;
    public float itemSize = 69f;
    private bool isDragging = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        SnapToNearestItem();
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private bool TryGetScrollState(out float distance, out float offset)
    {
        distance = 0f;
        offset = 0f;
        if (scrollRect == null || scrollRect.content == null || scrollRect.viewport == null)
            return false;

        float contentHeight = scrollRect.content.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height;
        distance = contentHeight - viewportHeight;
        if (!IsFinite(contentHeight) || !IsFinite(viewportHeight) || !IsFinite(distance))
        {
            scrollRect.StopMovement();
            return false;
        }
        if (distance <= 0f)
        {
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
            return false;
        }
        if (!IsFinite(itemSize) || itemSize <= 0f || !IsFinite(distance / itemSize) || distance / itemSize >= int.MaxValue)
        {
            scrollRect.StopMovement();
            return false;
        }

        float position = scrollRect.verticalNormalizedPosition;
        if (!IsFinite(position))
        {
            scrollRect.StopMovement();
            return false;
        }
        offset = (1f - Mathf.Clamp01(position)) * distance;
        return true;
    }

    private void ApplyOffset(float distance, float offset)
    {
        if (IsFinite(offset) && IsFinite(distance) && distance > 0f)
            scrollRect.verticalNormalizedPosition = 1f - Mathf.Clamp(offset, 0f, distance) / distance;
    }

    private void SnapToNearestItem()
    {
        if (!TryGetScrollState(out float distance, out float offset))
            return;
        int nearestIndex = Mathf.RoundToInt(offset / itemSize);
        ApplyOffset(distance, nearestIndex * itemSize);
    }

    private void ScrollBy(float scrollDelta)
    {
        if (isDragging || !IsFinite(scrollDelta) || Mathf.Abs(scrollDelta) <= 0.01f)
            return;
        if (!TryGetScrollState(out float distance, out float offset))
            return;
        int direction = scrollDelta > 0f ? -1 : 1;
        int currentIndex = Mathf.RoundToInt(offset / itemSize);
        int newIndex = Mathf.Clamp(currentIndex + direction, 0, Mathf.FloorToInt(distance / itemSize));
        ApplyOffset(distance, newIndex * itemSize);
    }

    private void Update()
    {
        if (!isDragging)
            ScrollBy(Input.GetAxis("Mouse ScrollWheel"));
    }
}
