using UnityEngine;

// Test buttons for the lab scene. Not part of the cocktail system itself.
public class CocktailDemo : MonoBehaviour
{
    public CocktailGlass glass;
    public PourController pour;
    public CocktailMenu[] menus;

    int menuIndex;

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 220, 200), GUI.skin.box);
        GUILayout.Label("Menu: " + (menus.Length > 0 ? menus[menuIndex].name : "-"));
        if (GUILayout.Button("Next Menu")) menuIndex = (menuIndex + 1) % menus.Length;

        // Ice always goes in before the liquid
        GUI.enabled = glass.Level01 <= 0f && !pour.IsPouring && !glass.IsDroppingIce;
        if (GUILayout.Button("Add Ice")) glass.DropIce();

        GUI.enabled = glass.Level01 < 1f && !pour.IsPouring && !glass.IsDroppingIce;
        if (GUILayout.Button("Pour")) pour.Pour(glass, menus[menuIndex]);

        GUI.enabled = !pour.IsPouring;
        if (GUILayout.Button("Reset")) glass.ResetGlass();

        GUI.enabled = true;
        GUILayout.EndArea();
    }
}
