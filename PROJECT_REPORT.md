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

## 2026-11-02 — Keep snow outside sheltered areas

- The existing snowEffect now receives roof protection automatically when its scene loads. Snowflakes disappear on solid scenery instead of resting on or travelling through it, and newly spawned flakes beneath overhead colliders are removed.
- Uses downward roof checks to support single-sided roof meshes, excludes Player and Enemy layers and trigger volumes, and only probes newly emitted particles to limit ongoing physics work. Roofs need solid colliders to provide shelter.
- Affected files: Assets/_Scripts/Systems/SnowShelter.cs.
- Verification: scene snowEffect and existing collision settings inspected; the new script compiled successfully against the installed Unity 6000.0.60f1 assemblies. Visual indoor/outdoor testing has not yet been performed.


## 2026-11-02 — Playable village opening

- Added a runtime opening in MainGame using the existing car, girl, alley and body discovery. The player hears her arrival thoughts, checks dispatch at the car with E, completes the girl's existing Japanese conversation, hears three original muffled knocks from the alley, then reaches the existing one-time body reveal and a final investigation thought.
- Early exploration remains possible; the dialogue and reveal unlock in order. Pausing freezes progression, cancelled dialogue does not count as completed, and reloading the scene starts the opening again. The existing nearby spawn is preserved; distant spawns are moved beside the car only when a NavMesh point is available.
- Added dialogue completion notification so progression follows finished conversations rather than button presses or timers. No scene assets or camera compositions are overwritten.
- Affected files: Assets/_Scripts/Systems/OpeningSequence.cs, Assets/_Scripts/Systems/DialogueSystem.cs, Assets/_Scripts/Systems/Editor/OpeningSequenceValidation.cs.
- Verification: runtime and editor scripts compiled successfully against the project Unity assemblies. An isolated additive-scene integration check is queued on editor import for ordered interactions, cancellation, pause and the complete opening flow; its execution has not been confirmed. In-game visual/audio verification remains pending.


## 2026-11-02 — Body reveal no longer missed during alley dialogue

- Fixed a missed-reveal case when the player passes the body while the alley subtitle is still playing. The opening now remembers entering the discovery area and retries the camera reveal after dialogue or quick-turn activity finishes.
- Distance triggers and the alley sound now use the subject's configured focus offset, keeping them aligned with the visible body.
- Strengthened opening validation to cross the body trigger during dialogue, walk away, and run the actual story camera through start/hold/return before checking the final thought.
- Affected files: Assets/_Scripts/Systems/OpeningSequence.cs, StoryFocusShot.cs, Editor/OpeningSequenceValidation.cs. Runtime and editor compilation passed. The stronger editor regression check is queued for the next import; its new result has not yet been confirmed. Visual gameplay check pending.


## 2026-11-02 — Live opening-sequence diagnosis

- Added editor-only state capture during Play mode to diagnose the reported missing alley cue and body reveal in the actual MainGame scene. Captures the current story beat, dialogue, trigger bounds, player/body positions, camera readiness and reveal status once per second to Logs/Opening/live-state.txt.
- No gameplay trigger or scene settings changed in this diagnostic step. Root cause remains under investigation despite the isolated regression test passing.
- Affected file: Assets/_Scripts/Systems/Editor/OpeningLiveDiagnostics.cs.

## 2026-11-02 — Restore the original body reveal

- Removed the new opening's alley knocking, alley thought, body-reveal gate/retries and added aftermath thought. The opening now ends after the girl's conversation and never changes the original Deadman Discovery trigger or camera/dialogue settings.
- Restored StoryFocusShot's original proximity calculation. The existing camera move, hold and body dialogue remain under the pre-existing story-camera system. Removed temporary live-state capture.
- Retained arrival, car dispatch and girl dialogue. Updated the opening check to assert that the original body trigger remains enabled throughout the intro.
- Affected files: OpeningSequence.cs, StoryFocusShot.cs, Editor/OpeningSequenceValidation.cs; removed Editor/OpeningLiveDiagnostics.cs and its metadata. Runtime and editor compilation passed; visual playtesting pending.


## 2026-11-04 — Shootable main-menu prototype

