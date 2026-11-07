using System;
using NUnit.Framework;
using UnityEngine;

namespace Bar410.Contracts.Tests
{
    public class ContractsTests
    {
        sealed class FakePointer : IPointerSource
        {
            public Vector2 ScreenPosition { get; set; }
            public bool PointerVisible => true;
        }

        [Test] public void PointerRay_NullCamera_Throws()
            => Assert.Throws<ArgumentNullException>(() => PointerRay.From(new FakePointer(), null));

        [Test] public void PointerRay_NullSource_Throws()
        {
            var go = new GameObject("cam", typeof(Camera));
            try { Assert.Throws<ArgumentNullException>(() => PointerRay.From(null, go.GetComponent<Camera>())); }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test] public void PointerRay_UsesScreenPosition()
        {
            var go = new GameObject("cam", typeof(Camera));
            try
            {
                var cam = go.GetComponent<Camera>();
                var p = new FakePointer { ScreenPosition = new Vector2(cam.pixelWidth / 2f, cam.pixelHeight / 2f) };
                var r = PointerRay.From(p, cam);
                Assert.AreEqual(cam.transform.forward.z, r.direction.z, 0.01f);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test] public void YarnVars_Diff_ReturnsChangedAndAddedOnly()
        {
            var a = new YarnVars(); a.F["x"] = 1; a.F["same"] = 2; a.S["s"] = "a";
            var b = new YarnVars(); b.F["x"] = 3; b.F["same"] = 2; b.F["new"] = 1; b.S["s"] = "a"; b.B["flag"] = true;
            var d = YarnVars.Diff(a, b);
            CollectionAssert.AreEquivalent(new[] { "x", "new" }, d.F.Keys);
            Assert.AreEqual(0, d.S.Count);
            Assert.IsTrue(d.B["flag"]);
        }

        [Test] public void YarnVars_Diff_IdenticalIsEmpty()
        {
            var a = new YarnVars(); a.F["x"] = 1;
            var d = YarnVars.Diff(a, a);
            Assert.AreEqual(0, d.F.Count + d.S.Count + d.B.Count);
        }

        [Test] public void DialogueResumeState_Defaults()
        {
            var s = new DialogueResumeState();
            Assert.IsNotNull(s.OptionChoices);
            Assert.IsNotNull(s.Journal);
            Assert.AreEqual(0, s.LineIndex);
            Assert.IsNull(s.LineId);
        }

        [Test] public void DayPart_None_IsDefault() => Assert.AreEqual(DayPart.None, default(DayPart));

        [Test] public void ScoreResult_IsSerializable()
            => Assert.IsTrue(typeof(ScoreResult).IsSerializable || Attribute.IsDefined(typeof(ScoreResult), typeof(SerializableAttribute)));
    }
}
