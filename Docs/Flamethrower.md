# World-space flamethrower

The shotgun prefab now uses a fixed particle pool rasterized into a world-aligned heat window. Core particles, wings and embers store world positions and velocities in sprite-pixel units. Only spawning reads the nozzle pose. Moving or rotating the gun never transforms existing fire.

`FlameSim.cs` is pure C# in the `Omnia.Fire.Simulation` assembly. It owns the seeded spawn RNG, pool, world-space movement, raster and raw heat sampling. Each core also has its own seeded jitter stream, so later emissions cannot change an old particle’s random motion. `FlameEmitter` samples the nozzle once per fixed tick and supplies swept physics collisions. `FlameRenderer` owns the unrotated, detached sprite, palette mapping and world-coordinate dithering. The core raster is a connected world-space ribbon through emission-age cross-sections, not a collection of projectile streaks. Its detached update loop keeps particles and damage ticking even when the weapon is hidden or its parent is disabled during a roll.

## Rules

- **Core:** particles continue to move independently in world space. At rasterization, particles of the same emission age form a cross-section, and a cubic curve joins those cross-sections in chronological order. The near end starts at the nozzle's current position/direction only while firing. A quick turn therefore bends a continuous stream toward the new aim while the older end carries on and cools, instead of revealing separate projectile heads. Heat and width interpolate along the curve, with a narrower fading tip. Gaps caused by missing cohorts, teleport-sized jumps, and solid collisions are not bridged. Turning off **Core Ribbon** restores the separate-streak rendering for comparison.
- **Wings:** shed more often downstream, alternating sides using distance travelled and tick phase. Each wing captures its parent's travel direction. The wispy preset sheds smaller, cooler particles more often, with broader, slower side alternation and longer lifetimes. It moves slowly forward, then stronger drag along that captured direction pulls it backward; early outward spread and world-up buoyancy give it independent motion. Turning the gun cannot redirect old wings. Tapered strokes of their last world-space movement make them read as wisps rather than isolated dots.
- **Embers:** core breakup creates one to three small, cooling blobs with inherited velocity and world-down gravity. A separate ember cap shares the fixed pool.
- **Release:** stop spawning immediately. Existing fire keeps travelling and cools faster, disappearing within about one second. Switching weapons or rolling also stops emission without deleting old flame.

## Inspector parameters

Select `Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab` and edit **Flame Emitter**.

| Parameter | Project prefab | Meaning |
| --- | --- | --- |
| Max Range | 96 pixels | Preserves the previously requested 3-world-unit reach at 32 PPU. Newly added emitters default to 48 pixels. |
| Jet Speed | 10 pixels/tick | Core travel speed; newly added emitters default to 2.5. |
| Animation FPS | 20 | Fixed simulation, raster and damage frequency. Particle velocities/accelerations and lifetimes use ticks, as in the design document. |
| Pulse Period / Slug Length | 1 / 10 | Ticks between particle cohorts / particles per cohort. With Core Ribbon enabled, the cohorts are joined into one stream. |
| Core Ribbon | on | Connected world-space core raster. |
| Ribbon Smoothness | 0.85 | Blend between straight links and smooth cubic arcs. |
| Ribbon Width Scale | 1.75 | Cross-section width relative to Core Radius; preserves the body of the jet while joining samples. |
| Ribbon Tip Taper | 0.2 | Amount the core narrows toward its older end. |
| Color Falloff | 0.55 | Exponent of core heat over lifetime: `heat = (1 − age/life)^falloff`. Higher values turn white into fiery colors sooner. |
| Core Radius / Radial Falloff | 1 / 0.3 | Pixel stamp thickness and cooling from its center to its edge. |
| Inherit Velocity | 0.3 | Fraction of nozzle movement added to newly emitted particles only. |
| Max Inherited Speed | 24 pixels/tick | Caps the movement input before multiplying by Inherit Velocity. |
| Core Width Jitter / Jitter Chance | 0 / 0.1 | Spawn width and chance of a one-pixel lateral perturbation per core tick. |
| Core Droop | 0.004 | World-down acceleration per core tick; use 0 for top-down. |
| Shed Chance | 0.55 | Wing probability near the tip; smoothly reduced nearer the nozzle. |
| Flap Wave Number / Flap Freq | 0.22 / 0.7 | Spatial phase per pixel travelled / temporal phase per tick. |
| Wing Offset Max | 4 pixels | Side offset when a wing is shed. |
| Wing Forward / Wing Lift | 0.35 / 0.25 | Initial speed along the captured parent direction / world up. |
| Wing Spread / Spread Steps | 0.16 / 5 | Early outward acceleration and its duration. Existing spread upgrades scale Wing Spread. |
| Back Drag / Lift Accel | 0.75 / 0.065 | Age-weighted backward pull / world-up acceleration per tick. |
| Wing Life Min / Max | 9 / 15 ticks | Randomized wisp lifetime. |
| Wing Heat / Radius | 0.62 / 0.85 | Cooler starting heat and thin pixel radius. |
| Wing Trail | 1 | Fraction of the last movement step drawn as a tapered strand. |
| Ember Gravity / Heat / Radius | 0.06 / 0.75 / 0.75 | Falling breakup blobs. |
| Ember Life Min / Max | 3 / 6 ticks | Short ember lifetime. |
| Max Particles / Max Embers | 512 / 64 | Fixed pool / maximum live embers inside that pool. |
| Window Size | 224 pixels | Fixed square raster; new emitters default to 160. Initialization expands it if necessary to fit `2 × Max Range + 32`. |
| Cutoff / Dither Edges | 0.08 / on | Binary transparency; dither uses absolute world pixel coordinates and simulation tick. |
| Damage Threshold | 0.35 | Minimum displayed heat that can hurt a target. |
| Core Heat Threshold | 0.85 | Heat above which Core Damage Multiplier applies; cooler hits use Wing Damage Multiplier. |
| Core / Wing Damage Multiplier | 1 / 0.6 | Scales the weapon's heat-weighted damage per second. |
| Release Cooling | 1.5 heat/second | Extra cooling after emission stops; trajectories are unchanged. |
| Palette | Fire.asset | Swap for BlueFire.asset or another **Omnia > Flame Palette** asset. |

