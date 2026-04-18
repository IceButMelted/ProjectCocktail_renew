// ============================================================
//  MinigameSystemManager — fixed
// ============================================================
using UnityEngine;
using static E_Cocktail;

public class MinigameSystemManager : MonoBehaviour
{
    private BaseMiniGame _currentMinigame;
    private ShakingMinigame _shakingMinigame;
    private MixingMinigame _mixingMinigame;

    [Header("Visual")]
    public Sprite MiniGamePanelSprite;

    [Header("Camera")]
    public CameraController cocktailCamera;

    private void Awake()
    {
        ShakingMinigame shaking = GetComponent<ShakingMinigame>();
        shaking.Initialize(cocktailCamera);
        _shakingMinigame = shaking;

        MixingMinigame mixing = GetComponent<MixingMinigame>();
        mixing.Initialize(cocktailCamera);
        _mixingMinigame = mixing;

        _currentMinigame = _shakingMinigame;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _currentMinigame?.EndGame();
            _currentMinigame = _shakingMinigame;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _currentMinigame?.EndGame();
            _currentMinigame = _mixingMinigame;
        }
        else if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.B))
        {
            _currentMinigame?.EndGame();
        }
        else if (Input.GetKeyDown(KeyCode.V))
        {
            _currentMinigame?.StartGame();
        }
#endif

        if (_currentMinigame == null)
        {
            Debug.LogWarning("No minigame assigned.");
            return;
        }

        _currentMinigame.ProcessedGame();
    }

    public void StartShakingMinigame()
    {
        _currentMinigame = _shakingMinigame;
        _currentMinigame.StartGame();
    }

    public void StartMixingMinigame()
    {
        _currentMinigame = _mixingMinigame;
        _currentMinigame.StartGame();
    }
}