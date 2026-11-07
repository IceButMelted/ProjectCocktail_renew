using Bar410.Contracts;

namespace Bar410.Flow
{
    /// <summary>Plain bag of the ports Flow uses, built once by GameRoot. No lookups at run time.</summary>
    public sealed class FlowPorts
    {
        public ICameraDirector Camera;
        public IBarCounter Bar;
        public IOrderDesk Orders;
        public IMinigameHost Minigames;
        public IGarnishHost Garnish;
        public IStory Story;
        public IDayUi DayUi;
    }
}
