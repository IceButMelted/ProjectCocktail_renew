using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static E_Cocktail;

[System.Serializable]
public class S_Drink
{
    public string Name;
    public E_Cocktail.TypeOfCocktail AlcoholStrength;
    public E_Cocktail.Method PreparationMethod;
    public bool AddIce;
    public float Price;

    [Header("Ingrdients")]
    [SerializedDictionary("Alcohol","Amount")]
    public SerializedDictionary<Alcohol, int> AlcoholList;
    [SerializedDictionary("Mixer", "Amount")]
    public SerializedDictionary<Mixer, int> MixerList;
    [SerializeField]
    public List<GlassType> CompatibleGlasses = new List<GlassType>();

    private const int MAX_TOTAL_PARTS = 10;

    public bool IsValidRatio()
    {
        int total = AlcoholList.Values.Sum() + MixerList.Values.Sum();
        Debug.Log(total <= MAX_TOTAL_PARTS);
        return total < MAX_TOTAL_PARTS; 
    }

    /// <summary>
    /// Compare this Drink to Other Drink
    /// </summary>
    /// <param name="other">Other Drink than get to Compare</param>
    /// <param name="technicalOnly">true = Not count Price and GlassType</param>
    public bool Check(S_Drink other, bool technicalOnly = true)
    {
        if (other == null) return false;

        // เช็ค property หลัก
        bool technicalMatch =
            AddIce == other.AddIce &&
            PreparationMethod == other.PreparationMethod &&
            IngredientsMatch(other);

        if (technicalOnly) return technicalMatch;

        // เช็คครบทุกอย่าง รวม Price และ GlassType
        bool priceMatch = Mathf.Approximately(Price, other.Price);
        bool glassMatch = CompatibleGlasses.OrderBy(g => g)
                                           .SequenceEqual(other.CompatibleGlasses.OrderBy(g => g));

        return technicalMatch && priceMatch && glassMatch;
    }

    /// <summary>
    /// Calculate Satisfaction with recipe BP
    /// </summary>
    /// <param name="recipe">Recipe than customer want</param>
    public Satisfaction CalculateSatisfaction(S_Drink recipe)
    {
        int ingredientErrors = CountIngredientErrors(recipe);

        // Perfect: ทุกอย่างถูกต้อง (ไม่นับ Price/Glass)
        if (AddIce == recipe.AddIce &&
            PreparationMethod == recipe.PreparationMethod &&
            ingredientErrors == 0)
        {
            return Satisfaction.Perfect;
        }

        // Acceptable: น้ำแข็งผิด หรือ วิธีชงผิด หรือ ส่วนผสมผิดเพียง 1 อย่าง
        bool minorError = (AddIce != recipe.AddIce) ||
                          (PreparationMethod != recipe.PreparationMethod) ||
                          (ingredientErrors == 1);

        if (minorError && ingredientErrors <= 1)
            return Satisfaction.Acceptable;

        // Fail: ส่วนผสมผิดมากกว่า 1 อย่าง
        return Satisfaction.Fail;
    }

    /// <summary>
    /// Overload 1: กำหนด TypeOfAlcohol จาก recipeList ภายนอก
    /// (ใช้ตอน Load recipe จาก ScriptableObject)
    /// </summary>
    public TypeOfCocktail GetTypeOfAlcohol(List<S_Drink> standardRecipes)
    {
        var match = standardRecipes?.FirstOrDefault(r => r.Name == Name);
        return match != null ? match.AlcoholStrength : GetTypeOfAlcohol();
    }

    /// <summary>
    /// Overload 2: คำนวณ TypeOfAlcohol จากปริมาณ Alcohol ที่มีอยู่จริง
    /// </summary>
    public TypeOfCocktail GetTypeOfAlcohol()
    {
        int totalAlcohol = AlcoholList.Values.Sum();

        if (totalAlcohol >= 5) return TypeOfCocktail.HighAlcohol;
        if (totalAlcohol > 0) return TypeOfCocktail.LowAlcohol;
        return TypeOfCocktail.None;
    }

