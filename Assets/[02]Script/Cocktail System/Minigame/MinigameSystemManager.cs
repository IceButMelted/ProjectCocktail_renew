// ============================================================
//  MinigameSystemManager — fixed
// ============================================================
using UnityEngine;

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
        //set minigame ShakingMinigame
        ShakingMinigame shaking = GetComponent<ShakingMinigame>();
        shaking.SetState(MiniGameState.Standby);
        shaking.Initialize(cocktailCamera);
        _shakingMinigame = shaking;
        
        //set minigame MixingMinigame
        MixingMinigame mixing = GetComponent<MixingMinigame>();
        mixing.SetState(MiniGameState.Standby);
        mixing.Initialize(cocktailCamera);
        _mixingMinigame = mixing;

        _currentMinigame = _shakingMinigame; // Start with no active minigame
    }

    
    private void Update()
    {
#if UNITY_EDITOR
        KeyCode? pressedKey = null;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            pressedKey = KeyCode.Alpha1;
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            pressedKey = KeyCode.Alpha2;
        else if (Input.GetKeyDown(KeyCode.R))
            pressedKey = KeyCode.R;
        else if (Input.GetKeyDown(KeyCode.V))
            pressedKey = KeyCode.V;
        else if (Input.GetKeyDown(KeyCode.B))
            pressedKey = KeyCode.B;

        if (pressedKey.HasValue)
        {
            switch (pressedKey.Value)
            {
                case KeyCode.Alpha1:
                    _currentMinigame?.EndGame();
                    _currentMinigame = _shakingMinigame;
                    break;

                case KeyCode.Alpha2:
                    _currentMinigame?.EndGame();
                    _currentMinigame = _mixingMinigame;
                    break;

                case KeyCode.R:
                case KeyCode.B:
                    _currentMinigame?.EndGame();
                    break;

                case KeyCode.V:
                    _currentMinigame?.StartGame();
                    break;
            }
        }
#endif


        if (_currentMinigame != null && _currentMinigame.IsRunning)
            _currentMinigame.ProcessedGame();
    }
}