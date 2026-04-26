using NUnit.Framework;

namespace DelphAi.Core.Tests
{
    public class ResourceTests
    {
        const float Eps = 1e-6f;

        [Test]
        public void New_Berry_Starts_At_Max_Amount()
        {
            var r = Resource.NewBerry(new TilePos(3, 4));
            Assert.That(r.Kind, Is.EqualTo(ResourceKind.Berry));
            Assert.That(r.TilePos, Is.EqualTo(new TilePos(3, 4)));
            Assert.That(r.Amount, Is.EqualTo(Resource.BerryAmountMax).Within(Eps));
        }

        [Test]
        public void New_Water_Starts_At_Max_Amount()
        {
            var r = Resource.NewWater(new TilePos(7, 2));
            Assert.That(r.Kind, Is.EqualTo(ResourceKind.Water));
            Assert.That(r.Amount, Is.EqualTo(Resource.WaterAmountMax).Within(Eps));
        }

        [Test]
        public void Consume_Decreases_Amount_By_Consume_Per_Tick()
        {
            var r = Resource.NewBerry(new TilePos(0, 0));
            float taken = r.ConsumeOneTick();
            Assert.That(taken, Is.EqualTo(Resource.ConsumePerTick).Within(Eps));
            Assert.That(r.Amount, Is.EqualTo(Resource.BerryAmountMax - Resource.ConsumePerTick).Within(Eps));
        }

        [Test]
        public void Consume_Returns_Zero_And_Leaves_Amount_At_Zero_When_Empty()
        {
            var r = Resource.WithAmount(ResourceKind.Berry, 0f, new TilePos(0, 0));
            float taken = r.ConsumeOneTick();
            Assert.That(taken, Is.EqualTo(0f).Within(Eps));
            Assert.That(r.Amount, Is.EqualTo(0f).Within(Eps));
        }

        [Test]
        public void Consume_On_Nearly_Empty_Takes_Only_Remaining()
        {
            var r = Resource.WithAmount(ResourceKind.Berry, 0.3f, new TilePos(0, 0));
            float taken = r.ConsumeOneTick();
            Assert.That(taken, Is.EqualTo(0.3f).Within(Eps));
            Assert.That(r.Amount, Is.EqualTo(0f).Within(Eps));
        }

        [Test]
        public void Berry_Regenerate_Increments_Amount_By_Rate()
        {
            var r = Resource.WithAmount(ResourceKind.Berry, 2f, new TilePos(0, 0));
            r.Regenerate();
            Assert.That(r.Amount, Is.EqualTo(2f + Resource.BerryRegenPerTick).Within(Eps));
        }

        [Test]
        public void Berry_Regenerate_Clamps_At_Max()
        {
            var r = Resource.WithAmount(ResourceKind.Berry, Resource.BerryAmountMax, new TilePos(0, 0));
            r.Regenerate();
            Assert.That(r.Amount, Is.EqualTo(Resource.BerryAmountMax).Within(Eps));
        }

        [Test]
        public void Berry_Regenerate_From_Empty_Eventually_Refills()
        {
            var r = Resource.WithAmount(ResourceKind.Berry, 0f, new TilePos(0, 0));
            // BERRY_AMOUNT_MAX=5, rate=0.04 → 125 ticks to full.
            for (int i = 0; i < 200; i++)
            {
                r.Regenerate();
            }
            Assert.That(r.Amount, Is.EqualTo(Resource.BerryAmountMax).Within(Eps));
        }

        [Test]
        public void Water_Regenerate_Uses_Water_Rate()
        {
            var r = Resource.WithAmount(ResourceKind.Water, 0f, new TilePos(0, 0));
            r.Regenerate();
            Assert.That(r.Amount, Is.EqualTo(Resource.WaterRegenPerTick).Within(Eps));
        }

        [Test]
        public void Is_Depleted_Reflects_Zero_Amount()
        {
            var r = Resource.NewBerry(new TilePos(0, 0));
            Assert.That(r.IsDepleted, Is.False);
            for (int i = 0; i < 10; i++) r.ConsumeOneTick();
            Assert.That(r.IsDepleted, Is.True);
        }
    }
}
