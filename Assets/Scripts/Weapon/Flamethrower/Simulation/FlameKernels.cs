using System;
using Unity.Burst;

namespace Omnia.Fire {
    // Direct Burst calls are synchronous and allocation-free after initialization.
    // Arrays stay managed; each caller pins them only for the duration of the call.
    // Strict floats avoid fast-math reassociation of the fluid solver.
    [BurstCompile]
    internal static unsafe class FlameKernels {
        public struct Grid {
            public int size, originX, originY, left, right, bottom, top;
            public int hasSolids;
            public float* heat, u, v, nextHeat, nextU, nextV, curl, divergence, transported, pressure, pressureNext;
            public bool* solid;
        }
        public struct FlowSettings {
            public float flowBuoyancy, flowUpperBias, flowVorticity, flowJetStability, flowCooling, releaseCooling, stepSeconds;
            public FlowSettings(FlameSimSettings settings) {
                flowBuoyancy = settings.flowBuoyancy; flowUpperBias = settings.flowUpperBias;
                flowVorticity = settings.flowVorticity; flowJetStability = settings.flowJetStability;
                flowCooling = settings.flowCooling; releaseCooling = settings.releaseCooling; stepSeconds = settings.stepSeconds;
            }
        }
        private static bool warmed;
        public static void WarmUp() {
            if (warmed) return;
            var grid = new Grid { left = 2, bottom = 2, right = -2, top = -2 };
            var settings = new FlowSettings(FlameSimSettings.Default);
            var bounds = FlameBounds.Empty;
            Velocity(ref grid, in settings, 0);
            Confinement(ref grid, null, in settings, 0);
            Project(ref grid, 0);
            Heat(ref grid, in settings, 0, 0, ref bounds);
            Upsample(ref grid, ref grid, 1);
            var stamp = new StampInput { extent = -1 };
            Stamp(ref stamp, null, null, null, 0);
            warmed = true;
        }
        private static float Clamp(float x, float low, float high) => x < low ? low : x > high ? high : x;
        private static void Swap(ref float* a, ref float* b) { float* t = a; a = b; b = t; }
        private static float Sample(ref Grid g, float* field, float x, float y) {
            x = Clamp(x, 0, g.size - 1.001f); y = Clamp(y, 0, g.size - 1.001f);
            int ix = (int)x, iy = (int)y, a = iy * g.size + ix;
            float fx = x - ix, fy = y - iy;
            if (g.hasSolids == 0)
                return (field[a] + (field[a + 1] - field[a]) * fx) * (1 - fy)
                    + (field[a + g.size] + (field[a + g.size + 1] - field[a + g.size]) * fx) * fy;
            float aa = g.solid[a] ? 0 : field[a], ab = g.solid[a + 1] ? 0 : field[a + 1];
            float ba = g.solid[a + g.size] ? 0 : field[a + g.size], bb = g.solid[a + g.size + 1] ? 0 : field[a + g.size + 1];
            return (aa + (ab - aa) * fx) * (1 - fy) + (ba + (bb - ba) * fx) * fy;
        }
        private static bool Blocked(ref Grid g, float x, float y, float tx, float ty) {
            if (g.hasSolids == 0) return tx < 0 || ty < 0 || tx >= g.size - 1 || ty >= g.size - 1;
            int n = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(tx - x), Math.Abs(ty - y)) * 2));
            for (int k = 0; k <= n; k++) {
                float t = (float)k / n;
                int ix = (int)Math.Floor(x + (tx - x) * t + 0.5f), iy = (int)Math.Floor(y + (ty - y) * t + 0.5f);
                if (ix < 0 || iy < 0 || ix >= g.size || iy >= g.size || g.solid[iy * g.size + ix]) return true;
            }
            return false;
        }
        [BurstCompile(FloatMode = FloatMode.Strict, CompileSynchronously = true)]
        public static void Velocity(ref Grid g, in FlowSettings settings, float dt) {
            // Semi-Lagrangian velocity transport with mild viscosity/damping.
            for (int y = g.bottom; y <= g.top; y++) for (int x = g.left; x <= g.right; x++) {
                int i = y * g.size + x;
                if (g.solid[i]) { g.nextU[i] = g.nextV[i] = 0; continue; }
                float tx = x - g.u[i] * dt, ty = y - g.v[i] * dt;
                bool blocked = Blocked(ref g, x, y, tx, ty);
                g.nextU[i] = blocked ? 0 : Sample(ref g, g.u, tx, ty) * 0.995f;
                g.nextV[i] = blocked ? 0 : Sample(ref g, g.v, tx, ty) * 0.995f + g.heat[i] * settings.flowBuoyancy * dt;
            }
        }
        [BurstCompile(FloatMode = FloatMode.Strict, CompileSynchronously = true)]
        public static void Confinement(ref Grid g, float* sourceHeat, in FlowSettings settings, float dt) {
            for (int y = g.bottom; y <= g.top; y++) for (int x = g.left; x <= g.right; x++) {
                int i = y * g.size + x;
                g.curl[i] = g.solid[i] ? 0 : 0.5f * (g.v[i + 1] - g.v[i - 1] - g.u[i + g.size] + g.u[i - g.size]);
            }
            for (int y = g.bottom; y <= g.top; y++) for (int x = g.left; x <= g.right; x++) {
                int i = y * g.size + x;
                if (g.solid[i]) continue;
                float nx = Math.Abs(g.curl[i + 1]) - Math.Abs(g.curl[i - 1]);
                float ny = Math.Abs(g.curl[i + g.size]) - Math.Abs(g.curl[i - g.size]);
                float inverse = 1f / (float)Math.Sqrt(nx * nx + ny * ny + 0.0001f);
                // Temperature decreases upward across a plume's upper boundary,
                // and downward underneath it. Use that local world-up gradient,
                // not aim direction or a stamped shape, to favor the upper rolls.
                float below = g.heat[i - g.size], above = g.heat[i + g.size];
                float upperEdge = (below - above) / (below + above + 0.02f);
                float circulationGain = 1 + Clamp(settings.flowUpperBias, 0, 1) * upperEdge;
                float force = Math.Min(2, Math.Max(0, settings.flowVorticity)) * g.curl[i] * dt
                    * (1 - Clamp(settings.flowJetStability, 0, 1) * Clamp(sourceHeat[i], 0, 1)) * circulationGain;
                g.u[i] = Clamp(g.u[i] + ny * inverse * force, -6, 6);
                g.v[i] = Clamp(g.v[i] - nx * inverse * force, -6, 6);
            }
        }
        [BurstCompile(FloatMode = FloatMode.Strict, CompileSynchronously = true)]
        public static void Project(ref Grid g, int iterations) {
            for (int y = g.bottom; y <= g.top; y++) for (int x = g.left; x <= g.right; x++) {
                int i = y * g.size + x;
                g.divergence[i] = g.solid[i] ? 0 : -0.5f * ((g.solid[i + 1] ? 0 : g.u[i + 1]) - (g.solid[i - 1] ? 0 : g.u[i - 1])
                    + (g.solid[i + g.size] ? 0 : g.v[i + g.size]) - (g.solid[i - g.size] ? 0 : g.v[i - g.size]));
            }
            for (int pass = 0; pass < iterations; pass++) {
                for (int y = g.bottom; y <= g.top; y++) for (int x = g.left; x <= g.right; x++) {
                    int i = y * g.size + x;
                    if (g.solid[i]) continue;
                    g.pressureNext[i] = (g.divergence[i] + (g.solid[i - 1] ? g.pressure[i] : g.pressure[i - 1])
                        + (g.solid[i + 1] ? g.pressure[i] : g.pressure[i + 1]) + (g.solid[i - g.size] ? g.pressure[i] : g.pressure[i - g.size])
                        + (g.solid[i + g.size] ? g.pressure[i] : g.pressure[i + g.size])) * 0.25f;
                }
                Swap(ref g.pressure, ref g.pressureNext);
            }
            for (int y = g.bottom; y <= g.top; y++) for (int x = g.left; x <= g.right; x++) {
                int i = y * g.size + x;
                if (g.solid[i]) continue;
                g.u[i] -= 0.5f * ((g.solid[i + 1] ? g.pressure[i] : g.pressure[i + 1]) - (g.solid[i - 1] ? g.pressure[i] : g.pressure[i - 1]));
                g.v[i] -= 0.5f * ((g.solid[i + g.size] ? g.pressure[i] : g.pressure[i + g.size]) - (g.solid[i - g.size] ? g.pressure[i] : g.pressure[i - g.size]));
                if (g.solid[i - 1] && g.u[i] < 0 || g.solid[i + 1] && g.u[i] > 0) g.u[i] = 0;
                if (g.solid[i - g.size] && g.v[i] < 0 || g.solid[i + g.size] && g.v[i] > 0) g.v[i] = 0;
            }
        }
        [BurstCompile(FloatMode = FloatMode.Strict, CompileSynchronously = true)]
        public static void Heat(ref Grid g, in FlowSettings settings, int firing, float dt, ref FlameBounds bounds) {
            for (int y = g.bottom; y <= g.top; y++) for (int x = g.left; x <= g.right; x++) {
                int i = y * g.size + x;
                float tx = x - g.u[i] * dt, ty = y - g.v[i] * dt;
                g.transported[i] = g.solid[i] || Blocked(ref g, x, y, tx, ty) ? 0 : Sample(ref g, g.heat, tx, ty);
            }
            float cooling = Math.Max(0, settings.flowCooling)
                + (firing != 0 ? 0 : Math.Max(0, settings.releaseCooling * settings.stepSeconds));
            for (int y = g.bottom; y <= g.top; y++) for (int x = g.left; x <= g.right; x++) {
                int i = y * g.size + x;
                if (g.solid[i] || g.transported[i] <= 0) { g.nextHeat[i] = 0; continue; }
                float tx = Clamp(x - g.u[i] * dt, 0, g.size - 1.001f), ty = Clamp(y - g.v[i] * dt, 0, g.size - 1.001f);
                int a = (int)ty * g.size + (int)tx;
                float lo = Math.Min(Math.Min(g.heat[a], g.heat[a + 1]), Math.Min(g.heat[a + g.size], g.heat[a + g.size + 1]));
                float hi = Math.Max(Math.Max(g.heat[a], g.heat[a + 1]), Math.Max(g.heat[a + g.size], g.heat[a + g.size + 1]));
                float reverse = Sample(ref g, g.transported, x + g.u[i] * dt, y + g.v[i] * dt);
                float corrected = Clamp(g.transported[i] + 0.5f * (g.heat[i] - reverse), lo, hi) - cooling * dt;
                g.nextHeat[i] = corrected < 0.015f ? 0 : Math.Min(1, corrected);
                if (g.nextHeat[i] > 0) bounds.Include(x, y);
            }
        }
        [BurstCompile(FloatMode = FloatMode.Strict, CompileSynchronously = true)]
        public static void Upsample(ref Grid g, ref Grid coarse, int scale) {
            for (int y = g.bottom - 1; y <= g.top + 1; y++) for (int x = g.left - 1; x <= g.right + 1; x++) {
                int i = y * g.size + x;
                if (g.solid[i]) { g.heat[i] = 0; continue; }
                float sx = (g.originX + x + 0.5f) / scale - coarse.originX - 0.5f;
                float sy = (g.originY + y + 0.5f) / scale - coarse.originY - 0.5f;
                g.u[i] = Sample(ref coarse, coarse.u, sx, sy) * scale;
                g.v[i] = Sample(ref coarse, coarse.v, sx, sy) * scale;
            }
        }
        private static float Smooth01(float x) {
            x = Clamp(x, 0, 1); return x * x * (3 - 2 * x);
        }
        private static float ThermalHash(int x, int y, uint seed) {
            uint h;
            unchecked {
                h = (uint)x * 73856093u ^ (uint)y * 19349663u ^ seed;
                h ^= h >> 13; h *= 1274126177u; h ^= h >> 16;
            }
            return (h & 0xffffff) * (1f / 16777216f);
        }
        private static float ThermalNoise(float x, float y, uint seed) {
            int ix = (int)Math.Floor(x), iy = (int)Math.Floor(y);
            float fx = Smooth01(x - ix), fy = Smooth01(y - iy);
            float a = ThermalHash(ix, iy, seed), b = ThermalHash(ix + 1, iy, seed);
            float c = ThermalHash(ix, iy + 1, seed), d = ThermalHash(ix + 1, iy + 1, seed);
            return (a + (b - a) * fx) * (1 - fy) + (c + (d - c) * fx) * fy;
        }
        public struct StampInput {
            public int centerX, centerY, originX, originY, size, extent;
            public float squared, value, falloff, variation, material, speed, length, width;
            public FlameVector position, forward;
            public uint seed;
        }
        [BurstCompile(FloatMode = FloatMode.Strict, CompileSynchronously = true)]
        public static void Stamp(ref StampInput stamp, float* heat, float* sourceHeat, float* samples, int core) {
            int stride = stamp.extent * 2 + 1;
            for (int dy = -stamp.extent; dy <= stamp.extent; dy++) for (int dx = -stamp.extent; dx <= stamp.extent; dx++) {
                int slot = (dy + stamp.extent) * stride + dx + stamp.extent;
                samples[slot] = 0;
                int x = stamp.centerX + dx, y = stamp.centerY + dy;
                int distance = dx * dx + dy * dy;
                if (x < 0 || y < 0 || x >= stamp.size || y >= stamp.size || distance > stamp.squared) continue;
                float sample = stamp.value * Math.Max(0, 1 - Math.Max(0, stamp.falloff) * distance / stamp.squared);
                int index = y * stamp.size + x;
                if (sample <= heat[index] && (core == 0 || sample <= sourceHeat[index])) continue;
                if (core != 0 && stamp.variation > 0) {
                    float ox = stamp.originX + x + 0.5f - stamp.position.x, oy = stamp.originY + y + 0.5f - stamp.position.y;
                    float along = stamp.material * stamp.speed - ox * stamp.forward.x - oy * stamp.forward.y;
                    float across = -ox * stamp.forward.y + oy * stamp.forward.x;
                    float coolPocket = 1 - ThermalNoise(along / stamp.length, across / stamp.width, stamp.seed);
                    sample *= 1 - stamp.variation * coolPocket * coolPocket;
                }
                samples[slot] = sample;
            }
        }
    }
}
