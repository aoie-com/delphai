using NUnit.Framework;

namespace DelphAi.Core.Tests
{
    public class SmokeTest
    {
        [Test]
        public void Core_Build_Version_IsAccessible()
        {
            Assert.That(Build.Version, Is.EqualTo("M1.0"));
        }
    }
}
