using System;

namespace Omnia.Fire {
    public struct FlameVector {
        public float x, y;
        public FlameVector(float x, float y) { this.x = x; this.y = y; }
        public float Length => (float)Math.Sqrt(x * x + y * y);
        public FlameVector Normalized { get { float length = Length; return length > 0.00001f ? this * (1 / length) : new FlameVector(1, 0); } }
        public static FlameVector operator +(FlameVector a, FlameVector b) => new FlameVector(a.x + b.x, a.y + b.y);
        public static FlameVector operator -(FlameVector a, FlameVector b) => new FlameVector(a.x - b.x, a.y - b.y);
        public static FlameVector operator *(FlameVector a, float b) => new FlameVector(a.x * b, a.y * b);
    }

    public struct FlamePose {
        public FlameVector position;
        public float angle;
        public FlamePose(float x, float y, float radians) { position = new FlameVector(x, y); angle = radians; }
    }

    public enum FlameParticleKind : byte { None, Core, Wing, Ember }

    public struct FlameParticle {
        public FlameParticleKind kind;
        public FlameVector pos, previousPos, vel, dir;
        public int age, life, side;
        public float heat0, dist, maxDistance, phase, cooling;
        public uint randomState;
    }

    public struct FlameSimSettings {
        public float maxRange, jetSpeed, inheritVelocity, maxInheritedSpeed;
        public int pulsePeriod, slugLength, coreWidthJitter;
        public float coreJitterChance, coreDroop, shedChance, flapWaveNumber, flapFreq;
        public int wingOffsetMax, wingLifeMin, wingLifeMax, wingSpreadSteps;
        public float wingForward, wingLift, wingSpread, backDrag, liftAccel, wingHeat;
        public float emberGravity, emberHeat;
        public int maxEmbers, emberLifeMin, emberLifeMax;
        public float coreRadius, wingRadius, emberRadius, radialFalloff, colorFalloff, releaseCooling, stepSeconds;
        public bool coreRibbon;
        public float ribbonSmoothness, ribbonWidthScale, ribbonTipTaper, wingTrail;
        public static FlameSimSettings Default => new FlameSimSettings {
            maxRange = 48, jetSpeed = 2.5f, inheritVelocity = 0.3f, maxInheritedSpeed = 24,
            pulsePeriod = 3, slugLength = 3, coreWidthJitter = 0, coreJitterChance = 0.1f, coreDroop = 0.004f,
            shedChance = 0.25f, flapWaveNumber = 0.35f, flapFreq = 1.2f, wingOffsetMax = 2,
            wingForward = 0.5f, wingLift = 0.15f, wingSpread = 0.08f, wingSpreadSteps = 3,
            backDrag = 0.35f, liftAccel = 0.03f, wingLifeMin = 4, wingLifeMax = 9, wingHeat = 0.7f,
            emberGravity = 0.06f, emberHeat = 0.75f, maxEmbers = 64, emberLifeMin = 3, emberLifeMax = 6,
            coreRadius = 1, wingRadius = 1, emberRadius = 0.75f, radialFalloff = 0.3f,
            colorFalloff = 0.55f, releaseCooling = 1.5f, stepSeconds = 0.05f,
            coreRibbon = true, ribbonSmoothness = 0.85f, ribbonWidthScale = 1.75f, ribbonTipTaper = 0.2f, wingTrail = 1
        };
    }

    // World coordinates are in pixels. Implementations must sweep the entire segment,
    // not just test its destination, so fast particles cannot tunnel through thin walls.
    public interface IFlameCollision {
        bool HitsSolid(FlameVector from, FlameVector to);
        bool CellSolid(int worldX, int worldY);
    }

    public sealed class FlameSim {
        private readonly FlameParticle[] particles;
        private readonly int[] free;
        private readonly float[] heat;
        private readonly int[] occupied;
        private readonly uint initialSeed;
        private readonly FlameVector[] cohortPositions;
        private readonly float[] cohortHeat, cohortDistance;
        private readonly int[] cohortAges, cohortCounts, cohortOrder;
        private readonly FlameVector[] ribbonPoints;
        private readonly float[] ribbonHeat, ribbonRadius;
        private readonly int[] ribbonAges;
        private int ribbonCount;
        private FlamePose rasterPose;
        private bool rasterFiring;
        private uint random;
        private int freeCount, pulseCountdown;
        public int Capacity => particles.Length;
        public int ParticleCount { get; private set; }
        public int EmberCount { get; private set; }
        public int StepIndex { get; private set; }
        public int WindowSize { get; }
        public int OriginX { get; private set; }
        public int OriginY { get; private set; }
        public int OccupiedCount { get; private set; }
        public int OccupiedIndex(int index) => occupied[index];
        public float CellHeat(int index) => heat[index];
        public FlameParticle ParticleAt(int index) => particles[index];

