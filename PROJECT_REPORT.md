# Project report

Ongoing record of completed bug fixes, new features, and meaningful improvements.
Dates use Asia/Tokyo.

## Bug fixes

- **2026-10-26 — Fixed camera switching.** Corrected obstacle recovery that could hold the rendered camera near the previous view; changed the alleyway and another moving zone to fixed views. Equal-priority overlapping zones now select the most recently entered zone, selected cameras are enabled automatically, and follow-camera control stops while a fixed zone is active. Fixed views cut instantly; aiming keeps smooth transitions. Verification reported in **Add shaders to the game**: Unity Play-mode checks passed at 42 sampled positions across eight zones, plus aiming transitions. Scene saved. The dead-body story sequence intentionally overrides exploration near the body.
- **2026-10-13 — Tree-rendering performance correction prepared.** Queued restoration of the previous tree rendering after an earlier warning repair extended full-detail visibility. Compilation passed; application in the editor and FPS improvement were not confirmed. Source: **Add more game features**.

## New features and improvements

- **2026-10-26 — Health shown through screen redness.** Replaced the HP number and bar with a red tint that strengthens as health drops, fades after health pickups, and clears at full health. File: `Assets/_Scripts/Systems/SurvivalHUD.cs`. Compilation passed; Play-mode appearance was not verified in that chat. Source: **Add more game features**.
- **2026-10-26 — Camera diagnostics and repair tools.** Added alleyway diagnostics, a fixed-alleyway reframing option, and camera-zone validation tools. Compilation reported successful. Source: **Add shaders to the game**.
- **2026-10-26 — Asset and script cleanup audits.** Created reports in `CleanupAudit/` identifying likely unused assets and tracing script usage. Static inspection only; no assets or scripts deleted and no build/play test performed for the audits. Source: **Troubleshoot ground painting**.
- **2026-10-16 — Exploration camera system.** Added follow behavior outside zones, fixed viewpoints, a moving-camera example, smooth blends, delayed exits, and shoulder-aim priority. Compilation passed; scene setup was queued and in-game framing remained unverified at that point. Source: **Add more game features**.
- **2026-10-12 — Japanese localization prepared.** Added Japanese UI, prompts, dialogue, and font setup. Compilation passed; automatic editor setup and in-game display still required confirmation. Source: **Add more game features**.

## Known issues from reviews

- **2026-10-22 — Rising cinematic shot setup.** Review found the shot in Follow mode, cinematic zones disabled, and Approach Marker progress unsuitable for the intended entrance-to-exit rise. No changes were made in that review. Runtime and editor compilation passed; motion and timing remained unverified because automated testing stalled during license initialization. Source: **Add more game features**. Later fixed-zone changes may supersede parts of this setup; recheck before treating it as a current bug.

## Reporting notes

- Tracking started on 2026-10-26.
- Initial partial backfill on 2026-10-26 covers recent turns from **Add more game features**, **Add shaders to the game**, and **Troubleshoot ground painting**. Older pages and the portfolio chat remain to be reviewed; this is not yet a complete history.
- Verification above is attributed to the source chats; these checks were not rerun while compiling this report.
- For each entry, include the date, what changed, affected files, and verification where available. Replace the empty-section placeholder when adding the first entry.

## 2026-10-27 — Rigged dog enemy

- Replaced the static dog in `MainGame` with a reusable enemy prefab using the existing dog model and material. Added an editable 21-bone quadruped skeleton, skin weights for 5,261 vertices, and idle, walk, run, bite, hurt, and death animations. The Unity prefab and mesh asset contain the rig; the original FBX remains the source model.
- Added 40 HP, a 15-damage bite with a dodgeable windup, bullet damage through the existing projectile system, hit reactions that interrupt attacks, and death that disables navigation/collisions before the body sinks away.
- Added random roaming around the spawn point, pauses between walks, sight and movement-based hearing, NavMesh pursuit, last-known-position memory, a territory limit, complete-path checks, and patrol recovery. The dog uses the existing agent-type-0 NavMesh; no navigation surface or bake was changed. Testing caught and corrected a patrol arrival-distance mismatch and Unity's automatically assigned agent height offset, so the dog continues patrolling and stands on the ground.
- Affected files: `Assets/EnemyTypes/Dog/` (runtime AI, rigged prefab, weighted mesh, controller, six animation clips, editor setup/verification tools, README, and Unity metadata), `Assets/Scenes/Prototyping/MainGame.unity`, and this report. Source assets are in the existing `Assets/SketchFabs/Dog/` folder.
- Verification: Unity 6000.0.60f1 editor compilation passed. Rig validation confirmed normalized skin weights, matching bind poses, animation assignment, and real mesh deformation; six animation poses were rendered and reviewed. All 21 Play-mode assertions passed, covering grounded placement, advancing frames, roaming, pursuit, bite timing and single-hit damage, dodging, wall obstruction, interruption, damage from the existing projectile prefab, invalid damage, pause, death, and disabled hitboxes/navigation. Results and previews are in `Logs/DogEnemy/`. The editor was returned to Edit mode. Standalone build and extended manual gameplay balancing remain unverified.

## 2026-10-27 — Dog jump attack