- Added a separate MainMenu scene workflow using the existing village/alley and player character. The character remains stationary, follows the mouse with her aim, and right-click fires a visible menu projectile with the existing weapon animation and sound.
- Play, Settings and Quit appear as simple targets on the right. A target only activates when the projectile reaches its collider; it breaks into twelve short-lived fragments before navigation. Misses do not navigate, and duplicate hits are blocked during the transition.
- Settings has shootable volume down/up, fullscreen and Back targets. Volume persists into gameplay; Quit stops Play mode in the Editor and exits in builds. Menu shots have no gameplay damage path or ammunition limit.
- Setup creates Assets/Scenes/MainMenu.unity from the saved MainGame scenery and places it first in Build Settings. The source MainGame scene and gameplay controls are not edited.
- Created MainMenu and registered it as the first enabled build scene; MainGame remains enabled. Added a foreground camera so scenery cannot hide the character or targets, plus an explicitly included unlit shader for the boards, fragments and tracers. Added MAIN_MENU.md with controls and setup notes.
- Affected files: Assets/_Scripts/Menu/GunMenu.cs, MenuBullet.cs, MenuFragment.cs, ShootableMenuTarget.cs; Assets/_Scripts/Systems/Editor/GunMenuSetup.cs; Assets/Resources/MenuSurface.shader; Assets/Scenes/MainMenu.unity and metadata; ProjectSettings/EditorBuildSettings.asset; MAIN_MENU.md; Logs/Menu/setup-result.txt and manual-validation.txt.
- Verification: Unity 6000.0.60f1 compiled the scripts and ran scene setup successfully. In Play mode, verified the visible alley menu, mouse aiming, Settings, volume down/up, Back, missed shots leaving the menu unchanged, Quit stopping the preview, and Play loading MainGame with its Japanese arrival dialogue and HUD. Rechecked target rendering, Settings, Back and Play after the final target-depth and shader changes. MainMenu is left open in Edit mode, ready to preview. Standalone fullscreen/application exit and audio listening remain unverified.

## 2026-11-04 — Plane Crash title and Pixelta game UI

- Converted the two supplied TTF files into reusable TextMeshPro assets with saved static SDF atlases and materials at Assets/Resources/Fonts/PlaneCrash.asset and Pixelta.asset. The original files remain in Assets/Fonts. Added Tools/Fonts/Prepare Game Fonts to repeat setup and validation.
- Applied Plane Crash only to the main-menu title. This font uses lowercase character slots for its distressed capital lettering, so the title renders as NO DAWN instead of the pictograms mapped to uppercase slots.
- Applied Pixelta to menu buttons, settings, hints, reticle, dialogue labels and the SurvivalHUD (including ammunition, controls, pause/death text). Pixelta is also the TMP default for new text. Existing Noto Sans JP remains the fallback for Japanese and unsupported symbols; Japanese text is not translated or replaced. Dynamic fallback data is cleared on build.
- Affected files: Assets/_Scripts/Systems/GameFonts.cs; Assets/_Scripts/Systems/Editor/GameFontSetup.cs; Assets/_Scripts/Menu/GunMenu.cs; Assets/_Scripts/Systems/DialogueSystem.cs and SurvivalHUD.cs; Assets/Resources/Fonts and metadata; Assets/Resources/Japanese/JapaneseFont.asset; Assets/Art/Packs/Unity/TextMesh Pro/Resources/TMP Settings.asset; MAIN_MENU.md. Verification notes: Logs/Fonts/setup-result.txt; preview captures: Assets/Logs/Fonts.
- Verification: Unity 6000.0.60f1 compiled the final scripts and successfully reran font setup. Verified saved atlas/material sub-assets, essential Latin glyphs and Japanese fallback. Play-mode inspection confirmed Plane Crash on the title and Pixelta on the remaining menu and gameplay text, including inactive HUD labels. Visually checked the corrected title, menu buttons, ammo and Japanese arrival subtitle; confirmed the player loop advanced. Restored the background-run setting and left MainMenu open outside Play mode. Standalone build rendering has not been tested.

## 2026-11-04 — Main-menu laser, editable placement and scene cleanup

