using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Bar410.WorldViews.Tests
{
    public class WorldViewTests
    {
        [Test]
        public void ToViewport_MapsIntoUvRect()
        {
            var v = WorldView.ToViewport(new Vector2(0.5f, 1f), new Rect(0.2f, 0.1f, 0.4f, 0.5f));
            Assert.AreEqual(0.4f, v.x, 1e-5f);
            Assert.AreEqual(0.6f, v.y, 1e-5f);
        }

        [Test]
        public void ClickMask_IgnoresDecoyOnOtherLayer()
        {
            var cam = new GameObject("cam").AddComponent<Camera>();
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                box.transform.position = new Vector3(0, 0, 5);
                box.layer = 0;
                Physics.SyncTransforms();
                var h = new WorldViewHandle("t", cam, null, 1 << 8);
                Assert.IsFalse(h.TryPick(new Vector2(.5f, .5f), out _));
                h = new WorldViewHandle("t", cam, null, 1 << 0);
                Assert.IsTrue(h.TryPick(new Vector2(.5f, .5f), out var hit));
                Assert.AreSame(box, hit);
            }
            finally { Object.DestroyImmediate(cam.gameObject); Object.DestroyImmediate(box); }
        }

        [Test]
        public void DestroyedCamera_NoOpsWithOneWarning()
        {
            var go = new GameObject("cam");
            var h = new WorldViewHandle("gone", go.AddComponent<Camera>(), null, ~0);
            Object.DestroyImmediate(go);
            LogAssert.Expect(LogType.Warning, "[WorldView] camera of 'gone' is destroyed");
            h.SetRendering(true);
            Assert.IsFalse(h.TryPick(Vector2.zero, out _));
            Assert.IsFalse(h.IsRendering);
        }

        [Test]
        public void SetRendering_TogglesCamera()
        {
            var go = new GameObject("cam");
            try
            {
                var h = new WorldViewHandle("t", go.AddComponent<Camera>(), null, ~0);
                h.SetRendering(false); Assert.IsFalse(h.IsRendering);
                h.SetRendering(true); Assert.IsTrue(h.IsRendering);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
