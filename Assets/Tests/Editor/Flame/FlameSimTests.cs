using System;
using NUnit.Framework;
using Omnia.Fire;

public class FlameSimTests {
    private static FlamePose Pose(float x = 0, float y = 0, float angle = 0) => new FlamePose(x, y, angle);
    private static FlameSimSettings Quiet() {
        var settings = FlameSimSettings.Default;
        settings.cellularFlow = false;
        settings.coreJitterChance = settings.shedChance = settings.releaseCooling = 0;
        settings.maxEmbers = 0;
        return settings;
    }
    private static void EqualParticles(FlameSim a, FlameSim b) {
        Assert.AreEqual(a.ParticleCount, b.ParticleCount);
        for (int i = 0; i < a.Capacity; i++) {
            var x = a.ParticleAt(i); var y = b.ParticleAt(i);
            Assert.AreEqual(x.kind, y.kind);
            if (x.kind == FlameParticleKind.None) continue;
            Assert.AreEqual(x.pos.x, y.pos.x); Assert.AreEqual(x.pos.y, y.pos.y);
            Assert.AreEqual(x.vel.x, y.vel.x); Assert.AreEqual(x.vel.y, y.vel.y);
            Assert.AreEqual(x.dir.x, y.dir.x); Assert.AreEqual(x.dir.y, y.dir.y);
            Assert.AreEqual(x.age, y.age); Assert.AreEqual(x.life, y.life);
        }
    }

    [Test] public void SameSeedAndPoseSequenceProduceIdenticalRaster() {
        var a = new FlameSim(seed: 123); var b = new FlameSim(seed: 123);
        var previous = Pose();
        for (int i = 0; i < 100; i++) {
            var current = Pose(i * 0.25f, i % 7, i * 0.13f);
            a.Step(i < 80, previous, current, FlameSimSettings.Default);
            b.Step(i < 80, previous, current, FlameSimSettings.Default);
            previous = current;
            EqualParticles(a, b);
            Assert.AreEqual(a.OriginX, b.OriginX); Assert.AreEqual(a.OriginY, b.OriginY);
            for (int j = 0; j < a.WindowSize * a.WindowSize; j++) Assert.AreEqual(a.CellHeat(j), b.CellHeat(j));
        }
    }

    [Test] public void TurningAndMovingNozzleNeverReaimExistingParticles() {
        var a = new FlameSim(); var b = new FlameSim();
        var settings = FlameSimSettings.Default;
        settings.cellularFlow = false; settings.shedChance = 1; settings.wingLifeMin = settings.wingLifeMax = 20;
        settings.releaseCooling = 0;
        for (int i = 0; i < 20; i++) { a.Step(true, Pose(), Pose(), settings); b.Step(true, Pose(), Pose(), settings); }
        bool hasWing = false;
        for (int i = 0; i < a.Capacity; i++) hasWing |= a.ParticleAt(i).kind == FlameParticleKind.Wing;
        Assert.IsTrue(hasWing);
        for (int i = 0; i < 4; i++) {
            a.Step(false, Pose(), Pose(), settings);
            b.Step(false, Pose(500, -200, 2), Pose(-500, 800, -2), settings);
            EqualParticles(a, b);
        }
    }

    [Test] public void OldCoreKeepsItsVelocityWhenNewFireGoesOppositeWay() {
        var sim = new FlameSim(); var settings = Quiet();
        settings.pulsePeriod = 1; settings.coreDroop = 0;
        sim.Step(true, Pose(), Pose(), settings);
        var before = sim.ParticleAt(0);
        sim.Step(true, Pose(20, 30, (float)Math.PI), Pose(20, 30, (float)Math.PI), settings);
        var after = sim.ParticleAt(0);
        Assert.AreEqual(before.pos.x + before.vel.x, after.pos.x);
        Assert.AreEqual(before.pos.y + before.vel.y, after.pos.y);
        Assert.AreEqual(before.vel.x, after.vel.x);
        Assert.Greater(after.vel.x, 0);
        Assert.Less(sim.ParticleAt(3).vel.x, 0);
    }

