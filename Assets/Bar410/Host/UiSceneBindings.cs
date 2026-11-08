using System;
using System.Collections.Generic;
using System.Threading;
using Bar410.Contracts;
using Bar410.Flow;
using Bar410.UiScenes;
using Bar410.WorldViews;
using UnityEngine;
using static E_Cocktail;

namespace Bar410.Host
{
    /// <summary>
    /// G3-T10 glue between the main scene and the additive UI scenes. Owns the UiSceneLoader (published through
    /// UiSceneRegistry), binds the main-scene world objects to each host once per registration, warms cameras and the
    /// minigame canvas during the preload, then hands the hosts to FlowPorts. GameRoot runs <see cref="PreloadAsync"/>
    /// before the day boots (BeforeOpen).
    /// </summary>
    public sealed class UiSceneBindings : IDisposable
    {
        public static readonly IReadOnlyDictionary<UiSceneId, string> SceneNames = new Dictionary<UiSceneId, string>
        {
            { UiSceneId.Minigame, "Minigame_UI" },
            { UiSceneId.Garnish, "Garnish_UI" },
        };

        readonly UiSceneLoader _loader;
        readonly IReadOnlyList<MinigameWorldSource> _minigameWorlds;
        readonly WorldViewSource _customerInset;
        readonly GarnishTable _garnishTable;
        readonly ICustomerStage _stage;

        public UiSceneBindings(IReadOnlyList<MinigameWorldSource> minigameWorlds, WorldViewSource customerInset, GarnishTable garnishTable, ICustomerStage stage)
        {
            _minigameWorlds = minigameWorlds ?? Array.Empty<MinigameWorldSource>();
            _customerInset = customerInset;
            _garnishTable = garnishTable;
            _stage = stage;
            _loader = new UiSceneLoader(SceneNames);
            _loader.Registered += OnRegistered;   // before any load, so no registration is missed
            UiSceneRegistry.Set(_loader);
        }

        public UiSceneLoader Loader => _loader;

        /// <summary>Loads both UI scenes (bind + warm happen on registration), then puts their hosts into <paramref name="ports"/>.</summary>
        public async Awaitable PreloadAsync(FlowPorts ports, CancellationToken ct)
        {
            await _loader.PreloadAsync(ct);
            ports.Minigames = await _loader.GetHostAsync<IMinigameHost>(UiSceneId.Minigame, ct);
            ports.Garnish = await _loader.GetHostAsync<IGarnishHost>(UiSceneId.Garnish, ct);
        }

        public void Dispose()
        {
            _loader.Registered -= OnRegistered;
            UiSceneRegistry.Clear(_loader);
        }

        void OnRegistered(UiSceneId id, object host)
        {
            switch (host)
            {
                case MinigameUiHost minigames: BindMinigames(minigames); break;
                case GarnishUiHost garnish: BindGarnish(garnish); break;
                default: Debug.LogError($"[UiSceneBindings] '{id}' registered an unknown host {host?.GetType().Name}"); break;
            }
        }

        void BindMinigames(MinigameUiHost host)
        {
            var worlds = new Dictionary<Enum_MiniGameType, MinigameWorld>();
            foreach (var source in _minigameWorlds)
            {
                if (source == null) continue;
                worlds[source.Type] = source.Build();
                if (source.View != null) source.View.Warm();
            }
            if (_customerInset == null) Debug.LogWarning("[UiSceneBindings] No customer inset source: the minigame customer picture stays empty");
            else _customerInset.Warm();
            host.Bind(worlds, _customerInset != null ? _customerInset.Handle : null);

            // Build the canvas meshes and layout once, off screen, so the first minigame does not pay for it.
            var canvas = host.Canvas;
            if (canvas != null && !canvas.enabled)
            {
                canvas.enabled = true;
                Canvas.ForceUpdateCanvases();
                canvas.enabled = false;
            }
        }

        void BindGarnish(GarnishUiHost host)
        {
            if (_garnishTable == null) { Debug.LogError("[UiSceneBindings] No GarnishTable in the main scene: the garnish host stays unbound"); return; }
            var view = _garnishTable.GlassView;
            if (view != null) view.Warm();
            host.Bind(_garnishTable.Table, view != null ? view.Handle : null, _garnishTable.Seats, _stage);
        }
    }
}
