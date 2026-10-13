// ============================================================
//  MethodPanel.cs — The Shake / Stir / Reset panel beside the shaker.
//
//  Wires its own three buttons and says which one was pressed. What a
//  press means (set the method, advance the flow, empty the shaker) is
//  decided by whoever listens; the panel knows nothing about the flow.
//  Before, the flow bridge held all three buttons and the panel object.
// ============================================================

using System;
using UnityEngine;
using UnityEngine.UI;

public class MethodPanel : MonoBehaviour
{
    [SerializeField] private Button _btnShaking;
    [SerializeField] private Button _btnMixing;
    [SerializeField] private Button _btnReset;

    public event Action ShakingChosen;
    public event Action MixingChosen;
    public event Action ResetChosen;

    // The panel starts inactive, so OnEnable is the first moment its buttons can be pressed.
    private void OnEnable()
    {
        if (_btnShaking != null) _btnShaking.onClick.AddListener(OnShaking);
        if (_btnMixing != null) _btnMixing.onClick.AddListener(OnMixing);
        if (_btnReset != null) _btnReset.onClick.AddListener(OnReset);
    }

    private void OnDisable()
    {
        if (_btnShaking != null) _btnShaking.onClick.RemoveListener(OnShaking);
        if (_btnMixing != null) _btnMixing.onClick.RemoveListener(OnMixing);
        if (_btnReset != null) _btnReset.onClick.RemoveListener(OnReset);
    }

    /// <summary>Closes the panel. It reopens through ShakerPanelController.ShowMethod.</summary>
    public void Hide() => gameObject.SetActive(false);

    private void OnShaking() => ShakingChosen?.Invoke();
    private void OnMixing() => MixingChosen?.Invoke();
    private void OnReset() => ResetChosen?.Invoke();
}
