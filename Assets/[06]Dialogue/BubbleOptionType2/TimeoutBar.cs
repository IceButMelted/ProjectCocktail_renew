/*
 * TimeoutBar.cs
 * Based on the YarnSpinner timeout sample:
 * https://docs.yarnspinner.dev/yarn-spinner-for-unity/samples/make-options-timeout
 *
 * Attach this to the timer bar container GameObject.
 * Drag the inner bar RectTransform into the Bar field in the Inspector.
 */

using System.Threading;
using UnityEngine;
using Yarn.Unity;

namespace YarnSpinner.Custom
{
    public class TimeoutBar : MonoBehaviour
    {
        [Tooltip("The RectTransform of the inner bar that shrinks horizontally.")]
        [SerializeField] private RectTransform bar;

        private float _originalWidth = 0f;

        private void Start()
        {
            if (bar != null)
                _originalWidth = bar.sizeDelta.x;
        }

        /// <summary>Resets the bar to its original full width instantly.</summary>
        public void ResetBar()
        {
            if (bar != null)
                bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _originalWidth);
        }

        /// <summary>
        /// Shrinks the bar from full width to zero over <paramref name="duration"/> seconds.
        /// Completes early if <paramref name="cancellationToken"/> is cancelled.
        /// </summary>
        public async YarnTask Shrink(float duration, CancellationToken cancellationToken)
        {
            if (bar == null) return;

            float accumulator = 0f;
            float startWidth  = bar.sizeDelta.x;

            while (accumulator < duration && !cancellationToken.IsCancellationRequested)
            {
                accumulator += Time.deltaTime;
                float newWidth = Mathf.Lerp(startWidth, 0f, accumulator / duration);
                bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
                await YarnTask.Yield();
            }

            // Ensure bar is fully gone regardless of how we exited
            bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
        }
    }
}
