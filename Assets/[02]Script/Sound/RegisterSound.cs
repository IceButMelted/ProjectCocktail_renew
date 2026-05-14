using UnityEngine;

public class RegisterSound : MonoBehaviour
{
    [SerializeField] SoundData soundData;

    void Awake()
    {
        ManagerSound.Init(soundData);
    }
}
