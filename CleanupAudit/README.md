# Game asset cleanup audit — 26 October 2026

No game assets were changed or deleted.

## Result

- Scanned 3308 source assets under Assets, excluding .meta files from asset counts and size estimates.
- The only enabled build scene is Assets/Scenes/Prototyping/MainGame.unity.
- 2601 files (446.9 MB) have no traced reference from any saved scene, project settings, or protected roots. These are review candidates, not a guaranteed-safe automatic deletion list.
- A further 616.9 MB is referenced by other saved scenes but not traced from MainGame/protected roots. Keep it if you want those test or demo scenes.
- Size figures describe source files on disk, not compressed player/build savings. MB = 1,000,000 bytes; CSV also includes MiB.

## Start here

1. Close Unity before clearing Library (3.71 GB), Logs (31.5 MB), or obj (0.15 MB). These are generated cache/log/build-intermediate folders. Library will be regenerated and the next import may take time. Logs also contains copies of validation projects; they are not the game project. Do not delete Assets, Packages, ProjectSettings, or .meta files indiscriminately.
2. Move unused archives, backup models and documentation outside the project if you still want to keep the originals. Candidate .rar/.blend1/.docx/.pdf files total 61.3 MB. These are included in the 447 MB estimate, not additional savings. Preserve package license files.
3. Review the completely unreferenced packs: Assets/Abandoned_House (61.3 MB), Assets/The Wasteland LITE (54.5 MB), and Assets/Art/Modles/Characters/PSX Zombie Pack 1.4 (32.3 MB). The game does use other zombie animations and character assets; do not delete the whole Characters or Animations directory.
4. Review individual candidates in mixed-use folders using unused-candidates.csv. In particular, Art, SketchFabs, LowpolyStreetPack, Industrial_exterior_v2 and EnemyTypes contain retained game assets. Do not delete these whole folders.
5. The JP Environmental Asset Pack and YughuesFreeConcreteMaterials are predominantly used by non-build demo/test scenes in this scan. Removing their demo scenes does not automatically mean every shared asset is safe to delete; review the other-scenes-only list as a separate batch.

## Candidate source sizes by folder

| Folder | Candidate MB | Candidate files | Retained/protected MB | Other-scenes-only MB |
|---|---:|---:|---:|---:|
| Assets/Art | 275.5 | 2043 | 28.8 | 55.0 |
| Assets/Abandoned_House | 61.3 | 83 | 0.0 | 0.0 |
| Assets/The Wasteland LITE | 54.5 | 81 | 0.0 | 0.0 |
| Assets/SketchFabs | 20.1 | 88 | 0.9 | 0.0 |
| Assets/JP Environmental Asset Pack | 13.4 | 13 | 0.0 | 347.1 |
| Assets/LowpolyStreetPack | 12.2 | 202 | 68.5 | 0.0 |
| Assets/EnemyTypes | 3.1 | 2 | 3.7 | 0.0 |
| Assets/Industrial_exterior_v2 | 2.8 | 70 | 0.7 | 0.0 |
| Assets/Logs | 2.2 | 3 | 0.0 | 0.0 |
| Assets/YughuesFreeConcreteMaterials | 1.7 | 3 | 0.0 | 213.2 |

## Largest individual candidates

