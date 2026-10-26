# Script usage audit

Inspected 60 C# files, totaling 260,325 bytes (about 260 KB of source). No scripts were changed or deleted.

## Strong cleanup candidates

- Assets/_Scripts/Zombie/ZombieAnimator.cs — effectively empty (1 byte). No serialized script references or code callers found.
- Assets/_Scripts/Zombie/ZombieState.cs — defines ZombieState and ZombieStateMachine, but no serialized references or external code callers were found. Existing ZombieAI uses its own implementation. Candidate for removal after compiling and testing.

## Tutorial-only code

Assets/Unity/TutorialInfo/Scripts/Readme.cs is used by ReadmeEditor.cs and Assets/Unity/Readme.asset. Remove these together only if you no longer want the Unity welcome/tutorial inspector. Do not delete Readme.cs alone while retaining ReadmeEditor.cs.

## Found only through non-build scenes

- `Assets/AmmoPickup.cs` (549 bytes)
- `Assets/Art/Packs/VFX/Camera FX Shaders (By Mylan.T)/C# scripts/PlayerMovement.cs` (2511 bytes)
- `Assets/Art/Packs/VFX/Camera FX Shaders (By Mylan.T)/C# scripts/Shader C sharp scripts/Blinking dot/BlinkingDotEffect.cs` (971 bytes)
- `Assets/Art/Packs/VFX/Camera FX Shaders (By Mylan.T)/C# scripts/Shader C sharp scripts/Blur/CameraBlur.cs` (741 bytes)
- `Assets/Art/Packs/VFX/Camera FX Shaders (By Mylan.T)/C# scripts/Shader C sharp scripts/Bulged/BulgedEffect.cs` (622 bytes)
- `Assets/Art/Packs/VFX/Camera FX Shaders (By Mylan.T)/C# scripts/Shader C sharp scripts/Color pulse/ColorPulseEffect.cs` (725 bytes)
- `Assets/Art/Packs/VFX/Camera FX Shaders (By Mylan.T)/C# scripts/Shader C sharp scripts/Dot mask/DotMaskEffect.cs` (712 bytes)
- `Assets/Art/Packs/VFX/Camera FX Shaders (By Mylan.T)/C# scripts/Shader C sharp scripts/Glitch/GlitchEffect.cs` (894 bytes)
- `Assets/Art/Packs/VFX/Camera FX Shaders (By Mylan.T)/C# scripts/Shader C sharp scripts/Nausea/NauseaEffect.cs` (868 bytes)
- `Assets/Art/Packs/VFX/Camera FX Shaders (By Mylan.T)/C# scripts/Shader C sharp scripts/Retro/RetroEffect.cs` (1000 bytes)
- `Assets/Art/Packs/VFX/Camera FX Shaders (By Mylan.T)/C# scripts/SwitchEffects.cs` (19994 bytes)
- `Assets/Art/Packs/VFX/PSXShaderKit/Demo/PSXExample_CameraMovement.cs` (689 bytes)
- `Assets/Art/Packs/VFX/PSXShaderKit/Demo/SpookyOrbFloat.cs` (710 bytes)
- `Assets/Art/Packs/VFX/PSXShaderKit/Scripts/PSXPostProcessEffect.cs` (8527 bytes)
- `Assets/Art/Packs/VFX/PSXShaderKit/Scripts/PSXShaderManager.cs` (6561 bytes)
- `Assets/Art/Packs/VFX/VFXPACK_IMPACT_WALLCOEUR_FreeVersion/Scripts/DropdownController.cs` (1528 bytes)
- `Assets/Art/Packs/VFX/VFXPACK_IMPACT_WALLCOEUR_FreeVersion/Scripts/VfxController.cs` (777 bytes)
- `Assets/HealthPickup.cs` (545 bytes)

These files still have references in test/demo scenes or their prefabs. Removing their scripts alone leaves missing components there. AmmoPickup and HealthPickup have pickup prefabs and can be useful future gameplay features; absence from MainGame is not proof they are unwanted.

## Keep for current game or runtime code dependencies

