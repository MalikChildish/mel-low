# Mel-Low — Music Companion

A lightweight Windows desktop overlay avatar that reacts to your music in real time. Mel-Low lives in a corner of your screen, listens to whatever is playing through your system audio, and responds with mood-based chat bubbles, head bobs, and personality quirks.

Built with Unity 6. **100% offline — no internet, no accounts, no data sent anywhere.**

**[Download on itch.io](https://malikchildish.itch.io/mel-low)**

---

## Table of Contents

- [Preview](#preview)
- [The Story](#the-story)
- [Download](#download)
- [Features](#features)
  - [Audio Reactivity](#audio-reactivity)
  - [Mood System](#mood-system)
  - [Chat Bubbles](#chat-bubbles)
  - [Avatar Behaviour](#avatar-behaviour)
  - [Interactions](#interactions)
  - [System Awareness](#system-awareness)
  - [Customization](#customization)
  - [Settings Panel](#settings-panel)
- [Getting Started](#getting-started)
  - [Requirements](#requirements)
  - [Setup in Unity](#setup-in-unity)
  - [Inspector Reference](#inspector-reference)
- [Project Structure](#project-structure)
- [Known Shortfalls](#known-shortfalls)
- [Contributing](#contributing)
- [Credits](#credits)
- [License](#license)

---

## Preview

![Mel-Low in action](https://media.giphy.com/media/jyrQbsh9mtiWJWBWX7/giphy.gif)

---

## The Story

Mel-Low started as an original character I drew back in 2022. I loved the design enough that I tried to get it made into an action figure — that didn't pan out, but I ended up making a small clay figurine by hand that's sat on my desk for the past four years.

This project was my way of finally moving Mel-Low forward. Not a figure, not a drawing — an actual companion. Something that lives on your screen, reacts to your music, and has its own personality. Four years on the desk, now on your desktop.

---

## Download

**Windows 10 / 11 only.**

1. [Download the latest release on itch.io](https://malikchildish.itch.io/mel-low)
2. Create a folder on your PC (e.g. `C:\Apps\MelLow`)
3. Extract the contents of the `.zip` into that folder — keep all files together
4. Run `Mel-Low.exe` from inside the folder
5. Mel-Low will appear in the bottom-left corner of your screen and start reacting to your system audio immediately

> **Note:** Windows may show a SmartScreen warning on first launch since the app is unsigned. Click **More info → Run anyway** to proceed.
>
> **Do not move** `Mel-Low.exe` out of its folder — it needs all surrounding files to run.

---

## Features

### Audio Reactivity

- Captures system audio via **WASAPI loopback** — no microphone needed, works with any app playing sound
- Real-time **FFT spectral analysis** with IIR band filtering across bass, sub-bass, mid, and high frequency bands
- **BPM detection** via autocorrelation with a smoothed beat interval history
- **Vibe energy** score derived from spectral flux, driving mood thresholds independent of raw BPM
- Bones in the spine and neck chain **wave and nod** to the beat with asymmetric attack/decay

### Mood System

Five moods determined each frame from energy, vibe score, and BPM:

| Mood | Trigger |
|------|---------|
| **Silence** | Energy below threshold |
| **Quiet** | Low vibe energy |
| **Vibing** | Moderate vibe energy |
| **Hype** | High vibe energy |
| **Intense** | Very high vibe + BPM above threshold, or extreme vibe escape gate |

Mood detection is disabled entirely when *React to Music* is toggled off, falling back to Quiet for ambient chat.

### Chat Bubbles

- Three bubble styles: **Normal**, **Thought**, and **Hype** — matched to the current mood
- Phrases fire on a randomized cooldown between configurable min/max windows
- Bubble position is screen-aware: floats above or below the avatar depending on available space
- **Opening greeting** on first launch, followed by a **time-of-day greeting** once music starts
- **Day-of-week awareness** — different energy on Fridays, Sundays, Mondays
- **Holiday awareness** — Halloween, Christmas, New Year, Valentine's Day
- **Session end** reaction when music stops after a meaningful play time
- **Long session** comment after a configurable number of minutes
- **Been a while** detection across launches using saved date
- **Volume reactions** — notices mute/unmute, large volume jumps, and drops via Windows Core Audio

### Avatar Behaviour

- **Idle float** — subtle sine-wave bobbing when not being dragged
- **Eye tracking** — pupils follow the cursor with configurable max offset and smoothing
- **Head tilt** — head turns and nods toward cursor proximity with beat-driven overlay
- **Blinking** — randomized blink intervals using a sprite swap pattern
- **Happy brows** — randomly activates during music, hides eyes, pauses blinking for a short duration. Rolls on a configurable interval with a probability chance
- **Drag lean** — whole avatar tilts opposite to drag velocity while being carried; straightens on release

### Interactions

- **Click / poke** — click without dragging counts as a poke
  - 5 pokes → annoyed reaction
  - 10 pokes → mad reaction, **remembered next session**
- **Hold** — hold the avatar for 10 seconds without moving → reaction + **remembered next session**
- **Spin hotkey** — `Left Ctrl + S + M` spins the avatar
- **Mood history streaks** — after 30 seconds of play, compares current session mood to recent history and comments if a streak is detected

### System Awareness

- **Windows system volume** polling via COM (`IAudioEndpointVolume`) — reacts to mute, unmute, large jumps, and drops independent of music silence detection
- **Multi-monitor support** — move the avatar window to any detected monitor via the Settings panel; avatar position is preserved relative to the new screen
- **Position persistence** — avatar position saved to `PlayerPrefs` and restored on next launch
- **Click-through transparency** — the window is transparent and only catches input when the cursor is over the avatar or UI

### Customization

- **Color pickers** for three avatar regions: Beanie, Hoodie, and Head/Neck mesh
- HSV color wheel generated procedurally — no external assets required
- Previous color and default color restore buttons per picker
- Colors persisted to `PlayerPrefs` across sessions

### Settings Panel

- Appears on hover near the avatar's head via a cog icon
- Click outside or press X to close
- **Sensitivity slider** — audio input sensitivity
- **Neck and spine angle sliders** — tune the beat nod range
- **React to Music toggle** — disables mood detection and BPM-driven reactions while keeping time/interaction bubbles active
- **Chat Bubble toggle** — disables all speech bubbles
- **Clear Mood History** button
- **Monitor selector** dropdown

---

## Getting Started

### Requirements

- **Unity 6** (tested on 6000.x)
- **Windows 10 / 11** — WASAPI loopback, transparent window, and Core Audio are Windows-only
- [CSCore](https://github.com/filoe/cscore) — included as a `.dll` in `Assets/Plugins/`
- Unity Input System package (`com.unity.inputsystem`)
- TextMesh Pro (`com.unity.ugui`)

### Setup in Unity

1. Clone or download the repository
2. Open the project in Unity 6
3. Open the main scene in `Assets/Scenes/`
4. Hit Play — the avatar should appear in the bottom-left corner and begin reacting to system audio immediately
5. For a standalone build: **File → Build Settings → Windows x86_64**, ensure *Windowed* and *Transparent* are configured per `TransparentWindow.cs`

### Inspector Reference

| Component | Key Fields |
|-----------|-----------|
| `BeatDetector` | `boneChain` — assign spine/neck bones in order from root up; `sensitivity` — audio input gain |
| `AvatarSpeech` | `beatDetector`, `volumeWatcher`, `moodHistory`, `bubble` — all required for full functionality |
| `AvatarDrag` | `avatarRoot` — the root transform; `_spineBone` / `_neckBone` — optional, for drag lean |
| `HeadTilt` | `headBone` — the head bone transform; `beatDetector` — for nod overlay |
| `BlinkController` | `leftEye`, `rightEye`, `blink` — GameObjects; `happyBrows` — the brows GameObject; `beatDetector` |
| `EyeTracking` | `leftEye`, `rightEye`, `leftPupil`, `rightPupil` — transforms |
| `ColorPickerPopup` | `settingsPanelRect` — assign the settings panel RectTransform for positioning awareness |
| `AvatarColorizer` | `beanieRenderer`, `hoodieRenderer`, `headRenderer`, `neckRenderer`, `picker` |
| `SettingsUI` | `beatDetector`, `avatarHead`, `cogRect`, `panelRect`, `avatarDrag`, `moodHistory` |
| `VolumeWatcher` | No setup required — initializes via COM on Awake |

---

## Project Structure

```
Assets/
├── Avatar/               # 3D model and materials
├── ChildishChatBubbles/  # Bubble sprite sheets
├── Plugins/
│   └── CSCore.dll        # Audio capture library
├── Scenes/               # Main scene
├── Shaders/              # Custom shaders
├── TextMesh Pro/         # TMP standard assets
└── *.cs                  # All scripts (flat structure)
```

---

## Known Shortfalls

These are areas where Mel-Low is intentionally limited in scope or technically constrained by the current implementation.

### Platform
- **Windows only.** WASAPI loopback capture, the transparent overlay window, Core Audio volume watching, and cursor position APIs are all Windows-specific. macOS and Linux are not supported without significant rework.

### Audio
- **No song or artist recognition.** Mel-Low reacts to audio characteristics (energy, BPM, vibe) but has no awareness of what song or artist is playing.
- **Loopback only.** Captures whatever is playing through the system output. Microphone input is not supported.
- **BPM detection is heuristic.** Works well for consistent rhythmic music; can be unreliable on ambient, classical, or heavily syncopated tracks.
- **No genre detection.** All music is treated equally — no differentiation between hip-hop, jazz, EDM, etc.

### Animations
- **Limited expression set.** Mel-Low has blinking, happy brows, head nod, and head tilt. There are no idle animations beyond floating, no arm or body gestures, and no transition animations between states.
- **Fixed rig.** The avatar cannot be swapped out or re-rigged without reassigning Inspector references manually.

### Memory and Persistence
- **Basic session history only.** Mood history tracks session duration and dominant mood but has no awareness of specific songs, playlists, or listening patterns beyond streaks.
- **PlayerPrefs storage.** All persistence uses Unity's PlayerPrefs — not suitable for large data sets and not easily portable between machines.
- **No cloud sync.** Settings and history are local only.

### Language and Localization
- **English only.** All phrases are hardcoded English strings in the Inspector. There is no localization system or external phrase file.
- **No dynamic phrase generation.** Phrases are hand-authored arrays — no LLM or generative text integration.

### UI and Settings
- **No in-app phrase editing.** Adding or changing what Mel-Low says requires editing Inspector arrays in Unity and rebuilding.
- **No theme system.** The UI style is fixed; there is no dark/light mode or skin support beyond the color pickers for the avatar mesh.

---

## Contributing

Pull requests are welcome. For larger changes please contact me or hop into my Discord to discuss: https://linktr.ee/malikallen

When contributing new phrases, keep them short (under 6 words where possible), mood-appropriate, and platform-agnostic.

---

## Credits

- [CSCore](https://github.com/filoe/cscore) by Florian — MIT License. Used for WASAPI loopback audio capture.
- Childish Gang — the YouTube community that made this worth building. Thank you for the support.

---

## License

MIT — see `LICENSE` for details.