- 12.9 MB — Assets/Abandoned_House/Models/Abandoned_House.glb
- 12.8 MB — Assets/Abandoned_House/Models/Abandoned_House.blend
- 12.8 MB — Assets/Abandoned_House/Models/Abandoned_House.blend1
- 11.0 MB — Assets/Abandoned_House/Models/Abandoned_House.dae
- 10.7 MB — Assets/The Wasteland LITE/Textures/Walls_Map_1A.png
- 10.4 MB — Assets/Art/Textures/SBSTextures/SBS - Tiny Texture Pack - 128x128/SBS - Tiny Texture Pack - 128x128.rar
- 10.0 MB — Assets/The Wasteland LITE/Textures/Structures_Map_1A.png
- 9.9 MB — Assets/The Wasteland LITE/Textures/Walls_Map_1A(Darkened).png
- 9.8 MB — Assets/Art/Packs/Models/Mine/Minas/Models/Mines_Modu.dae
- 9.4 MB — Assets/Art/Textures/SBSTextures/SBS - Tiny Texture Pack 3 - Small/SBS - Tiny Texture Pack 3 - Small.rar
- 9.2 MB — Assets/JP Environmental Asset Pack/Shaders/How to set up.docx
- 8.5 MB — Assets/The Wasteland LITE/Textures/Metals_Map_1A.png
- 8.5 MB — Assets/Art/Textures/SBSTextures/SBS - Noise Texture Pack - 128x128/SBS - Noise Texture Pack - 128x128.rar
- 7.7 MB — Assets/Art/Packs/Models/Trailer_Park/Trailer_Park/Models/Trailer_Park.fbx
- 5.4 MB — Assets/Art/Textures/SBSTextures/SBS - Tiny Texture Pack 2 - 128x128/SBS - Tiny Texture Pack 2 - 128x128.rar
- 5.1 MB — Assets/SketchFabs/Bar/bar_end.fbx
- 4.7 MB — Assets/Art/Modles/Characters/PSX Zombie Pack 1.4/PS1_Zombie.blend
- 4.5 MB — Assets/Art/Modles/Characters/PSX Zombie Pack 1.4/PS1_Zombie1_Mix.blend
- 4.2 MB — Assets/SketchFabs/Apartments/psx-apartment (1)/textures/1(10).png
- 4.1 MB — Assets/Art/Textures/SBSTextures/SBS - Horror Texture Pack 128x128/SBS - Horror Texture Pack 128x128.rar

## Keep / investigate for player-size optimization

- LowpolyStreetPack/Meshes/Textures/atlas.tga is referenced and is 62.0 MB on disk. Inspect its Unity texture import resolution/compression before changing it. Source file size does not equal its built size.
- Resources/Japanese/NotoSansJP.ttf is used by JapaneseText and should be kept (9.6 MB source).
- Audio/2-05. 3000 Cycles (I missed you).flac is referenced (4.6 MB source). Inspect AudioClip compression settings if build size is the actual target.
- MainGame, terrain data, runtime Resources, shaders, scripts and editor/plugin code were conservatively retained. A protected status means keep for this audit, not proof that every asset is used during gameplay.

## Method and limitations

Static recursive reference audit of saved scene/prefab/material/controller/animation/terrain-layer and other serialized assets, their importer .meta references, Unity binary serialized GUID references, and ProjectSettings. All saved scenes were traced separately from the enabled build scene. All scripts, shaders/includes, Resources, StreamingAssets, Editor, plugin and Gizmos folders were retained conservatively, along with literal Assets paths found in scripts. The code search found Resources.Load calls for dialogue, Japanese fonts and the retro screen effect; those assets were preserved. No Addressables or AssetBundle loading calls were found in the searched Assets C# files.

This is not Unity AssetDatabase.GetDependencies or a fresh BuildReport. Unsaved editor changes, procedural/name-based loading, model-embedded/relative file dependencies, package-defined references, custom importers and external build scripts can escape static tracing. Materials may also contain stale references which overestimate usage. Review and remove candidate assets in Unity in small batches after a backup, then test MainGame and make a fresh build before permanently discarding the backup. Keep .meta files with any assets you archive or restore.

The last stored build report is dated 13 October and may not match the current project, so it was not treated as the current game inventory. No build or play test was run for this read-only audit.

Unity normally excludes unused, unreferenced assets from a player build; Resources assets are included even without direct scene references. Consequently this cleanup primarily reduces the project folder, not necessarily the exported game:
https://docs.unity3d.com/kr/6000.0/Manual/assets-managing-introduction.html

## Files

- unused-candidates.csv — review candidates sorted by largest source size.
- other-scenes-only.csv — assets referenced by saved scenes outside the current enabled game scene.
- asset-inventory.csv — all scanned files and their classifications.
- summary.json — folder totals, retained files, scene roots and cache sizes.
