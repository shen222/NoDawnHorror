# Shootable main menu

Open **Assets/Scenes/MainMenu.unity** and press Play. The scene is also first in Build Settings, so builds start at the menu.

- Move the mouse to aim. The character stays planted.
- Left-click to fire. The projectile must reach a target before it breaks and activates.
- Shoot Play to load MainGame, Settings to open settings, or Quit to exit.
- Settings has shootable volume minus/plus, fullscreen and Back targets. Escape also returns from Settings.
- Quit stops Play mode when testing in Unity; it closes the application in a build.

Volume is saved between sessions. Fullscreen is saved for standalone builds. The menu has unlimited ammunition and its projectiles cannot damage gameplay characters.

MainMenu contains the menu character, camera, controller, visible alley scenery, lighting, ambience and snow. The authored Menu Controller references these objects; only targets, overlay and foreground camera are created at runtime. MainGame and shared prefab assets remain independent. Change labels/layout in Assets/_Scripts/Menu/GunMenu.cs.

Play-mode checks are recorded in Logs/Menu/manual-validation.txt. Standalone fullscreen and executable quit still need a build check.

The menu title uses Assets/Resources/NoDawnLogo.png: the approved white Pixelta logo with a small Japanese subtitle and a transparent background. GunMenu displays it at the top left, excluding the export's transparent margins. Pixelta is used for all other menu and gameplay text. The usable TMP assets are in Assets/Resources/Fonts. Japanese uses the existing Noto Sans JP fallback. Run Tools/Fonts/Prepare Game Fonts to validate/recreate missing font assets.

To place the character: stop Play mode, select Menu Character in the Hierarchy, press W in the Scene view and drag the movement arrows, or type exact Transform > Position values. Press F to frame her and Ctrl+S to save. GunMenu preserves that position; only facing and aim change during play. Movement keys do not move her in the running menu. Select Menu Camera to change the shot framing. Large framing changes may need additional scenery because off-screen village objects were removed.

The red laser runs from the muzzle toward the mouse aim point and stops at the first menu target using the same collision radius as menu bullets. Laser color and width are editable on Menu Controller > GunMenu. It is separate from the gameplay gun laser.

The original scene backup is in Logs/Menu/MainMenu-before-cleanup-20261104-074311.unity. Cleanup results are in Logs/Menu/cleanup-result.txt. Shared meshes, textures and gameplay scenes were kept; this reduces the menu scene size and active object count, without deleting shared project assets.

Menu shots now trigger a short camera kick, warm flash and reticle pulse. Successful target hits show a fading hit marker before the existing break/navigation. The effect restores the authored camera before mouse aiming each frame and uses nonblocking UI. MenuShotFeedback contains the effect strength and duration defaults.