        public FlameSim(int capacity = 512, int windowSize = 160, uint seed = 1) {
            if (capacity < 1 || windowSize < 1) throw new ArgumentOutOfRangeException();
            particles = new FlameParticle[capacity];
            free = new int[capacity];
            WindowSize = windowSize;
            heat = new float[windowSize * windowSize];
            occupied = new int[heat.Length];
            cohortPositions = new FlameVector[capacity];
            cohortHeat = new float[capacity]; cohortDistance = new float[capacity];
            cohortAges = new int[capacity]; cohortCounts = new int[capacity]; cohortOrder = new int[capacity];
            ribbonPoints = new FlameVector[capacity + 2];
            ribbonHeat = new float[capacity + 2]; ribbonRadius = new float[capacity + 2]; ribbonAges = new int[capacity + 2];
            initialSeed = seed == 0 ? 1 : seed;
            Clear();
        }

        public void Clear() {
            Array.Clear(particles, 0, particles.Length);
            Array.Clear(heat, 0, heat.Length);
            for (int i = 0; i < free.Length; i++) free[i] = free.Length - 1 - i;
            freeCount = free.Length;
            ParticleCount = EmberCount = OccupiedCount = StepIndex = pulseCountdown = 0;
            random = initialSeed;
            rasterFiring = false; ribbonCount = 0;
        }

        private uint RandomBits() {
            random ^= random << 13; random ^= random >> 17; random ^= random << 5;
            return random;
        }
        private float Random01() => (RandomBits() >> 8) * (1f / 16777216f);
        private static float ParticleRandom(ref FlameParticle particle) {
            uint state = particle.randomState;
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            particle.randomState = state;
            return (state >> 8) * (1f / 16777216f);
        }
        private int RandomInt(int min, int max) => min + (int)(RandomBits() % (uint)(max - min + 1));
        private static float Clamp01(float value) => value < 0 ? 0 : value > 1 ? 1 : value;
        private void Add(FlameParticle particle) {
            if (freeCount == 0) return;
            particles[free[--freeCount]] = particle;
            ParticleCount++;
            if (particle.kind == FlameParticleKind.Ember) EmberCount++;
        }
        private void Kill(int index) {
            if (particles[index].kind == FlameParticleKind.Ember) EmberCount--;
            particles[index].kind = FlameParticleKind.None;
            free[freeCount++] = index;
            ParticleCount--;
        }

        private void Emit(FlamePose previous, FlamePose current, FlameSimSettings settings) {
            int count = Math.Max(1, Math.Min(Capacity, settings.slugLength));
            float angleDelta = current.angle - previous.angle;
            while (angleDelta > Math.PI) angleDelta -= (float)(Math.PI * 2);
            while (angleDelta < -Math.PI) angleDelta += (float)(Math.PI * 2);
            FlameVector inherited = current.position - previous.position;
            float inheritedSpeed = inherited.Length;
            if (inheritedSpeed > Math.Max(0, settings.maxInheritedSpeed))
                inherited = inherited.Normalized * Math.Max(0, settings.maxInheritedSpeed);
            inherited = inherited * Math.Max(0, settings.inheritVelocity);
            for (int i = 0; i < count && freeCount > 0; i++) {
                float t = (float)i / count;
                float angle = previous.angle + angleDelta * t;
                var dir = new FlameVector((float)Math.Cos(angle), (float)Math.Sin(angle));
                var perp = new FlameVector(-dir.y, dir.x);
                int jitter = Math.Max(0, settings.coreWidthJitter);
                FlameVector pos = previous.position + (current.position - previous.position) * t
                    + dir * i + perp * (jitter > 0 ? RandomInt(-jitter, jitter) : 0);
                float range = Math.Max(1, settings.maxRange);
                int life = Math.Max(2, (int)Math.Ceiling(range / Math.Max(0.1f, settings.jetSpeed) * (0.94f + Random01() * 0.12f)));
                Add(new FlameParticle {
                    kind = FlameParticleKind.Core, pos = pos, previousPos = pos, dir = dir,
                    vel = dir * Math.Max(0.1f, settings.jetSpeed) + inherited, life = life,
                    heat0 = 1, maxDistance = range, phase = (Random01() - 0.5f) * 0.8f, randomState = RandomBits()
                });
            }
        }

