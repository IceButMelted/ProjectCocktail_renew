// ============================================================
//  ShakingMinigame — with target zone handle visual
// ============================================================
using UnityEngine;
using UnityEngine.UI;

using static E_Cocktail;

public class ShakingMinigame : BaseMiniGame
{
    private SO_ShakingSetting _cfg => Setting as SO_ShakingSetting;

    [Header("UI")]
    [SerializeField] private Slider _gaugeSlider;
    [SerializeField] private Slider _ProgressSlider;
    [SerializeField] private Slider _targetZoneSlider;   // handle visually shows the zone

    // Internal state
    private float _targetZoneCurrentSize;
    private float _targetZoneCenter;
    private float _targetZoneBorderMin;
    private float _targetZoneBorderMax;

    //slide panel
    private bool _isPanelSlidingIn = false;
    private bool _isPanelSlidingOut = false;

    // Cache the handle's original height so we only change width
    private float _handleOriginalHeight;
    private float _handleOriginalWidth;

    public float GaugeValue { get; private set; }
    public float TimeInZone { get; private set; }

    // ─────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────

    public override void StartGame()
    {
        // Cache handle width once before anything resizes it
        _handleOriginalWidth = _targetZoneSlider.handleRect.sizeDelta.x;

        InitTargetZone();
        OnProcessing();
        base.StartGame();
        Debug.Log("Shaking Minigame Started");
    }

    protected override void OnProcessing() {
        //_minigamePanel.anchoredPosition = new Vector2(-Screen.width, 0f);
    }

    public override void ProcessedGame()
    {
        
        if (_isPanelSlidingIn)
            if (!SlideMinigame(Direction.Right, Vector2.zero)) return;
            else _isPanelSlidingIn = false;
        if (_isPanelSlidingOut)
        {
            Vector2 _OutPoint = new Vector2(-(_minigamePanel.transform as RectTransform).rect.width, 0);
            if (!SlideMinigame(Direction.Left, _OutPoint)) return;
            else
            {
                _isPanelSlidingOut = false;
                SetState(MiniGameState.Success);
            }
        }

        if (!IsRunning) return;

        base.ProcessedGame();

        // 1. Click → increase gauge
        if (IsClickedThisFrame)
        {
            GaugeValue += _cfg.GaugeIncreasePerClick * _cfg.DifficultyMultiplier;
            GaugeValue = Mathf.Clamp01(GaugeValue);
        }

        // 2. Decay every frame
        GaugeValue -= _cfg.GaugeDecayRate * Time.deltaTime;
        GaugeValue = Mathf.Clamp01(GaugeValue);

        // 3. Zone bounds (clamp for safety)
        float zoneStart = Mathf.Max(_targetZoneBorderMin, 0f);
        float zoneEnd = Mathf.Min(_targetZoneBorderMax, 1f);

        // 4. Zone accumulation — progress increases by rate * dt
        bool inZone = GaugeValue >= zoneStart && GaugeValue <= zoneEnd;
        if (inZone)
        {
            float oldProgress = TimeInZone;
            TimeInZone += _cfg.ProgressBar_IncreaseRate * Time.deltaTime;

            float progressDelta = TimeInZone - oldProgress;
            DecreaseTargetZone(progressDelta);
        }

        Debug.Log($"In Zone: {inZone} | Gauge={GaugeValue:F2} | Zone=[{zoneStart:F2},{zoneEnd:F2}] | Progress={TimeInZone:F2}");

        // 5. Win condition
        if (TimeInZone >= _cfg.Duration)
        {
            _isPanelSlidingOut = true;
        }

        UpdateUI();
    }

    public override void EndGame()
    {
        base.EndGame();
        Debug.Log($"Shaking Ended | Gauge={GaugeValue:F2} | Progress={TimeInZone:F2} | {CurrentState}");
    }

    public override void UpdateUI()
    {
        base.UpdateUI();

        _gaugeSlider.value = GaugeValue;
        _ProgressSlider.value = TimeInZone / _cfg.Duration;

        UpdateTargetZoneVisual();
        UpdateProgressBarVisaul();
    }

    public override string GetGameState()
        => $"Shaking | Gauge: {GaugeValue:P0} | Progress: {TimeInZone:F1}s | {CurrentState}";

    protected override void ResetGame()
    {
        base.ResetGame();
        GaugeValue = 0f;
        TimeInZone = 0f;
        _isPanelSlidingIn = false;
        _isPanelSlidingOut = false;
        Debug.Log("Shaking Minigame Reset");
    }

    private void InitTargetZone()
    {
        _targetZoneCurrentSize = _cfg.TargetZoneMaxSize;

        float halfSize = _targetZoneCurrentSize / 2f;
        float minCenter = Mathf.Max(_cfg.InitTargetZone_MinValue, halfSize);
        float maxCenter = 1f - halfSize;

        _targetZoneCenter = Random.Range(minCenter, maxCenter);
        _targetZoneBorderMin = _targetZoneCenter - halfSize;
        _targetZoneBorderMax = _targetZoneCenter + halfSize;

        ResetGame();

        _isPanelSlidingIn = true;
        _isPanelSlidingOut = false;

        //update Visuals
        UpdateTargetZoneVisual();
        UpdateProgressBarVisaul();
    }

    private void DecreaseTargetZone(float progressDelta)
    {
        if (_targetZoneCurrentSize <= _cfg.TargetZoneMinSize) return;

        float shrink = _cfg.TargetZone_DecreasePerProgress * progressDelta;
        _targetZoneCurrentSize -= shrink;
        _targetZoneCurrentSize = Mathf.Max(_targetZoneCurrentSize, _cfg.TargetZoneMinSize);

        float halfSize = _targetZoneCurrentSize / 2f;
        _targetZoneBorderMin = _targetZoneCenter - halfSize;
        _targetZoneBorderMax = _targetZoneCenter + halfSize;
    }

    private void UpdateTargetZoneVisual()
    {
        // Move slider thumb to zone center
        _targetZoneSlider.value = _targetZoneCenter;

        // Get the pixel width of the full slider track
        float trackHeight = (_targetZoneSlider.transform as RectTransform).rect.height;

        // Scale handle width to match zone size in pixels
        float handleHeight = _targetZoneCurrentSize * trackHeight;

        RectTransform handle = _targetZoneSlider.handleRect;
        handle.sizeDelta = new Vector2(_handleOriginalWidth, handleHeight);

    }

    private void UpdateProgressBarVisaul() { 
        Image fillImage = _ProgressSlider.fillRect.GetComponent<Image>();

        // Change color based on progress (green to red)
        float t = TimeInZone / _cfg.Duration;
        float smoothT = Mathf.SmoothStep(0f, 1f, t); // for smoother color transition

        fillImage.color = Oklab.OklabLerp(_cfg.ProgressBar_StartColor, _cfg.ProgressBar_EndColor, smoothT);

    }

}