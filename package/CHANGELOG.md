# Changelog

## 1.4.0

- Redesigned F3 in the game's cream/coral style with Appearance, Messages, History and Controls tabs, live previews, and collapsible advanced controls. Settings still save automatically.
- Added a settings gear and gesture hints inside the chat header. The input and move handle keep their native positions.
- Fixed 0% opacity for native chat backgrounds and Global/Local tab fills and borders while preserving text, stencil masks and the settings gear.
- Fixed Shift + drag selection for Input System mouse devices and native font-material refreshes, including selection started from a focused input. Ordinary dragging still scrolls.
- Kept preview text inside its IMGUI box and deferred preview rendering to LateUpdate with a reentry guard, fixing UniversalCameraData errors. Preview output now has the depth buffer required by URP Render Graph.

## 1.3.1

- Restored native chat scrolling by holding the left mouse button and dragging, including when starting on message text. Touch swipes also scroll.
- Text selection now uses Shift + left-drag: hold Shift before pressing the mouse button. The mode stays fixed throughout the gesture, even if Shift is released or pressed while dragging.
- Forwarded the complete scroll drag lifecycle, including initialization to stop previous inertia. Dragging no longer opens a player's card on release; an ordinary click still does.
- Updated English/Russian settings hints and usage instructions.

## 1.3.0

- Select parts of chat text by dragging with the left mouse button, including across messages and wrapped lines. Ctrl+C copies the selection; Escape or an outside click clears it. The mouse wheel scrolls, and dragging beyond the viewport scrolls the selection.
- Right-click within the selection copies it; otherwise right-click still copies the whole message. A normal left click still opens the player's card.
- Added a background opacity slider (0–100%) beside the existing text size slider (60–200%) in F3. Background opacity leaves text and controls unchanged and respects the game's fade.
- Selection is enabled by default and can be disabled in F3 or with Extras.SelectText. Window.BackgroundOpacity defaults to 100%; settings are saved and apply during play.

## 1.2.0

- Added a message editor to the top of the F3 settings window, with a draft, live TextMeshPro preview and an Insert into chat button.
- Added optional outgoing solid colors and gradients, HEX entry, color presets and RGB sliders, and bold/italic text.
- Added font choices for the normal game font and built-in Liberation Sans. Recipients do not need the mod; Liberation Sans changes Latin letters, while Cyrillic uses the usual fallback font.
- Outgoing formatting uses standard font/color tags. Gradients use up to eight color bands and reduce the number to fit the 250-character message limit, preserving Unicode text elements. Oversized styled messages are rejected with a local explanation and keep the draft.
- Commands, explicitly tagged messages, and the raw drafts used by Up/Down recall keep their original text. Formatting is off by default; all options are saved in the Outgoing config section.

## 1.1.1

- Updated the package icon and README with the rounded LightShaper logo and black, white and purple visual style.
- Plugin behavior is unchanged.

## 1.1.0

- Split collapsed-chat notifications into separate Global and Local message counters (up to 99 each), reset when the chat is expanded.
- Added independent visibility settings for both counters in the F3 window (English/Russian) and the Notifications config section. Changes apply while the game is running.
- Only received player messages count; own messages, restored history and game notifications are excluded.

## 1.0.1

- Added the public GitHub repository link to the package website and README.
- Published source code and packaged downloads on GitHub. Mod behavior is unchanged.

## 1.0.0

- Initial release.
- Time in front of every chat message (format, color and size can be changed; optional for notifications; the date for messages from another day).
- Longer chat history: 200 lines in Global and 100 in Local by default (the game keeps 50 and 25), up to 1000 each.
- The game session's chat comes back after a lobby change, and is saved to `BepInEx/ChatPlus/chat-history.tsv`; optionally kept across game restarts, optional readable daily logs.
- Resizable chat window: a handle at the top-right corner of the chat, width and height settings, a 9-sliced frame; text size setting; the chat remembers where it was dragged.
- A long chat stays fast: lines scrolled out of view are not drawn, lines are re-wrapped once a resize settles, and the game's rebuild of all lines when the chat fades in or out is skipped when nothing changed.
- Highlight of messages that mention you, Up / Down recall of sent messages, right-click copy of a message.
- In-game settings window (F3 or `/chatplus`), `/chatplus` (`/chp`) commands, config edits in the mod manager apply while the game is running.
- English and Russian interface.
- Optional integration with CommandAPI (`/help` listing, CommandTypeahead suggestions).
