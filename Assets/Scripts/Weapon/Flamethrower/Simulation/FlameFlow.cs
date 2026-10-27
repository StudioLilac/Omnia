using System;

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
        private readonly bool[] solid;
        private bool hasSolids;
        private int originX, originY, left, right, bottom, top;
        public bool HasHeat { get; private set; }
        public int MinWorldX { get; private set; }
        public int MaxWorldX { get; private set; }
        public int MinWorldY { get; private set; }
        public int MaxWorldY { get; private set; }
        public float CellHeat(int cell) => heat[cell];
        public FlameVector CellVelocity(int cell) => new FlameVector(u[cell], v[cell]);

        public FlameFlow(int windowSize, uint randomSeed, int velocityResolution = 2) {
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
        }
        public void Clear() {
            Array.Clear(heat, 0, heat.Length); Array.Clear(u, 0, u.Length); Array.Clear(v, 0, v.Length);
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
        internal void Recenter(int x, int y) {
            int dx = x - originX, dy = y - originY;
            if (dx == 0 && dy == 0) return;
            Array.Clear(nextHeat, 0, nextHeat.Length); Array.Clear(nextU, 0, nextU.Length); Array.Clear(nextV, 0, nextV.Length);
            for (int row = Math.Max(0, -dy); row < Math.Min(size, size - dy); row++) {
                int start = Math.Max(0, -dx), length = Math.Min(size, size - dx) - start;
                if (length <= 0) continue;
                int from = (row + dy) * size + start + dx, to = row * size + start;
                Array.Copy(heat, from, nextHeat, to, length);
                Array.Copy(u, from, nextU, to, length); Array.Copy(v, from, nextV, to, length);
            }
            Swap(ref heat, ref nextHeat); Swap(ref u, ref nextU); Swap(ref v, ref nextV);
            originX = x; originY = y;
        }
        private float Sample(float[] field, float x, float y) {
            x = Clamp(x, 0, size - 1.001f); y = Clamp(y, 0, size - 1.001f);
            int ix = (int)x, iy = (int)y, a = iy * size + ix;
            float fx = x - ix, fy = y - iy;
            if (!hasSolids)
                return (field[a] + (field[a + 1] - field[a]) * fx) * (1 - fy)
                    + (field[a + size] + (field[a + size + 1] - field[a + size]) * fx) * fy;
            float aa = solid[a] ? 0 : field[a], ab = solid[a + 1] ? 0 : field[a + 1];
            float ba = solid[a + size] ? 0 : field[a + size], bb = solid[a + size + 1] ? 0 : field[a + size + 1];
            return (aa + (ab - aa) * fx) * (1 - fy) + (ba + (bb - ba) * fx) * fy;
        }
        // Check every crossed grid cell, including subpixel-wall crossings. This is
        // also used by the corrected heat transport, so it cannot reconstruct past walls.
        private bool Blocked(float x, float y, float tx, float ty) {
            if (!hasSolids) return tx < 0 || ty < 0 || tx >= size - 1 || ty >= size - 1;
            int n = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(tx - x), Math.Abs(ty - y)) * 2));
            for (int k = 0; k <= n; k++) {
                float t = (float)k / n;
                int ix = (int)Math.Floor(x + (tx - x) * t + 0.5f), iy = (int)Math.Floor(y + (ty - y) * t + 0.5f);
                if (ix < 0 || iy < 0 || ix >= size || iy >= size || solid[iy * size + ix]) return true;
            }
            return false;
        }
        public void Step(int worldX, int worldY, float[] sourceHeat, FlameVector[] sourceVelocity,
            FlameSimSettings settings, bool firing, int tick, IFlameCollision collision) {
            if (coarse != null) {
                StepCoarse(worldX, worldY, sourceHeat, sourceVelocity, settings, firing, tick, collision);
                return;
            }
            StepFine(worldX, worldY, sourceHeat, sourceVelocity, settings, firing, tick, collision);
        }
        private void StepFine(int worldX, int worldY, float[] sourceHeat, FlameVector[] sourceVelocity,
            FlameSimSettings settings, bool firing, int tick, IFlameCollision collision) {
            Recenter(worldX, worldY);
            if (size < 4) { Clear(); return; }
            left = bottom = size; right = top = -1;
            for (int y = 1; y < size - 1; y++) for (int x = 1; x < size - 1; x++) {
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
            // The beam is a heat/momentum boundary condition. Small seeded disturbances
            // break perfect symmetry; the neighbor solver, not that noise, determines the rolls.
            for (int y = bottom; y <= top; y++) for (int x = left; x <= right; x++) {
                int i = y * size + x;
                if (solid[i]) { heat[i] = u[i] = v[i] = 0; continue; }
                float h = sourceHeat[i]; var velocity = sourceVelocity[i];
                if (h > 0 && (velocity.x != 0 || velocity.y != 0)) {
                    float speed = Math.Max(0.001f, settings.flowSpeed);
                    float disturbance = Noise((originX + x) / 4, (originY + y) / 4, tick / 3) * settings.flowPerturbation;
                    float blend = 0.45f * h;
                    u[i] += (velocity.x - velocity.y / speed * disturbance - u[i]) * blend;
                    v[i] += (velocity.y + velocity.x / speed * disturbance - v[i]) * blend;
                    heat[i] = Math.Max(heat[i], h * settings.flowHeat);
                }
            }
            const int substeps = 2;
            const float dt = 1f / substeps;
            for (int sub = 0; sub < substeps; sub++) {
                Array.Clear(nextU, 0, nextU.Length); Array.Clear(nextV, 0, nextV.Length);
                // Semi-Lagrangian velocity transport with mild viscosity/damping.
                for (int y = bottom; y <= top; y++) for (int x = left; x <= right; x++) {
                    int i = y * size + x;
                    if (solid[i]) { nextU[i] = nextV[i] = 0; continue; }
                    float tx = x - u[i] * dt, ty = y - v[i] * dt;
                    bool blocked = Blocked(x, y, tx, ty);
                    nextU[i] = blocked ? 0 : Sample(u, tx, ty) * 0.995f;
                    nextV[i] = blocked ? 0 : Sample(v, tx, ty) * 0.995f + heat[i] * settings.flowBuoyancy * dt;
                }
                Swap(ref u, ref nextU); Swap(ref v, ref nextV);
                // Curl is generated by the fast jet bordering slow air. Confinement
                // restores rotational energy otherwise lost to coarse-grid advection.
                Array.Clear(curl, 0, curl.Length);
                for (int y = bottom; y <= top; y++) for (int x = left; x <= right; x++) {
                    int i = y * size + x;
                    curl[i] = solid[i] ? 0 : 0.5f * (v[i + 1] - v[i - 1] - u[i + size] + u[i - size]);
                }
                for (int y = bottom; y <= top; y++) for (int x = left; x <= right; x++) {
                    int i = y * size + x;
                    if (solid[i]) continue;
                    float nx = Math.Abs(curl[i + 1]) - Math.Abs(curl[i - 1]);
                    float ny = Math.Abs(curl[i + size]) - Math.Abs(curl[i - size]);
                    float inverse = 1f / (float)Math.Sqrt(nx * nx + ny * ny + 0.0001f);
                    float force = Math.Min(2, Math.Max(0, settings.flowVorticity)) * curl[i] * dt;
                    u[i] = Clamp(u[i] + ny * inverse * force, -6, 6);
                    v[i] = Clamp(v[i] - nx * inverse * force, -6, 6);
                }
                Project(Math.Max(2, Math.Min(24, settings.flowPressureIterations)));
                TransportHeat(settings, firing, dt);
            }
            UpdateBounds();
        }
        private void TransportHeat(FlameSimSettings settings, bool firing, float dt) {
                // Corrected heat advection preserves narrow hot filaments instead of
                // repeatedly blurring them into a soft expanding blob.
                Array.Clear(transported, 0, transported.Length);
                Array.Clear(nextHeat, 0, nextHeat.Length);
                for (int y = bottom; y <= top; y++) for (int x = left; x <= right; x++) {
                    int i = y * size + x;
                    float tx = x - u[i] * dt, ty = y - v[i] * dt;
                    transported[i] = solid[i] || Blocked(x, y, tx, ty) ? 0 : Sample(heat, tx, ty);
                }
                float cooling = Math.Max(0, settings.flowCooling)
                    + (firing ? 0 : Math.Max(0, settings.releaseCooling * settings.stepSeconds));
                for (int y = bottom; y <= top; y++) for (int x = left; x <= right; x++) {
                    int i = y * size + x;
                    if (solid[i] || transported[i] <= 0) { nextHeat[i] = 0; continue; }
                    float tx = Clamp(x - u[i] * dt, 0, size - 1.001f), ty = Clamp(y - v[i] * dt, 0, size - 1.001f);
                    int a = (int)ty * size + (int)tx;
                    float lo = Math.Min(Math.Min(heat[a], heat[a + 1]), Math.Min(heat[a + size], heat[a + size + 1]));
                    float hi = Math.Max(Math.Max(heat[a], heat[a + 1]), Math.Max(heat[a + size], heat[a + size + 1]));
                    float reverse = Sample(transported, x + u[i] * dt, y + v[i] * dt);
                    float corrected = Clamp(transported[i] + 0.5f * (heat[i] - reverse), lo, hi) - cooling * dt;
                    nextHeat[i] = corrected < 0.015f ? 0 : Math.Min(1, corrected);
                }
                Swap(ref heat, ref nextHeat);
        }
        private void UpdateBounds() {
            HasHeat = false;
            MinWorldX = MinWorldY = int.MaxValue; MaxWorldX = MaxWorldY = int.MinValue;
            for (int y = bottom; y <= top; y++) for (int x = left; x <= right; x++) {
                int i = y * size + x;
                if (heat[i] <= 0) continue;
                HasHeat = true;
                MinWorldX = Math.Min(MinWorldX, originX + x); MaxWorldX = Math.Max(MaxWorldX, originX + x);
                MinWorldY = Math.Min(MinWorldY, originY + y); MaxWorldY = Math.Max(MaxWorldY, originY + y);
            }
        }
        private void StepCoarse(int worldX, int worldY, float[] sourceHeat, FlameVector[] sourceVelocity,
            FlameSimSettings settings, bool firing, int tick, IFlameCollision collision) {
            Recenter(worldX, worldY);
            int scale = velocityCellSize;
            int cx = FloorDivide(worldX, scale), cy = FloorDivide(worldY, scale);
            Array.Clear(coarseSourceHeat, 0, coarseSourceHeat.Length);
            Array.Clear(coarseSourceWeight, 0, coarseSourceWeight.Length);
            Array.Clear(coarseSourceVelocity, 0, coarseSourceVelocity.Length);
            left = bottom = size; right = top = -1;
            for (int y = 1; y < size - 1; y++) for (int x = 1; x < size - 1; x++) {
                int i = y * size + x;
                float h = sourceHeat[i]; var velocity = sourceVelocity[i];
                if (h > 0 && (velocity.x != 0 || velocity.y != 0)) {
                    int cell = (FloorDivide(worldY + y, scale) - cy) * coarse.size + FloorDivide(worldX + x, scale) - cx;
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
            var rules = settings;
            rules.flowSpeed /= scale; rules.flowBuoyancy /= scale; rules.flowPerturbation /= scale;
            coarseCollision.source = collision;
            coarse.StepFine(cx, cy, coarseSourceHeat, coarseSourceVelocity, rules, firing, tick, collision == null ? null : coarseCollision);
            if (right < left) { Clear(); return; }
            left = Math.Max(1, left - 16); right = Math.Min(size - 2, right + 16);
            bottom = Math.Max(1, bottom - 16); top = Math.Min(size - 2, top + 16);
            Array.Clear(solid, 0, solid.Length); hasSolids = false;
            Array.Clear(u, 0, u.Length); Array.Clear(v, 0, v.Length);
            for (int y = bottom - 1; y <= top + 1; y++) for (int x = left - 1; x <= right + 1; x++) {
                int i = y * size + x;
                bool blocked = collision != null && collision.CellSolid(worldX + x, worldY + y);
                solid[i] = blocked; hasSolids |= blocked;
                if (blocked) { heat[i] = 0; continue; }
                float sx = (worldX + x + 0.5f) / scale - cx - 0.5f;
                float sy = (worldY + y + 0.5f) / scale - cy - 0.5f;
                u[i] = coarse.Sample(coarse.u, sx, sy) * scale;
                v[i] = coarse.Sample(coarse.v, sx, sy) * scale;
            }
            TransportHeat(settings, firing, 1);
            UpdateBounds();
        }
        private void Project(int iterations) {
            Array.Clear(pressure, 0, pressure.Length); Array.Clear(pressureNext, 0, pressureNext.Length);
            for (int y = bottom; y <= top; y++) for (int x = left; x <= right; x++) {
                int i = y * size + x;
                divergence[i] = solid[i] ? 0 : -0.5f * ((solid[i + 1] ? 0 : u[i + 1]) - (solid[i - 1] ? 0 : u[i - 1])
                    + (solid[i + size] ? 0 : v[i + size]) - (solid[i - size] ? 0 : v[i - size]));
            }
            for (int pass = 0; pass < iterations; pass++) {
                for (int y = bottom; y <= top; y++) for (int x = left; x <= right; x++) {
                    int i = y * size + x;
                    if (solid[i]) continue;
                    pressureNext[i] = (divergence[i] + (solid[i - 1] ? pressure[i] : pressure[i - 1])
                        + (solid[i + 1] ? pressure[i] : pressure[i + 1]) + (solid[i - size] ? pressure[i] : pressure[i - size])
                        + (solid[i + size] ? pressure[i] : pressure[i + size])) * 0.25f;
                }
                Swap(ref pressure, ref pressureNext);
            }
            for (int y = bottom; y <= top; y++) for (int x = left; x <= right; x++) {
                int i = y * size + x;
                if (solid[i]) continue;
                u[i] -= 0.5f * ((solid[i + 1] ? pressure[i] : pressure[i + 1]) - (solid[i - 1] ? pressure[i] : pressure[i - 1]));
                v[i] -= 0.5f * ((solid[i + size] ? pressure[i] : pressure[i + size]) - (solid[i - size] ? pressure[i] : pressure[i - size]));
                if (solid[i - 1] && u[i] < 0 || solid[i + 1] && u[i] > 0) u[i] = 0;
                if (solid[i - size] && v[i] < 0 || solid[i + size] && v[i] > 0) v[i] = 0;
            }
        }
    }
}