- Added a red muzzle-to-aim laser with endpoint dot. The beam checks the first menu target with the same 0.025m sweep radius as menu bullets. Color and width can be adjusted on Menu Controller. Only the menu is affected.
- Removed the runtime character/camera placement override. Menu Character and Menu Camera are now authored scene objects with serialized references. Move Menu Character with W or Transform Position outside Play mode, then save; the running menu preserves her position while rotating to aim. Retained weapon animation, muzzle flash and shot audio without gameplay Gun, movement or health components.
- Cleaned MainMenu from 954 to 117 objects: removed enemies, NPCs, dialogue, discovery triggers, extra gameplay cameras, navigation and off-screen/inactive scenery. Kept the visible alley, character, light, ambience, terrain and menu snow with its roof-protection component and scenery colliders. Shared project assets and MainGame were not edited. A pre-cleanup backup is Logs/Menu/MainMenu-before-cleanup-20261104-074311.unity.
- Scene size reduced from 847,920 bytes to 341473 bytes (about 60%). This measures serialized scene size; FPS and standalone load time were not benchmarked.
- Affected files: Assets/_Scripts/Menu/GunMenu.cs; Assets/_Scripts/Systems/Editor/MenuSceneCleanup.cs and metadata; Assets/Scenes/MainMenu.unity; MAIN_MENU.md; Logs/Menu/cleanup-result.txt. Preview: Assets/Logs/Menu/clean-menu.png.
- Verification: Unity compiled the menu changes and saved the scene through the Editor. Confirmed advancing Play-mode frames, one laser renderer, no TankControls or ZombieAI in the menu, and visible character/title/buttons/alley. Fired actual menu projectiles at Settings, Back and Play; each navigated correctly and Play loaded MainGame. Runtime character position matched the authored scene position. No new errors appeared in the check; a prior CLI timeout remained in historical logs. Added explicit snow shelter protection after this visual check; indoor snow behavior and standalone build remain unverified. Restored the temporary background-run setting, exited Play mode and selected Menu Character for editing.

## 2026-11-04 — Main-menu shooting feedback

- Added a short camera recoil kick, an 80ms warm screen flash and a reticle pulse whenever a menu shot actually fires. Camera motion settles over 180ms and restores the authored transform before mouse aiming is calculated, preventing accumulated camera drift.
- Added a four-tick hit marker that fades over 220ms when a projectile reaches a menu target, alongside the existing break fragments. Feedback uses a separate nonblocking overlay and runs only in MainMenu; it does not add gameplay damage effects or change the character's authored position.
- Affected files: Assets/_Scripts/Menu/MenuShotFeedback.cs and metadata; Assets/_Scripts/Menu/GunMenu.cs; MAIN_MENU.md. Peak-effect inspection capture: Assets/Logs/Menu/shot-feedback-peak.png.
- Verification: Unity compiled the changes; confirmed advancing Play-mode frames. Live probes verified nonzero camera recoil, visible flash alpha and exact camera position/rotation recovery. Fired real projectiles to open Settings and return with Back. Checked zero raycast-blocking images in the feedback overlay and visually inspected flash/hit-marker rendering by temporarily holding the peak effect; normal timing was restored afterward. No new errors were observed. Restored the background-run setting and returned to Edit mode, preserving the existing unsaved scene edits. Standalone rendering and subjective motion feel remain unverified.

## 2026-11-04 — Left-click menu shooting

- Changed MainMenu shooting to the left mouse button and updated both main-menu/settings hints and the controls documentation.
- Affected files: Assets/_Scripts/Menu/GunMenu.cs; MAIN_MENU.md.
- Verification: inspected the input binding (GetMouseButtonDown(0)) and both hint strings. Live mouse input and Unity compilation were not retested for this binding-only change.

## 2026-11-04 — First-person equipment and condition inspection

- Added an I-key first-person equipment check with smooth entry/return, mouse focus between the gun and wrist, and I/Esc exit. The character remains stationary and cannot fire or reload during inspection. Esc closes the view without also opening Pause. Enemies/world simulation keep running.
- Added a separate visual-only hands/pistol prefab, using the current sleeves and weapon plus basic five-finger close-up palm meshes, wrist bandage/stains and readable skin shading. A procedural tilt/magazine-out-and-back motion checks ammunition without modifying loaded/reserve counts. These are basic low-poly meshes, not the detailed textured hands of the concept artwork or a newly authored reload animation.
- Japanese contextual labels show actual loaded/capacity/reserve ammo, condition and an approximate remaining-hit count. Condition updates on damage/healing; the estimate uses the last observed damage amount, initially 10. Removed the permanent ammo/combat/controls HUD and shoulder-aim crosshair. Kept dialogue, interaction prompts, pause/restart controls and the existing blood health vignette; dialogue overlays defer/hide during inspection.
- Added camera/control ownership guards for story shots, dialogue, movement and weapons. Closing, death and scene teardown restore the authored camera, cursor, layer mask and body rendering. The inspection camera/models are reused while in the scene and removed on teardown. No MainGame scene or source player prefab asset was edited.
- Affected files: Assets/_Scripts/Systems/PlayerInspection.cs, InspectionViewModel.cs, Editor/InspectionViewSetup.cs and metadata; Assets/Resources/Inspection (prefab, generated meshes/materials/skin shader and metadata); Assets/_Scripts/Systems/SurvivalHUD.cs, DialogueSystem.cs, StoryCameraDirector.cs; Assets/_Scripts/Player/TankControls.cs and ShoulderAim.cs; Assets/_Scripts/Gun/GunController.cs; INSPECTION.md; Logs/Inspection/manual-validation.txt. Preview: Assets/Logs/Inspection/inspection-preview.png.
- Verification: Unity 6000.0.60f1 compiled the final scripts and shader; visually inspected the view and final hand shader. Direct keyboard/mouse testing covered I opening, left-click checking, W movement blocking and Esc closing without pausing. Confirmed unchanged ammo, blocked shooting/reloading, live damage/healing labels, death restoring camera/body and clean restart with one hands/system instance. Final prefab opens after art refinements. Corrected intermediate generated-mesh name warnings. Restored the temporary background-run setting and left MainGame open in Edit mode. Standalone builds, a complete tour of camera/story zones and audio listening remain unverified; detailed hand artwork/animation polish remains a limitation of this first implementation.

