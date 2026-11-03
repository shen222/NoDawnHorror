# First-person equipment view

In MainGame, press I to open the first-person equipment view. Move the mouse left to look at the wrist watch or right to look toward the gun. Press I or Esc to return to exploration. Esc closes this view without also opening Pause.

The gun is held still, with live loaded/capacity and reserve ammunition text above it. Clicking no longer starts a gun check, moves the magazine or plays reload audio. Firing and actual reloading remain blocked while this view is open.

Both hands now use the original player model geometry, original pose and materials. The custom procedural palms and skin shader/material were removed. The wrist band has a physical watch screen with five red boxes and dark empty slots, replacing the previous health description and estimated-hit text.

MainGame player health is now 50; standard zombie/scissor damage is 10, so five normal hits are fatal from full health and one normal hit removes one box. Each lit box represents up to 10 remaining health, rounding partial boxes up. The dog still deals 15 damage and can deplete boxes faster. Healing restores boxes according to actual health. The HealthManager default is also 50; other explicitly serialized health settings are not overridden at runtime.

Movement and firing are stopped, but the world continues running. Death and closing the view restore the camera, cursor and original character visibility. Dialogue, interaction prompts, pause/restart and the existing health vignette remain; no permanent ammo or controls panel is shown during exploration.

To rebuild the visual prefab, open MainGame outside Play mode and choose Tools > Player > Build Inspection Hands. The builder uses a temporary copy and never edits the original player hand meshes or materials. Resources/Inspection/Hands.prefab contains the original-hand copies, static pistol and wrist watch. Watch case/box layout is defined in InspectionViewSetup; live boxes are updated by InspectionViewModel.

Validation: Logs/Inspection/manual-validation.txt. Current preview: Assets/Logs/Inspection/wrist-watch-preview.png. Standalone build behavior has not been tested.
