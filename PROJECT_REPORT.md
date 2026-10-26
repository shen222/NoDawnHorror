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
