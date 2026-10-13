using UnityEngine;

[CreateAssetMenu(menuName = "Cocktail/Liquid Settings")]
public class LiquidSettings : ScriptableObject
{
    [Tooltip("Global liquid alpha. Menu colours never affect alpha.")]
    [Range(0f, 1f)] public float liquidOpacity = 0.85f;
}