## 2026-11-04 — Simplify equipment view and add wrist health watch

- Removed the gun-inspection/magazine animation, click-to-check action and check/reload sound. The pistol stays in its held pose and actual ammunition text stays visible above it. Normal gameplay reload behavior remains available outside the equipment view.
- Restored both hands from the original player geometry, pose and materials. Removed custom procedural palm generation and its mesh/skin assets after verifying they were unreferenced. Replaced the added bandage/stain details and floating health text with a compact physical wrist watch: five red health boxes, with depleted boxes shown dark.
- Set the active MainGame player maximum health and HealthManager's default to 50. Standard zombies/scissors deal 10 damage, so one normal hit removes one box and five normal hits are fatal from full health. The dog's existing 15 damage is preserved. Boxes follow actual health, rounding a partially occupied box up, and healing restores them.
- Affected files: Assets/_Scripts/Systems/PlayerInspection.cs, InspectionViewModel.cs, Editor/InspectionViewSetup.cs; Assets/_Scripts/Player/Health Manager.cs; Assets/Scenes/Prototyping/MainGame.unity; Assets/Resources/Inspection/Hands.prefab and regenerated original-hand meshes; new WatchStrap/Case/Screen/Red/Empty materials and metadata. Removed L/R-InspectionPalm meshes, Skin material/shader, Bandage/Blood materials and metadata. Updated INSPECTION.md and Logs/Inspection/manual-validation.txt; preview at Assets/Logs/Inspection/wrist-watch-preview.png.
- Verification: Unity compiled the final scripts and rebuilt the prefab; inspected the original hands, static pistol, ammo text and smaller five-box watch in Play mode. Verified magazine transforms remain unchanged, five-to-four boxes after 10 damage, restoration after healing, survival after four normal hits and death/clean view exit on the fifth. Inspected scene enemy damage values and verified removed assets were not prefab dependencies. Restored temporary background-run state and left MainGame open in Edit mode. Standalone build remains untested.

## 2026-11-04 — Restore shoulder-aim laser sight

- Enabled the existing red weapon beam and impact dot during active shoulder aiming as well as classic aiming. Shoulder mode points the beam from the muzzle toward the same mouse-controlled aim point used by projectiles and extends its trace to the shoulder aim distance.
- Laser stays hidden during equipment viewing, reloads, pause, death, blocking dialogue and story-camera shots. The original mesh guide remains hidden.
- Affected file: Assets/_Scripts/Gun/ClassicLaserSight.cs.
- Verification: Unity compiled the final script successfully; inspected shoulder-mode visibility, aim-point direction, range and hide conditions. A live aiming/impact visual check was not performed in this change.

## 2026-11-05 — First-person aiming with laser sight

