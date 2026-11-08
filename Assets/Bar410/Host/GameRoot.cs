using System.Collections.Generic;
using System.Threading;
using Bar410.Contracts;
using Bar410.Core;
using Bar410.Flow;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Bar410.Host
{
    /// <summary>
    /// Composition root of the main scene (G1-12): builds the adapters, FlowPorts, FlowState, BartendingSession, DayRunner and
    /// FlowCommands by hand, binds MainWiring, and boots the day. Scene-scoped singleton: a duplicate destroys itself;
    /// no DontDestroyOnLoad. Not wired into any scene yet (G1-13 creates Main_V2).
    /// ponytail: stubs until later tasks (Main_V2 cannot boot until the garnish host exists; Awake/Start log an error):
    /// - no IStory implementation yet (G2 Director), and no G2 verb bind (Director.Init / YarnVerbRegistry);
    /// - no LegacyGarnish yet (IGarnishHost): with 'Use Ui Scenes' off the garnish host must be set by hand;
    /// - no "order" snapshot (CocktailFeature is not an ISnapshotSource, needs N4);
    /// - no resume (PendingLoad) or autosave (G4).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameRoot : MonoBehaviour
    {
        public static GameRoot Current { get; private set; }

        [Title("Flow")]
        [Tooltip("Order matters: Prepare, Garnish, Serve.")]
        [SerializeField, Required] Station[] _steps;
        [SerializeField, Required] SO_DaySchedule _schedule;
        [Tooltip("A component that implements IStory (the dialogue Director).")]
        [SerializeField, Required] MonoBehaviour _story;
        [Tooltip("A component that implements IGarnishHost. Empty = garnish step will fail loudly.")]
        [SerializeField] MonoBehaviour _garnishHost;

        [Title("Cocktail")]
        [SerializeField, Required] CocktailSystemManager _cocktail;
        [SerializeField, Required] ShakerContents _shaker;
        [SerializeField, Required] ShakerPanelController _shakerPanels;
        [SerializeField, Required] IngredientButtonGroup _bottles;
        [SerializeField, Required] MethodPanel _method;
        [SerializeField, Required] VisualizeCocktail _fillBars;
        [SerializeField, Required] Post_It_Order _orderNote;
        [SerializeField, Required] BookUI_V2 _book;
        [SerializeField, Required] Button _bookButton;
        [SerializeField] IngredientUnlocks _unlocks;   // empty = everything unlocked

        [Title("Customers, camera, minigame")]
        [SerializeField, Required] TalkToCustomerManager _customers;
        [SerializeField, Required] CameraManager _cameras;
        [SerializeField] Bar410.WorldViews.WorldViewSource _customerInset;
        [SerializeField] Vector3 _customerInsetOffset;
        [Tooltip("Legacy minigame manager in this scene; used only when 'Use Ui Scenes' is off.")]
        [SerializeField] MinigameSystemManager _minigames;

        [Title("UI scenes (G3)")]
        [Tooltip("On: the minigame and garnish hosts come from Minigame_UI / Garnish_UI, preloaded before the day boots. Off: legacy managers (and 'Garnish Host') in this scene.")]
        [SerializeField] bool _useUiScenes;
        [SerializeField] MinigameWorldSource[] _minigameWorlds;
        [SerializeField] GarnishTable _garnishTable;

        [Title("Day UI")]
        [SerializeField] PanelTransition _transition;
        [SerializeField] EventPopupQueue _popups;
        [SerializeField] ChapterTitleUI _chapterTitle;
        [SerializeField] HubManager _hub;

        public CancellationToken Lifetime => destroyCancellationToken;
        public FlowState State { get; private set; }
        public FlowPorts Ports { get; private set; }
        public BartendingSession Session { get; private set; }
        public DayRunner Day { get; private set; }
        public FlowCommands Commands { get; private set; }
        /// <summary>Save sources built here (G4 registers them).</summary>
        public IReadOnlyList<ISnapshotSource> Snapshots { get; private set; }

        CocktailFeature _bar;
        UnlocksAdapter _unlockPort;
        MainWiring _wiring;
        UiSceneBindings _uiScenes;

        void Awake()
        {
            if (Current != null && Current != this)
            {
                Debug.LogError("[GameRoot] A second GameRoot was found: destroying it", this);
                Destroy(this);
                return;
            }
            Current = this;

            if (_steps == null || _steps.Length == 0) { Debug.LogError("[GameRoot] No steps assigned", this); return; }
            if (_story is not IStory story) { Debug.LogError("[GameRoot] 'Story' must be a component implementing IStory", this); return; }

            if (_customerInset == null) Debug.LogWarning("[GameRoot] No customer inset source: the customer picture will not show in minigames (LegacyMinigames ignores ShowCustomer)", this);
            _unlockPort = _unlocks != null ? new UnlocksAdapter(_unlocks) : null;
            var stage = new CustomerStage(_customers);
            var camera = new CameraDirector(_cameras, _customerInset, stage, _customerInsetOffset);
            _bar = new CocktailFeature(_cocktail, _shaker, _shakerPanels, _bottles, _method, _fillBars, _orderNote, _book, _bookButton, _unlockPort);
            var dayUi = new DayUiAdapter(this, _transition, _popups, _unlockPort, _chapterTitle);

            if (!_useUiScenes && _minigames == null) Debug.LogError("[GameRoot] 'Use Ui Scenes' is off and no legacy MinigameSystemManager is set", this);
            if (_garnishHost != null && _garnishHost is not IGarnishHost) Debug.LogError("[GameRoot] 'Garnish Host' does not implement IGarnishHost", this);
            Ports = new FlowPorts
            {
                Camera = camera, Bar = _bar, Orders = _bar, Story = story, DayUi = dayUi,
                Minigames = _useUiScenes ? null : new LegacyMinigames(_minigames),   // UI scenes: set by the preload in Start
                Garnish = _garnishHost as IGarnishHost,
            };

            State = new FlowState();
            // DayRunner needs the session and the session needs DayRunner.EnterTalkingMode: close the loop through a local.
            DayRunner day = null;
            Session = new BartendingSession(_steps, Ports, State, Lifetime, () => day.EnterTalkingMode());
            Day = day = new DayRunner(Ports, _schedule, State, Session, Lifetime);
            Commands = new FlowCommands(Day, Session, State);

            var snaps = new List<ISnapshotSource> { stage, camera };
            if (_unlockPort != null) snaps.Add(_unlockPort);
            Snapshots = snaps;

            _wiring = new MainWiring(State, _bar, _bar, _unlockPort, _customers, _hub);
            if (_useUiScenes) _uiScenes = new UiSceneBindings(_minigameWorlds, _customerInset, _garnishTable, stage);
        }

        void Start()
        {
            if (Current != this || Day == null) return;
            FlowAwait.Run(BootAsync, Lifetime, "GameRoot.Boot", this);
        }

        async Awaitable BootAsync(CancellationToken ct)
        {
            if (_uiScenes != null) await _uiScenes.PreloadAsync(Ports, ct);   // BeforeOpen: UI scenes loaded, bound and warm
            if (Ports.Garnish == null) Debug.LogError("[GameRoot] No IGarnishHost: a drink cannot get past the Garnish step", this);
            // ponytail: new game only; G4 resume (PendingLoad -> ResumeAsync) goes here.
            await Day.BootAsync(ct);
        }

        void OnDestroy()
        {
            if (Current != this) return;
            Current = null;
            _wiring?.Dispose();
            _uiScenes?.Dispose();
            _bar?.Dispose();
            _unlockPort?.Dispose();
        }
    }
}
