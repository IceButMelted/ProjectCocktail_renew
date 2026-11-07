using UnityEngine;

/// <summary>
/// Standard mouse / touch implementation. IInputProvider now lives in Bar410.Contracts;
/// this stays in Assembly-CSharp until G4-I9.
/// </summary>
public class UnityInputProvider : IInputProvider
{
    public bool IsClickedThisFrame { get; private set; }

    /// <summary>Reads left-mouse or first touch. Safe to call every frame.</summary>
    public void Poll()
        => IsClickedThisFrame = Input.GetMouseButtonDown(0);
}
