# ChatPlus 1.5.0 — UI validation

Version 1.5.0 adds nickname styling in chat, nameplates, Tab and the game's shared ID-card name. On October 9, 2026, the user requested release of the revision 2 implementation without installing or testing that revision in game. Automated formatting, encoding, owner-only synchronization, debouncing, acknowledgement/retry and restoration checks use narrow native doubles. Actual rendering and reception on a second unmodified client have not been validated; this checklist is retained for future verification.

## Install

Close On-Together normally before replacing the plugin. Install only one ChatPlus DLL in the active mod-manager profile. Keep a copy of the previously installed DLL for rollback. Settings and chat history do not need to be deleted.

## In-game checks

- Open F3 and use the new chat settings button. Both should open the cream/coral settings window. Check all five tabs, the close button, window dragging, repeated reopening, and the Russian/English labels. On narrow screens verify the final Controls tab is present on the last row.
- In Nickname, enable styling and try solid colors, gradients, font, bold and italic. Check the preview with Cyrillic and emoji. Send a normal message in Global and Local; have an unmodified client verify the styled name, ordinary message body and correct sender card. Repeat with message styling independently on and off, including a message filling its 250-character budget.
- With Above my character and in Tab on, change styles without sending a chat message. Nameplate, an already open Tab list, and the game's shared ID-card name should update on owner, host and an unmodified remote client after a short pause. Join another client after styling to verify the current name is sent to late arrivals. Repeat with host and guest ownership; styling must never change another player's profile or any non-name profile fields.
- Turn the world option off while leaving nickname styling enabled: chat should keep its style and nameplates/Tab should restore the raw name. Toggle nickname styling off entirely and repeat. After restoration, the mod must not continuously overwrite other mods' world-name changes. Rejoin/restart with both scope settings to check persistence.
- Disable nickname styling and send again: the new message should use the original name, and old history should retain its previous appearance. Reset nickname style without changing message style, restart and rejoin to verify saved settings, and check that the player's saved nickname itself never changes.
- Under Appearance, move text size from 60% through 100% to 200%, and background opacity through 0%, 50% and 100%. The real chat, settings button including its gear, and resize handle should update immediately and restore their original alpha at 100%. Chat text and selection highlighting retain their opacity. Restore the preferred values afterwards.
- At exactly 0% opacity, verify the frame, chat underlay, input background, World/Local tab fills and borders, settings button and resize handle disappear. The controls' hit areas remain usable while the native chat is active; F3 also opens settings. Repeat with off-screen line culling disabled to check the stencil mask still shows messages.
- Expand time and window controls. Check that all prior options are present, advanced settings scroll on a small screen, and switching tabs preserves each tab's scroll position. Check 720p and 1080p if available.
- In the chat, drag with the left mouse button starting on a message. It should scroll without opening a player card. Hold Shift before pressing the left mouse button to select across wrapped lines/messages; releasing Shift during selection must keep selecting. Ctrl+C and right-click should copy the selection. A plain click should still open a player card.
- Let the chat fade, then start Shift + drag on the first press that activates it. Font-outline refresh must not cancel selection. Also start a selection after focusing the input; the unsent draft must remain intact.
- Disable text selection and check that the header updates, normal dragging still scrolls, and the settings button still works. Re-enable it afterwards.
- Check the chat input background, typed text and panel move handle keep their native positions. Resize to minimum width/height and back, drag the chat, collapse/expand it and let it fade. The header must not overlap messages or remain clickable after the chat fades out. The Settings/F3 button should be visible inside the frame at the top right.
- The settings button is now a gear icon. Hover it to see Settings and the configured shortcut; clicking opens F3 without focusing or shifting the chat input.
- In Messages, type a draft and test font, solid color, gradient, bold, italic and RGB controls. Preview should appear only in the active tab. Insert a draft into the input without sending it; confirm normal input focus and the preserved draft.
- Drag F3 and scroll its panels at 1080p and 1440p. Preview text must stay inside its own box, clip at the scroll viewport and disappear when changing to History/Controls or closing F3. Check the Messages preview also preserves colors and gradients.
- On Appearance and Messages, repeatedly change preview text, font, color and size, switch tabs and reopen F3. The log must contain no new UniversalCameraData/already-created or SubmitRenderRequest exceptions. Preview requests are deferred to LateUpdate; the queue tests cover coalescing, cancellation and reentrant callbacks, but cannot validate URP rendering in the game.
- Change lobby, then restart the game. Confirm one header per chat, unchanged Global/Local behavior, saved settings and normal history restoration. Check BepInEx/LogOutput.log for new ChatPlus, GUILayout, TMP or raycast errors.

## Automated checks

The suite exercises history, formatting, selection text, the actual chat pointer routing and vertex opacity with narrow Unity doubles, and deferred preview scheduling. It does not simulate Unity layout, render the new UI or replace the in-game checks above.
