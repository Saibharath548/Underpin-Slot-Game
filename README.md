# Underpin — 3-Reel Slot Machine

A small slot machine game prototype created for the **Underpin Studio Slot Game Assignment** using **Unity 6**.

The project includes a 3-reel, 3-row slot machine, weighted symbol selection, reel animations, a credit/bet system, win evaluation, audio feedback, and optional bonus features.

> **Development note:** This project was completed with substantial assistance from AI tools. AI was used to help generate, revise, and troubleshoot parts of the code and implementation. I reviewed the project, made adjustments, and integrated the systems, but I am not presenting the code as entirely written manually by me.

---

## Play the WebGL build

A WebGL build is included in [Build/WebGL](Build/WebGL).

To run it locally, use a local web server rather than opening `index.html` directly.

### Python
From the repository root:

```bash
python -m http.server 8000 --directory Build/WebGL
```

Open [http://localhost:8000](http://localhost:8000).

### Node.js
```bash
npx serve Build/WebGL -p 8000
```

### VS Code
Open the repository in VS Code, install the Live Server extension, then launch `Build/WebGL/index.html` with Live Server.

---

## Gameplay

- Use **-** and **+** to change the bet.
- Press **SPIN** to start a round.
- The three reels spin and stop one after another.
- Matching symbols on an active payline award credits according to the paytable.
- The balance and win amount are displayed in the interface.

### Symbols and payouts

The default paytable contains four symbols:

| Symbol | Three matching symbols | Two matching cherries |
|---|---:|---:|
| Lucky Seven | 100× | — |
| Cherry | 30× | 5× |
| Bell | 15× | — |
| BAR | 10× | — |

Payout multipliers are applied to the bet allocated per active payline. The configured scene currently starts with the centre horizontal payline active. The win evaluator contains definitions for up to five paylines: centre, top, bottom, and two diagonals.

### Additional features

- **Free spins:** Three matching BAR symbols on an active payline can trigger the configured free-spin reward. Free spins use the configured payout multiplier.
- **Double-up gamble:** After a win, the player can choose to collect or risk the win in a double-up round.
- **Anticipation:** A matching pair of Lucky Seven symbols on the first two centre positions can extend the final reel's wait.
- **Auto-spin:** Allows consecutive spins until stopped or the game can no longer continue.
- **Paytable panel:** Displays the symbol payout rules.
- **Audio feedback:** Includes button, spin, reel-stop, and win sound effects.

---

## Thought Process & Approach

I approached the project by first breaking down the slot machine into its core systems: symbol generation, reel movement, win evaluation, betting, and UI interaction.

I kept the game logic separate from the presentation so that reel outcomes could be generated and evaluated independently of their animations. The paytable stores the symbol weights and payout rules, while the win evaluator checks the resulting grid against the active paylines.

I then integrated the UI, audio feedback, and bonus features around the core gameplay loop. Throughout development, I tested the mechanics and adjusted the implementation to address issues discovered during integration.

AI tools were used extensively as development assistance for implementation, debugging, and code review, with the generated work reviewed and integrated into the project.

---

## Project structure

The main project content is grouped under `Assets/Underpin/`.

```text
Assets/Underpin/
├── Scripts/
│   ├── Core/       # RNG, paytable data, win evaluation, game flow
│   ├── Reels/      # Reel movement and symbol presentation
│   ├── UI/         # HUD, popups, paytable, lever view
│   ├── Audio/      # Audio playback
│   └── Editor/     # Editor utilities and simulation runner
├── Textures/       # UI and symbol sprites
├── Audio/          # Sound assets
├── Prefabs/        # Reusable game objects
└── Scenes/
    └── SlotMachineScene.unity
Build/
└── WebGL/          # Browser build
Packages/           # Unity package manifest and lock file
ProjectSettings/    # Unity project settings
```

### Main systems

- **SlotGameManager:** Coordinates a round, manages credits and bets, starts reel spins, and connects the game systems.
- **SlotRNGService:** Selects symbols using the weights in the paytable. It supports a deterministic seed for repeatable tests; the default constructor creates a seed using a cryptographic random-number source.
- **WinEvaluator:** Checks the generated 3×3 result against the configured paylines and payout rules.
- **ReelStripController:** Handles reel scrolling, symbol recycling, and settling on the generated result.
- **SlotUIManager:** Connects the interface buttons and updates the displayed balance, bet, win, and game messages.
- **SlotAudioManager:** Plays the game's sound effects.

---

## Testing and verification

An Editor utility is included at **Underpin → Tests → Run RNG & Win Simulation (10,000 Spins)**.

It runs a repeatable simulation with a fixed seed, generates 10,000 outcomes, evaluates the configured five paylines, and reports symbol frequencies, win frequency, jackpot counts, and estimated return-to-player (RTP) for that simulation.

The simulation is a development check for the implemented rules and a sample of outcomes. It is **not** an independent certification of randomness, fairness, or a guaranteed RTP for every play session. Results can vary if the paytable weights or payout settings change.

---

## Notes

- Open the project with a compatible Unity version and let Unity import the assets before running the scene.
- The WebGL build is provided as a convenience; browser performance and compatibility may vary by device.
- The Editor utilities are development tools and are not required to play the included WebGL build.

---

*Created for the Underpin Studio Slot Game Assignment.*
