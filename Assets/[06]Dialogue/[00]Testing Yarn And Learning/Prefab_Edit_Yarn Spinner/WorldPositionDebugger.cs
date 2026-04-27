/*
 * WorldPositionDebugger.cs
 *
 * Tracks a 3-D target through world → screen → canvas → refRect coordinate spaces.
 *
 * Displays in:
 *   • Inspector  (readonly fields, live in Play mode)
 *   • OnGUI      (overlay in Game view)
 *   • Gizmos     (Scene view sphere + label)
 *   • Console    (optional, per-frame)
 *
 * SETUP
 * ─────
 * 1. Add to any GameObject.
 * 2. Assign Target      → the 3-D object to track.
 * 3. Assign Canvas      → your UI Canvas.
 * 4. Assign Ref Rect    → any RectTransform to measure against
 *                         (e.g. BubbleContainer, Bubble Presenter, a panel…).
 * 5. Press Play — all values update every frame.
 */

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YarnSpinner.Custom
{
    [ExecuteAlways]
    public class WorldPositionDebugger : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        // Inspector — inputs
        // ─────────────────────────────────────────────────────────────────────

        [Header("Target")]
        [Tooltip("3-D Transform to track.")]
        public Transform target;

        [Header("Canvas Reference")]
        [Tooltip("The Canvas your UI lives in.")]
        public Canvas referenceCanvas;

        [Tooltip("Override the render camera. " +
                 "Leave empty → auto (null for Overlay, worldCamera for Camera mode).")]
        public Camera renderCamera;

        [Header("Ref Rect  (optional)")]
        [Tooltip("Any RectTransform to measure the target position against.\n" +
                 "E.g. BubbleContainer, Bubble Presenter, or any UI panel.\n" +
                 "When assigned, extra 'Ref Rect' debug values appear below.")]
        public RectTransform referenceRect;

        [Header("Display Options")]
        public bool showOnScreenGUI  = true;
        public bool showSceneGizmos  = true;
        [Tooltip("Log to Console every frame (noisy).")]
        public bool logEveryFrame    = false;

        [Header("Gizmo Style")]
        public Color gizmoColor        = Color.cyan;
        public Color refRectGizmoColor = new Color(1f, 0.6f, 0f);   // orange
        public float gizmoSphereRadius = 0.1f;

        // ─────────────────────────────────────────────────────────────────────
        // Inspector — debug output (read-only)
        // ─────────────────────────────────────────────────────────────────────

        [Header("── World / Screen / Canvas ─────────────────────────────────")]

        [Tooltip("Target world position.")]
        [SerializeField] private Vector3 _worldPosition;

        [Tooltip("Screen pixels. (0,0) = bottom-left.")]
        [SerializeField] private Vector2 _screenPosition;

        [Tooltip("Normalised screen. (0,0) = BL, (1,1) = TR.")]
        [SerializeField] private Vector2 _screenNorm;

        [Tooltip("Is the target in front of the camera?")]
        [SerializeField] private bool _isVisible;

        [Tooltip("Canvas local space (pivot-relative). " +
                 "Same as ScreenPointToLocalPointInRectangle on the canvas rect.")]
        [SerializeField] private Vector2 _canvasLocalPos;

        [Tooltip("anchoredPosition for a child whose anchor = (0.5, 0.5) inside the Canvas. " +
                 "This is what BubblePresenter uses.")]
        [SerializeField] private Vector2 _canvasCentreRelPos;

        [Tooltip("Canvas rect size.")]
        [SerializeField] private Vector2 _canvasSize;

        [Tooltip("Canvas pivot.")]
        [SerializeField] private Vector2 _canvasPivot;

        [Tooltip("Canvas render mode.")]
        [SerializeField] private string _canvasRenderMode;

        // ── Ref Rect section ──────────────────────────────────────────────────

        [Header("── Ref Rect ─────────────────────────────────────────────────")]

        [Tooltip("Ref Rect local space (pivot-relative). " +
                 "Same as ScreenPointToLocalPointInRectangle on the ref rect.")]
        [SerializeField] private Vector2 _refRectLocalPos;

        [Tooltip("anchoredPosition to use if a child of Ref Rect has its anchor at (0.5, 0.5). " +
                 "i.e. centre-relative inside Ref Rect.")]
        [SerializeField] private Vector2 _refRectCentreRelPos;

        [Tooltip("anchoredPosition a child of Ref Rect would need to sit at the target " +
                 "given its CURRENT anchor setting. " +
                 "Useful when your child already has a custom anchor.")]
        [SerializeField] private Vector2 _refRectAnchorRelPos;

        [Tooltip("Ref Rect rect.size in canvas units.")]
        [SerializeField] private Vector2 _refRectSize;

        [Tooltip("Ref Rect pivot.")]
        [SerializeField] private Vector2 _refRectPivot;

        [Tooltip("Ref Rect anchorMin.")]
        [SerializeField] private Vector2 _refRectAnchorMin;

        [Tooltip("Ref Rect anchorMax.")]
        [SerializeField] private Vector2 _refRectAnchorMax;

        [Tooltip("Ref Rect's current anchoredPosition (live).")]
        [SerializeField] private Vector2 _refRectAnchoredPos;

        [Tooltip("Ref Rect's world-space centre position.")]
        [SerializeField] private Vector3 _refRectWorldCentre;

        // ─────────────────────────────────────────────────────────────────────
        // Runtime
        // ─────────────────────────────────────────────────────────────────────

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;

        // ─────────────────────────────────────────────────────────────────────
        // Unity
        // ─────────────────────────────────────────────────────────────────────

        private void Update()
        {
            if (target == null || referenceCanvas == null) return;
            RefreshValues();
            if (logEveryFrame) LogValues();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Core calculation
        // ─────────────────────────────────────────────────────────────────────

        private void RefreshValues()
        {
            Camera cam = ResolveCamera();

            // ── World ─────────────────────────────────────────────────────
            _worldPosition = target.position;

            // ── Screen ────────────────────────────────────────────────────
            if (cam != null)
            {
                Vector3 sp = cam.WorldToScreenPoint(_worldPosition);
                _isVisible      = sp.z > 0f;
                _screenPosition = new Vector2(sp.x, sp.y);
            }
            else
            {
                _screenPosition = new Vector2(_worldPosition.x, _worldPosition.y);
                _isVisible      = true;
            }

            _screenNorm = new Vector2(
                _screenPosition.x / Screen.width,
                _screenPosition.y / Screen.height);

            // ── Canvas ────────────────────────────────────────────────────
            var canvasRect = referenceCanvas.GetComponent<RectTransform>();
            _canvasSize       = canvasRect.rect.size;
            _canvasPivot      = canvasRect.pivot;
            _canvasRenderMode = referenceCanvas.renderMode.ToString();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, _screenPosition, cam, out Vector2 canvasLocal);
            _canvasLocalPos = canvasLocal;

            _canvasCentreRelPos = canvasLocal - RectCentre(canvasRect);

            // ── Ref Rect ──────────────────────────────────────────────────
            if (referenceRect != null)
            {
                _refRectSize         = referenceRect.rect.size;
                _refRectPivot        = referenceRect.pivot;
                _refRectAnchorMin    = referenceRect.anchorMin;
                _refRectAnchorMax    = referenceRect.anchorMax;
                _refRectAnchoredPos  = referenceRect.anchoredPosition;
                _refRectWorldCentre  = referenceRect.TransformPoint(
                                           referenceRect.rect.center);

                // Local space of ref rect (pivot-relative)
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    referenceRect, _screenPosition, cam, out Vector2 refLocal);
                _refRectLocalPos = refLocal;

                // Centre-relative (for anchor 0.5, 0.5 child)
                _refRectCentreRelPos = refLocal - RectCentre(referenceRect);

                // Anchor-relative: what anchoredPosition a child needs given
                // the ref rect's CURRENT anchor.
                // anchoredPosition = localPos - anchorPointInParentLocalSpace
                // anchorPoint (for a child of referenceRect's parent) is computed
                // from referenceRect's parent rect and its anchorMin/Max.
                _refRectAnchorRelPos = refLocal - AnchorPointInLocalSpace(referenceRect);
            }
            else
            {
                _refRectSize         = Vector2.zero;
                _refRectPivot        = Vector2.zero;
                _refRectAnchorMin    = Vector2.zero;
                _refRectAnchorMax    = Vector2.zero;
                _refRectAnchoredPos  = Vector2.zero;
                _refRectWorldCentre  = Vector3.zero;
                _refRectLocalPos     = Vector2.zero;
                _refRectCentreRelPos = Vector2.zero;
                _refRectAnchorRelPos = Vector2.zero;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public mapping functions
        // ─────────────────────────────────────────────────────────────────────
        //
        // These are static so any script can call them without needing a
        // reference to this debugger instance.
        //
        // All overloads accept a screen-space pixel position (Vector2) and
        // return a canvas/rect position ready to assign to anchoredPosition.
        //
        // Usage examples:
        //
        //   // Map a world object to a canvas anchoredPosition (anchor 0.5,0.5):
        //   Vector2 pos = WorldPositionDebugger.WorldToCanvasAnchoredPos(
        //                     myTransform.position, referenceCanvas, cam);
        //   bubbleRect.anchoredPosition = pos;
        //
        //   // Map a screen point to a RectTransform local position:
        //   Vector2 local = WorldPositionDebugger.ScreenToRectLocal(
        //                       screenPt, myRectTransform, cam);
        // ─────────────────────────────────────────────────────────────────────

        // ── Screen → Canvas ───────────────────────────────────────────────────

        /// <summary>
        /// Maps a screen-pixel position to the Canvas RectTransform's
        /// LOCAL space (pivot-relative).
        /// This is the raw value of ScreenPointToLocalPointInRectangle.
        /// </summary>
        public static Vector2 ScreenToCanvasLocal(
            Vector2 screenPos, Canvas canvas, Camera cam)
        {
            if (canvas == null) return Vector2.zero;
            var rt = canvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rt, screenPos, cam, out Vector2 local);
            return local;
        }

        /// <summary>
        /// Maps a screen-pixel position to an anchoredPosition suitable for
        /// a direct child of <paramref name="canvas"/> whose anchor is (0.5, 0.5).
        /// i.e. returns a CENTRE-RELATIVE position inside the Canvas.
        /// </summary>
        public static Vector2 ScreenToCanvasAnchoredPos(
            Vector2 screenPos, Canvas canvas, Camera cam)
        {
            if (canvas == null) return Vector2.zero;
            var rt = canvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rt, screenPos, cam, out Vector2 local);
            Rect r = rt.rect;
            Vector2 centre = new Vector2(r.x + r.width * 0.5f, r.y + r.height * 0.5f);
            return local - centre;
        }

        /// <summary>
        /// Maps a screen-pixel position to an anchoredPosition for a child of
        /// <paramref name="canvas"/> with a CUSTOM anchor defined by
        /// <paramref name="childAnchorMin"/> and <paramref name="childAnchorMax"/>.
        /// </summary>
        public static Vector2 ScreenToCanvasAnchoredPos(
            Vector2 screenPos, Canvas canvas, Camera cam,
            Vector2 childAnchorMin, Vector2 childAnchorMax)
        {
            if (canvas == null) return Vector2.zero;
            var rt = canvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rt, screenPos, cam, out Vector2 local);
            Rect r   = rt.rect;
            float ax = Mathf.Lerp(childAnchorMin.x, childAnchorMax.x, 0.5f);
            float ay = Mathf.Lerp(childAnchorMin.y, childAnchorMax.y, 0.5f);
            Vector2 anchor = new Vector2(r.x + r.width * ax, r.y + r.height * ay);
            return local - anchor;
        }

        // ── Screen → RectTransform ────────────────────────────────────────────

        /// <summary>
        /// Maps a screen-pixel position to <paramref name="rect"/>'s
        /// LOCAL space (pivot-relative).
        /// </summary>
        public static Vector2 ScreenToRectLocal(
            Vector2 screenPos, RectTransform rect, Camera cam)
        {
            if (rect == null) return Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect, screenPos, cam, out Vector2 local);
            return local;
        }

        /// <summary>
        /// Maps a screen-pixel position to an anchoredPosition for a child of
        /// <paramref name="rect"/> whose anchor is (0.5, 0.5).
        /// i.e. centre-relative inside <paramref name="rect"/>.
        /// </summary>
        public static Vector2 ScreenToRectAnchoredPos(
            Vector2 screenPos, RectTransform rect, Camera cam)
        {
            if (rect == null) return Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect, screenPos, cam, out Vector2 local);
            Rect r = rect.rect;
            Vector2 centre = new Vector2(r.x + r.width * 0.5f, r.y + r.height * 0.5f);
            return local - centre;
        }

        /// <summary>
        /// Maps a screen-pixel position to an anchoredPosition for a child of
        /// <paramref name="rect"/> with a CUSTOM anchor defined by
        /// <paramref name="childAnchorMin"/> and <paramref name="childAnchorMax"/>.
        /// </summary>
        public static Vector2 ScreenToRectAnchoredPos(
            Vector2 screenPos, RectTransform rect, Camera cam,
            Vector2 childAnchorMin, Vector2 childAnchorMax)
        {
            if (rect == null) return Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect, screenPos, cam, out Vector2 local);
            Rect r   = rect.rect;
            float ax = Mathf.Lerp(childAnchorMin.x, childAnchorMax.x, 0.5f);
            float ay = Mathf.Lerp(childAnchorMin.y, childAnchorMax.y, 0.5f);
            Vector2 anchor = new Vector2(r.x + r.width * ax, r.y + r.height * ay);
            return local - anchor;
        }

        // ── World → Canvas / Rect (convenience wrappers) ─────────────────────

        /// <summary>
        /// Maps a world-space position to an anchoredPosition for a child of
        /// <paramref name="canvas"/> whose anchor is (0.5, 0.5).
        /// Combines WorldToScreenPoint + ScreenToCanvasAnchoredPos.
        /// </summary>
        public static Vector2 WorldToCanvasAnchoredPos(
            Vector3 worldPos, Canvas canvas, Camera cam)
        {
            if (canvas == null || cam == null) return Vector2.zero;
            Vector2 screen = cam.WorldToScreenPoint(worldPos);
            return ScreenToCanvasAnchoredPos(screen, canvas, cam);
        }

        /// <summary>
        /// Maps a world-space position to an anchoredPosition for a child of
        /// <paramref name="rect"/> whose anchor is (0.5, 0.5).
        /// Combines WorldToScreenPoint + ScreenToRectAnchoredPos.
        /// </summary>
        public static Vector2 WorldToRectAnchoredPos(
            Vector3 worldPos, RectTransform rect, Camera cam)
        {
            if (rect == null || cam == null) return Vector2.zero;
            Vector2 screen = cam.WorldToScreenPoint(worldPos);
            return ScreenToRectAnchoredPos(screen, rect, cam);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Rect helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the rect centre in the RectTransform's own pivot-relative space.
        /// rect.x = -(pivot.x * width), rect.y = -(pivot.y * height)
        /// centre = rect.x + width/2, rect.y + height/2
        /// </summary>
        private static Vector2 RectCentre(RectTransform rt)
        {
            Rect r = rt.rect;
            return new Vector2(r.x + r.width * 0.5f, r.y + r.height * 0.5f);
        }

        /// <summary>
        /// Returns the anchor point of <paramref name="rt"/> expressed in
        /// <paramref name="rt"/>'s own LOCAL space (pivot-relative).
        ///
        /// This is the origin for anchoredPosition: a child placed at
        /// anchoredPosition=(0,0) will sit at this point inside the parent.
        ///
        /// For a uniform anchor (anchorMin == anchorMax) we use that point.
        /// For a stretched anchor we use the centre of the anchor range.
        /// </summary>
        private static Vector2 AnchorPointInLocalSpace(RectTransform rt)
        {
            var parent = rt.parent as RectTransform;
            if (parent == null) return Vector2.zero;

            Rect pr    = parent.rect;
            float ancX = Mathf.Lerp(rt.anchorMin.x, rt.anchorMax.x, 0.5f);
            float ancY = Mathf.Lerp(rt.anchorMin.y, rt.anchorMax.y, 0.5f);

            // Anchor point in parent local space (pivot-relative)
            Vector2 anchorInParent = new Vector2(
                pr.x + pr.width  * ancX,
                pr.y + pr.height * ancY);

            // Convert from parent-local to rt-local
            // rt-local = (point - rt.localPosition projected to 2D)
            // Since we want it in rt's pivot-relative space and
            // ScreenPointToLocalPointInRectangle also returns pivot-relative,
            // we subtract rt's local position (X,Y) from the parent-local anchor.
            Vector3 rtLocalPos = rt.localPosition;
            return anchorInParent - new Vector2(rtLocalPos.x, rtLocalPos.y);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Camera
        // ─────────────────────────────────────────────────────────────────────

        private Camera ResolveCamera()
        {
            if (renderCamera != null) return renderCamera;
            if (referenceCanvas == null) return Camera.main;
            return referenceCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : (referenceCanvas.worldCamera != null ? referenceCanvas.worldCamera : Camera.main);
        }

        // ─────────────────────────────────────────────────────────────────────
        // OnGUI overlay
        // ─────────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (!showOnScreenGUI || target == null || referenceCanvas == null) return;
            if (!Application.isPlaying) return;

            EnsureStyles();

            bool hasRef = referenceRect != null;

            // Section 1 — World / Screen / Canvas
            string s1 =
                $"<b>[ WorldPositionDebugger ]  target: {target.name}</b>\n" +
                $"<b>World</b>              {Fmt(_worldPosition)}" +
                    $"   visible={_isVisible}\n" +
                $"<b>Screen (px)</b>        {Fmt(_screenPosition)}\n" +
                $"<b>Screen (norm)</b>      {FmtN(_screenNorm)}" +
                    $"   ({(_screenNorm.x < 0.5f ? "LEFT" : _screenNorm.x > 0.5f ? "RIGHT" : "CENTER")})\n" +
                $"<b>Canvas local</b>       {Fmt(_canvasLocalPos)}" +
                    $"   size={Fmt(_canvasSize)}  pivot={FmtN(_canvasPivot)}\n" +
                $"<b>Canvas centre-rel</b>  {Fmt(_canvasCentreRelPos)}" +
                    $"  ← anchoredPos for anchor(0.5,0.5) child\n" +
                $"<b>Canvas mode</b>        {_canvasRenderMode}";

            float w = 560f;
            float h1 = 130f;
            GUI.Box(new Rect(10, 10, w, h1), s1, _boxStyle);

            // Section 2 — Ref Rect
            if (hasRef)
            {
                string refName = referenceRect.name;
                string s2 =
                    $"<b>[ Ref Rect: {refName} ]</b>\n" +
                    $"<b>Size</b>               {Fmt(_refRectSize)}" +
                        $"   pivot={FmtN(_refRectPivot)}\n" +
                    $"<b>Anchor min/max</b>     {FmtN(_refRectAnchorMin)}  /  {FmtN(_refRectAnchorMax)}\n" +
                    $"<b>anchoredPosition</b>   {Fmt(_refRectAnchoredPos)}  (current)\n" +
                    $"<b>World centre</b>       {Fmt(_refRectWorldCentre)}\n" +
                    "──\n" +
                    $"<b>Target in local</b>    {Fmt(_refRectLocalPos)}" +
                        $"   (pivot-relative)\n" +
                    $"<b>Centre-relative</b>    {Fmt(_refRectCentreRelPos)}" +
                        $"  ← anchoredPos if child anchor=(0.5,0.5)\n" +
                    $"<b>Anchor-relative</b>    {Fmt(_refRectAnchorRelPos)}" +
                        $"  ← anchoredPos respecting current anchor";

                float h2 = 180f;
                GUI.Box(new Rect(10, 10 + h1 + 6, w, h2), s2, _boxStyle);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Scene Gizmos
        // ─────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showSceneGizmos) return;

            // Target sphere + label
            if (target != null)
            {
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireSphere(target.position, gizmoSphereRadius);
                Gizmos.DrawLine(transform.position, target.position);

                Handles.color = gizmoColor;
                Handles.Label(
                    target.position + Vector3.up * gizmoSphereRadius * 2.5f,
                    $"{target.name}\n" +
                    $"World:  {Fmt(_worldPosition)}\n" +
                    $"Screen: {Fmt(_screenPosition)}\n" +
                    $"CanvasCentre: {Fmt(_canvasCentreRelPos)}");
            }

            // Ref Rect — draw its world-space bounding box + label
            if (referenceRect != null)
            {
                Vector3[] corners = new Vector3[4];
                referenceRect.GetWorldCorners(corners);

                Gizmos.color = refRectGizmoColor;
                for (int i = 0; i < 4; i++)
                    Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);

                // Draw sphere at world centre
                Gizmos.DrawWireSphere(_refRectWorldCentre, gizmoSphereRadius * 0.6f);

                Handles.color = refRectGizmoColor;
                Handles.Label(
                    corners[1] + Vector3.up * 0.05f,   // top-left corner + tiny offset
                    $"[{referenceRect.name}]\n" +
                    $"size={Fmt(_refRectSize)}\n" +
                    $"centreRel={Fmt(_refRectCentreRelPos)}\n" +
                    $"anchorRel={Fmt(_refRectAnchorRelPos)}");
            }
        }
#endif

        // ─────────────────────────────────────────────────────────────────────
        // Console
        // ─────────────────────────────────────────────────────────────────────

        private void LogValues()
        {
            string refSection = referenceRect != null
                ? $"\n  RefRect local      : {_refRectLocalPos}" +
                  $"\n  RefRect centre-rel : {_refRectCentreRelPos}" +
                  $"\n  RefRect anchor-rel : {_refRectAnchorRelPos}" +
                  $"\n  RefRect size       : {_refRectSize}" +
                  $"\n  RefRect anchor     : {_refRectAnchorMin} / {_refRectAnchorMax}" +
                  $"\n  RefRect pivot      : {_refRectPivot}"
                : "\n  RefRect: (not assigned)";

            Debug.Log(
                $"[WorldPositionDebugger] {target.name}\n" +
                $"  World          : {_worldPosition}  visible={_isVisible}\n" +
                $"  Screen (px)    : {_screenPosition}\n" +
                $"  Screen (norm)  : {_screenNorm}\n" +
                $"  Canvas local   : {_canvasLocalPos}\n" +
                $"  Canvas centre  : {_canvasCentreRelPos}\n" +
                $"  Canvas size    : {_canvasSize}  mode={_canvasRenderMode}" +
                refSection
            );
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private void EnsureStyles()
        {
            if (_boxStyle != null) return;
            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize  = 12,
                richText  = true,
                padding   = new RectOffset(8, 8, 6, 6)
            };
            _boxStyle.normal.textColor = Color.white;
        }

        private static string Fmt(Vector2 v) => $"({v.x:F1}, {v.y:F1})";
        private static string Fmt(Vector3 v) => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
        private static string FmtN(Vector2 v) => $"({v.x:F3}, {v.y:F3})";
    }
}
