using System.Collections.Generic;
using Bar410.Contracts;
using NUnit.Framework;
using static E_Cocktail;

namespace Bar410.Flow.Tests
{
    public class FlowStateTests
    {
        // Oracle: old GameFlowCommands.Yarn_CurrentPhase/Step, PLAYTEST_Demo.yarn, TEST_AllCommands.yarn.
        // (part, step) -> phase, step, hub. Renaming any enum member or string fails here.
        static readonly object[] Table =
        {
            new object[] { DayPart.None,  BarStep.Idle,          "",           "",                    HubPhase.None },
            new object[] { DayPart.Event, BarStep.Idle,          "BeforeOpen", "Event",               HubPhase.BeforeOpen },
            new object[] { DayPart.Story, BarStep.Idle,          "BeforeOpen", "StoryBeforeOpen",     HubPhase.BeforeOpen },
            new object[] { DayPart.Open,  BarStep.Idle,          "Open",       "TalkingWithCustomer", HubPhase.Talking },
            new object[] { DayPart.Open,  BarStep.AddIngredient, "Open",       "AddIngredient",       HubPhase.AddIngredient },
            new object[] { DayPart.Open,  BarStep.Minigame,      "Open",       "Minigame",            HubPhase.Minigame },
            new object[] { DayPart.Open,  BarStep.Garnish,       "Open",       "Garnish",             HubPhase.Garnish },
            new object[] { DayPart.Open,  BarStep.Serve,         "Open",       "Serve",               HubPhase.Serve },
            new object[] { DayPart.Close, BarStep.Idle,          "Close",      "",                    HubPhase.Close },
            new object[] { DayPart.Close, BarStep.Serve,         "Close",      "",                    HubPhase.Close },
        };

        [TestCaseSource(nameof(Table))]
        public void Table_Row(DayPart part, BarStep step, string phase, string stepStr, HubPhase hub)
        {
            var s = new FlowState();
            s.SetPart(part); s.SetStep(step);
            Assert.AreEqual(phase, s.PhaseString);
            Assert.AreEqual(stepStr, s.StepString);
            Assert.AreEqual(hub, s.HubPhase);
        }

        [Test]
        public void EnumNames_AreLocked()
        {
            CollectionAssert.AreEqual(new[] { "None", "Event", "Story", "Open", "Close" }, System.Enum.GetNames(typeof(DayPart)));
            CollectionAssert.AreEqual(new[] { "Idle", "AddIngredient", "Minigame", "Garnish", "Serve" }, System.Enum.GetNames(typeof(BarStep)));
            // flow_minigame_type() strings; "Stiring" spelling is load-bearing.
            CollectionAssert.AreEqual(new[] { "None", "Shaking", "Stiring", "Building" }, System.Enum.GetNames(typeof(Enum_MiniGameType)));
            CollectionAssert.AreEqual(new[] { "Completed", "Cancelled" }, System.Enum.GetNames(typeof(MinigameOutcome)));
        }

        [Test]
        public void Defaults_AreEmpty()
        {
            var s = new FlowState();
            Assert.AreEqual("", s.PhaseString); Assert.AreEqual("", s.StepString);
            Assert.AreEqual("", s.LastMinigameOutcome); Assert.AreEqual(Enum_MiniGameType.None, s.SelectedMinigame);
            Assert.IsFalse(s.SessionRunning);
        }

        [Test]
        public void Changed_FiresOncePerChangingSet_NotOnSameValue()
        {
            var s = new FlowState(); int n = 0; s.Changed += () => n++;
            s.SetPart(DayPart.Open); Assert.AreEqual(1, n);
            s.SetPart(DayPart.Open); Assert.AreEqual(1, n);
            s.SetStep(BarStep.Garnish); s.SetSession(true);
            s.SelectMinigame(Enum_MiniGameType.Stiring); s.SetMinigameOutcome("Completed");
            Assert.AreEqual(5, n);
        }

        [Test]
        public void Changed_ObserversSeeNewValue()
        {
            var s = new FlowState(); var seen = new List<string>();
            s.Changed += () => seen.Add(s.StepString);
            s.SetPart(DayPart.Story);
            Assert.AreEqual(new[] { "StoryBeforeOpen" }, seen);
        }
    }
}