    [Test] public void ContinuedEmissionCannotChangeAnOldParticlesJitterStream() {
        var a = new FlameSim(); var b = new FlameSim(); var settings = Quiet();
        settings.maxRange = 200; settings.coreJitterChance = 1; settings.inheritVelocity = 1;
        settings.maxInheritedSpeed = 100; settings.pulsePeriod = 1; settings.slugLength = 5;
        a.Step(true, Pose(), Pose(), settings); b.Step(true, Pose(), Pose(), settings);
        var previous = Pose();
        for (int i = 0; i < 12; i++) {
            var changed = Pose(i % 2 == 0 ? 700 : -700, i * 100, i * 1.1f);
            a.Step(true, Pose(), Pose(), settings); b.Step(true, previous, changed, settings);
            previous = changed;
            var x = a.ParticleAt(0); var y = b.ParticleAt(0);
            Assert.AreEqual(FlameParticleKind.Core, x.kind); Assert.AreEqual(x.kind, y.kind);
            Assert.AreEqual(x.pos.x, y.pos.x); Assert.AreEqual(x.pos.y, y.pos.y);
            Assert.AreEqual(x.vel.x, y.vel.x); Assert.AreEqual(x.vel.y, y.vel.y);
        }
    }

    [Test] public void PulsesHaveGapsAndInterpolateSpawnPoses() {
        var sim = new FlameSim(); var settings = Quiet();
        settings.slugLength = 6; settings.inheritVelocity = 0; settings.jetSpeed = 0.1f;
        sim.Step(true, Pose(), Pose(30, 0, (float)Math.PI / 2), settings);
        Assert.AreEqual(6, sim.ParticleCount);
        Assert.AreEqual(0, sim.ParticleAt(0).previousPos.x);
        Assert.Greater(sim.ParticleAt(5).previousPos.x, 20);
        Assert.Greater(sim.ParticleAt(5).vel.y, sim.ParticleAt(0).vel.y);
        sim.Step(true, Pose(), Pose(), settings); Assert.AreEqual(6, sim.ParticleCount);
        sim.Step(true, Pose(), Pose(), settings); Assert.AreEqual(6, sim.ParticleCount);
        sim.Step(true, Pose(), Pose(), settings); Assert.AreEqual(12, sim.ParticleCount);
    }

    [Test] public void ParticleAndEmberCountsNeverExceedCapsAndHeatIsBounded() {
        var sim = new FlameSim(32, 64); var settings = FlameSimSettings.Default;
        settings.slugLength = 64; settings.pulsePeriod = 1; settings.maxRange = 4; settings.maxEmbers = 3;
        for (int i = 0; i < 250; i++) {
            sim.Step(true, Pose(), Pose(), settings);
            Assert.That(sim.ParticleCount, Is.InRange(0, 32));
            Assert.That(sim.EmberCount, Is.InRange(0, 3));
            for (int j = 0; j < sim.OccupiedCount; j++) Assert.That(sim.CellHeat(sim.OccupiedIndex(j)), Is.InRange(0, 1));
        }
    }

    [Test] public void ReleaseExtinguishesEverythingWithinOneSecond() {
        var sim = new FlameSim();
        for (int i = 0; i < 50; i++) sim.Step(true, Pose(), Pose(), FlameSimSettings.Default);
        for (int i = 0; i < 20; i++) sim.Step(false, Pose(), Pose(), FlameSimSettings.Default);
        Assert.AreEqual(0, sim.ParticleCount); Assert.AreEqual(0, sim.OccupiedCount);
    }

    [Test] public void WindowFollowsLingeringFireRatherThanTheNozzle() {
        var sim = new FlameSim(); var settings = Quiet();
        sim.Step(true, Pose(), Pose(), settings);
        sim.Step(false, Pose(), Pose(1000, 1000, 3), settings);
        Assert.Less(Math.Abs(sim.OriginX + sim.WindowSize / 2), 20);
        Assert.Less(Math.Abs(sim.OriginY + sim.WindowSize / 2), 20);
    }

    [Test] public void ParticlesOutsideRasterKeepSimulating() {
        var sim = new FlameSim(512, 8); var settings = Quiet();
        for (int i = 0; i < 8; i++) sim.Step(true, Pose(), Pose(), settings);
        var before = sim.ParticleAt(0);
        Assert.AreEqual(FlameParticleKind.Core, before.kind);
        Assert.Greater(before.pos.x, sim.OriginX + sim.WindowSize);
        sim.Step(false, Pose(), Pose(), settings);
        Assert.Greater(sim.ParticleAt(0).pos.x, before.pos.x);
    }

    [Test] public void WingsEventuallyMoveBackwardAlongTheirOwnParentDirection() {
        var sim = new FlameSim(); var settings = FlameSimSettings.Default;
        settings.cellularFlow = false; settings.shedChance = 1; settings.backDrag = 2; settings.wingLifeMin = settings.wingLifeMax = 20;
        for (int i = 0; i < 40; i++) sim.Step(true, Pose(), Pose(), settings);
        bool backward = false;
        for (int i = 0; i < sim.Capacity; i++) {
            var p = sim.ParticleAt(i);
            if (p.kind == FlameParticleKind.Wing && p.vel.x * p.dir.x + p.vel.y * p.dir.y < 0) backward = true;
        }
        Assert.IsTrue(backward);
    }

