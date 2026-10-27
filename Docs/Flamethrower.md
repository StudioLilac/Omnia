# World-space flamethrower

The fire opens from the barrel into a broad hot jet, then breaks into cellular flame farther downstream. A connected world-space carrier supplies heat and momentum to a persistent lattice in `FlameFlow.cs`. Its directly rendered center fades before the end of the range, leaving the evolving heat field to define the tip. Coherent hot/cool pockets now travel with the emitted material across the broad core, with a twelve-color ramp giving orange, gold, yellow, cream and occasional white highlights.

## How the fire develops

1. `FlameSim` advances the world-space core particles and rasterizes their connected ribbon. Its width grows from a narrow nozzle to a broad body over Core Bloom Pixels. Emission-age and cross-stream coordinates sample smooth seeded temperature patches. Their pattern travels with the material instead of changing randomly per screen pixel. This temperature variation modulates both the direct core heat and the heat supplied to the flow. Heat and the ribbon's local forward velocity feed the lattice independently of the directly rendered core fading out.
2. The lattice mixes that momentum into the surrounding slow air. Small seeded disturbances break perfect symmetry. Flow Jet Stability reduces disturbances and vorticity amplification in the hottest injected cells so the near-nozzle body stays coherent before the downstream rolls develop.
3. Each cell advects its velocity from neighboring cells. World-up buoyancy acts on hot cells.
4. Neighbor differences measure curl. Vorticity confinement restores rotational energy lost to numerical diffusion at pixel resolution. Pressure projection redistributes velocity to reduce compression and create entrainment around the jet. Flow Upper Bias uses the local vertical heat gradient to strengthen circulation on upward-facing edges and damp it underneath; it stays tied to world up when aiming left or sweeping.
5. Heat moves through this evolving velocity field. Corrected advection preserves thin filaments, with neighborhood clamping to prevent heat overshoot. Cells cool and extinguish. Backward eddies are actual hot cells moving backward in the field, rather than a prescribed outline or backward-moving point particles.
6. The displayed raster combines the core with the transported heat. The same final opaque pixels drive damage.

Velocity and pressure use two substeps on a coarser lattice (2 game pixels per velocity cell by default). Heat is transported with correction at full game-pixel resolution once per tick. Both use fixed preallocated arrays. All state is world-aligned. When the raster window moves, heat and velocity are copied by integer cell offsets, preserving their world positions. Old heat continues independently after the gun turns or moves. The detached `FlameRenderer` keeps simulation and damage ticking when the weapon is hidden or the player rolls.

## Tuning

Edit **Flame Emitter** on `Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab`.

| Parameter | Project value | Effect |
| --- | --- | --- |
| Use Fuel (weapon component) | off | Continuous fire without depletion or an empty-tank gate. The HUD tank stays full. Enable later to restore consumption and refill. |
| Max Range | 96 pixels | Core reach: 3 world units at 32 PPU. |
| Jet Speed / Pulse Period / Slug Length | 10 / 1 / 10 | Preserved core movement and continuous emission. |
| Animation FPS | 40 | Fixed simulation, rendering and damage frequency. |
| Core Ribbon / Smoothness / Width Scale | on / 0.85 / 2.4 | Connected beam geometry. |
| Color Falloff | 0.8 | How quickly the core cools from white into fiery colors. |
| Core Radius / Radial Falloff | 2.5 / 0.65 | About 11–13 pixels across the broad body, with cooler orange edges around the hot interior. |
| Core Nozzle Scale / Bloom Pixels | 0.18 / 14 | Width at the barrel relative to the body / distance over which the jet opens up. |
| Core Fade Start / End | 0.35 / 0.85 | Fractions of Max Range over which the rigid center gives way to the cellular field. Heat/momentum still feed the lattice throughout the carrier. Equal endpoints disable this handoff. |
| Core Heat Variation | 0.5 | Strength of cooler pockets inside the hot body. Zero restores the uniform center. Hot pockets retain white/cream highlights; cooler ones show yellow/orange. |
| Core Heat Patch Length / Width | 10 / 3 pixels | Coherent temperature patch size along and across emitted material. |
| Cellular Flow | on | Persistent neighbor-based heat/velocity simulation; off restores legacy wing particles for comparison. |
| Flow Cell Size | 2 pixels | Velocity/pressure resolution. Heat and damage always retain one cell per game pixel. Set before Play; 1 enables the full-resolution reference solver. |
| Flow Speed | 3.5 pixels/tick | Momentum fed into the surrounding heat lattice. Independent of the fast core's propagation speed. |
| Flow Vorticity | 1.8 | Strength of rotational-energy preservation, clamped to 0–2 for stability. Higher values produce stronger folds and backward circulation. |
| Flow Cooling | 0.032 heat/tick | How long hot filaments survive. Higher values shorten wisps. |
| Flow Upper Bias | 0.8 | Amplifies upper-edge circulation and reduces lower-edge circulation using neighboring heat cells. Zero restores neutral amplification. |
| Flow Buoyancy | 0.12 | World-up acceleration from heat; set to zero for top-down. |
| Flow Perturbation | 0.35 | Seeded small disturbances to the injected momentum. These seed instability, without prescribing a curl shape. Existing spread upgrades scale this input. |
| Flow Heat | 0.85 | Heat supplied to the outer flow relative to the core. |
| Flow Jet Stability | 0.8 | Keeps the hottest injected jet coherent before stronger downstream turbulence. |
| Flow Pressure Iterations | 6 per substep | Pressure solver quality/cost. |
| Window Size / PPU | 288 / 32 | World raster dimensions and sprite pixel scale. Set before Play; initialization expands undersized windows for range and flow margin. |
| Max Particles / Max Embers | 512 / 32 | Core/legacy-wing/ember pool limits. Cellular fire consumes no particle slots. |
| Ember Life Min / Max | 2 / 4 ticks | At 40 FPS, tip sparks disappear within 0.1 seconds. |
| Ember Heat / Radius | 0.55 / 0.55 | Cooler, single-pixel sparks rather than bright lingering blobs. |
| Cutoff / Dither Edges | 0.08 / on | Binary opacity; deterministic dithering uses absolute world pixels and simulation tick. |
| Damage Threshold / Core Heat Threshold | 0.35 / 0.85 | Minimum damaging heat / threshold distinguishing core damage from cooler burn damage. |
| Core / Wing Damage Multiplier | 1 / 0.6 | Heat-weighted damage scaling. Cellular wisps use the cooler multiplier. |
| Release Cooling | 1.5 heat/second | Additional cooling after emission stops. |
| Palette | Fire.asset (12 colors) | Swap for BlueFire.asset or another **Omnia > Flame Palette** asset. |

