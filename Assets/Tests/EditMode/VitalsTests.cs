using NUnit.Framework;

namespace DelphAi.Core.Tests
{
    public class VitalsTests
    {
        const float Eps = 1e-6f;

        [Test]
        public void Default_Is_Fully_Fed_And_Hydrated()
        {
            var v = Vitals.Default;
            Assert.That(v.Fed, Is.EqualTo(1f).Within(Eps));
            Assert.That(v.Hydration, Is.EqualTo(1f).Within(Eps));
        }

        [Test]
        public void With_Fed_Decay_Subtracts_And_Clamps_To_Zero()
        {
            var v = new Vitals(0.1f, 1f).WithFedDecay(0.5f);
            Assert.That(v.Fed, Is.EqualTo(0f).Within(Eps), "clamped at 0");
            Assert.That(v.Hydration, Is.EqualTo(1f).Within(Eps), "hydration untouched");
        }

        [Test]
        public void With_Hydration_Decay_Subtracts_And_Clamps_To_Zero()
        {
            var v = new Vitals(1f, 0.05f).WithHydrationDecay(0.1f);
            Assert.That(v.Hydration, Is.EqualTo(0f).Within(Eps));
            Assert.That(v.Fed, Is.EqualTo(1f).Within(Eps));
        }

        [Test]
        public void With_Fed_Gain_Adds_And_Clamps_To_One()
        {
            var v = new Vitals(0.9f, 0.5f).WithFedGain(0.3f);
            Assert.That(v.Fed, Is.EqualTo(1f).Within(Eps), "clamped at 1");
            Assert.That(v.Hydration, Is.EqualTo(0.5f).Within(Eps));
        }

        [Test]
        public void With_Hydration_Gain_Adds_And_Clamps_To_One()
        {
            var v = new Vitals(0.5f, 0.9f).WithHydrationGain(0.5f);
            Assert.That(v.Hydration, Is.EqualTo(1f).Within(Eps));
            Assert.That(v.Fed, Is.EqualTo(0.5f).Within(Eps));
        }

        [Test]
        public void Vitals_Are_Immutable_Each_With_Returns_New_Instance()
        {
            var v = new Vitals(0.5f, 0.5f);
            var v2 = v.WithFedDecay(0.1f);
            // Original unchanged, new value reflects decay.
            Assert.That(v.Fed, Is.EqualTo(0.5f).Within(Eps));
            Assert.That(v2.Fed, Is.EqualTo(0.4f).Within(Eps));
        }
    }
}
