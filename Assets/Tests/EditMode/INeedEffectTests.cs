using NUnit.Framework;

namespace DelphAi.Core.Tests
{
    public class INeedEffectTests
    {
        const float Eps = 1e-6f;

        [Test]
        public void Fed_Gain_Adds_Per_Unit_Times_Intensity_Clamped()
        {
            INeedEffect effect = new FedGain(0.2f);
            var v = new Vitals(0.5f, 0.5f);
            var v2 = effect.Apply(v, 1f);
            Assert.That(v2.Fed, Is.EqualTo(0.7f).Within(Eps));
            Assert.That(v2.Hydration, Is.EqualTo(0.5f).Within(Eps), "untouched");
        }

        [Test]
        public void Fed_Gain_Affects_Only_Fed_Vital_Kind()
        {
            INeedEffect effect = new FedGain(0.2f);
            Assert.That(effect.Affects(VitalKind.Fed), Is.True);
            Assert.That(effect.Affects(VitalKind.Hydration), Is.False);
        }

        [Test]
        public void Hydration_Gain_Adds_Per_Unit_Times_Intensity()
        {
            INeedEffect effect = new HydrationGain(0.2f);
            var v = new Vitals(0.3f, 0.4f);
            var v2 = effect.Apply(v, 0.5f);
            Assert.That(v2.Hydration, Is.EqualTo(0.5f).Within(Eps), "+0.1 = 0.2*0.5");
            Assert.That(v2.Fed, Is.EqualTo(0.3f).Within(Eps));
        }

        [Test]
        public void Hydration_Gain_Affects_Only_Hydration_Vital_Kind()
        {
            INeedEffect effect = new HydrationGain(0.2f);
            Assert.That(effect.Affects(VitalKind.Hydration), Is.True);
            Assert.That(effect.Affects(VitalKind.Fed), Is.False);
        }

        [Test]
        public void Apply_Is_Pure_Original_Vitals_Untouched()
        {
            INeedEffect effect = new FedGain(0.2f);
            var v = new Vitals(0.5f, 0.5f);
            effect.Apply(v, 1f);
            Assert.That(v.Fed, Is.EqualTo(0.5f).Within(Eps), "original immutable");
        }
    }
}
