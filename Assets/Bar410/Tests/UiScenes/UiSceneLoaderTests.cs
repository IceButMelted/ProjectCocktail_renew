using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Bar410.UiScenes.Tests
{
    // Scene loading, the register timeout and the forbidden-component scan need Play mode (G3-T8/T9 harness).
    public class UiSceneLoaderTests
    {
        sealed class Other : IUiSceneRegistry
        {
            public void Register(UiSceneId id, object host) { }
            public void Unregister(UiSceneId id, object host) { }
        }

        static UiSceneLoader Loader() =>
            new(new Dictionary<UiSceneId, string> { { UiSceneId.Minigame, "NotARealScene" } });

        [TearDown] public void TearDown()
        {
            var c = UiSceneRegistry.Current;
            if (c != null) UiSceneRegistry.Clear(c);
        }

        [Test] public void Registry_SetClear_MismatchedClearIsNoOp()
        {
            var a = Loader();
            UiSceneRegistry.Set(a);
            UiSceneRegistry.Clear(new Other());
            Assert.AreSame(a, UiSceneRegistry.Current);
            UiSceneRegistry.Clear(a);
            Assert.IsNull(UiSceneRegistry.Current);
        }

        [Test] public void Registry_SecondSet_IsIgnoredWithError()
        {
            var a = Loader();
            UiSceneRegistry.Set(a);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Set called twice"));
            UiSceneRegistry.Set(new Other());
            Assert.AreSame(a, UiSceneRegistry.Current);
        }

        [Test] public void DuplicateRegister_KeepsFirstHost()
        {
            var loader = Loader();
            object first = new(), second = new();
            loader.Register(UiSceneId.Minigame, first);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("already registered"));
            loader.Register(UiSceneId.Minigame, second);
            loader.Unregister(UiSceneId.Minigame, second);   // wrong host: no-op
            Assert.IsFalse(loader.IsReady(UiSceneId.Minigame));
        }

        [Test] public async Task Preload_AlreadyRegistered_RaisesRegisteredOnceAndIsReady()
        {
            var loader = Loader();
            var host = new object();
            int raised = 0;
            loader.Registered += (id, h) => { raised++; Assert.AreSame(host, h); };
            loader.Register(UiSceneId.Minigame, host);

            await loader.PreloadAsync(CancellationToken.None);
            await loader.PreloadAsync(CancellationToken.None);

            Assert.IsTrue(loader.IsReady(UiSceneId.Minigame));
            Assert.AreEqual(1, raised);
            Assert.AreSame(host, await loader.GetHostAsync<object>(UiSceneId.Minigame, CancellationToken.None));
        }

        [Test] public async Task Unregister_ThenRegisterAgain_RaisesAgain()
        {
            var loader = Loader();
            object a = new(), b = new();
            int raised = 0;
            loader.Registered += (_, _) => raised++;
            loader.Register(UiSceneId.Minigame, a);
            await loader.PreloadAsync(CancellationToken.None);
            loader.Unregister(UiSceneId.Minigame, a);
            Assert.IsFalse(loader.IsReady(UiSceneId.Minigame));
            loader.Register(UiSceneId.Minigame, b);
            await loader.PreloadAsync(CancellationToken.None);
            Assert.AreEqual(2, raised);
        }
    }
}
