// ============================================================
//  MinigameSystemManager — updated
//  Added GetShakingMinigame() / GetMixingMinigame() so
//  GameLoopManager can subscribe to OnGameEnd before starting.
// ============================================================

using UnityEngine;
using static E_Cocktail;

public class MinigameSystemManager : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private CameraController _cocktailCamera;
    [SerializeField] private CocktailSystemManager cocktailSystemManager;

    private ShakingMinigame _shakingMinigame;
    private MixingMinigame _mixingMinigame;
    private BaseMiniGame _activeMinigame;
    

    private void Awake()
    {
        _shakingMinigame = GetComponent<ShakingMinigame>();
        _shakingMinigame.Initialize(_cocktailCamera, cocktailSystemManager);

        _mixingMinigame = GetComponent<MixingMinigame>();
        _mixingMinigame.Initialize(_cocktailCamera, cocktailSystemManager);

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

    // Accessors for GameLoopManager to subscribe OnGameEnd before starting
    public ShakingMinigame GetShakingMinigame() => _shakingMinigame;
    public MixingMinigame GetMixingMinigame() => _mixingMinigame;

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