    /// <summary>
    /// อัปเดต AlcoholStrength field โดยใช้ Overload 2
    /// </summary>
    public void UpdateTypeOfAlcohol() => AlcoholStrength = GetTypeOfAlcohol();

    /// <summary>
    /// Get Total Alcohol
    /// </summary>
    /// <returns>Number of Alcohol Ratio</returns>
    public int GetTotalAlcohol() { 
        return AlcoholList.Values.Sum();
    }
    /// <summary>
    /// Get Total Mixer
    /// </summary>
    /// <returns> Number of Mixer Ratio</returns>
    public int GetTotalMixer() { 
        return MixerList.Values.Sum();
    }
    /// <summary>
    /// Get Total of Ingredient
    /// </summary>
    /// <returns>Total of Ingredient</returns>
    public int GetTotalIngredient() { 
        return AlcoholList.Values.Sum() + MixerList.Values.Sum();
    }

    /// <summary>
    /// Try to Add Alcohol to S_Drink
    /// </summary>
    /// <param name="alcohol">Alcohol that want to Add</param>
    /// <param name="amount">Amount of Shot that want to add</param>
    public void TryToAddAlcohol(Alcohol alcohol, int amount)
    {

        if (!IsValidRatio())
            return;

        Debug.Log($"Add {alcohol} for {amount} shot");

        if (AlcoholList.ContainsKey(alcohol))
        {
            AlcoholList[alcohol] += amount;
        }
        else
        {
            AlcoholList.Add(alcohol, amount);
        }

    }

    /// <summary>
    /// Try to Add Mixer to S_Drink
    /// </summary>
    /// <param name="mixer">Mixer that want to Add</param>
    /// <param name="amount">Amount of Shot that want to add</param>
    public void TryToAddMixer(Mixer mixer, int amount)
    {
        if (!IsValidRatio())
            return;

        Debug.Log($"Add {mixer} for {amount} shot");

        if (MixerList.ContainsKey(mixer))
        {
            MixerList[mixer] += amount;
        }
        else
        {
            MixerList.Add(mixer, amount);
        }

    }

    /// <summary>
    /// ตรวจว่า Dictionary ของส่วนผสมตรงกันทุก key/value หรือไม่
    /// </summary>
    private bool IngredientsMatch(S_Drink other)
    {
        return DictEquals(AlcoholList, other.AlcoholList) &&
               DictEquals(MixerList, other.MixerList);
    }

    /// <summary>
    /// นับจำนวนส่วนผสมที่ปริมาณไม่ตรงกับ recipe
    /// (รวมทั้ง Alcohol และ Mixer)
    /// </summary>
    private int CountIngredientErrors(S_Drink recipe)
    {
        int errors = 0;
        errors += CountDictErrors(AlcoholList, recipe.AlcoholList);
        errors += CountDictErrors(MixerList, recipe.MixerList);
        return errors;
    }

    private static bool DictEquals<TKey, TValue>(
        Dictionary<TKey, TValue> a, Dictionary<TKey, TValue> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var kv in a)
        {
            if (!b.TryGetValue(kv.Key, out var val)) return false;
            if (!EqualityComparer<TValue>.Default.Equals(kv.Value, val)) return false;
        }
        return true;
    }

    /// <summary>
    /// Cont Key different or have in other dict but not have in anoter
    /// </summary>
    private static int CountDictErrors<TKey>(
        Dictionary<TKey, int> player, Dictionary<TKey, int> recipe)
    {
        int errors = 0;
        var allKeys = new HashSet<TKey>(player.Keys);
        allKeys.UnionWith(recipe.Keys);

        foreach (var key in allKeys)
        {
            player.TryGetValue(key, out int pVal);
            recipe.TryGetValue(key, out int rVal);
            if (pVal != rVal) errors++;
        }
        return errors;
    }

}
