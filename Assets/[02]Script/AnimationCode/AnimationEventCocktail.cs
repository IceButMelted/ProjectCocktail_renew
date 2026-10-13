using UnityEngine;

public class AnimationEventCocktail : MonoBehaviour
{
    public void PlaySound(string IdSFX) {
        if (IdSFX != null || IdSFX == string.Empty)
            SoundManager.Instance?.PlaySFX(IdSFX);
    }

    
}
