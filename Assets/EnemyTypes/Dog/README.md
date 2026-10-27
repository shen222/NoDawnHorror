# Dog enemy

`DogEnemy.prefab` replaces the static dog in `MainGame`. Drag additional copies onto the existing baked NavMesh to add more enemies. Each dog remembers its starting position as the center of its territory.

The original `Assets/SketchFabs/Dog/Dog.fbx` remains the source model. The enemy prefab contains an editable 21-bone skeleton and a new weighted mesh (`DogSkinnedMesh.asset`); the rig is stored in Unity rather than written into the source FBX. Its original textures and material are reused. Six generated animation clips provide breathing idle, diagonal walk, bounding run, jump attack, hit reaction, and a falling death pose. These are authored starter animations, not motion capture.

## Gameplay defaults

| Setting | Default |
| --- | --- |
| Health | 40 HP (four hits from the default 10-damage projectile) |
| Bite damage | 15 |
| Jump initiation / impact range | 2 m / 1.25 m |
| Attack cone | 90 degrees |
| Jump travel / height | Up to 1.1 m / 0.62 m |
| Impact time / full attack duration | 0.65 s / 1.15 s |
| Minimum time between bites | 1.6 s |
| Roaming / pursuit speed | 1.5 / 3.8 m/s |
| Roaming / pursuit territory | 10 / 24 m from spawn |
| Vision / hearing | 12 / 5 m |
| Last-known-position memory | 5 s |

Change these fields on `DogEnemyAI` in the Inspector. The component inherits `ZombieAI` for compatibility with the existing `Projectile` damage lookup, but implements its own dog behavior and health. `CurrentHealth`, `State`, and `IsDead` expose its runtime state.

Roaming chooses reachable random points, waits between walks, and retries stalled patrols. Pursuit requires sight, nearby player movement, or taking damage. The jump attack crouches, pushes off, tucks the legs, reaches toward the player, and absorbs the landing before recovering. Its direction is fixed when the attack begins. The skeleton supplies the vertical arc while collision-checked NavMesh movement advances the dog horizontally; the hitbox follows the airborne body. Damage is checked once at impact. Sidestepping, leaving range, or putting a wall between the dog and player avoids the hit. Damage interrupts the attack and cancels further travel. Pause freezes the jump. Death stops navigation and damage collisions, plays a fall, then removes the body after a delayed sink.

The animation retains the `DogBite.anim` path and `Bite` Animator state for existing references. `AttackSpeed` scales that state to match `biteDuration`; `biteWindup` controls its damage timing. The authored vertical arc is fixed at 0.62 m, and `jumpDistance` controls horizontal travel. The shared jump-height function keeps the collider aligned with the animation.

The dog uses agent type 0 on the scene's existing NavMesh. No new surface, links, obstacles, or bake are required for its current placement. Re-bake the existing surface after changing level geometry. Place each dog on a connected walkable area; an invalid spawn only attempts a nearby 2 m recovery and otherwise reports a warning.

## Editor tools and verification

- **Tools > Enemies > Dog > Create Rigged Enemy And Replace Scene Dog** rebuilds the rigged mesh, animation clips, controller, and prefab, and replaces instances of the original static dog model in the current scene. It saves the scene when replacing a dog. Existing enemy instances inherit regenerated prefab assets.
- **Tools > Enemies > Dog > Validate Rig And Preview Animations** checks skin weights, bones, animation assignment and mesh deformation, and saves a six-pose contact sheet to `Logs/DogEnemy/animation-poses.png`.
- **Tools > Enemies > Dog > Upgrade Jump Attack** updates the attack clip, controller playback parameter, and prefab attack settings while preserving the existing rig and other enemy settings.
- **Tools > Enemies > Dog > Validate And Preview Jump Attack** checks airborne paws and grounded recovery, and renders six jump phases to `Logs/DogEnemy/jump-attack-poses.png`.
- `DogEnemyPlayVerification.Begin()` runs editor-only integration checks during Play mode. It temporarily isolates the dog and controls the player for patrol, pursuit, bite, dodge, wall obstruction, hit interruption, existing projectile damage, pause, and death checks. Stop Play mode afterward to restore the authored scene. Results are written to `Logs/DogEnemy/play-verification.txt`.

Verified in the running Unity 6000.0.60f1 editor on 2026-10-27 (Asia/Tokyo): compilation; 5,261 normalized weighted vertices; 21 bones; six clips; rendered jump-pose review; and all 32 Play-mode assertions, including forward lunge, airborne skeleton/hitbox alignment, impact timing, sidestepping, obstacle sweeps, interrupted jumps, pause, and landing. A standalone player build was not run.
