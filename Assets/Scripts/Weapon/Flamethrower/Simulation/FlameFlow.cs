using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Omnia.Fire {
    // Persistent Eulerian heat + velocity lattice. All updates use neighboring cells;
    // there are no eddy particles, curve stamps, vortex centers or prescribed outlines.
    public sealed class FlameFlow {
        private readonly int size;
        private readonly int velocityCellSize;
        private readonly FlameFlow coarse;
        private readonly float[] coarseSourceHeat, coarseSourceWeight;
        private readonly FlameVector[] coarseSourceVelocity;
        private readonly CoarseCollision coarseCollision;
        private sealed class CoarseCollision : IFlameCollision {
            public IFlameCollision source;
            public int scale;
            public bool CellSolid(int x, int y) {
                for (int dy = 0; dy < scale; dy++) for (int dx = 0; dx < scale; dx++)
                    if (source.CellSolid(x * scale + dx, y * scale + dy)) return true;
                return false;
            }
            public bool HitsSolid(FlameVector a, FlameVector b) => source.HitsSolid(a * scale, b * scale);
        }
        private static int FloorDivide(int value, int divisor) => (int)Math.Floor((double)value / divisor);
        private readonly uint seed;
        private float[] heat, u, v, nextHeat, nextU, nextV;
        private readonly float[] curl, divergence, transported;
        private float[] pressure, pressureNext;
        // Track writes by array identity because transport and pressure swap buffers.
        // Registration happens once; ticks do not allocate dictionary entries.
        private readonly Dictionary<float[], FlameBounds> written = new Dictionary<float[], FlameBounds>(11);
        private readonly bool[] solid;
        private bool hasSolids;
        private int originX, originY, left, right, bottom, top;
        public bool ProfileTiming { get; set; }
        public long SourceTicks { get; private set; }
        public long VelocityTicks { get; private set; }
        public long HeatTicks { get; private set; }
        public bool HasHeat { get; private set; }
        public int MinWorldX { get; private set; }
        public int MaxWorldX { get; private set; }
        public int MinWorldY { get; private set; }
        public int MaxWorldY { get; private set; }
        public float CellHeat(int cell) => heat[cell];
        public FlameVector CellVelocity(int cell) => new FlameVector(u[cell], v[cell]);

        public FlameFlow(int windowSize, uint randomSeed, int velocityResolution = 2) {
            FlameKernels.WarmUp();
            size = windowSize; seed = randomSeed;
            velocityCellSize = Math.Max(1, Math.Min(4, velocityResolution));
            if (velocityCellSize > 1 && size >= 16) {
                coarse = new FlameFlow((size + velocityCellSize - 1) / velocityCellSize + 1, seed, 1);
                coarseSourceHeat = new float[coarse.size * coarse.size];
                coarseSourceWeight = new float[coarseSourceHeat.Length];
                coarseSourceVelocity = new FlameVector[coarseSourceHeat.Length];
                coarseCollision = new CoarseCollision { scale = velocityCellSize };
            }
            int n = size * size;
            heat = new float[n]; u = new float[n]; v = new float[n];
            nextHeat = new float[n]; nextU = new float[n]; nextV = new float[n];
            curl = new float[n]; divergence = new float[n];
            pressure = new float[n]; pressureNext = new float[n]; transported = new float[n];
            solid = new bool[n];
            foreach (var field in new[] { heat, u, v, nextHeat, nextU, nextV, curl, divergence, pressure, pressureNext, transported })
                written.Add(field, FlameBounds.Empty);
        }
        public void Clear() {
            ClearField(heat); ClearField(u); ClearField(v);
            HasHeat = false;
            coarse?.Clear();
        }
        private static void Swap(ref float[] a, ref float[] b) { var t = a; a = b; b = t; }
        private static float Clamp(float x, float low, float high) => x < low ? low : x > high ? high : x;
        private float Noise(int x, int y, int time) {
            uint h;
            unchecked {
                h = (uint)x * 73856093u ^ (uint)y * 19349663u ^ (uint)time * 83492791u ^ seed;
                h ^= h >> 13; h *= 1274126177u; h ^= h >> 16;
            }
            return (h & 65535) / 32767.5f - 1;
        }
        private FlameBounds ActiveBounds => new FlameBounds { left = left, right = right, bottom = bottom, top = top };
        private void MarkField(float[] field, FlameBounds bounds) {
            if (bounds.right < bounds.left || bounds.top < bounds.bottom) return;
            var prior = written[field];
            prior.Include(bounds.left, bounds.bottom); prior.Include(bounds.right, bounds.top);
            written[field] = prior;
        }
        private void ClearField(float[] field) {
            var bounds = written[field];
            for (int y = bounds.bottom; y <= bounds.top; y++)
                Array.Clear(field, y * size + bounds.left, bounds.right - bounds.left + 1);
            written[field] = FlameBounds.Empty;
        }
        private void ShiftField(float[] source, float[] destination, int dx, int dy) {
            ClearField(destination);
            var bounds = written[source];
            if (bounds.right < bounds.left || bounds.top < bounds.bottom) return;
            bounds.left = Math.Max(0, bounds.left - dx); bounds.right = Math.Min(size - 1, bounds.right - dx);
            bounds.bottom = Math.Max(0, bounds.bottom - dy); bounds.top = Math.Min(size - 1, bounds.top - dy);
            if (bounds.right < bounds.left || bounds.top < bounds.bottom) return;
            for (int row = bounds.bottom; row <= bounds.top; row++)
                Array.Copy(source, (row + dy) * size + bounds.left + dx, destination, row * size + bounds.left, bounds.right - bounds.left + 1);
            MarkField(destination, bounds);
        }
        internal void Recenter(int x, int y, bool shiftVelocity = true) {
            int dx = x - originX, dy = y - originY;
            if (dx == 0 && dy == 0) return;
            ShiftField(heat, nextHeat, dx, dy); Swap(ref heat, ref nextHeat);
            // StepCoarse reconstructs fine velocity immediately after recentering.
            if (shiftVelocity) {
                ShiftField(u, nextU, dx, dy); ShiftField(v, nextV, dx, dy);
                Swap(ref u, ref nextU); Swap(ref v, ref nextV);
            }
            originX = x; originY = y;
        }
        public void Step(int worldX, int worldY, float[] sourceHeat, FlameVector[] sourceVelocity,
            FlameSimSettings settings, bool firing, int tick, IFlameCollision collision, FlameBounds? sourceBounds = null) {
            SourceTicks = VelocityTicks = HeatTicks = 0;
            if (coarse != null) {
                StepCoarse(worldX, worldY, sourceHeat, sourceVelocity, settings, firing, tick, collision, sourceBounds);
                return;
            }
            StepFine(worldX, worldY, sourceHeat, sourceVelocity, settings, firing, tick, collision, sourceBounds);
        }
        private void StepFine(int worldX, int worldY, float[] sourceHeat, FlameVector[] sourceVelocity,
            FlameSimSettings settings, bool firing, int tick, IFlameCollision collision, FlameBounds? sourceBounds = null) {
            Recenter(worldX, worldY);
            if (size < 4) { Clear(); return; }
            left = bottom = size; right = top = -1;
            var search = SearchBounds(sourceBounds);
            for (int y = search.bottom; y <= search.top; y++) for (int x = search.left; x <= search.right; x++) {
                int i = y * size + x;
                if (heat[i] < 0.015f && sourceHeat[i] <= 0) continue;
                left = Math.Min(left, x); right = Math.Max(right, x); bottom = Math.Min(bottom, y); top = Math.Max(top, y);
            }
            if (right < left) { Clear(); return; }
            left = Math.Max(1, left - 12); right = Math.Min(size - 2, right + 12);
            bottom = Math.Max(1, bottom - 12); top = Math.Min(size - 2, top + 12);
            Array.Clear(solid, 0, solid.Length);
            hasSolids = false;
            if (collision != null)
                for (int y = bottom - 1; y <= top + 1; y++) for (int x = left - 1; x <= right + 1; x++) {
                    bool blocked = collision.CellSolid(originX + x, originY + y);
                    solid[y * size + x] = blocked; hasSolids |= blocked;
                }
            MarkField(heat, ActiveBounds); MarkField(u, ActiveBounds); MarkField(v, ActiveBounds);
            // The beam is a heat/momentum boundary condition. Small seeded disturbances
            // break perfect symmetry; the neighbor solver, not that noise, determines the rolls.
            for (int y = bottom; y <= top; y++) for (int x = left; x <= right; x++) {
                int i = y * size + x;
                if (solid[i]) { heat[i] = u[i] = v[i] = 0; continue; }
                float h = sourceHeat[i]; var velocity = sourceVelocity[i];
                if (h > 0 && (velocity.x != 0 || velocity.y != 0)) {
                    float speed = Math.Max(0.001f, settings.flowSpeed);
                    float disturbance = Noise((originX + x) / 4, (originY + y) / 4, tick / 3) * settings.flowPerturbation
                        * (1 - Clamp(settings.flowJetStability, 0, 1) * h);
                    float blend = 0.45f * h;
                    u[i] += (velocity.x - velocity.y / speed * disturbance - u[i]) * blend;
                    v[i] += (velocity.y + velocity.x / speed * disturbance - v[i]) * blend;
                    heat[i] = Math.Max(heat[i], h * settings.flowHeat);
                }
            }
            const int substeps = 2;
            const float dt = 1f / substeps;
            for (int sub = 0; sub < substeps; sub++) {
                ClearField(nextU); ClearField(nextV);
                MarkField(nextU, ActiveBounds); MarkField(nextV, ActiveBounds);
                AdvectVelocity(settings, dt);
                Swap(ref u, ref nextU); Swap(ref v, ref nextV);
                // Curl is generated by the fast jet bordering slow air. Confinement
                // restores rotational energy otherwise lost to coarse-grid advection.
                ClearField(curl); MarkField(curl, ActiveBounds);
                ConfineVelocity(sourceHeat, settings, dt);
                Project(Math.Max(2, Math.Min(24, settings.flowPressureIterations)));
                TransportHeat(settings, firing, dt);
            }
        }
        private unsafe FlameKernels.Grid Grid() => new FlameKernels.Grid {
            size = size, originX = originX, originY = originY,
            left = left, right = right, bottom = bottom, top = top, hasSolids = hasSolids ? 1 : 0
        };
        private unsafe void AdvectVelocity(FlameSimSettings settings, float dt) {
            fixed (float* h = heat, a = u, b = v, na = nextU, nb = nextV)
            fixed (bool* mask = solid) {
                var grid = Grid(); grid.heat = h; grid.u = a; grid.v = b;
                grid.nextU = na; grid.nextV = nb; grid.solid = mask;
                var rules = new FlameKernels.FlowSettings(settings);
                FlameKernels.Velocity(ref grid, in rules, dt);
            }
        }
        private unsafe void ConfineVelocity(float[] sourceHeat, FlameSimSettings settings, float dt) {
            fixed (float* h = heat, a = u, b = v, c = curl, source = sourceHeat)
            fixed (bool* mask = solid) {
                var grid = Grid(); grid.heat = h; grid.u = a; grid.v = b; grid.curl = c; grid.solid = mask;
                var rules = new FlameKernels.FlowSettings(settings);
                FlameKernels.Confinement(ref grid, source, in rules, dt);
            }
        }
        private unsafe void TransportHeat(FlameSimSettings settings, bool firing, float dt) {
            ClearField(transported); ClearField(nextHeat);
            MarkField(transported, ActiveBounds); MarkField(nextHeat, ActiveBounds);
            var bounds = FlameBounds.Empty;
            fixed (float* h = heat, a = u, b = v, next = nextHeat, temp = transported)
            fixed (bool* mask = solid) {
                var grid = Grid(); grid.heat = h; grid.u = a; grid.v = b;
                grid.nextHeat = next; grid.transported = temp; grid.solid = mask;
                var rules = new FlameKernels.FlowSettings(settings);
                FlameKernels.Heat(ref grid, in rules, firing ? 1 : 0, dt, ref bounds);
            }
            Swap(ref heat, ref nextHeat);
            HasHeat = bounds.right >= bounds.left;
            MinWorldX = HasHeat ? originX + bounds.left : int.MaxValue;
            MaxWorldX = HasHeat ? originX + bounds.right : int.MinValue;
            MinWorldY = HasHeat ? originY + bounds.bottom : int.MaxValue;
            MaxWorldY = HasHeat ? originY + bounds.top : int.MinValue;
        }
        private void StepCoarse(int worldX, int worldY, float[] sourceHeat, FlameVector[] sourceVelocity,
            FlameSimSettings settings, bool firing, int tick, IFlameCollision collision, FlameBounds? sourceBounds = null) {
            long started = ProfileTiming ? Stopwatch.GetTimestamp() : 0;
            Recenter(worldX, worldY, false);
            int scale = velocityCellSize;
            int cx = FloorDivide(worldX, scale), cy = FloorDivide(worldY, scale);
            Array.Clear(coarseSourceHeat, 0, coarseSourceHeat.Length);
            Array.Clear(coarseSourceWeight, 0, coarseSourceWeight.Length);
            Array.Clear(coarseSourceVelocity, 0, coarseSourceVelocity.Length);
            left = bottom = size; right = top = -1;
            var coarseBounds = FlameBounds.Empty;
            var search = SearchBounds(sourceBounds);
            MarkField(heat, search);
            for (int y = search.bottom; y <= search.top; y++) for (int x = search.left; x <= search.right; x++) {
                int i = y * size + x;
                float h = sourceHeat[i]; var velocity = sourceVelocity[i];
                if (h > 0 && (velocity.x != 0 || velocity.y != 0)) {
                    int sourceX = FloorDivide(worldX + x, scale) - cx, sourceY = FloorDivide(worldY + y, scale) - cy;
                    int cell = sourceY * coarse.size + sourceX;
                    coarseBounds.Include(sourceX, sourceY);
                    coarseSourceHeat[cell] = Math.Max(coarseSourceHeat[cell], h);
                    coarseSourceVelocity[cell] = coarseSourceVelocity[cell] + velocity * h;
                    coarseSourceWeight[cell] += h;
                    heat[i] = Math.Max(heat[i], h * settings.flowHeat);
                }
                if (heat[i] < 0.015f) continue;
                left = Math.Min(left, x); right = Math.Max(right, x); bottom = Math.Min(bottom, y); top = Math.Max(top, y);
            }
            for (int i = 0; i < coarseSourceHeat.Length; i++)
                if (coarseSourceWeight[i] > 0) coarseSourceVelocity[i] = coarseSourceVelocity[i] * (1f / (scale * coarseSourceWeight[i]));
            long velocityStarted = ProfileTiming ? Stopwatch.GetTimestamp() : 0;
            SourceTicks = velocityStarted - started;
            var rules = settings;
            rules.flowSpeed /= scale; rules.flowBuoyancy /= scale; rules.flowPerturbation /= scale;
            coarseCollision.source = collision;
            coarse.StepFine(cx, cy, coarseSourceHeat, coarseSourceVelocity, rules, firing, tick, collision == null ? null : coarseCollision, coarseBounds);
            if (right < left) { Clear(); return; }
            left = Math.Max(1, left - 16); right = Math.Min(size - 2, right + 16);
            bottom = Math.Max(1, bottom - 16); top = Math.Min(size - 2, top + 16);
            Array.Clear(solid, 0, solid.Length); hasSolids = false;
            ClearField(u); ClearField(v);
            var velocityBounds = new FlameBounds { left = left - 1, right = right + 1, bottom = bottom - 1, top = top + 1 };
            MarkField(u, velocityBounds); MarkField(v, velocityBounds);
            for (int y = bottom - 1; y <= top + 1; y++) for (int x = left - 1; x <= right + 1; x++) {
                int i = y * size + x;
                bool blocked = collision != null && collision.CellSolid(worldX + x, worldY + y);
                solid[i] = blocked; hasSolids |= blocked;
            }
            UpsampleVelocity(scale);
            long heatStarted = ProfileTiming ? Stopwatch.GetTimestamp() : 0;
            VelocityTicks = heatStarted - velocityStarted;
            TransportHeat(settings, firing, 1);
            HeatTicks = ProfileTiming ? Stopwatch.GetTimestamp() - heatStarted : 0;
        }
        private FlameBounds SearchBounds(FlameBounds? sources) {
            if (!sources.HasValue) return new FlameBounds { left = 1, bottom = 1, right = size - 2, top = size - 2 };
            var bounds = sources.Value;
            if (HasHeat) {
                bounds.Include(MinWorldX - originX, MinWorldY - originY);
                bounds.Include(MaxWorldX - originX, MaxWorldY - originY);
            }
            bounds.left = Math.Max(1, bounds.left); bounds.right = Math.Min(size - 2, bounds.right);
            bounds.bottom = Math.Max(1, bounds.bottom); bounds.top = Math.Min(size - 2, bounds.top);
            return bounds;
        }

        private unsafe void UpsampleVelocity(int scale) {
            fixed (float* h = heat, a = u, b = v, ca = coarse.u, cb = coarse.v)
            fixed (bool* mask = solid, coarseMask = coarse.solid) {
                var grid = Grid(); grid.heat = h; grid.u = a; grid.v = b; grid.solid = mask;
                var coarseGrid = coarse.Grid(); coarseGrid.u = ca; coarseGrid.v = cb; coarseGrid.solid = coarseMask;
                FlameKernels.Upsample(ref grid, ref coarseGrid, scale);
            }
        }
        private unsafe void Project(int iterations) {
            ClearField(pressure); ClearField(pressureNext);
            MarkField(pressure, ActiveBounds); MarkField(pressureNext, ActiveBounds);
            MarkField(divergence, ActiveBounds);
            fixed (float* a = u, b = v, p = pressure, pn = pressureNext, d = divergence)
            fixed (bool* mask = solid) {
                var grid = Grid(); grid.u = a; grid.v = b; grid.pressure = p;
                grid.pressureNext = pn; grid.divergence = d; grid.solid = mask;
                FlameKernels.Project(ref grid, iterations);
            }
            if ((iterations & 1) != 0) Swap(ref pressure, ref pressureNext);
        }
    }
}