The **Legacy Wings** particle parameters are retained for comparison when Cellular Flow is disabled. They do not control the new lattice. Embers remain short-lived world-space particles at core breakup. Velocities, flow cooling and lifetimes are tick-based: increasing Animation FPS to 40 doubles temporal motion and cooling relative to 20 FPS while preserving the 96-pixel core range. Damage remains scaled by tick duration.

## Rendering, collision and damage

The detached sprite has zero rotation and an integer world-pixel origin. Its point-filtered texture has no mipmaps; the alpha-clipped material disables blending. There is no full-screen pixelation pass. Heat is interpolated inside the numerical solver, then rasterized into hard opaque/transparent pixels.

Core motion and ribbon links use swept collisions. The flow builds a solid-cell mask each tick. Transport checks every crossed cell, including thin-wall crossings, and interpolation excludes solid cells. Thick core stamps also check their offsets against walls. Solid cells are removed from the final raster.

`FlameEmitter.SampleHeat(worldPos)` returns displayed heat, including dithering. Damage candidates come from the world raster rectangle and are checked against opaque pixel centers. Multiple colliders on one enemy yield one weighted hit per tick. Lingering fire can damage after turning, weapon switching, or rolling. `Clear()` resets particles, heat, velocity and seeded state.

## Verification and previews

- `FlameSimTests`: temperature variation along and across the center; material-following temperature after aim changes; nozzle-to-body expansion, cooler core edges, cellular heat beyond the faded direct core, fast ember cleanup; seeded repeatability; existing-particle independence from nozzle changes; core continuity during fast turns; curved-ribbon collision; range and color controls; caps; release cleanup; world gravity; retained legacy wings.
- `FlameFlowTests`: backward-moving hot cells and both signs of curl without wing particles; nozzle-independent lingering heat/velocity; world preservation when recentering; one-pixel wall transport; clear/reset; allocation checks; upper-plume dominance while aiming right and left.
- `AgentScripts/PreviewFlameEddies.cs`, entry `Run`: legacy backward points on the left, cellular flow on the right, at three successive times. Entry `Animate`: five seconds at the preset Animation FPS of a steady jet, fast full turn, steady jet and release, saved under `Temp/Flamethrower/barrel-plume-frames`.
- `AgentScripts/PreviewBarrelPlume.cs`: previous thin 20 FPS core on the left, broad 40 FPS barrel plume on the right in `Temp/Flamethrower/barrel-plume-comparison.png`.
- `AgentScripts/PreviewRisingFire.cs`: previous balanced plume on the left, stronger upper eddies and a restrained underside on the right, in `Temp/Flamethrower/rising-fire-comparison.png`.
- `AgentScripts/PreviewTemperatureFire.cs`: previous uniform center/six-color ramp on the left, thermal pockets/twelve-color ramp on the right, in `Temp/Flamethrower/temperature-comparison.png`.
- `AgentScripts/PlaytestFlamethrower.cs`: unlimited fire from an empty tank, sustained fire with a one-unit tank, optional fuel depletion and restart; CopyableScene Play-mode checks for a fast sweep, player movement, old-trail and hidden-weapon damage, release, and a one-pixel physics wall.
- `AgentScripts/ValidateFlamethrower.cs`: pixel alignment, binary alpha, query agreement, allocation and timing measurements. Timings exclude physics, texture upload, enemy callbacks and GPU time.

This lattice costs more than the former curve stamps. Measure its timing separately from the older ribbon-only benchmark; the original 0.2 ms simulation budget is not assumed to hold for the pressure/heat solver.

Performance remains above the original 0.2 ms budget. The temperature-varied 40 FPS preset measured 11.25 ms per simulation tick in the Editor with zero managed allocations (previous broad-core preset: 8.90 ms/tick). The higher tick rate increases total CPU work per second despite the slightly cheaper individual tick. All 32 flame tests passed, including material-carried temperature pockets, variation across the center, zero allocations with core variation enabled, and world-up plume dominance for left and right aim. CopyableScene live checks passed for unlimited fire from empty, sustained firing, the optional fuel toggle, sweeping, lingering damage, release and walls. Timings exclude physics, texture upload, enemy callbacks and GPU work.

Animated preview: `Temp/Flamethrower/temperature-fire.gif` (steady jet, full turn, steady jet, release at 40 FPS).
