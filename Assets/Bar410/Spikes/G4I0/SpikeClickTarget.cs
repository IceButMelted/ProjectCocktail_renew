// Throwaway (G4-I0 spike). Reports a click and the device that sent it.
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Bar410.Spikes
{
    public sealed class SpikeClickTarget : MonoBehaviour, IPointerClickHandler
    {
        public Action<SpikeClickTarget, string> Clicked;

        public void OnPointerClick(PointerEventData e)
        {
            string device = (e as ExtendedPointerEventData)?.device?.name ?? "?";
            Clicked?.Invoke(this, device);
        }
    }
}
