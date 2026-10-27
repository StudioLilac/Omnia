# World-space flamethrower

The core remains the connected beam approved during the previous iteration. Its outer fire now comes from a persistent heat-and-velocity lattice, implemented in plain C# in `FlameFlow.cs`. No crescents, spiral outlines, eddy particles, vortex centers, or traveling-wave shapes are stamped into the image.

## How the fire develops

1. `FlameSim` advances the world-space core particles and rasterizes their connected ribbon. Each core pixel supplies heat and the ribbon's local forward velocity to the lattice.
2. The lattice mixes that momentum into the surrounding slow air. Small seeded disturbances break perfect symmetry.
3. Each cell advects its velocity from neighboring cells. World-up buoyancy acts on hot cells.
4. Neighbor differences measure curl. Vorticity confinement restores rotational energy lost to numerical diffusion at pixel resolution. Pressure projection redistributes velocity to reduce compression and create entrainment around the jet.
5. Heat moves through this evolving velocity field. Corrected advection preserves thin filaments, with neighborhood clamping to prevent heat overshoot. Cells cool and extinguish. Backward eddies are actual hot cells moving backward in the field, rather than a prescribed outline or backward-moving point particles.
6. The displayed raster combines the core with the transported heat. The same final opaque pixels drive damage.

Velocity and pressure use two substeps on a coarser lattice (2 game pixels per velocity cell by default). Heat is transported with correction at full game-pixel resolution once per tick. Both use fixed preallocated arrays. All state is world-aligned. When the raster window moves, heat and velocity are copied by integer cell offsets, preserving their world positions. Old heat continues independently after the gun turns or moves. The detached `FlameRenderer` keeps simulation and damage ticking when the weapon is hidden or the player rolls.

## Tuning

Edit **Flame Emitter** on `Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab`.

| Parameter | Project value | Effect |
| --- | --- | --- |
| Max Range | 96 pixels | Core reach: 3 world units at 32 PPU. |
| Jet Speed / Pulse Period / Slug Length | 10 / 1 / 10 | Preserved core movement and continuous emission. |
| Animation FPS | 20 | Fixed simulation, rendering and damage frequency. |
| Core Ribbon / Smoothness / Width Scale | on / 0.85 / 1.75 | Connected beam geometry. |
| Color Falloff | 0.55 | How quickly the core cools from white into fiery colors. |
| Cellular Flow | on | Persistent neighbor-based heat/velocity simulation; off restores legacy wing particles for comparison. |
| Flow Cell Size | 2 pixels | Velocity/pressure resolution. Heat and damage always retain one cell per game pixel. Set before Play; 1 enables the full-resolution reference solver. |
| Flow Speed | 3.5 pixels/tick | Momentum fed into the surrounding heat lattice. Independent of the fast core's propagation speed. |
| Flow Vorticity | 1.8 | Strength of rotational-energy preservation, clamped to 0–2 for stability. Higher values produce stronger folds and backward circulation. |
| Flow Cooling | 0.022 heat/tick | How long hot filaments survive. Higher values shorten wisps. |
| Flow Buoyancy | 0.035 | World-up acceleration from heat; set to zero for top-down. |
| Flow Perturbation | 0.35 | Seeded small disturbances to the injected momentum. These seed instability, without prescribing a curl shape. Existing spread upgrades scale this input. |
| Flow Heat | 0.8 | Heat supplied to the outer flow relative to the core. |
| Flow Pressure Iterations | 8 per substep | Pressure solver quality/cost. |
| Window Size / PPU | 288 / 32 | World raster dimensions and sprite pixel scale. Set before Play; initialization expands undersized windows for range and flow margin. |
| Max Particles / Max Embers | 512 / 64 | Core/legacy-wing/ember pool limits. Cellular fire consumes no particle slots. |
| Cutoff / Dither Edges | 0.08 / on | Binary opacity; deterministic dithering uses absolute world pixels and simulation tick. |
| Damage Threshold / Core Heat Threshold | 0.35 / 0.85 | Minimum damaging heat / threshold distinguishing core damage from cooler burn damage. |
| Core / Wing Damage Multiplier | 1 / 0.6 | Heat-weighted damage scaling. Cellular wisps use the cooler multiplier. |
| Release Cooling | 1.5 heat/second | Additional cooling after emission stops. |
| Palette | Fire.asset | Swap for BlueFire.asset or another **Omnia > Flame Palette** asset. |

The **Wing Flow** particle parameters are retained for comparison when Cellular Flow is disabled. They do not control the new lattice. Embers remain short-lived world-space particles at core breakup.

## Rendering, collision and damage

The detached sprite has zero rotation and an integer world-pixel origin. Its point-filtered texture has no mipmaps; the alpha-clipped material disables blending. There is no full-screen pixelation pass. Heat is interpolated inside the numerical solver, then rasterized into hard opaque/transparent pixels.

Core motion and ribbon links use swept collisions. The flow builds a solid-cell mask each tick. Transport checks every crossed cell, including thin-wall crossings, and interpolation excludes solid cells. Thick core stamps also check their offsets against walls. Solid cells are removed from the final raster.

`FlameEmitter.SampleHeat(worldPos)` returns displayed heat, including dithering. Damage candidates come from the world raster rectangle and are checked against opaque pixel centers. Multiple colliders on one enemy yield one weighted hit per tick. Lingering fire can damage after turning, weapon switching, or rolling. `Clear()` resets particles, heat, velocity and seeded state.

## Verification and previews

- `FlameSimTests`: seeded repeatability; existing-particle independence from nozzle changes; core continuity during fast turns; curved-ribbon collision; range and color controls; caps; release cleanup; world gravity; retained legacy wings.
- `FlameFlowTests`: backward-moving hot cells and both signs of curl without wing particles; nozzle-independent lingering heat/velocity; world preservation when recentering; one-pixel wall transport; clear/reset; allocation checks.
- `AgentScripts/PreviewFlameEddies.cs`, entry `Run`: legacy backward points on the left, cellular flow on the right, at three successive times. Entry `Animate`: 100 frames of a steady jet, fast full turn, steady jet and release, saved under `Temp/Flamethrower/cellular-frames`.
- `AgentScripts/PlaytestFlamethrower.cs`: CopyableScene Play-mode checks for a fast sweep, player movement, old-trail and hidden-weapon damage, release, and a one-pixel physics wall.
- `AgentScripts/ValidateFlamethrower.cs`: pixel alignment, binary alpha, query agreement, allocation and timing measurements. Timings exclude physics, texture upload, enemy callbacks and GPU time.

This lattice costs more than the former curve stamps. Measure its timing separately from the older ribbon-only benchmark; the original 0.2 ms simulation budget is not assumed to hold for the pressure/heat solver.

Final verification: all 25 flame tests passed, plus CopyableScene sweep, motion, lingering damage, release and one-pixel wall checks. The optimized two-pixel velocity lattice measured 5.49 ms/tick at the 48-pixel defaults, 8.88 ms/tick for the 96-pixel reference, and 9.84 ms/tick for the actual project preset in the Editor, with zero managed allocations. The original 0.2 ms budget remains unmet. The full-resolution velocity reference cost about 15.41 ms/tick for the project preset. These are CPU simulation/raster measurements excluding physics, texture upload, enemy callbacks and GPU work.

Animated preview: `Temp/Flamethrower/cellular-fire.gif` (steady jet, full turn, steady jet, release).