- Replaced the shoulder aiming view with a centered, eye-level pistol view using the original hands and pistol from the inspection prefab. The camera zone and player dropdowns show **First Person Aim** and preserve existing selections.
- The red laser and projectiles share the visible muzzle position and mouse-controlled aim point. Nearby solid scenery limits the muzzle position, and the first-person beam and close-range impact dot are smaller for the tighter framing.
- Updated `Assets/_Scripts/Player/ShoulderAim.cs`, `FirstPersonAimModel.cs` and metadata, `ShootingMode.cs`, `Assets/_Scripts/Gun/GunController.cs` and `ClassicLaserSight.cs`, `Assets/_Scripts/Systems/CamSwitcher.cs` and `Editor/ShootingModeValidation.cs`, and `CAMERA_SHOTS.md`.
- Unity compilation and 42 controlled Play-mode checks passed, covering eye-level camera placement, model visibility, shared muzzle alignment, firing and switching back to Classic. Reviewed the MainGame render at `Logs/Combat/first-person-aim.png`. Standalone builds and a manual keyboard playthrough remain unverified.

## 2026-11-05 — Fix FPS aim with non-readable meshes

- Fixed the errors on entering FPS aim. Pistol placement now uses mesh bounds, which are available at runtime with Read/Write disabled. The setup also checks that both weapon meshes exist before creating the foreground camera.
- Updated `Assets/_Scripts/Player/FirstPersonAimModel.cs`. Unity compiled successfully and all 42 shooting checks passed in MainGame Play mode with both the barrel and slide reporting `isReadable == false`. Standalone builds were not tested.

## 2026-11-05 — Hide FPS hands on exit and widen aim

- Leaving FPS aim now deactivates the entire foreground model, preventing exploration and equipment inspection cameras from drawing leftover hands or a duplicate pistol. The model reactivates on the next aim.
- Widened the default FPS field of view from 50° to 58° for a less zoomed-in view.
- Updated `FirstPersonAimModel.cs`, `ShoulderAim.cs`, and `ShootingModeValidation.cs`. All 43 controlled MainGame Play-mode checks passed, including model deactivation on mode switch and focus loss. The live player reported a 58° aim setting. Manual visual inspection and standalone builds remain unverified.

## 2026-11-05 — Clearer FPS Glock and smaller bullets

- Added a dedicated low-poly Glock for first-person aiming with a dark beveled slide, rear plate, slide serrations and distinct iron sights. Adjusted the hand placement and aligned the laser and shot origin with the close-up muzzle.
- Scaled the gameplay projectile visual to 35% of its previous size. The root hit sphere, damage and speed retain their existing values.
- Updated `Assets/_Scripts/Player/FirstPersonAimModel.cs`, added `FirstPersonPistol.cs` and `Assets/Resources/FirstPersonPistol.shader` with metadata, and updated `Assets/Prefabs/projectile.prefab`. Unity compilation and all 43 MainGame Play-mode shooting checks passed. Inspected the final render at `Logs/Combat/detailed-glock.png`. Standalone builds were not tested.

## 2026-11-05 — Restore the original textured Glock in FPS aim

- Restored the game's original textured Glock meshes and materials for FPS aim and removed the procedural pistol script and shader. Lowered the pistol slightly, moved it farther from the camera, and brought the support hand closer to the grip. The laser uses the original barrel position, and bullets retain their smaller visual size.
- Updated `Assets/_Scripts/Player/FirstPersonAimModel.cs` and removed the procedural pistol files and their Unity metadata. Unity compilation and all 43 MainGame Play-mode shooting checks passed. The final pose was not visually reviewed.

## 2026-11-05 — Sharper FPS Glock texture and angled pose

- Applied the existing 256×256 Glock texture through a material used by the FPS weapon. Turned the hands and pistol 13° around the muzzle to show more of the slide while keeping the laser and shot origin aligned.
- Updated `Assets/_Scripts/Player/FirstPersonAimModel.cs` and added `Assets/Resources/FirstPersonGlock.mat` and its metadata. Unity compiled, all 43 MainGame shooting checks passed, and the rendered preview confirmed the 256×256 texture and angled pose. Standalone builds were not tested.

## 2026-11-05 — Straighten the FPS Glock

- Removed the 13° turn from the FPS pistol and hands in `Assets/_Scripts/Player/FirstPersonAimModel.cs`. The original Glock points straight ahead with the 256×256 texture.
- Verified the pose change in source. Unity Play mode was not rerun.

## 2026-11-05 — Restore the straight-on Glock silhouette

- Widened the FPS Glock by 20% around the muzzle and disabled retro vertex snapping on its close-up material. The full rear slide is visible in straight-on aim, with the laser still centered.
- Updated `FirstPersonAimModel.cs`, `FirstPersonGlock.mat`, and the PSX Lite Unlit shader and include. Unity compiled and all 43 Play-mode shooting checks passed. Visually verified the full slide in `Logs/Combat/wide-fps-glock.png`.
