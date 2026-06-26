using System.Collections;
using TMPro;
using UnityEngine;
using Yarn.Unity;
using YarnSpinner.Custom;

public class Post_It_Order : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI OrderText;
    [SerializeField] BubblePresenter presenter;

    [SerializeField] private Animator animator;
    [SerializeField] private float autoHideDuration = 3f;


    private bool _isPostItVisible = false;
    private bool _isOutScreen = true;
    private Coroutine _autoHideCoroutine;

    public bool IsPostItVisible => _isPostItVisible;
    public bool IsOutScreen => _isOutScreen;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if(presenter != null)
            presenter = FindAnyObjectByType<BubblePresenter>();
    }

    // ─── Core Visibility (Open / Close) ───────────────────────────────────────
    public void SetPostItOrderText() {
        //OrderText.text = presenter.CurrentLine.TextWithoutCharacterName.ToString();
    }

    public void SetPostItOrderText(string Order) { 
        OrderText.text = Order;
    }

    public void SetPostItVisible(bool visible)
    {
        _isPostItVisible = visible;
        animator.SetBool("IsVisible", visible);
        OnVisibleChange();
    }

    public void TogglePostIt()
    {
        CancelAutoHide();
        SetPostItVisible(!_isPostItVisible);
    }

    // ─── Out Screen ────────────────────────────────────────────────────────────

    public void SetOutScreen(bool outScreen)
    {
        _isOutScreen = outScreen;
        animator.SetBool("IsOutScreen", outScreen);
        OnOutScreenChange();
    }

    public void ToggleOutScreen()
    {
        SetOutScreen(!_isOutScreen);
    }

    // ─── Auto Hide ─────────────────────────────────────────────────────────────

    public void ShowPostItForDuration(float duration)
    {
        CancelAutoHide();
        SetOutScreen(false);
        SetPostItVisible(true);
        _autoHideCoroutine = StartCoroutine(AutoHideRoutine(duration));
    }

    public void ShowPostItForDefaultDuration() => ShowPostItForDuration(autoHideDuration);

    private IEnumerator AutoHideRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        SetPostItVisible(false);
        _autoHideCoroutine = null;
    }

    private void CancelAutoHide()
    {
        if (_autoHideCoroutine != null)
        {
            StopCoroutine(_autoHideCoroutine);
            _autoHideCoroutine = null;
        }
    }

    // ─── Callbacks ─────────────────────────────────────────────────────────────

    public void OnVisibleChange()
    {
        Debug.Log($"Post-it is now: {(_isPostItVisible ? "Open" : "Closed")}");
    }

    public void OnOutScreenChange()
    {
        Debug.Log($"Post-it is now: {(_isOutScreen ? "Out of Screen" : "On Screen")}");
    }

    [ContextMenu("Open Menu")]
    public void Init() { 
        SetOutScreen(false);
        SetPostItVisible(true);
    }

    [ContextMenu("Open Init Ani")]
    public void OpenInit() { 
        SetOutScreen(false);
        ShowPostItForDefaultDuration();
    }

    [ContextMenu("Out")]
    public void Out()
    {
        SetOutScreen(true);
        SetPostItVisible(false);
    }
}