using System;
using UnityEngine;

namespace Bar410.Contracts
{
    public interface IPointerSource { Vector2 ScreenPosition { get; } bool PointerVisible { get; } }

    public static class PointerRay
    {
        public static Ray From(IPointerSource p, Camera cam)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            if (cam == null) throw new ArgumentNullException(nameof(cam));
            return cam.ScreenPointToRay(p.ScreenPosition);
        }
    }
}
