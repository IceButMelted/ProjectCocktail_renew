using NUnit.Framework;

namespace Bar410.Flow.Tests
{
    public class AsmdefSmokeTests
    {
        [Test]
        public void CoreAssemblyIsReachable() => Assert.IsNotNull(new Bar410.Core.AssemblyMarker());
    }
}
