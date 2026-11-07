// EditMode tests for the cocktail rules (GDD §15-18, CLAUDE.md) against the real recipe assets.
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using static E_Cocktail;

namespace Bar410.Domain.Tests
{
    public class CocktailRuleTests
    {
        static List<S_Drink> Recipes() =>
            AssetDatabase.FindAssets("t:S_Drink")
                .Select(g => AssetDatabase.LoadAssetAtPath<S_Drink>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(d => d != null).ToList();

        static string Signature(S_Drink d)
        {
            var sb = new StringBuilder();
            foreach (var a in d.AlcoholList) sb.Append('A').Append((int)a.Type).Append(':').Append(a.Amount).Append('|');
            foreach (var l in d.LiqueurList) sb.Append('L').Append((int)l.Type).Append(':').Append(l.Amount).Append('|');
            foreach (var m in d.MixerList) sb.Append('M').Append((int)m.Type).Append(':').Append(m.Amount).Append('|');
            return sb.ToString();
        }

        static DrinkOrderContext OrderFor(S_Drink recipe)
        {
            var c = new DrinkOrderContext();
            c.BeginOrder(CharacterName.None, OrderMode.FixedByName, recipe, AlcoholClassifier.Resolve(recipe));
            return c;
        }

        [Test] public void RecipesExist() => Assert.That(Recipes().Count, Is.GreaterThan(0));

        [Test] public void EveryRecipeTotalsTenUnits() =>
            Assert.That(Recipes().Where(d => DrinkQuery.GetTotalIngredient(d) != 10).Select(d => d.name), Is.Empty);

        [Test] public void NoTwoRecipesShareIngredients() =>
            Assert.That(Recipes().GroupBy(Signature).Where(g => g.Count() > 1).Select(g => string.Join("/", g.Select(d => d.name))), Is.Empty);

        [Test] public void EveryRecipeHasName() =>
            Assert.That(Recipes().Where(d => string.IsNullOrEmpty(d.Name)).Select(d => d.name), Is.Empty);

        [Test] public void CocktailListsContainNoNulls()
        {
            foreach (var g in AssetDatabase.FindAssets("t:SO_CocktailList"))
            {
                var l = AssetDatabase.LoadAssetAtPath<SO_CocktailList>(AssetDatabase.GUIDToAssetPath(g));
                Assert.That(l.GetDrinks().Count(d => d == null), Is.Zero, l.name);
            }
        }

        [Test] public void ServingRecipeExactlyIsPerfect()
        {
            var recipes = Recipes();
            var svc = new DrinkScoringService(new CompositeDrinkRepository(
                AssetDatabase.FindAssets("t:SO_CocktailList").Select(g => AssetDatabase.LoadAssetAtPath<SO_CocktailList>(AssetDatabase.GUIDToAssetPath(g))).ToArray()));
            var d = recipes[0];
            Assert.AreEqual(Satisfaction.Perfect, svc.Score(OrderFor(d), d));
        }

        [Test] public void ScoringSameInputTwiceGivesSameResult()
        {
            var d = Recipes()[0];
            var svc = new DrinkScoringService(new CompositeDrinkRepository(
                AssetDatabase.FindAssets("t:SO_CocktailList").Select(g => AssetDatabase.LoadAssetAtPath<SO_CocktailList>(AssetDatabase.GUIDToAssetPath(g))).ToArray()));
            var a = OrderFor(d); var b = OrderFor(d);
            Assert.AreEqual(svc.Score(a, d), svc.Score(b, d));
            Assert.AreEqual(a.Payout, b.Payout);
            Assert.AreEqual(a.RelationshipDelta, b.RelationshipDelta);
        }

        [Test] public void ScoringWithoutOrderIsNone()
        {
            var svc = new DrinkScoringService(null);
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning, new System.Text.RegularExpressions.Regex("customer ordered"));
            Assert.AreEqual(Satisfaction.None, svc.Score(new DrinkOrderContext(), Recipes()[0]));
        }

        [Test] public void ClearResultKeepsOrderForRemake()
        {
            var d = Recipes()[0]; var c = OrderFor(d);
            c.Score(RecipeMatch.None, Satisfaction.Perfect, TypeOfCocktail.None, 1f, 1f);
            c.ClearResult();
            Assert.IsTrue(c.HasOrder); Assert.IsFalse(c.IsScored);
        }
    }
}