- `Assets/EnemyTypes/scissors/Animated/Runtime/ScissorsAnimationPreview.cs` (1062 bytes)
- `Assets/EnemyTypes/scissors/Animated/Runtime/ScissorsEnemyAI.cs` (8089 bytes)
- `Assets/_Scripts/Gun/bullet.cs` (4363 bytes)
- `Assets/_Scripts/Gun/GunController.cs` (6273 bytes)
- `Assets/_Scripts/Gun/shell.cs` (682 bytes)
- `Assets/_Scripts/Player/AnimationDriver.cs` (1947 bytes)
- `Assets/_Scripts/Player/Health Manager.cs` (2891 bytes)
- `Assets/_Scripts/Player/MovementState.cs` (1587 bytes)
- `Assets/_Scripts/Player/ShoulderAim.cs` (8870 bytes)
- `Assets/_Scripts/Player/TankControls.cs` (6418 bytes)
- `Assets/_Scripts/Systems/CameraObstacleAvoidance.cs` (7356 bytes)
- `Assets/_Scripts/Systems/CamSwitcher.cs` (6113 bytes)
- `Assets/_Scripts/Systems/DialogueInteractable.cs` (1310 bytes)
- `Assets/_Scripts/Systems/DialogueSequence.cs` (411 bytes)
- `Assets/_Scripts/Systems/DialogueSystem.cs` (8935 bytes)
- `Assets/_Scripts/Systems/ExplorationCameraRig.cs` (10457 bytes)
- `Assets/_Scripts/Systems/JapaneseText.cs` (1095 bytes)
- `Assets/_Scripts/Systems/NpcIdleAnimation.cs` (2021 bytes)
- `Assets/_Scripts/Systems/RetroScreenEffect.cs` (3470 bytes)
- `Assets/_Scripts/Systems/RetroScreenSettings.cs` (699 bytes)
- `Assets/_Scripts/Systems/StoryCameraDirector.cs` (8018 bytes)
- `Assets/_Scripts/Systems/StoryFocusShot.cs` (3494 bytes)
- `Assets/_Scripts/Systems/SurvivalHUD.cs` (10549 bytes)
- `Assets/_Scripts/Zombie/ZombieAI.cs` (8555 bytes)

DialogueSystem, SurvivalHUD, RetroScreenEffect and StoryCameraDirector have runtime initialization hooks. Some gameplay scripts are added or accessed through code rather than attached directly to the saved scene. In particular, do not remove ShoulderAim, CameraObstacleAvoidance, JapaneseText or StoryCameraDirector merely because no saved component reference appears. ScissorsAnimationPreview is referenced by the animated enemy prefab and ScissorsEnemyAI.

## Editor tools

- `Assets/EnemyTypes/scissors/Animated/Editor/ScissorsAnimationSetup.cs` (12179 bytes)
- `Assets/EnemyTypes/scissors/Animated/Editor/ScissorsCombatVerification.cs` (6712 bytes)
- `Assets/Unity/TutorialInfo/Scripts/Editor/ReadmeEditor.cs` (6677 bytes)
- `Assets/_Scripts/Systems/Editor/AlleywayCameraDiagnostics.cs` (5141 bytes)
- `Assets/_Scripts/Systems/Editor/AlleywayCameraSetup.cs` (8553 bytes)
- `Assets/_Scripts/Systems/Editor/CameraObstacleValidation.cs` (6126 bytes)
- `Assets/_Scripts/Systems/Editor/CameraZoneValidation.cs` (5259 bytes)
- `Assets/_Scripts/Systems/Editor/CinematicCameraSetup.cs` (12069 bytes)
- `Assets/_Scripts/Systems/Editor/DialogueSetup.cs` (4467 bytes)
- `Assets/_Scripts/Systems/Editor/ExplorationCameraSetup.cs` (5999 bytes)
- `Assets/_Scripts/Systems/Editor/JapaneseSetup.cs` (4270 bytes)
- `Assets/_Scripts/Systems/Editor/ProjectWarningRepair.cs` (5751 bytes)
- `Assets/_Scripts/Systems/Editor/RetroScreenSetup.cs` (905 bytes)
- `Assets/_Scripts/Systems/Editor/StoryCameraSetup.cs` (5389 bytes)
- `Assets/_Scripts/Systems/Editor/StoryCameraValidation.cs` (6420 bytes)

These provide editor setup, repairs, validation and tutorial tooling. They are not player gameplay scripts and removing them offers no meaningful player-size savings. Retain them if you want their menu tools or setup behavior.

## Scope

Static tracing from MainGame, project settings (excluding disabled build-scene entries), Resources, and runtime initialization methods, followed through serialized references and C# type-name mentions. A second pass included every saved scene. Code-name matching is conservative and can count comments or name collisions as dependencies. This is not a compiler call graph or a runtime coverage test. Unsaved changes, reflection, string-based component loading and custom packages may escape the scan. No compile/build/play test was performed because no gameplay code was changed.

Detailed references and callers are in scripts.json. The earlier general asset audit conservatively kept all scripts; this report is the separate script-specific analysis.