Set pool capacity, raster size and PPU before Play mode. Other rule parameters can be tuned while playing. Core range and lifetime are captured at spawn, so changing Max Range does not move existing particles.

The raster origin is snapped to whole world pixels around live-particle bounds, including core line stamps. With no particles it centers on the nozzle. The sprite stays at that origin between simulation ticks. Particles outside the fixed window keep simulating but are neither rendered nor sampled. Texture filtering is Point with no mipmaps; the unlit, alpha-clipped material disables blending. No new full-screen post-process is used.

Physics sweeps the entire movement segment and checks its endpoints, preventing fast cores from tunnelling through thin walls. Every short curve link is swept against walls before drawing, even where both particle paths are unobstructed. Thick pixel stamps also check their short offsets against walls; occupied solid cells are removed after rasterization. A fixed per-tick cache of empty 8-pixel regions avoids repeated narrowphase physics queries and refreshes every tick for moving colliders.

## Damage and lifecycle

Damage candidates come from the current world raster rectangle, regardless of aim. Their colliders are checked against actual opaque pixel centers. Multiple colliders on one enemy yield one weighted hit per tick. `FlameEmitter.SampleHeat(worldPos)` returns displayed heat, including dithering; `FlameSim.SampleWorld(pixelX, pixelY)` returns raw raster heat.

Lingering flame can hit a target after a turn, a weapon swap, or a roll. Normal release/hiding stops emission without clearing particles. `Clear()` explicitly resets fire for test/setup use. Destroying the emitter destroys its detached renderer and ends the effect. The legacy `Shotgun` component remains a compatibility subclass to preserve existing prefab, pickup and upgrade references.

## Verification

- `FlameSimTests`: connected core under repeated fast spins; positive heat in the curved bridge between samples; curve collision between otherwise-clear particle paths; detached ribbon after release; seeded pose-sequence repeatability; nozzle independence for existing cores/wings/embers; interpolation and pulses; pool/ember caps and heat bounds; world gravity/buoyancy; backward wing motion; release cleanup; window placement; out-of-window simulation; swept thin-wall collision; tuning and clearing.
- `AgentScripts/ValidateFlamethrower.cs`: 60 sweep poses, binary alpha, world alignment, query agreement, simulation/raster timing and zero-allocation check. The 0.2 ms budget applies to the design-document defaults (48-pixel range, 160-cell window); the larger 96-pixel reference is measured separately.
- `AgentScripts/ProfileFlamethrower.cs`: Play-mode measurement of the complete tick, including Unity physics, palette mapping and texture upload, with no enemy callbacks.
- `AgentScripts/PreviewFlameRibbon.cs`: exports `Temp/Flamethrower/ribbon-comparison.png`, with old particle streaks on the left and connected ribbons on the right during the same rapid full spin.
- `AgentScripts/PreviewFlameRules.cs`: exports six frames to `Temp/Flamethrower/world-space-sweep.png`.
- `AgentScripts/PlaytestFlamethrower.cs`: run in **CopyableScene Play mode** with a player. It temporarily drives a half-second 180-degree sweep, moves the player, tests damage in old/hidden-weapon flame, release cleanup, and a one-pixel physics wall. It exports `Temp/Flamethrower/copyable-sweep.png`. Exit Play mode afterward to restore the scene.

```sh
unity command run_tests --mode editor --filter FlameSimTests
unity command run_script --file AgentScripts/ValidateFlamethrower.cs --entry ValidateFlamethrower.Run
```

All simulation and rendering arrays, collision cache, and damage-query buffers are fixed allocations. Damage queries cap candidates at 256 colliders per emitter tick. The pure simulation/raster timing benchmark excludes Unity physics, texture upload, rendering, and enemy callbacks.

The connected-ribbon CPU simulation/raster benchmark measured about 0.17 ms/tick at the 48-pixel design defaults and 0.21 ms/tick for the 96-pixel reference, with zero managed allocations. These measurements exclude Unity physics, texture upload, enemy callbacks and GPU frame time; denser artist presets can cost more.
