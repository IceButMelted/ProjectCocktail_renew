using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Yarn.Unity;

public class PanelTransition : MonoBehaviour
{
   
    [SerializeField] private Animator animator;
    [SerializeField] private float FadeInSpeed = 1f;
    [SerializeField] private float FadeOutSpeed = 1f;

    private bool isPanelShouldVisible = false;

    private Coroutine CurrentControlCoroutine;

    [YarnCommand("Panel_FadeIn")]
    public IEnumerator FadeInPanel()
    {
        yield return null;
        animator.SetTrigger("FadeIn");
        yield return new WaitForSeconds(FadeInSpeed);
    }

    [YarnCommand("Panel_FadeOut")]
    public IEnumerator FadeOutPanel()
    {
        yield return null;
        animator.SetTrigger("FadeOut");
        yield return new WaitForSeconds(FadeOutSpeed);
    }

    [YarnCommand("Fade_To_Summmary")]
    public IEnumerator FadeToSummary()
    {
        yield return null;
        animator.SetTrigger("FadeToSummary");
        yield return new WaitForSeconds(FadeOutSpeed);
    }
}