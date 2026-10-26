# PS1 screen effect

Silent Hill 1 inspired atmosphere adds muted green-gray grading and depth fog
from 8 to 32 world units. Adjust Atmosphere, Saturation, Color Tint, Fog Color,
Fog Start, and Fog End in the shared settings. Set Atmosphere to zero to return
to the plain PS1 screen treatment. VHS effects are absent.

Fog uses the camera depth texture. Opaque materials need a ShadowCaster pass to
appear correctly in depth; transparent particles and glass generally do not write
depth, so fog over those surfaces follows the opaque geometry behind them.
This treatment does not reduce geometry draw distance or replace scene lighting.

Switch to Unity and let scripts import, then press Play. For the project's PC
quality setting (Built-in renderer), RetroScreenEffect automatically attaches to
gameplay cameras, including cameras activated later. No scene setup is required.

Open **Tools > REimagined > PS1 Screen Settings** to select the shared settings asset
at Assets/Resources/RetroScreenSettings.asset. Change settings outside Play mode
to save them. Turn off Effect Enabled to disable the look across all cameras.

Defaults: 240 vertical pixels with the camera's aspect ratio preserved, 32 levels
per color channel, ordered dithering.
This is a screen treatment; it does not add vertex
wobble or affine texture mapping to models. Overlay UI stays sharp because Unity
draws it after camera image effects. Camera-space/world-space UI is affected.

The automatic setup skips URP, render-texture cameras, and Editor preview cameras.
The Mobile quality setting currently uses URP and needs a separate renderer feature.
If other post effects are attached to a camera, their component order determines
whether they run before or after this effect. Avoid stacking another pixelation pass.

Validation: the three C# files compile against the installed Unity 6000.0.60f1
assemblies. Shader compilation and the final appearance still need verification
in the Editor after asset import.
