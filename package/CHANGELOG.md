# Changelog

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
