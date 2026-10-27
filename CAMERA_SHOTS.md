# Fixed, cinematic, and story cameras

## MainGame alleyway and deadman discovery

The existing **TriggerZone alleyway** now drives **CinemachineCamera Alleyway**
along a slow retreating and rising path. Its **Alleyway Dolly Markers** contain
independent Player Path Start/End and Camera Start/End transforms for tuning.
The alley uses 0.8-second position damping, 0.65-second rotation damping,
a 2-unit/second movement cap, a 25-degree/second rotation cap, and a 1.6-second blend.
The rail ends before the corner wall, with a 52-degree field of view shared by
the alley and discovery shots to avoid a sudden lens change.

The rig now waits for a stable destination zone for 0.3 seconds, holds each shot
for at least 1.6 seconds, and lets an active exploration blend finish before
switching again. Normal fixed zones remain fixed. Smooth Zone Transitions ignores
zone cuts while enabled, and Minimum Blend Duration prevents old short serialized
blend settings from creating sudden changes. Zone evaluation happens after player
movement and before Cinemachine applies its camera state.

Authored shots use collision checks along the camera's travel. Scenery between
the player and a deliberately placed fixed camera no longer drags that camera
toward the player. The camera still checks for solid overlaps and blocked travel.
Compose paths that stay clear of walls; collision protection can shorten a blocked
move rather than pass through solid geometry. During a camera handoff, near-plane
overlaps are resolved without pinning the incoming shot to the previous camera's
side of a wall. Continuous movement within a shot still sweeps its travel path.

**Deadman Discovery** plays once per scene session when the player comes within
3 meters of the visible body. Its subject uses the actual mesh transform because
the top-level deadman container's pivot is far away. **Deadman Camera Position**
copies your existing **CinemachineCamera Deadbody** position. Discovery blends in
over 1.2 seconds, displays 「死んでる……ここで何があったの？」 for 4.5 seconds, then
blends back over 1.2 seconds. The camera waits for dialogue completion and blocks
movement/aiming through the return. The old persistent body-camera zone is disabled
if it was assigned to the same body camera, so leaving and re-entering cannot replay it.

Edit the line or its duration in
`Assets/Resources/Dialogue/DeadmanDiscovery.asset`. Assign another Dialogue Sequence
to a Story Focus Shot to play dialogue during that shot. Only the owning story
director can start dialogue while a story camera is active; ordinary interactions
remain blocked until it returns. Cancellation also stops that owned dialogue.

To reapply the comfort settings, use **Tools > Cameras > Set Up Smooth Alleyway
And Deadman Discovery**. Existing path markers and authored camera positions are
preserved when the setup runs again. Review and save the scene after adjustments.

## Fixed exploration (default)

Your existing Fixed camera zones remain the normal exploration cameras. Outside
a zone, the rig keeps the last fixed camera, or uses **Default Fixed Camera**.
If that reference is empty it chooses the fixed zone nearest the player at startup.
Only when no fixed camera is available does it fall back to a follow camera.

**Tools > Cameras > Use Fixed Exploration Cameras** disables follow-outside-zones
and cinematic movement zones on the rig. Existing zones and cameras are preserved.
**Follow Outside Zones** is an explicit optional setting.

## Brief story focus (separate system)

1. Select the object you want the player to notice in the scene.
2. Choose **Tools > Cameras > Create Story Focus Shot**.
3. Move/resize the new shot's trigger volume to where the story beat should happen.
4. Move **Story Camera Position** to compose the subject. Alternatively, compose
   Scene view, select the shot, and choose **Tools > Cameras > Align Selected Story
   Shot to Scene View**. The camera aims at **Subject + Subject Offset**.
5. Test the trigger in Play mode and save the scene after reviewing it.

The default sequence blends in for 0.4 seconds, **holds on the subject for one
second**, then blends back to exploration for 0.4 seconds. Each shot plays once
per scene session and locks input through the return blend. Change **Hold Duration**,
**Blend In Time**, **Blend Out Time**, and **Field Of View** per shot. Disable
**Lock Player** to permit movement; aiming stays disabled during story shots.

Story focus works independently of cinematic movement zones. Exploration cameras
keep being selected underneath it, so it returns to the current appropriate view.

For scripted events, disable **Play On Enter** and call `StoryFocusShot.PlayShot()`
from a UnityEvent or story script. **On Started**, **On Finished**, and **On Cancelled**
can connect story actions. Turn off **Play Once** to allow replay on another entry.
Concurrent requests are rejected; triggers retry while the player remains inside
if dialogue or another shot temporarily blocks them. Timers pause with the game.
Missing subjects or disabled shots return safely. Death or disabling the director
immediately releases camera ownership and input.

An optional **Camera End** marker adds a slow move during the timed hold. Without
it, the shot stays at its authored position and tracks the subject. Collision
protection shifts toward the subject and back to the player during the transitions.

Use **Tools > Cameras > Validate Story Cameras** to check timing and cleanup.

## Optional cinematic movement zones

The new **Cinematic** mode on `CamSwitcher` moves an authored camera according to
the player's position. Walking back reverses the shot. Existing Fixed, Moving,
Follow and shoulder aiming behavior remain available. **Enable Cinematic Zones**
on the rig opts into movement zones; it is off by default.

## First shot

Return to Unity and let scripts compile. The earlier automatic starter-shot
request now performs validation only; it does not create or replace exploration shots.

Choose **Tools > Cameras > Create Cinematic Rising Shot** to create another shot
ahead of the player. This explicitly enables cinematic zones on the rig and
creates a trigger volume, an independent Cinemachine
camera, and editable position markers. The starter positions are examples;
place them to suit the actual terrain and avoid walls and trees.

