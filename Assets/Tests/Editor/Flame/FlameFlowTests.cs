using System;
using NUnit.Framework;
using Omnia.Fire;

public class FlameFlowTests {
    private static FlameSimSettings Jet() {
        var s = FlameSimSettings.Default;
        s.cellularFlow = true; s.maxRange = 96; s.jetSpeed = 10;
        s.pulsePeriod = 1; s.slugLength = 10; s.maxEmbers = 0;
        return s;
    }
    [Test] public void JetShearGeneratesBackwardMovingHotCellsWithoutWingParticles() {
        var sim = new FlameSim(512, 224, 31); var s = Jet(); var pose = new FlamePose();
        for (int t = 0; t < 80; t++) sim.Step(true, pose, pose, s);
        int backward = 0, offAxis = 0, positiveCurl = 0, negativeCurl = 0;
        for (int i = 0; i < sim.Capacity; i++) Assert.AreNotEqual(FlameParticleKind.Wing, sim.ParticleAt(i).kind);
        for (int y = 1; y < sim.WindowSize - 1; y++) for (int x = 1; x < sim.WindowSize - 1; x++) {
            int i = y * sim.WindowSize + x;
            float heat = sim.Flow.CellHeat(i); if (heat <= 0.08f) continue;
            Assert.That(heat, Is.InRange(0, 1));
            if (sim.Flow.CellVelocity(i).x < -0.1f) backward++;
            if (Math.Abs(sim.OriginY + y) > 4) offAxis++;
            float curl = sim.Flow.CellVelocity(i + 1).y - sim.Flow.CellVelocity(i - 1).y
                - sim.Flow.CellVelocity(i + sim.WindowSize).x + sim.Flow.CellVelocity(i - sim.WindowSize).x;
            if (curl > 0.1f) positiveCurl++; if (curl < -0.1f) negativeCurl++;
        }
        Assert.Greater(backward, 10, "Hot material must actually flow backward, not just change its outline");
        Assert.Greater(offAxis, 20);
        Assert.Greater(positiveCurl, 10); Assert.Greater(negativeCurl, 10);
    }
    [Test] public void ReleasedHeatAndVelocityIgnoreTheNewNozzlePose() {
        var a = new FlameSim(); var b = new FlameSim(); var s = Jet();
        s.releaseCooling = 0;
        for (int t = 0; t < 30; t++) { a.Step(true, default, default, s); b.Step(true, default, default, s); }
        for (int t = 0; t < 10; t++) {
            a.Step(false, default, default, s);
            b.Step(false, new FlamePose(-1000, 700, 2), new FlamePose(900, -800, -2), s);
            Assert.AreEqual(a.OriginX, b.OriginX); Assert.AreEqual(a.OriginY, b.OriginY);
            for (int i = 0; i < a.WindowSize * a.WindowSize; i++) {
                Assert.AreEqual(a.CellHeat(i), b.CellHeat(i));
                Assert.AreEqual(a.Flow.CellVelocity(i).x, b.Flow.CellVelocity(i).x);
                Assert.AreEqual(a.Flow.CellVelocity(i).y, b.Flow.CellVelocity(i).y);
            }
        }
        Assert.IsTrue(a.Flow.HasHeat, "This check must include lingering grid heat after core expiry");
        Assert.AreEqual(0, a.ParticleCount);
    }
    [Test] public void MovingRasterWindowPreservesWorldHeatAndVelocity() {
        const int side = 96;
        var a = new FlameFlow(side, 3); var b = new FlameFlow(side, 3);
        var heat = new float[side * side]; var velocity = new FlameVector[heat.Length];
        int cell = 48 * side + 48; heat[cell] = 1; velocity[cell] = new FlameVector(2, 0);
        var s = Jet();
        for (int t = 0; t < 6; t++) { a.Step(0, 0, heat, velocity, s, true, t, null); b.Step(0, 0, heat, velocity, s, true, t, null); }
        Array.Clear(heat, 0, heat.Length); Array.Clear(velocity, 0, velocity.Length);
        a.Step(0, 0, heat, velocity, s, false, 6, null);
        b.Step(7, -9, heat, velocity, s, false, 6, null);
        Assert.IsTrue(a.HasHeat && b.HasHeat);
        for (int y = 10; y < 80; y++) for (int x = 10; x < 80; x++) {
            int i = y * side + x, shifted = (y + 9) * side + x - 7;
            Assert.AreEqual(a.CellHeat(i), b.CellHeat(shifted), 0.0001f);
            Assert.AreEqual(a.CellVelocity(i).x, b.CellVelocity(shifted).x, 0.0001f);
            Assert.AreEqual(a.CellVelocity(i).y, b.CellVelocity(shifted).y, 0.0001f);
        }
    }
    private sealed class Wall : IFlameCollision {
        public bool CellSolid(int x, int y) => x == 20;
        public bool HitsSolid(FlameVector a, FlameVector b) => Math.Min(a.x, b.x) <= 21 && Math.Max(a.x, b.x) >= 20;
    }
    [Test] public void FastCellTransportCannotLeakHeatThroughOnePixelWall() {
        var sim = new FlameSim(); var s = Jet(); s.flowSpeed = 6;
        var wall = new Wall();
        for (int t = 0; t < 60; t++) {
            sim.Step(true, default, default, s, wall);
            for (int i = 0; i < sim.WindowSize * sim.WindowSize; i++)
                if (sim.OriginX + i % sim.WindowSize >= 20) Assert.AreEqual(0, sim.CellHeat(i));
        }
    }
    [Test] public void ClearRemovesHeatAndVelocityAndRestoresSeededResult() {
        var a = new FlameSim(); var b = new FlameSim(); var s = Jet();
        for (int t = 0; t < 25; t++) a.Step(true, default, default, s);
        Assert.IsTrue(a.Flow.HasHeat); a.Clear();
        Assert.IsFalse(a.Flow.HasHeat);
        for (int i = 0; i < a.WindowSize * a.WindowSize; i++) {
            Assert.AreEqual(0, a.Flow.CellHeat(i));
            Assert.AreEqual(0, a.Flow.CellVelocity(i).x); Assert.AreEqual(0, a.Flow.CellVelocity(i).y);
        }
        for (int t = 0; t < 5; t++) { a.Step(true, default, default, s); b.Step(true, default, default, s); }
        Assert.AreEqual(a.OriginX, b.OriginX); Assert.AreEqual(a.OriginY, b.OriginY);
        for (int i = 0; i < a.WindowSize * a.WindowSize; i++) Assert.AreEqual(a.CellHeat(i), b.CellHeat(i));
    }
    [Test] public void SteadyCellularTicksAllocateNoManagedMemory() {
        var sim = new FlameSim(); var s = Jet();
        s.coreHeatVariation = 0.5f;
        for (int t = 0; t < 40; t++) sim.Step(true, default, default, s);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int t = 0; t < 50; t++) sim.Step(true, default, default, s);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.AreEqual(0, allocated);
    }

    [Test] public void RasterizingAgainDoesNotAdvanceHeatOrVelocity() {
        var sim = new FlameSim(); var s = Jet();
        for (int t = 0; t < 30; t++) sim.Step(true, default, default, s);
        int ox = sim.OriginX, oy = sim.OriginY, size = sim.WindowSize;
        var cells = new float[size * size];
        for (int i = 0; i < cells.Length; i++) cells[i] = sim.CellHeat(i);
        int tick = sim.StepIndex;
        sim.Rasterize(default, s); sim.Rasterize(default, s);
        Assert.AreEqual(tick, sim.StepIndex);
        for (int i = 0; i < cells.Length; i++)
            Assert.AreEqual(cells[i], sim.SampleWorld(ox + i % size + 0.5f, oy + i / size + 0.5f));
    }

    [TestCase(0f)]
    [TestCase(3.1415927f)]
    public void UpperEddiesFavorWorldUpWhenAimingEitherDirection(float angle) {
        var s = Jet(); s.coreDroop = 0;
        s.coreRadius = 2.5f; s.ribbonWidthScale = 2.4f;
        s.coreNozzleScale = 0.18f; s.coreBloomPixels = 14;
        s.coreFadeStart = 0.35f; s.coreFadeEnd = 0.85f;
        s.radialFalloff = 0.65f; s.colorFalloff = 0.8f;
        s.flowHeat = 0.85f; s.flowCooling = 0.032f; s.flowJetStability = 0.8f;
        s.flowUpperBias = 0.8f; s.flowBuoyancy = 0.12f; s.flowPressureIterations = 6;
        var sim = new FlameSim(512, 288, 31); var pose = new FlamePose(0,0,angle);
        double upper = 0, lower = 0;
        for (int t = 0; t < 100; t++) {
            sim.Step(true, pose, pose, s);
            if (t < 40) continue;
            for (int j = 0; j < sim.OccupiedCount; j++) {
                int cell = sim.OccupiedIndex(j); float heat = sim.CellHeat(cell);
                if (heat < 0.08f) continue;
                float y = sim.OriginY + cell / sim.WindowSize + 0.5f;
                // Exclude the broad core; measure the extent/heat of the outer wisps.
                if (y > 8) upper += heat * (y - 8);
                if (y < -8) lower += heat * (-y - 8);
            }
        }
        Assert.Greater(upper, 100, "Upper wisps must actually develop");
        Assert.Greater(upper, lower * 2, "The upper plume must dominate even when facing left");
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void BoundedSourcesMatchFullScanAcrossRecenterReleaseAndReset(int cellSize) {
        const int side = 48;
        var bounded = new FlameFlow(side, 19, cellSize);
        var reference = new FlameFlow(side, 19, cellSize);
        var heat = new float[side * side];
        var velocity = new FlameVector[heat.Length];
        var settings = Jet();
        for (int tick = 0; tick < 50; tick++) {
            if (tick == 30) { bounded.Clear(); reference.Clear(); }
            Array.Clear(heat, 0, heat.Length); Array.Clear(velocity, 0, velocity.Length);
            int ox = tick < 30 ? -20 + tick / 3 : 80 - tick / 2;
            int oy = -24 - tick / 4;
            var sources = FlameBounds.Empty;
            bool firing = tick < 15 || tick >= 30 && tick < 37;
            if (firing) {
                int x = 12 + tick % 7, y = 19;
                heat[y * side + x] = 1;
                velocity[y * side + x] = new FlameVector(3, 1);
                sources.Include(x, y);
            }
            bounded.Step(ox, oy, heat, velocity, settings, firing, tick, null, sources);
            reference.Step(ox, oy, heat, velocity, settings, firing, tick, null);
            Assert.AreEqual(reference.HasHeat, bounded.HasHeat);
            for (int i = 0; i < heat.Length; i++) {
                Assert.AreEqual(reference.CellHeat(i), bounded.CellHeat(i), "Heat at tick " + tick + ", cell " + i);
                Assert.AreEqual(reference.CellVelocity(i).x, bounded.CellVelocity(i).x);
                Assert.AreEqual(reference.CellVelocity(i).y, bounded.CellVelocity(i).y);
            }
        }
    }

    [TestCase(0.2f)]
    [TestCase(0.6f)]
    [TestCase(1.2f)]
    public void SustainedFastSpinAllocatesNothingAndRetainsFireUntilRelease(float radiansPerTick) {
        var settings = Jet(); settings.coreRadius = 2.5f; settings.ribbonWidthScale = 2.4f;
        settings.coreNozzleScale = 0.18f; settings.coreBloomPixels = 14;
        settings.coreFadeStart = 0.35f; settings.coreFadeEnd = 0.85f;
        settings.coreHeatVariation = 0.5f; settings.flowCooling = 0.032f;
        settings.flowJetStability = 0.8f; settings.flowPressureIterations = 6;
        var sim = new FlameSim(512, 288, 31); var previous = new FlamePose();
        for (int tick = 0; tick < 40; tick++) {
            var pose = new FlamePose(0, 0, tick * radiansPerTick);
            sim.Step(true, previous, pose, settings); previous = pose;
        }
        long before = GC.GetAllocatedBytesForCurrentThread();
        int peakParticles = 0;
        for (int tick = 40; tick < 160; tick++) {
            var pose = new FlamePose(0, 0, tick * radiansPerTick);
            sim.Step(true, previous, pose, settings); previous = pose;
            peakParticles = Math.Max(peakParticles, sim.ParticleCount);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.AreEqual(0, allocated);
        Assert.LessOrEqual(peakParticles, sim.Capacity);
        Assert.IsTrue(sim.Flow.HasHeat);
        int hot = 0;
        for (int cell = 0; cell < sim.WindowSize * sim.WindowSize; cell++) {
            float heat = sim.CellHeat(cell);
            Assert.That(heat, Is.InRange(0, 1));
            if (heat > 0.08f) hot++;
        }
        Assert.Greater(hot, 100, "Spinning must retain the old fire rather than clearing it for speed");
        var away = new FlamePose(900, -800, -2);
        sim.Step(false, previous, away, settings);
        Assert.IsTrue(sim.Flow.HasHeat, "Stopping emission must preserve lingering heat");
        Assert.Less(Math.Abs(sim.OriginX), 300);
        Assert.Less(Math.Abs(sim.OriginY), 300);
        for (int tick = 0; tick < 100; tick++) sim.Step(false, away, away, settings);
        Assert.AreEqual(0, sim.ParticleCount); Assert.IsFalse(sim.Flow.HasHeat);
        Assert.AreEqual(0, sim.OccupiedCount);
    }

}
