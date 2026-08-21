// ============================================================
//  SO_Customer.cs / SO_CustomerRoster.cs — GDD §19.1.
//
//  One asset per character instead of one dictionary on a scene
//  MonoBehaviour (plan S11): editable without opening a scene, and
//  readable in a git diff.
//
//  relationshipValue is deliberately ABSENT. GDD §19.1 lists it as a
//  mirror of Yarn's $rel_<id>, but §22 makes that Yarn variable the
//  single source of truth, and a ScriptableObject written at runtime
//  keeps its value between Play sessions in the Editor. Plan decision
//  D8 resolves the conflict in favour of §22 — read the value from
//  DialogueRunner.VariableStorage, never from here.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

[CreateAssetMenu(fileName = "Customer_New", menuName = "Bar410/Customer")]
public class SO_Customer : ScriptableObject
{
    [Tooltip("Must match the NPC_Name used by dialogue and NPC_Base.")]
    public NPC_Name Id = NPC_Name.None;

    public string CustomerName;

    [Tooltip("Drink TYPES this customer likes — not specific recipes (GDD §7).")]
    public List<TypeOfCocktail> PreferredDrinkTypes = new List<TypeOfCocktail>();
}


