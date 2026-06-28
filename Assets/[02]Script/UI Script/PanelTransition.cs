using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Yarn.Unity;

public class PanelTransition : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private Coroutine CurrentControlCoroutine;

    [YarnCommand("Panel_FadeIn")]
    public IEnumerator FadeInPanel()
    {
        yield return null;
        animator.SetTrigger("FadeIn");
        yield return WaitForAnimation("Transition_FadeIn");
    }

    [YarnCommand("Panel_FadeOut")]
    public IEnumerator FadeOutPanel()
    {
        yield return null;
        animator.SetTrigger("FadeOut");
        yield return WaitForAnimation("Transition_FadeOut");
    }

    [YarnCommand("Fade_To_Summmary")]
    public IEnumerator FadeToSummary()
    {
        yield return null;
        animator.SetTrigger("FadeToSummary");
        yield return WaitForAnimation("Transition_ToSummary");
    }

    // Waits one frame for the transition to start, then waits until the
    // named state finishes playing on layer 0.
    private IEnumerator WaitForAnimation(string stateName)
    {
        // Wait for the Animator to transition INTO the target state
        yield return new WaitUntil(() =>
            animator.GetCurrentAnimatorStateInfo(0).IsName(stateName));

        // Wait until that state has finished playing (normalizedTime >= 1)
        yield return new WaitUntil(() =>
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            return info.IsName(stateName) && info.normalizedTime >= 1f;
        });
    }
}