    [Test] public void GravityAndBuoyancyUseWorldUpAndDown() {
        var sim = new FlameSim(); var settings = FlameSimSettings.Default;
        settings.cellularFlow = false; settings.coreDroop = 0.1f; settings.shedChance = 1; settings.backDrag = 0;
        settings.wingForward = settings.wingSpread = 0; settings.wingLift = 0.2f; settings.liftAccel = 0.1f;
        settings.wingLifeMin = settings.wingLifeMax = 20;
        var left = Pose(0, 0, (float)Math.PI);
        for (int i = 0; i < 30; i++) sim.Step(true, left, left, settings);
        bool core = false, wing = false;
        for (int i = 0; i < sim.Capacity; i++) {
            var p = sim.ParticleAt(i);
            if (p.kind == FlameParticleKind.Core) { Assert.Less(p.vel.y, 0); core = true; }
            if (p.kind == FlameParticleKind.Wing) { Assert.Greater(p.vel.y, 0); wing = true; }
        }
        Assert.IsTrue(core && wing);
    }

    private sealed class ThinWall : IFlameCollision {
        public bool HitsSolid(FlameVector from, FlameVector to) => Math.Min(from.x, to.x) <= 21 && Math.Max(from.x, to.x) >= 20;
        public bool CellSolid(int x, int y) => x == 20;
    }
    [Test] public void SweptCollisionStopsFastCoreAndThickStampsAtOnePixelWall() {
        var sim = new FlameSim(); var settings = FlameSimSettings.Default;
        settings.jetSpeed = 12; settings.coreRadius = 2;
        var wall = new ThinWall();
        for (int i = 0; i < 30; i++) {
            sim.Step(true, Pose(), Pose(), settings, wall);
            for (int j = 0; j < sim.OccupiedCount; j++) {
                int cell = sim.OccupiedIndex(j);
                if (sim.OriginX + cell % sim.WindowSize >= 20) Assert.AreEqual(0, sim.CellHeat(cell));
            }
        }
    }

    [Test] public void ColorFalloffAndRangeChangeHeatAndBreakupWithoutAffectingOldVelocity() {
        var a = new FlameSim(); var b = new FlameSim(); var hot = Quiet(); var cool = hot;
        cool.colorFalloff = 2;
        for (int i = 0; i < 5; i++) { a.Step(true, Pose(), Pose(), hot); b.Step(true, Pose(), Pose(), cool); }
        var p = a.ParticleAt(0);
        Assert.Greater(a.SampleWorld(p.pos.x, p.pos.y), b.SampleWorld(p.pos.x, p.pos.y));
        var shortJet = new FlameSim(); var longJet = new FlameSim();
        hot.maxRange = 12; cool = hot; cool.maxRange = 48;
        for (int i = 0; i < 8; i++) { shortJet.Step(true, Pose(), Pose(), hot); longJet.Step(true, Pose(), Pose(), cool); }
        float shortDistance = 0;
        for (int i = 0; i < shortJet.Capacity; i++) if (shortJet.ParticleAt(i).kind == FlameParticleKind.Core)
            shortDistance = Math.Max(shortDistance, shortJet.ParticleAt(i).dist);
        Assert.Less(shortDistance, 12);
        Assert.Greater(longJet.ParticleAt(0).dist, 12);
    }

