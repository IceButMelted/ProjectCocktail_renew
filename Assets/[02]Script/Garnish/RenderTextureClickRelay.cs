// ============================================================
//  RenderTextureClickRelay.cs — Sits on the RawImage that shows a
//  camera's render texture and passes a click on the picture through
//  to the collider drawn under it, so world objects shown as UI stay
//  clickable (the garnish slots on the glass).
// ============================================================

using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class RenderTextureClickRelay : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("The camera that renders into the texture this image shows.")]
    [SerializeField] private Camera _camera;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_camera == null) return;

        var rect = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return;

        Ray ray = _camera.ViewportPointToRay(Rect.PointToNormalized(rect.rect, local));
        if (Physics.Raycast(ray, out RaycastHit hit, _camera.farClipPlane))
            ExecuteEvents.ExecuteHierarchy(hit.collider.gameObject, eventData, ExecuteEvents.pointerClickHandler);
    }
}
