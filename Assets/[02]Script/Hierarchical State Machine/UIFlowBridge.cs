using UnityEngine;
using UnityEngine.UI;

namespace Bar410.GameFlow
{
    // ── Global chrome ──────────────────────────────────────

    /// <summary>
    /// Buttons that aren't tied to any one flow state entering/exiting — the recipe book
    /// (open/next/previous/close), the order post-it toggle, and the two "end shift" /
    /// "thanks for playing" scene-load buttons. Everything here is wired in Awake instead
    /// of as Inspector persistent listeners so it can't be silently lost or duplicated
    /// across a prefab merge (see GarnishFlowBridge/CocktailFlowBridge for the same rule
    /// applied to state-specific buttons).
    /// </summary>
    public class UIFlowBridge : MonoBehaviour
    {
        [Header("Book")]
        [SerializeField] private BookUI_V2 _bookUI;
        [SerializeField] private Button _btnOpenBook;
        [SerializeField] private Button _btnBookNext;
        [SerializeField] private Button _btnBookPrevious;
        [SerializeField] private Button _btnBookClose;

        [Header("Order Post-it")]
        [SerializeField] private Post_It_Order _postIt;
        [SerializeField] private Button _btnTogglePostIt;

        [Header("Scene Loading")]
        [SerializeField] private Button _btnEndShift;
        [SerializeField] private SceneLoader _endShiftLoader;
        [SerializeField] private string _endShiftScene = "MainMenu";

        [SerializeField] private Button _btnThanks;
        [SerializeField] private SceneLoader _thanksLoader;
        [SerializeField] private string _thanksScene = "MainMenu";

        private void Awake()
        {
            if (_btnOpenBook != null) _btnOpenBook.onClick.AddListener(OnOpenBookClicked);
            if (_btnBookNext != null) _btnBookNext.onClick.AddListener(OnBookNextClicked);
            if (_btnBookPrevious != null) _btnBookPrevious.onClick.AddListener(OnBookPreviousClicked);
            if (_btnBookClose != null) _btnBookClose.onClick.AddListener(OnBookCloseClicked);

            if (_btnTogglePostIt != null) _btnTogglePostIt.onClick.AddListener(OnTogglePostItClicked);

            if (_btnEndShift != null) _btnEndShift.onClick.AddListener(OnEndShiftClicked);
            if (_btnThanks != null) _btnThanks.onClick.AddListener(OnThanksClicked);
        }

        private void OnDestroy()
        {
            if (_btnOpenBook != null) _btnOpenBook.onClick.RemoveListener(OnOpenBookClicked);
            if (_btnBookNext != null) _btnBookNext.onClick.RemoveListener(OnBookNextClicked);
            if (_btnBookPrevious != null) _btnBookPrevious.onClick.RemoveListener(OnBookPreviousClicked);
            if (_btnBookClose != null) _btnBookClose.onClick.RemoveListener(OnBookCloseClicked);

            if (_btnTogglePostIt != null) _btnTogglePostIt.onClick.RemoveListener(OnTogglePostItClicked);

            if (_btnEndShift != null) _btnEndShift.onClick.RemoveListener(OnEndShiftClicked);
            if (_btnThanks != null) _btnThanks.onClick.RemoveListener(OnThanksClicked);
        }

        private void OnOpenBookClicked() => _bookUI?.SetActive(true);
        private void OnBookNextClicked() => _bookUI?.NextSpread();
        private void OnBookPreviousClicked() => _bookUI?.PreviousSpread();
        private void OnBookCloseClicked() => _bookUI?.Toggle();

        private void OnTogglePostItClicked() => _postIt?.TogglePostIt();

        private void OnEndShiftClicked() => _endShiftLoader?.LoadScene(_endShiftScene);
        private void OnThanksClicked() => _thanksLoader?.LoadScene(_thanksScene);
    }
}