        private static float Heat(FlameParticle particle, FlameSimSettings settings) {
            float remaining = Clamp01(1 - (float)particle.age / Math.Max(1, particle.life));
            if (particle.kind == FlameParticleKind.Core)
                remaining = (float)Math.Pow(remaining, Math.Max(0.1f, settings.colorFalloff));
            return Clamp01(particle.heat0 * remaining - particle.cooling);
        }

        private void Embers(FlameParticle parent, FlameSimSettings settings) {
            int count = RandomInt(1, 3);
            for (int i = 0; i < count && EmberCount < Math.Max(0, settings.maxEmbers) && freeCount > 0; i++) {
                int minLife = Math.Max(1, settings.emberLifeMin), maxLife = Math.Max(minLife, settings.emberLifeMax);
                Add(new FlameParticle {
                    kind = FlameParticleKind.Ember, pos = parent.pos, previousPos = parent.pos,
                    vel = parent.vel * 0.3f + new FlameVector((Random01() - 0.5f) * 0.2f, -0.2f + Random01() * 0.6f),
                    life = RandomInt(minLife, maxLife), heat0 = Clamp01(settings.emberHeat), dir = parent.dir
                });
            }
        }

        private void Shed(FlameParticle parent, FlameSimSettings settings) {
            float distance = Clamp01(parent.dist / parent.maxDistance);
            float probability = Clamp01(settings.shedChance) * distance * distance * (3 - 2 * distance);
            if (freeCount == 0 || Random01() >= probability) return;
            FlameVector forward = parent.vel.Normalized;
            FlameVector perp = new FlameVector(-forward.y, forward.x);
            int side = Math.Sin(parent.dist * settings.flapWaveNumber - StepIndex * settings.flapFreq + parent.phase) < 0 ? -1 : 1;
            FlameVector pos = parent.pos + perp * (side * RandomInt(1, Math.Max(1, settings.wingOffsetMax)));
            int minLife = Math.Max(1, settings.wingLifeMin), maxLife = Math.Max(minLife, settings.wingLifeMax);
            Add(new FlameParticle {
                kind = FlameParticleKind.Wing, pos = pos, previousPos = pos, side = side, dir = forward,
                vel = forward * settings.wingForward + new FlameVector(0, settings.wingLift * (0.5f + Random01() * 0.5f)),
                life = RandomInt(minLife, maxLife), heat0 = Clamp01(settings.wingHeat)
            });
        }

        public void Step(bool firing, FlamePose previousPose, FlamePose currentPose, FlameSimSettings settings, IFlameCollision collision = null) {
            StepIndex++;
            rasterPose = currentPose;
            rasterFiring = firing;
            if (firing) {
                if (pulseCountdown <= 0) { Emit(previousPose, currentPose, settings); pulseCountdown = Math.Max(1, settings.pulsePeriod); }
                pulseCountdown--;
            } else pulseCountdown = 0;
            for (int i = 0; i < Capacity; i++) {
                var particle = particles[i];
                if (particle.kind != FlameParticleKind.Core) continue;
                particle.previousPos = particle.pos;
                FlameVector forward = particle.vel.Normalized;
                FlameVector displacement = particle.vel;
                float length = displacement.Length;
                float available = particle.maxDistance - particle.dist;
                if (length > available) { displacement = displacement * (available / Math.Max(0.0001f, length)); length = available; }
                FlameVector destination = particle.pos + displacement;
                // New emissions may change the shared spawn RNG's consumption. Each core
                // owns its jitter stream so even that cannot redirect an old particle.
                if (ParticleRandom(ref particle) < Clamp01(settings.coreJitterChance))
                    destination = destination + new FlameVector(-forward.y, forward.x) * (ParticleRandom(ref particle) < 0.5f ? -1 : 1);
                if (collision != null && collision.HitsSolid(particle.pos, destination)) { Kill(i); continue; }
                particle.pos = destination;
                particle.dist += length;
                particle.vel.y -= settings.coreDroop;
                particle.age++;
                if (!firing) particle.cooling += Math.Max(0, settings.releaseCooling * settings.stepSeconds);
                if (particle.age >= particle.life || particle.dist >= particle.maxDistance) { Kill(i); Embers(particle, settings); continue; }
                if (Heat(particle, settings) <= 0) { Kill(i); continue; }
                particles[i] = particle;
                Shed(particle, settings);
            }
            for (int i = 0; i < Capacity; i++) {
                var particle = particles[i];
                if (particle.kind != FlameParticleKind.Wing && particle.kind != FlameParticleKind.Ember) continue;
                particle.previousPos = particle.pos;
                if (particle.kind == FlameParticleKind.Wing) {
                    particle.vel = particle.vel - particle.dir * (settings.backDrag * particle.age / Math.Max(1, particle.life));
                    particle.vel.y += settings.liftAccel;
                    if (particle.age < settings.wingSpreadSteps)
                        particle.vel = particle.vel + new FlameVector(-particle.dir.y, particle.dir.x) * (particle.side * settings.wingSpread);
                } else particle.vel.y -= settings.emberGravity;
                FlameVector destination = particle.pos + particle.vel;
                if (collision != null && collision.HitsSolid(particle.pos, destination)) { Kill(i); continue; }
                particle.pos = destination;
                particle.age++;
                if (!firing) particle.cooling += Math.Max(0, settings.releaseCooling * settings.stepSeconds);
                if (particle.age >= particle.life || Heat(particle, settings) <= 0) { Kill(i); continue; }
                particles[i] = particle;
            }
            Rasterize(currentPose.position, settings, collision);
        }

