using UnityEngine;

public class MinigameSystemManager : MonoBehaviour
{
    BaseMiniGame currentMinigame;
    ShakingMinigame shakingMinigame;

    [Header("Visual")]
    public Sprite MiniGamePanelSprite;

    [Header("Camera")]
    public CameraController cocktailCamera;

    public void Awake()
    {
        shakingMinigame = GetComponent<ShakingMinigame>();
    }

    public void Start()
    {
        currentMinigame = shakingMinigame;
   
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            currentMinigame.StartGame();
        
        }
        if (currentMinigame != null && currentMinigame.IsRunning)
        {
            currentMinigame.ProcessedGame();
        }
    }
}