1. Resize the shot's Box Collider to cover the part of the level using this shot.
2. Move **Camera Start** to compose the distant view.
3. Move **Camera End** higher to make the camera rise as the player approaches.
4. Move **Approach Point** to where the player should finish approaching.
5. Set **Far Distance** and **Near Distance** (Far must be greater than Near).
   These use horizontal distance, so changing player height does not shift the shot.
6. Play through the zone in both directions, adjust composition, and save the scene.

## Focus during movement

Select a door, statue, enemy, or other scene object and choose
**Tools > Cameras > Create Cinematic Movement With Object Focus**. The selected object becomes
the shot's **Focus Target**. The default **Focus Weight** curve holds attention
on the player initially, then turns toward the object as the player approaches.
This follows player position; use a **Story Focus Shot** for a timed story beat.

On any Cinematic zone, assign **Focus Target** directly to change the reveal.
Use **Focus Offset** to aim at the right part of a large object. A focus weight
of 0 looks at the player; 1 looks entirely at the object. Keep the final value
below 1 to keep attention between them. For a stationary camera that only pans,
put Camera Start and Camera End at the same position.

## Other shot controls

- **Progress Mode / Along Path:** Player Path Start and End define the player's
  route. The camera travels as the player advances along that route; sideways
  movement does not advance it. Use this for corridors and tracking shots.
- **Camera Control:** Optional middle control point for a curved camera path.
  This is a quadratic curve handle, not a waypoint the camera must pass through.
- **Travel Curve:** Controls how quickly camera movement progresses across the
  player's route or approach distance. The default eases at both ends.
- **Movement Damping / Rotation Damping:** Higher values give slower responses.
- **Animate Field Of View:** Optional gradual lens change between the two values.
- **Zone Priority:** Higher wins in overlapping zones. Equal priority keeps the
  current zone until it exits. The starter shot uses 20; existing zones default to 10.
- **Cut On Enter:** Cut to the shot instead of using the rig's normal smooth blend.

The main camera automatically receives `CameraObstacleAvoidance` when the
exploration rig starts. It checks the final camera position after Cinemachine
and shoulder-aim transitions, pulls in before solid obstacles, and eases back
when clear. The protection radius expands to cover the near clipping plane.
Player colliders and trigger volumes are ignored. Walls and scenery must have
non-trigger colliders on the component's **Obstacle Layers** to be detected.
Use **Tools > Cameras > Validate Camera Obstacle Avoidance** to verify the solver.

Keep the shot's Cinemachine camera free of other position/rotation control
components: this script authors its transform. Keep path markers outside the
camera object so they do not move with it. The creation tools do this for you.
Existing shoulder aiming still takes over while aiming and returns to exploration.

Selected shots display a cyan camera path, yellow approach rings, and a magenta
focus marker in Scene view with Gizmos enabled. Run **Tools > Cameras > Validate
Cinematic Cameras** to check approach distance, endpoints, reverse travel,
curved motion, object focus, lens changes, and legacy movement.

## Choosing shooting style per camera zone

Select a camera trigger zone, such as **TriggerZone alleyway**, in the Hierarchy.
On its **Cam Switcher** component, set **Shooting Mode**:

- **Use Player Default** uses **Default Shooting Mode** on the player's
  **Tank Controls** component. Existing zones use this option; the player default
  is Shoulder Aim.
- **Classic** keeps the exploration camera in control while the player aims,
  hides the crosshair, and fires along the gun barrel. The zone's Fixed, Moving,
  Follow, or Cinematic camera behavior is preserved.
- **Shoulder Aim** uses the shoulder camera, mouse targeting, and crosshair.

Save the scene after choosing a mode outside Play mode. Entering a zone applies
its setting automatically. Overlapping zones use the same winner as camera
selection (Zone Priority, then the newest entry at equal priority). Leaving all
zones restores the player's default shooting mode, even when exploration retains
the last fixed camera. Mode changes preserve a raised gun; switching to Classic
immediately releases the shoulder camera and cancels its pending return blend.

**Classic controls:** Space toggles the aiming stance. While aiming, A/D (or
Left/Right) turns the character, and W/S (or Up/Down) raises/lowers the aim within
the player's Classic Aim Pitch Limit. The player stands in place. Left click or
J fires; R or K reloads. Shooting remains blocked by the existing pause, dialogue,
story-camera, and death controls. Classic aiming uses the existing AimAngle
animation parameter to pose the gun; it does not add automatic target locking.

**Tools > Combat > Validate Zone Shooting Modes (Play Mode)** runs isolated checks
for mode inheritance, zone overlap and exit, classic controls, camera/cursor
handoffs, crosshair visibility, and actual projectile directions. It restores
its temporary camera and cursor state when finished. Results are saved to
`Logs/Combat/shooting-modes.txt`.

### Classic laser sight

Classic aiming now shows a red laser from the muzzle, with a small impact dot on
the nearest wall or enemy. It follows the animated barrel direction, including
vertical aim. Player colliders, projectiles, and non-combat trigger volumes are
ignored; enemy trigger hitboxes can stop the sight. A ray into open space ends
at the configured range without displaying a floating dot.

The sight appears while the gun is raised in Classic mode. It hides when the
gun is lowered, during reloading, pause, dialogue, story shots, death, or Shoulder
Aim. It is added automatically to existing guns at runtime. To customize its
color, beam width, dot size, or maximum distance permanently, add
**Classic Laser Sight** to the same prefab object as **Gun** and edit its fields.
Defaults are red, 30 m range, 0.015 m beam width, and a 0.045 m impact dot.

The original gun's long green cylinder stays hidden; the new sight uses a
depth-tested beam that stops at scenery. Its unlit shader is stored in Resources
so it is included in builds. The combat validation now includes laser behavior
and a rendered visibility check; its preview is `Logs/Combat/classic-laser.png`.