- Replaced the dog's short standing bite animation with a 1.15-second pounce: crouch, hind-leg push-off, airborne leg poses, forward reach, landing compression, and recovery. The dog jumps approximately 0.62 m high and lunges up to 1.1 m toward the player's position when the attack begins.
- Synchronized the damage collider with the animated jump and moved the single 15-damage check to 0.65 seconds. Attacks begin within 2 m and require the player within 1.25 m at impact. Players can sidestep the committed direction; scenery and NavMesh edges constrain travel. Hits interrupt the pounce, cancel its damage/travel, and restore the collider. Pause freezes the airborne attack.
- Updated the existing prefab and attack clip in place, with an editor upgrade tool that preserves the rig and other enemy settings. Fixed pose previews to render fresh baked skinning for each sample, and added jump-pose and Play-mode verification.
- Affected files: `Assets/EnemyTypes/Dog/Runtime/DogEnemyAI.cs`, `Assets/EnemyTypes/Dog/Editor/DogEnemySetup.cs`, `Assets/EnemyTypes/Dog/Editor/DogEnemyVerification.cs`, `Assets/EnemyTypes/Dog/Editor/DogEnemyPlayVerification.cs`, `Assets/EnemyTypes/Dog/Animations/DogBite.anim`, `Assets/EnemyTypes/Dog/DogAnimator.controller`, `Assets/EnemyTypes/Dog/DogEnemy.prefab`, and `Assets/EnemyTypes/Dog/README.md`. Saved the current `MainGame` scene.
- Verification: editor compilation passed; six jump phases were rendered and visually reviewed; baked-mesh checks confirmed all paws leave the ground and return after landing. All 32 Play-mode assertions passed, including forward motion, airborne skeleton/hitbox alignment, single impact damage, sidestepping, non-homing direction, wall obstruction, midair interruption, pause, landing, original projectile damage, and death. Preview and results: `Logs/DogEnemy/jump-attack-poses.png` and `Logs/DogEnemy/play-verification.txt`. Standalone build and extended gameplay balancing were not tested.

## 2026-10-27 — Shooting style selected by camera zone

- Added a **Shooting Mode** dropdown to each camera zone: **Use Player Default**, **Classic**, or **Shoulder Aim**. Added **Default Shooting Mode** to the player. Existing zones inherit the player default, which remains shoulder aiming.
- Classic mode keeps the exploration camera, hides the crosshair, and restores stationary character-relative aiming: A/D turns and W/S raises/lowers the gun while aiming. Space toggles the aiming stance; left click/J fires and R/K reloads. Shots follow the muzzle rather than the camera target. Fixed-camera obstacle handling remains active while aiming in Classic mode.
- Zone overrides follow the winning camera zone, including overlap priority and entry order. Leaving all zones restores the player default. Switching styles while aiming keeps the gun raised, safely restores camera/cursor control, and cancels the old shoulder blend when entering Classic mode.
- Affected files: `Assets/_Scripts/Player/ShootingMode.cs`, `Assets/_Scripts/Player/TankControls.cs`, `Assets/_Scripts/Player/ShoulderAim.cs`, `Assets/_Scripts/Gun/GunController.cs`, `Assets/_Scripts/Systems/CamSwitcher.cs`, `Assets/_Scripts/Systems/ExplorationCameraRig.cs`, `Assets/_Scripts/Systems/Editor/ShootingModeValidation.cs`, associated new Unity metadata, `CAMERA_SHOTS.md`, and the saved `MainGame` scene with the new default fields. No zone was forced to Classic; the user can choose the desired encounters in the Inspector.
- Verification: Unity editor compilation passed; existing camera-zone regression checks passed; all 28 Play-mode integration checks passed with controlled inputs and actual projectile instances. Checks covered classic turning/pitch limits/no movement, camera ownership, fixed-shot obstacle handling, muzzle versus crosshair trajectories, shared magazine consumption, live mode switching while aiming, cursor/crosshair cleanup, inheritance, overlap, disabled zones, exit fallback, and focus loss. Results: `Logs/Combat/shooting-modes.txt`. Standalone build and manual keyboard/controller playtesting were not performed.

## 2026-10-27 — Laser sight for classic shooting

- Added a red laser beam and small surface impact dot while the player aims in Classic mode. The sight follows the animated muzzle, stops at the nearest scenery or enemy hitbox, and ignores the player's colliders, projectiles, and non-combat triggers. Open-space aiming uses a 30 m beam without a floating impact dot.
- The sight hides when the player lowers the gun, reloads, pauses, dies, enters dialogue or a story shot, or switches to Shoulder Aim. Existing weapons receive the component automatically at runtime. The original long green guide cylinder remains hidden. Color, width, range, and dot size can be customized with the Classic Laser Sight component.
- Affected files: `Assets/_Scripts/Gun/ClassicLaserSight.cs`, `Assets/Resources/ClassicLaserSight.shader`, their Unity metadata, `Assets/_Scripts/Gun/GunController.cs`, `Assets/_Scripts/Systems/Editor/ShootingModeValidation.cs`, and `CAMERA_SHOTS.md`.
- Verification: Unity editor compilation passed and all 39 controlled Play-mode integration checks passed, including the previous shooting-mode checks plus laser creation, muzzle alignment/range, wall and enemy trigger detection, owner/non-combat-trigger filtering, reload/pause/lowered-gun hiding, and mode switching. A rendered visibility check passed, and `Logs/Combat/classic-laser.png` was visually reviewed. Results: `Logs/Combat/shooting-modes.txt`. Unity was returned to Edit mode. Standalone builds and extended manual gameplay were not tested.
