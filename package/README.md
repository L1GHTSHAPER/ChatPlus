# ChatPlus

![LightShaper](https://raw.githubusercontent.com/L1GHTSHAPER/ChatPlus/main/tools/assets/lightshaper-wordmark.png)

[Source code on GitHub](https://github.com/L1GHTSHAPER/ChatPlus) | [Report an issue](https://github.com/L1GHTSHAPER/ChatPlus/issues) | [Thunderstore](https://thunderstore.io/c/on-together/p/LightShaper/ChatPlus/)

A BepInEx mod for [On Together](https://store.steampowered.com/app/2688490/On_Together/) that makes the chat nicer to use: the time of every message, a much longer history that comes back after a lobby change and is saved to a file, and a chat window you can resize.

## Features

- **Message time** in front of every message (and, optionally, of the game's notifications), in the format you like: `14:05`, `14:05:09`, `2:05 PM`, `[14:05]` or any .NET time format. Its color and size can be changed. Messages from another day also show the date.
- **Longer history.** The game keeps only 50 lines in the Global tab and 25 in Local; ChatPlus keeps 200 and 100 by default (up to 1000 each).
- **History that survives a lobby change.** Everything said during the game session is remembered: when you join a lobby (again), the chat shows the earlier messages, marked by an "earlier messages" line. Clicking an earlier message still opens the player's ID card. Players you ignore or muted stay hidden.
- **History file.** The session's chat is written to `BepInEx/ChatPlus/chat-history.tsv` (opens in Excel, LibreOffice or any text editor). On the next game start it is moved to `chat-history.previous.tsv`, so the history lasts until the game is restarted. Optionally it can be kept across restarts, and readable daily logs (`BepInEx/ChatPlus/logs/yyyy-MM-dd.txt`) can be written too.
- **Resizable chat window.** Drag the handle at the chat's top-right corner (it shows while the chat is active), or set the width and height in the settings. The frame, the message area and the input field follow; double-click the handle for the game's size. The text size can be changed separately.
- **The chat stays where you put it**, also after a lobby change or a restart.
- **Separate counters while the chat is collapsed:** Global (`G`, or `Г` in Russian) and Local (`L` / `Л`), each independently shown or hidden in the settings. Both are enabled by default and reset when you expand the chat. Each counts received player messages up to 99; your messages, restored history and game notifications do not count. Hiding a counter keeps its count until you expand the chat or leave the lobby.
- **Mention highlight:** messages with your name or your keywords are highlighted.
- **Up / Down** in the empty input field bring back the messages and commands you sent before.
- **Select and copy text:** hold **Shift**, then drag with the left mouse button, within a message or across multiple messages, and press **Ctrl+C**. **Escape** or a click elsewhere clears the selection. An ordinary left-button drag or touch swipe scrolls the chat; the mouse wheel also scrolls. The gesture mode is fixed when you press, so changing Shift during a drag does not switch actions. Dragging a selection beyond the viewport scrolls too. An ordinary click still opens the player's card; a swipe does not.
- **Right-click** within the selection to copy it, or right-click another message to copy its whole text.
- **Background opacity:** set it from 0% (transparent) to 100% (the original background), separately from the text size. This includes the frame, chat and input backgrounds, and Global/Local tab backgrounds. Text and the settings gear remain readable.
- **Settings in the game's style:** a cream/coral F3 window with **Appearance**, **Messages**, **History** and **Controls** tabs, live previews and collapsible advanced options. Open it with **F3**, `/chatplus` or the **gear in the chat header**. The header also shows scrolling, selection and copying hints. Settings save automatically; mod-manager config edits apply while the game is running.
- **Message editor** in **F3 → Messages**: a draft, live preview using the game's text renderer, font selection, solid colors or gradients, bold and italic. Choose colors from presets, type `#RRGGBB`, or click the swatch for RGB sliders. **Insert into chat** copies the draft into the normal chat input; press Enter there to send.
- Optional outgoing formatting uses the game's standard text tags, so recipients do not need ChatPlus. Font choices are the normal game font and built-in **Liberation Sans**; Liberation Sans changes Latin letters, while Russian letters keep the usual fallback font. Gradients use up to eight color bands and fewer bands when needed to fit the game's 250-character limit (including tags). Messages that cannot fit are not sent or truncated, and the draft is kept. Commands and manually tagged messages are left alone.
- Only the sender needs the mod. Other features remain local; outgoing formatting is sent as part of the ordinary message text. English and Russian interface (follows the game's language).

## Usage

| Action | How |
|---|---|
| Settings window | **F3**, `/chatplus` or the gear in the chat header |
| Resize the chat | drag the handle at its top-right corner; double-click it for the normal size |
| Size from the chat | `/chatplus size 150 120` (width and height in %), `/chatplus size reset` |
| Text size | `/chatplus text 120` |
| Background opacity | **F3 → Appearance → Background opacity** (0–100%) |
| Message editor | **F3 → Messages** |
| Scroll the chat | left-button drag / swipe, or mouse wheel |
| Select / copy text | hold **Shift** before left-dragging, then **Ctrl+C** or right-click within the selection; **Escape** clears |
| Message time | `/chatplus time on`, `/chatplus time off`, `/chatplus time format HH:mm:ss` |
| Lines kept in the chat | `/chatplus lines 300 150` (Global, Local) |
| Clear the chat | `/chatplus clear` (the history keeps the lines) |
| Forget the history | `/chatplus clearhistory` |
| Open the history folder | `/chatplus folder` |
| Size and position back to normal | `/chatplus reset` |
| Highlight words | `/chatplus keyword add mark, markus`, `keyword remove mark`, `keyword list`, `keyword clear` |
| Current settings | `/chatplus status` |
| Command list | `/chatplus help` |

`/chp` is a short alias for `/chatplus`. Commands are handled on your side and are never posted to the chat. If [CommandAPI](https://thunderstore.io/c/on-together/p/jaide/CommandAPI/) is installed, the command is also listed by its `/help` and suggested by CommandTypeahead.

## Configuration

`BepInEx/config/ontogether.chatplus.cfg` (created on first launch; editable from the mod manager's Config editor, also while the game is running).

| Section | Key | Default | Description |
|---|---|---|---|
| General | `Language` | `Auto` | `Auto` (the game's language), `English`, `Russian`. |
| General | `SettingsWindow` | `F3` | Opens / closes the settings window. |
| Outgoing | `Enabled` | `false` | Apply the editor's style to messages you send; visible on unmodified clients. |
| Outgoing | `Font` | `GameDefault` | `GameDefault` or `LiberationSans`. The latter changes Latin letters; Cyrillic uses the normal fallback. |
| Outgoing | `ColorMode` | `Original` | `Original`, `Solid` or `Gradient`. |
| Outgoing | `Color`, `EndColor` | `#F2C46D`, `#6AA8FF` | Solid/first color and last gradient color, as `#RRGGBB`. |
| Outgoing | `Bold`, `Italic` | `false`, `false` | Bold and italic tags on outgoing messages. |
| Time | `Enabled` | `true` | Show the time in front of every message. |
| Time | `Format` | `HH:mm` | .NET time format: `HH:mm`, `HH:mm:ss`, `h:mm tt`, `[HH:mm]`... |
| Time | `Color` | `#F5EDE1A6` | `#RRGGBB` or `#RRGGBBAA`; empty = the text's color. |
| Time | `Size` | `85` | Size of the time, in % of the text. |
| Time | `OnNotifications` | `true` | Also for the game's notifications. |
| History | `GlobalLines` | `200` | Lines kept in the Global tab (the game: 50). |
| History | `LocalLines` | `100` | Lines kept in the Local tab (the game: 25). |
| History | `RestoreAfterLobbyChange` | `true` | Show the session's earlier messages after joining a lobby. |
| History | `RestoreNotifications` | `false` | Bring back the game's notifications too. |
| History | `SaveToFile` | `true` | Write the session's chat to `BepInEx/ChatPlus/chat-history.tsv`. |
| History | `KeepAfterRestart` | `false` | Keep (and show) the previous game session's history after a restart. |
| History | `DailyLogs` | `false` | Also write readable logs, one file per day, to `BepInEx/ChatPlus/logs`. |
| Window | `Width`, `Height` | `100`, `100` | Chat window size in % of the game's size (70-300, 50-200). |
| Window | `TextSize` | `100` | Chat text size in % (60-200). |
| Window | `BackgroundOpacity` | `100` | Background opacity in % (0-100). Text and controls are unaffected. |
| Window | `ResizeHandle` | `true` | Show the resize handle on the chat. |
| Window | `RememberPosition` | `true` | Keep the chat where you dragged it. |
| Window | `Position` | *(empty)* | Saved automatically. |
| Window | `CullHiddenLines` | `true` | Lines scrolled out of view are not drawn (keeps a long chat fast). Turn it off if chat text shows outside the chat window. |
| Notifications | `ShowGlobalCounter` | `true` | Show the Global message counter beside the reopen-chat button while the chat is collapsed. Applies immediately. |
| Notifications | `ShowLocalCounter` | `true` | Show the Local message counter beside the reopen-chat button while the chat is collapsed. Applies immediately. Disable both to hide all collapsed-chat counters. |
| Extras | `HighlightMentions` | `true` | Highlight messages that mention you. |
| Extras | `HighlightColor` | `#F2C46D` | Highlight color (made see-through). |
| Extras | `Keywords` | *(empty)* | More words that highlight a message, separated by commas. |
| Extras | `RecallSentMessages` | `true` | Up / Down in the empty input field bring back sent messages. |
| Extras | `RightClickCopies` | `true` | A right click copies the selection on that message, or the whole message without a selection. |
| Extras | `SelectText` | `true` | Shift + left-drag selects text across messages; Ctrl+C copies, Escape clears. Normal dragging and touch swipes always scroll. Disable to turn off selection. |

## Files

| File | What it is |
|---|---|
| `BepInEx/ChatPlus/chat-history.tsv` | This game session's chat: time, channel, type, Steam ID, name, message, the line as shown. Tab-separated, UTF-8. |
| `BepInEx/ChatPlus/chat-history.previous.tsv` | The previous game session's chat. |
| `BepInEx/ChatPlus/logs/yyyy-MM-dd.txt` | Readable logs (only with `DailyLogs`). |

The files are outside `BepInEx/config`, so they are not included when you export or share a mod manager profile.

## Notes

- Very large line limits make the chat slower: every line is a text object in the game's UI. To keep a long chat fast, lines scrolled out of view are not drawn, and the game's rebuild of all chat lines when the chat fades in or out is skipped unless the font outline really changed (it changes only in the desktop overlay modes).
- The size is in % of the game's chat window. The chat is kept on the screen: if a new size would make it stick out, it is moved.
- Works together with ChatSounds, LocalChatRange, CommandAPI / CommandTypeahead and AndrewLin's mods. ChatResizer (jaide) scales the same window; use one of the two.

## Installation

**Thunderstore Mod Manager / r2modman:** install from the mod list, or use *Settings -> Import local mod* with the package zip.

**Manual:** install [BepInExPack](https://thunderstore.io/c/on-together/p/BepInEx/BepInExPack/), then copy `ChatPlus.dll` into `BepInEx/plugins/`.

## Building from source

Requires Windows, .NET SDK 6.0 or newer, an installed copy of On Together, and BepInEx 5 (for example, a Thunderstore Mod Manager / r2modman profile).

From the repository directory, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -GameDir "C:\path\to\On-Together" -BepInExCore "C:\path\to\profile\BepInEx\core"
```

`GameDir` must contain `OnTogether.exe` and `OnTogether_Data\Managed`. `BepInExCore` must contain `BepInEx.dll` and `0Harmony.dll`. Game and BepInEx assemblies are referenced locally and are not distributed in this repository.

The build creates the plugin DLL in `src/bin/Release/` and the installable Thunderstore archive in `dist/`. Ready-to-install archives are also available in [GitHub Releases](https://github.com/L1GHTSHAPER/ChatPlus/releases).

Run the existing tests with .NET SDK 9.0 or newer: `dotnet run --project tests/ChatPlus.Tests.csproj`.