        public void Rasterize(FlameVector fallback, FlameSimSettings settings, IFlameCollision collision = null) {
            for (int i = 0; i < OccupiedCount; i++) heat[occupied[i]] = 0;
            OccupiedCount = 0;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < Capacity; i++) {
                var particle = particles[i];
                if (particle.kind == FlameParticleKind.None) continue;
                minX = Math.Min(minX, particle.pos.x); minY = Math.Min(minY, particle.pos.y);
                maxX = Math.Max(maxX, particle.pos.x); maxY = Math.Max(maxY, particle.pos.y);
                if (particle.kind == FlameParticleKind.Core) {
                    minX = Math.Min(minX, particle.previousPos.x); minY = Math.Min(minY, particle.previousPos.y);
                    maxX = Math.Max(maxX, particle.previousPos.x); maxY = Math.Max(maxY, particle.previousPos.y);
                }
            }
            float centerX = ParticleCount == 0 ? fallback.x : (minX + maxX) * 0.5f;
            float centerY = ParticleCount == 0 ? fallback.y : (minY + maxY) * 0.5f;
            if (rasterFiring && settings.coreRibbon && ParticleCount > 0) {
                centerX = (Math.Min(minX, rasterPose.position.x) + Math.Max(maxX, rasterPose.position.x)) * 0.5f;
                centerY = (Math.Min(minY, rasterPose.position.y) + Math.Max(maxY, rasterPose.position.y)) * 0.5f;
            }
            OriginX = (int)Math.Floor(centerX - WindowSize * 0.5f);
            OriginY = (int)Math.Floor(centerY - WindowSize * 0.5f);
            if (settings.coreRibbon) RasterizeCoreRibbon(settings, collision);
            for (int i = 0; i < Capacity; i++) {
                var particle = particles[i];
                if (particle.kind == FlameParticleKind.None) continue;
                if (settings.coreRibbon && particle.kind == FlameParticleKind.Core) continue;
                float value = Heat(particle, settings);
                float radius = particle.kind == FlameParticleKind.Core ? settings.coreRadius
                    : particle.kind == FlameParticleKind.Wing ? settings.wingRadius : settings.emberRadius;
                if (particle.kind == FlameParticleKind.Core) {
                    FlameVector delta = particle.pos - particle.previousPos;
                    int count = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(delta.x), Math.Abs(delta.y))));
                    for (int j = 0; j <= count; j++) Stamp(particle.previousPos + delta * ((float)j / count), radius, value, settings.radialFalloff, collision);
                } else if (particle.kind == FlameParticleKind.Wing && settings.wingTrail > 0) {
                    FlameVector start = particle.pos + (particle.previousPos - particle.pos) * Clamp01(settings.wingTrail);
                    StampSegment(start, particle.pos, radius * 0.5f, radius, value * 0.4f, value, settings.radialFalloff, collision);
                } else Stamp(particle.pos, radius, value, settings.radialFalloff, collision);
            }
            if (collision != null) {
                for (int i = 0; i < OccupiedCount; i++) {
                    int cell = occupied[i];
                    if (collision.CellSolid(OriginX + cell % WindowSize, OriginY + cell / WindowSize)) heat[cell] = 0;
                }
            }
        }

        private void AddRibbonPoint(FlameVector position, float value, float radius, int age) {
            ribbonPoints[ribbonCount] = position; ribbonHeat[ribbonCount] = value;
            ribbonRadius[ribbonCount] = radius; ribbonAges[ribbonCount++] = age;
        }

        private void RasterizeCoreRibbon(FlameSimSettings settings, IFlameCollision collision) {
            int cohorts = 0;
            // Samples born on the same tick are one cross-section of the stream,
            // not ten unrelated projectile heads. Average their world-space positions.
            for (int i = 0; i < Capacity; i++) {
                var p = particles[i];
                if (p.kind != FlameParticleKind.Core) continue;
                int group = 0;
                while (group < cohorts && cohortAges[group] != p.age) group++;
                if (group == cohorts) {
                    cohortAges[group] = p.age; cohortPositions[group] = default;
                    cohortHeat[group] = cohortDistance[group] = 0; cohortCounts[group] = 0;
                    cohortOrder[group] = group; cohorts++;
                }
                cohortPositions[group] = cohortPositions[group] + p.pos;
                cohortHeat[group] += Heat(p, settings);
                cohortDistance[group] += Clamp01(p.dist / p.maxDistance);
                cohortCounts[group]++;
            }
            // Pool slots are recycled; chronological order must not depend on slot order.
            for (int i = 1; i < cohorts; i++) {
                int item = cohortOrder[i], j = i - 1;
                while (j >= 0 && cohortAges[cohortOrder[j]] > cohortAges[item]) {
                    cohortOrder[j + 1] = cohortOrder[j]; j--;
                }
                cohortOrder[j + 1] = item;
            }
            ribbonCount = 0;
            float coreRadius = settings.coreRadius * Math.Max(0.25f, settings.ribbonWidthScale);
            if (rasterFiring) {
                var forward = new FlameVector((float)Math.Cos(rasterPose.angle), (float)Math.Sin(rasterPose.angle));
                AddRibbonPoint(rasterPose.position, 1, coreRadius, -1);
                AddRibbonPoint(rasterPose.position + forward * (Math.Max(0.1f, settings.jetSpeed) * 0.4f),
                    1, coreRadius, 0);
            }
            for (int i = 0; i < cohorts; i++) {
                int group = cohortOrder[i];
                float inverse = 1f / cohortCounts[group];
                float radius = coreRadius * (1 - Clamp01(settings.ribbonTipTaper) * cohortDistance[group] * inverse);
                AddRibbonPoint(cohortPositions[group] * inverse, cohortHeat[group] * inverse, radius, cohortAges[group]);
            }
            if (ribbonCount == 1) Stamp(ribbonPoints[0], ribbonRadius[0], ribbonHeat[0], settings.radialFalloff, collision);
            float linkLimit = Math.Max(1, settings.maxRange) * 2 + Math.Max(0, settings.maxInheritedSpeed);
            int allowedAgeGap = Math.Max(1, settings.pulsePeriod);
            for (int i = 0; i + 1 < ribbonCount; i++) {
                // Missing cohorts can mean collision/expiry: never bridge such holes or
                // connect across a teleport. Smooth only a contiguous emission history.
                if (ribbonAges[i + 1] - ribbonAges[i] > allowedAgeGap
                    || (ribbonPoints[i + 1] - ribbonPoints[i]).Length > linkLimit) continue;
                FlameVector a = ribbonPoints[i], b = ribbonPoints[i + 1];
                FlameVector before = i == 0 ? a + (a - b) : ribbonPoints[i - 1];
                FlameVector after = i + 2 >= ribbonCount ? b + (b - a) : ribbonPoints[i + 2];
                float smooth = Clamp01(settings.ribbonSmoothness);
                // Cubic Bezier form of a Catmull-Rom spline, blended with the straight
                // chord. Geometry and heat are rasterized together for matching damage.
                FlameVector c1 = a + (b - a) * ((1 - smooth) / 3) + (b - before) * (smooth / 6);
                FlameVector c2 = b - (b - a) * ((1 - smooth) / 3) - (after - a) * (smooth / 6);
                int samples = Math.Max(1, Math.Min(WindowSize * 4,
                    (int)Math.Ceiling(((c1 - a).Length + (c2 - c1).Length + (b - c2).Length) * 0.5f)));
                // Cache the polynomial once per span; Horner evaluation avoids creating
                // and adding four vector terms at every raster sample.
                FlameVector linear = (c1 - a) * 3;
                FlameVector quadratic = (a - c1 * 2 + c2) * 3;
                FlameVector cubic = b - a - linear - quadratic;
                float inverseSamples = 1f / samples;
                float heatDelta = ribbonHeat[i + 1] - ribbonHeat[i];
                float radiusDelta = ribbonRadius[i + 1] - ribbonRadius[i];
                FlameVector last = a;
                float lastValue = ribbonHeat[i], lastRadius = ribbonRadius[i];
                if (collision != null && collision.HitsSolid(a, a)) continue;
                Stamp(a, ribbonRadius[i], ribbonHeat[i], settings.radialFalloff, collision);
                for (int j = 1; j <= samples; j++) {
                    float t = j * inverseSamples;
                    FlameVector point = j == samples ? b : new FlameVector(
                        ((cubic.x * t + quadratic.x) * t + linear.x) * t + a.x,
                        ((cubic.y * t + quadratic.y) * t + linear.y) * t + a.y);
                    if (collision != null && collision.HitsSolid(last, point)) break;
                    float value = ribbonHeat[i] + heatDelta * t;
                    float radius = ribbonRadius[i] + radiusDelta * t;
                    // Flatten the curve in short spans, then rasterize every intervening
                    // pixel. This avoids expensive cubic evaluation for duplicate pixels.
                    StampSegment(last, point, lastRadius, radius, lastValue, value, settings.radialFalloff, collision, false, true);
                    last = point;
                    lastValue = value; lastRadius = radius;
                }
            }
        }

        private void StampSegment(FlameVector from, FlameVector to, float fromRadius, float toRadius,
            float fromHeat, float toHeat, float falloff, IFlameCollision collision, bool includeStart = true, bool pathChecked = false) {
            FlameVector delta = to - from;
            int count = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(delta.x), Math.Abs(delta.y))));
            FlameVector last = from;
            for (int i = includeStart ? 0 : 1; i <= count; i++) {
                float t = (float)i / count;
                FlameVector point = from + delta * t;
                if (!pathChecked && collision != null && collision.HitsSolid(last, point)) break;
                Stamp(point, fromRadius + (toRadius - fromRadius) * t, fromHeat + (toHeat - fromHeat) * t, falloff, collision);
                last = point;
            }
        }

        private void Stamp(FlameVector position, float radius, float value, float falloff, IFlameCollision collision) {
            int cx = (int)Math.Floor(position.x) - OriginX, cy = (int)Math.Floor(position.y) - OriginY;
            radius = Math.Max(0, Math.Min(8, radius));
            float squared = radius * radius + 0.25f;
            // The disk's actual integer footprint is smaller than ceil(radius).
            // In particular, subpixel wisps occupy one cell, not a 3x3 search.
            int extent = (int)radius;
            if ((extent + 1) * (extent + 1) <= squared) extent++;
            for (int dy = -extent; dy <= extent; dy++) for (int dx = -extent; dx <= extent; dx++) {
                int x = cx + dx, y = cy + dy;
                int distance = dx * dx + dy * dy;
                if (x < 0 || y < 0 || x >= WindowSize || y >= WindowSize || distance > squared) continue;
                float sample = value * Math.Max(0, 1 - Math.Max(0, falloff) * distance / squared);
                int index = y * WindowSize + x;
                if (sample <= heat[index]) continue;
                // A thick stamp must not bleed across a one-pixel wall either.
                if (collision != null && (dx != 0 || dy != 0)
                    && collision.HitsSolid(position, new FlameVector(OriginX + x + 0.5f, OriginY + y + 0.5f))) continue;
                if (heat[index] == 0) occupied[OccupiedCount++] = index;
                heat[index] = sample;
            }
        }

        public float SampleWorld(float worldPixelX, float worldPixelY) {
            int x = (int)Math.Floor(worldPixelX) - OriginX, y = (int)Math.Floor(worldPixelY) - OriginY;
            return x < 0 || y < 0 || x >= WindowSize || y >= WindowSize ? 0 : heat[y * WindowSize + x];
        }
    }
}
