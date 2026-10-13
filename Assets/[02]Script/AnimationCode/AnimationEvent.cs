using UnityEngine;

public class AnimationEvent : MonoBehaviour
{
    public void PlaySound(string IdSFX) {
        if (IdSFX != null || IdSFX == string.Empty)
            SoundManager.Instance?.PlaySFX(IdSFX);
    }

    
}
