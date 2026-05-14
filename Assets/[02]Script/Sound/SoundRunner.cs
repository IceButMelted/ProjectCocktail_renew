using UnityEngine;
using System.Collections;

public class SoundRunner : MonoBehaviour
{
    public void Fade(AudioSource src, AudioClip clip, float targetVol, float duration)
        => StartCoroutine(DoFade(src, clip, targetVol, duration));

    public void FadeOut(AudioSource src, float duration)
        => StartCoroutine(DoFadeOut(src, duration));

    IEnumerator DoFade(AudioSource src, AudioClip clip, float targetVol, float duration)
    {
        // fade out เพลงเก่า
        if (src.isPlaying)
        {
            float start = src.volume;
            for (float t = 0; t < duration / 2; t += Time.deltaTime)
            {
                src.volume = Mathf.Lerp(start, 0, t / (duration / 2));
                yield return null;
            }
            src.Stop();
        }

        // fade in เพลงใหม่
        src.clip = clip;
        src.volume = 0f;
        src.Play();
        for (float t = 0; t < duration / 2; t += Time.deltaTime)
        {
            src.volume = Mathf.Lerp(0, targetVol, t / (duration / 2));
            yield return null;
        }
        src.volume = targetVol;
    }

    IEnumerator DoFadeOut(AudioSource src, float duration)
    {
        float start = src.volume;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            src.volume = Mathf.Lerp(start, 0, t / duration);
            yield return null;
        }
        src.Stop();
    }
}