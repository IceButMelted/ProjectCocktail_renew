#if UNITY_EDITOR
using TMPro;
using UnityEngine;

/// <summary>
/// Debug overlay — editor / development builds only.
/// Displays target vs. current cocktail info and satisfaction.
/// </summary>
public class DebugCocktail : MonoBehaviour
{
    [SerializeField] private CocktailShaker        _shaker;
    [SerializeField] private CocktailSystemManager _system;

    [Header("UI Text")]
    [SerializeField] private TextMeshProUGUI _targetText;
    [SerializeField] private TextMeshProUGUI _currentText;
    [SerializeField] private TextMeshProUGUI _satisfactionText;

    private void Update()
    {
        _targetText.text  = "Target: " + _system.GetTargetName();
        _currentText.text = "Shaker:\n" + _shaker.CurrentCocktail;
    }

    public void UpdateSatisfaction()
        => _satisfactionText.text = "Satisfaction: " + _system.CalculateSatisfaction();
}
#endif
