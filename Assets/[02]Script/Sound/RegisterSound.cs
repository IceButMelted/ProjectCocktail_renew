using UnityEngine;

public class RegisterSound : MonoBehaviour
{
    [SerializeField] SoundData soundData;
    [SerializeField] public string BGMStart;

    void Awake()
    {
        ManagerSound.Init(soundData);
    }

    private void Start()
    {
        ManagerSound.PlayBGM(BGMStart);
    }
}

