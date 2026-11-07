// EditMode tests for the cocktail rules (GDD §15-18, CLAUDE.md) against the real recipe assets.
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
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
            // Multiset rule: order of entries must not matter, so sort by (category, type).
            var e = new List<(int cat, int type, int amount)>();
            foreach (var a in d.AlcoholList) e.Add((0, (int)a.Type, a.Amount));
            foreach (var l in d.LiqueurList) e.Add((1, (int)l.Type, l.Amount));
            foreach (var m in d.MixerList) e.Add((2, (int)m.Type, m.Amount));
            var sb = new StringBuilder();
            foreach (var x in e.OrderBy(t => t.cat).ThenBy(t => t.type))
                sb.Append(x.cat).Append(':').Append(x.type).Append(':').Append(x.amount).Append('|');
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

        static DrinkScoringService Service() => new DrinkScoringService(new CompositeDrinkRepository(
            AssetDatabase.FindAssets("t:SO_CocktailList").Select(g => AssetDatabase.LoadAssetAtPath<SO_CocktailList>(AssetDatabase.GUIDToAssetPath(g))).ToArray()));

        readonly List<S_Drink> _made = new List<S_Drink>();
        [TearDown] public void TearDown() { foreach (var d in _made) if (d != null) Object.DestroyImmediate(d); _made.Clear(); }

        S_Drink Make(string name, float price = 0f, params (BaseSpirit, int)[] spirits)
        {
            var d = ScriptableObject.CreateInstance<S_Drink>();
            d.name = name; d.Name = name; d.Price = price;
            foreach (var (t, n) in spirits) Assert.IsTrue(DrinkBuilder.TryAddAlcohol(d, t, n));
            _made.Add(d);
            return d;
        }

        [Test] public void ServingRecipeExactlyIsPerfect_ForEveryRecipe()
        {
            var svc = Service();
            var notPerfect = Recipes().Where(d => svc.Score(OrderFor(d), d) != Satisfaction.Perfect).Select(d => d.name);
            Assert.That(notPerfect, Is.Empty);
        }

        // Scores a deliberately imperfect drink (recipe copy with ice flipped -> GDD 18 case 2) and compares
        // against numbers computed here from the GDD, not from the code under test.
        [Test] public void ImperfectDrinkScoresAsGddExpects_AndRepeats()
        {
            var svc = Service();
            var recipe = Recipes().First();
            var poured = Make("poured");
            foreach (var a in recipe.AlcoholList) Assert.IsTrue(DrinkBuilder.TryAddAlcohol(poured, a.Type, a.Amount));
            foreach (var l in recipe.LiqueurList) Assert.IsTrue(DrinkBuilder.TryAddLiqueur(poured, l.Type, l.Amount));
            foreach (var m in recipe.MixerList) Assert.IsTrue(DrinkBuilder.TryAddMixer(poured, m.Type, m.Amount));
            poured.PreparationMethod = recipe.PreparationMethod;
            poured.AddIce = !recipe.AddIce;

            for (int run = 0; run < 2; run++)
            {
                var c = OrderFor(recipe);
                Assert.AreEqual(Satisfaction.Acceptable, svc.Score(c, poured));      // 18 case 2
                Assert.AreEqual(recipe.Price * 1.0f, c.Payout);                       // 18.1
                Assert.AreEqual(0.25f, c.RelationshipDelta);                          // 18.2
            }
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

        // ---------- pure rule tables ----------

        [TestCase(0, TypeOfCocktail.NoneAlcohol)]
        [TestCase(1, TypeOfCocktail.LowAlcohol)]
        [TestCase(5, TypeOfCocktail.LowAlcohol)]
        [TestCase(6, TypeOfCocktail.HighAlcohol)]
        public void AlcoholClassifier_FromUnits(int units, TypeOfCocktail expected) =>
            Assert.AreEqual(expected, AlcoholClassifier.FromUnits(units));

        [Test] public void Deviation_GddExample_IsTwo()
        {
            var recipe = Make("r", 0, (BaseSpirit.Gin, 7), (BaseSpirit.Vodka, 3));
            var poured = Make("p", 0, (BaseSpirit.Gin, 7), (BaseSpirit.Vodka, 2));
            Assert.IsTrue(DrinkBuilder.TryAddMixer(poured, Mixer.Syrup, 1));
            Assert.AreEqual(2, DrinkDeviation.Compute(poured, recipe));
        }

        [Test] public void Deviation3_IsSeemLike_Deviation4_IsFail()
        {
            var recipe = Make("r", 0, (BaseSpirit.Gin, 7), (BaseSpirit.Vodka, 3));
            var three = Make("p3", 0, (BaseSpirit.Gin, 4), (BaseSpirit.Vodka, 3));   // |7-4| = 3
            var four = Make("p4", 0, (BaseSpirit.Gin, 3), (BaseSpirit.Vodka, 3));    // |7-3| = 4
            var m3 = DrinkDeviation.MatchAgainst(three, recipe);
            var m4 = DrinkDeviation.MatchAgainst(four, recipe);
            Assert.AreEqual(3, m3.Deviation); Assert.AreEqual(DrinkFlag.Seem_Like, m3.Flag); Assert.IsFalse(m3.IsFailB);
            Assert.AreEqual(4, m4.Deviation); Assert.AreEqual(DrinkFlag.Fail, m4.Flag); Assert.IsTrue(m4.IsFailB);
        }

        [Test] public void Tie_ResolvesToFirstIndex()
        {
            var a = Make("a", 0, (BaseSpirit.Gin, 7), (BaseSpirit.Vodka, 3));
            var b = Make("b", 0, (BaseSpirit.Gin, 7), (BaseSpirit.Rum, 3));
            var poured = Make("p", 0, (BaseSpirit.Gin, 5));                           // dev 5 against both
            Assert.AreEqual(DrinkDeviation.Compute(poured, a), DrinkDeviation.Compute(poured, b));
            Assert.AreSame(a, DrinkDeviation.FindBestMatch(poured, new[] { a, b }).Recipe);
            Assert.AreSame(b, DrinkDeviation.FindBestMatch(poured, new[] { b, a }).Recipe);
        }

        static RecipeMatch M(S_Drink r, int dev, bool method = true, bool ice = true) =>
            new RecipeMatch(r, dev, method, ice, DrinkFlagResolver.Resolve(dev, method, ice, true));

        [Test] public void Satisfaction_GddSection18_FiveRows()
        {
            var r = Make("r");
            var T = TypeOfCocktail.LowAlcohol; var U = TypeOfCocktail.HighAlcohol;
            Assert.AreEqual(Satisfaction.Perfect, SatisfactionEvaluator.Evaluate(M(r, 0), T, T));                 // 1
            Assert.AreEqual(Satisfaction.Acceptable, SatisfactionEvaluator.Evaluate(M(r, 0, method: false), T, T)); // 2
            Assert.AreEqual(Satisfaction.Acceptable, SatisfactionEvaluator.Evaluate(M(r, 2), T, T));              // 3
            var four = M(r, 2);
            Assert.AreEqual(Satisfaction.Fail, SatisfactionEvaluator.Evaluate(four, T, U));                        // 4
            Assert.IsFalse(four.IsFailB);
            var five = M(r, 4);
            Assert.AreEqual(Satisfaction.Fail, SatisfactionEvaluator.Evaluate(five, T, T));                        // 5
            Assert.IsTrue(five.IsFailB);
        }

        [Test] public void Satisfaction_UnpinnedOrderType_FallsBackToAcceptable()
        {
            // GDD mismatch: GDD 18 has no row for "customer ordered no type"; code treats it as Acceptable
            // (case 3 without an expectation to violate). Asserting the CODE behaviour.
            Assert.AreEqual(Satisfaction.Acceptable,
                SatisfactionEvaluator.Evaluate(M(Make("r"), 2), TypeOfCocktail.LowAlcohol, TypeOfCocktail.None));
        }

        [Test] public void Pricing_GddSection18_1()
        {
            var r = Make("r", 80f);
            Assert.AreEqual(120f, PricingRules.Payout(M(r, 0), Satisfaction.Perfect));        // 1.5x
            Assert.AreEqual(80f, PricingRules.Payout(M(r, 0, ice: false), Satisfaction.Acceptable)); // 1.0x
            Assert.AreEqual(40f, PricingRules.Payout(M(r, 2), Satisfaction.Fail));            // Fail (a) 0.5x
            Assert.AreEqual(50f, PricingRules.Payout(M(r, 4), Satisfaction.Fail));            // Fail (b) flat
            Assert.AreEqual(50f, PricingRules.Payout(RecipeMatch.None, Satisfaction.Fail));   // nothing matched
        }

        [Test] public void RelationshipDelta_GddSection18_2()
        {
            Assert.AreEqual(0.5f, PricingRules.RelationshipDelta(Satisfaction.Perfect));
            Assert.AreEqual(0.25f, PricingRules.RelationshipDelta(Satisfaction.Acceptable));
            Assert.AreEqual(0f, PricingRules.RelationshipDelta(Satisfaction.Fail));
            Assert.AreEqual(0f, PricingRules.RelationshipDelta(Satisfaction.None));
        }
    }
}
