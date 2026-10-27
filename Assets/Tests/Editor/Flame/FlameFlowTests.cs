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

}
