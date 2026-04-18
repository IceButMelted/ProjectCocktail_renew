using UnityEngine;
using static E_Cocktail;

/// <summary>
/// Owns all minigame instances, drives the active one each frame,
/// and exposes public entry points for external systems (e.g. BeverageManager).
/// </summary>
public class MinigameSystemManager : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────

    [Header("Camera")]
    [SerializeField] private CameraController _cocktailCamera;

    // ── Private ────────────────────────────────────────────

    private ShakingMinigame _shakingMinigame;
    private MixingMinigame _mixingMinigame;
    private BaseMiniGame _activeMinigame;

    // ── Unity Lifecycle ────────────────────────────────────

    private void Awake()
    {
        _shakingMinigame = GetComponent<ShakingMinigame>();
        _shakingMinigame.Initialize(_cocktailCamera);

        _mixingMinigame = GetComponent<MixingMinigame>();
        _mixingMinigame.Initialize(_cocktailCamera);

        _activeMinigame = _shakingMinigame;
    }

    private void Update()
    {
#if UNITY_EDITOR
        HandleEditorHotkeys();
#endif
        if (_activeMinigame == null)
        {
            Debug.LogWarning("MinigameSystemManager: No active minigame assigned.");
            return;
        }

        _activeMinigame.ProcessedGame();
    }

    // ── Public API ─────────────────────────────────────────

    public void StartShakingMinigame()
    {
        SwitchTo(_shakingMinigame);
        _activeMinigame.StartGame();
    }

    public void StartMixingMinigame()
    {
        SwitchTo(_mixingMinigame);
        _activeMinigame.StartGame();
    }

    // ── Private Helpers ────────────────────────────────────

    private void SwitchTo(BaseMiniGame next)
    {
        _activeMinigame?.EndGame();
        _activeMinigame = next;
    }

#if UNITY_EDITOR
    private void HandleEditorHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchTo(_shakingMinigame);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchTo(_mixingMinigame);
        else if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.B)) _activeMinigame?.EndGame();
        else if (Input.GetKeyDown(KeyCode.V)) _activeMinigame?.StartGame();
    }
#endif
}