    [Test] public void ClearResetsPoolRasterAndSeed() {
        var sim = new FlameSim();
        for (int i = 0; i < 20; i++) sim.Step(true, Pose(), Pose(), FlameSimSettings.Default);
        sim.Clear();
        Assert.AreEqual(0, sim.ParticleCount); Assert.AreEqual(0, sim.EmberCount);
        for (int i = 0; i < sim.WindowSize * sim.WindowSize; i++) Assert.AreEqual(0, sim.CellHeat(i));
        var fresh = new FlameSim();
        sim.Step(true, Pose(), Pose(), FlameSimSettings.Default); fresh.Step(true, Pose(), Pose(), FlameSimSettings.Default);
        EqualParticles(sim, fresh);
    }
    private static int Components(FlameSim sim) {
        int side = sim.WindowSize;
        var visited = new bool[side * side]; var queue = new int[visited.Length];
        int components = 0;
        for (int seed = 0; seed < sim.OccupiedCount; seed++) {
            int start = sim.OccupiedIndex(seed);
            if (visited[start] || sim.CellHeat(start) <= 0.08f) continue;
            components++; int read = 0, write = 0; queue[write++] = start; visited[start] = true;
            while (read < write) {
                int cell = queue[read++], x = cell % side, y = cell / side;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= side || ny >= side) continue;
                    int next = ny * side + nx;
                    if (visited[next] || sim.CellHeat(next) <= 0.08f) continue;
                    visited[next] = true; queue[write++] = next;
                }
            }
        }
        return components;
    }

    [Test] public void FastSpinRendersOneConnectedCoreInsteadOfProjectileIslands() {
        var sim = new FlameSim(512, 224); var settings = Quiet();
        settings.maxRange = 96; settings.jetSpeed = 10; settings.pulsePeriod = 1; settings.slugLength = 6;
        settings.coreDroop = 0; settings.coreRibbon = true;
        var previous = Pose();
        for (int i = 0; i < 24; i++) {
            var current = Pose(0, 0, i * 0.8f);
            sim.Step(true, previous, current, settings); previous = current;
            Assert.AreEqual(1, Components(sim), "Separated core pieces on spin tick " + i);
        }
    }

    private static FlameSim Corner(bool ribbon, IFlameCollision collision = null) {
        var sim = new FlameSim(512, 224); var settings = Quiet();
        settings.maxRange = 96; settings.jetSpeed = 10; settings.pulsePeriod = 1; settings.slugLength = 1;
        settings.coreDroop = settings.inheritVelocity = 0; settings.coreRibbon = ribbon;
        for (int i = 0; i < 4; i++) sim.Step(true, Pose(), Pose(), settings, collision);
        var up = Pose(0, 0, (float)Math.PI / 2);
        sim.Step(true, Pose(), up, settings, collision); sim.Step(true, up, up, settings, collision);
        return sim;
    }

    [Test] public void RibbonFillsTheCurvingArcBetweenEmissionSamples() {
        var ribbon = Corner(true); var streaks = Corner(false);
        Assert.Greater(ribbon.SampleWorld(9.5f, 5.5f), 0.35f, "The visible arc must also contain damaging heat");
        Assert.AreEqual(0, streaks.SampleWorld(9.5f, 5.5f));
    }

    private sealed class ArcWall : IFlameCollision {
        private static bool Axis(float from, float delta, float min, float max, ref float enter, ref float exit) {
            if (Math.Abs(delta) < 0.00001f) return from >= min && from <= max;
            float a = (min - from) / delta, b = (max - from) / delta;
            enter = Math.Max(enter, Math.Min(a, b)); exit = Math.Min(exit, Math.Max(a, b));
            return enter <= exit;
        }
        public bool HitsSolid(FlameVector from, FlameVector to) {
            float enter = 0, exit = 1;
            return Axis(from.x, to.x - from.x, 8, 12, ref enter, ref exit)
                && Axis(from.y, to.y - from.y, 4, 8, ref enter, ref exit);
        }
        public bool CellSolid(int x, int y) => x >= 8 && x < 12 && y >= 4 && y < 8;
    }

    [Test] public void InterpolatedArcStopsAtWallsBetweenClearParticlePaths() {
        var clear = Corner(true); var blocked = Corner(true, new ArcWall());
        Assert.Greater(clear.SampleWorld(14.5f, 2.5f), 0);
        Assert.AreEqual(0, blocked.SampleWorld(9.5f, 5.5f));
        Assert.AreEqual(0, blocked.SampleWorld(14.5f, 2.5f), "A curve must not resume beyond an obstacle within one link");
        Assert.Greater(blocked.SampleWorld(25.5f, 0.5f), 0, "Independent old fire on the clear side is still valid");
    }

    [Test] public void ReleasedRibbonDoesNotFollowTheNewNozzlePose() {
        var a = new FlameSim(); var b = new FlameSim(); var settings = Quiet();
        settings.pulsePeriod = 1;
        for (int i = 0; i < 8; i++) { a.Step(true, Pose(), Pose(), settings); b.Step(true, Pose(), Pose(), settings); }
        a.Step(false, Pose(), Pose(), settings); b.Step(false, Pose(), Pose(500, 500, 3), settings);
        Assert.AreEqual(a.OriginX, b.OriginX); Assert.AreEqual(a.OriginY, b.OriginY);
        for (int i = 0; i < a.WindowSize * a.WindowSize; i++) Assert.AreEqual(a.CellHeat(i), b.CellHeat(i));
    }

}
