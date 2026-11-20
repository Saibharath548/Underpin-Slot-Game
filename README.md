# 🎰 Underpin - Classic 3x3 Video Slot Machine

An architectural, highly-polished 3-Reel Classic Slot Machine game built with **Unity (Unity 6 / 2022+ LTS compatible)** for the **Underpin Studio Slot Game Assignment**.

Features smooth physics-driven reel spinning, certified deterministic and seedable RNG mathematics, multi-line paytable evaluation, responsive UI/UX, synthesized retro arcade audio, and creative bonus mechanics including **Free Spins**, **Double-Up Gamble Mini-Game**, and **Anticipation Spins**.

---

## 🎮 Playable WebGL Build & Run Instructions

A fully compiled and optimized WebGL build is included directly in this repository inside the [`Build/WebGL`](Build/WebGL) directory.

### Quick Start (Local HTTP Server)

Because modern web browsers enforce strict security policies regarding WebAssembly (`.wasm`) and Cross-Origin Resource Sharing (CORS), opening `index.html` directly via `file://` protocol is blocked by the browser engine. You can run the build locally using any standard command-line web server:

#### Option 1: Python 3 (Recommended)
```bash
# From the repository root
python -m http.server 8000 --directory Build/WebGL
```
Then navigate to: **[http://localhost:8000](http://localhost:8000)**

#### Option 2: Node.js / npx
```bash
# Using npx serve
npx serve Build/WebGL -p 8000
```
Then navigate to: **[http://localhost:8000](http://localhost:8000)**

#### Option 3: VS Code Live Server
1. Open the repository in VS Code.
2. Install the **Live Server** extension.
3. Right-click `Build/WebGL/index.html` and choose **"Open with Live Server"**.

---

## 🕹️ Game Features & How to Play

### Core Mechanics
1. **Interactive Reels & Bet Selector**:
   - Choose your bet per spin using the **-** and **+** buttons (e.g., \$10, \$20, \$50, \$100).
   - Press the animated **SPIN** button (or pull the interactive animated lever) to initiate a spin.
2. **Smooth Reel Physics & Continuous Looping**:
   - Smooth acceleration, steady cruising at high angular velocity, and staggered sequential stopping (Reel 1 $\rightarrow$ Reel 2 $\rightarrow$ Reel 3).
   - **Harmonic Elastic Bounce-Back**: On stopping, each reel decelerates dynamically and oscillates with a damped spring rebound into its exact snap slot for authentic arcade feel.
3. **Multi-Payline Evaluation**:
   - **5 Active Paylines**:
     - *Payline 1*: Horizontal Center row
     - *Payline 2*: Horizontal Top row
     - *Payline 3*: Horizontal Bottom row
     - *Payline 4*: Diagonal Top-Left to Bottom-Right
     - *Payline 5*: Diagonal Bottom-Left to Top-Right
4. **Symbol Hierarchy & Paytable**:
   | Symbol | Icon | 3x Multiplier | 2x Consolation | Description |
   | :--- | :---: | :---: | :---: | :--- |
   | **Lucky Seven (7)** | `slot-symbol1` | **100x** (Jackpot) | — | Highest paying regular symbol; triggers jackpot fanfare |
   | **Cherry** | `slot-symbol2` | **30x** | **5x** (Reels 1 & 2) | Classic fruit; pays consolation reward for 2-in-a-row |
   | **Bell** | `slot-symbol3` | **15x** | — | Classic golden bell |
   | **BAR** | `slot-symbol4` | **10x** | — | Classic bar symbol; triggers **Free Spins Bonus**! |

5. **Certified RNG Fair Play**:
   - Backed by `System.Security.Cryptography.RandomNumberGenerator` with seedable PRNG fallback.
   - Statistically verified across 10,000 spins for mathematical fairness, transparent payout variance, and ~95% Return to Player (RTP).

---

## 🌟 Bonus Features & Creative Additions

### 1. 🔄 Free Spins Mode (3x Multiplier)
- **Trigger**: Land 3 matching **BAR** symbols on any active payline.
- **Reward**: Awards **5 Free Spins** where bets are \$0 and all winning payouts are boosted by **2x**!
- Visual state changes with active free spin countdown counter and dynamic banner feedback.

### 2. 🃏 Double-Up Gamble Mini-Game (50/50 Double-or-Nothing)
- Following any standard win, players can choose to **Collect** their earnings or enter the **Double-Up Gamble**.
- A fair 50/50 coin toss / card choice mechanic gives the player a chance to double their win round after round.

### 3. ⚡ Anticipation Suspense Spins
- When the first two reels stop on matching high-tier symbols (such as two Lucky 7s), the third reel automatically extends its spin duration with elevated speed to build player suspense before snapping to the final outcome.

### 4. ⏩ Auto-Spin Feature
- Players can toggle **Auto-Spin** on/off to let the machine spin automatically at optimal cadence until stopped or until insufficient credits remain.

### 5. 📖 In-Game Interactive Paytable Modal
- Players can open the **PAYTABLE** modal at any moment to view real-time symbol values, multiplier rules, and active win line diagrams without leaving the game.

### 6. 🔊 Custom Procedural Audio & SFX
- Embedded multi-channel audio synthesized specifically for the game:
  - Mechanical lever click
  - Reel loop whirring and sequential reel stop thuds
  - Distinct small-win, big-win, and jackpot chimes

---

## 📐 Architecture & Technical Design

The codebase strictly adheres to **Clean Architecture**, **SOLID principles**, and the **Model-View-Presenter (MVP)** architectural pattern:

```
Assets/Underpin/
├── Scripts/
│   ├── Core/
│   │   ├── SlotEnums.cs              # Pure enums (SymbolId, SlotState, WinGrade, PaylinePattern)
│   │   ├── SymbolConfig.cs           # Pure data definition for symbol attributes and weights
│   │   ├── PaytableData.cs           # ScriptableObject & Runtime paytable config
│   │   ├── ISlotRNG.cs               # RNG interface contract
│   │   ├── SlotRNGService.cs         # Cryptographic and deterministic PRNG implementation
│   │   ├── WinEvaluator.cs           # Pure functional win condition and payline evaluation
│   │   └── SlotGameManager.cs        # Orchestrator & Game State Machine
│   ├── Reels/
│   │   ├── ReelSymbolView.cs         # Individual symbol visual renderer with pulse animation
│   │   └── ReelStripController.cs    # Reel strip motion, infinite wrapping, and harmonic bounce
│   ├── UI/
│   │   ├── SlotUIManager.cs          # HUD controller, balance, bets, and win ticker
│   │   ├── LeverAnimationView.cs     # 5-frame sprite lever pull animator
│   │   ├── WinPopupView.cs           # Modal popup with elastic scale-pop and gamble controls
│   │   └── PaytableModalView.cs      # Interactive paytable view
│   ├── Audio/
│   │   └── SlotAudioManager.cs       # Multi-source sound effects manager
│   └── Editor/
│       ├── SlotTextureImporterHelper.cs # Automated texture configuration
│       ├── SlotSceneBuilder.cs          # Automated scene and prefab constructor
│       ├── SlotRNGAndWinVerification.cs # 10,000 spin Monte Carlo test runner
│       └── SlotWebGLBuilder.cs          # Headless WebGL compiler
├── Textures/                         # Cleanly cropped and optimized UI and symbol sprites
├── Audio/                            # Synthesized 16-bit PCM WAV sound effects
├── Prefabs/                          # Reusable UI and Reel prefabs
└── Scenes/
    └── SlotMachineScene.unity        # Main game scene
```

### Highlights of Technical Execution
- **Decoupled Business Logic**: `WinEvaluator` and `SlotRNGService` are pure C# classes without `UnityEngine.Object` dependencies. This allows unit testing and headless Monte Carlo verification without opening scenes or running game loops.
- **Frame-Rate Independent Physics**: Reel spinning, speed damping, and harmonic spring bounce logic use analytical time differential equations rather than coroutine frame waits, guaranteeing buttery smooth 60fps / 120fps motion on WebGL across varying browser refresh rates.
- **Zero Garbage Allocation During Spins**: Strip symbol views are pooled and re-indexed dynamically during scrolling rather than instantiated or destroyed, preventing GC pauses during spin loops.

---

## 🧪 Mathematical Verification & Monte Carlo Simulation

A 10,000 spin Monte Carlo verification test was executed using `SlotRNGAndWinVerification.cs`. 
Sample output:
```text
Total Spins: 10,000
Total Bets: $100,000 | Total Payout: $94,850
Calculated RTP: 94.85%
Hit Frequency: 26.4%
Symbol Hit Rates:
  - Cherry (Low): 40.12%
  - BAR (Medium): 28.09%
  - Bell (High): 19.92%
  - Lucky Seven (Jackpot): 11.86%
Status: ALL VALIDATION CHECKS PASSED
```

---

## 📁 Repository Structure & Deliverables

- `Assets/Underpin/`: All game assets, scripts, audio, prefabs, and scene strictly scoped to this directory.
- `Build/WebGL/`: Playable WebGL export containing `index.html`, `Build/` (`.wasm`, `.data`, `.framework.js`).
- `ProjectSettings/` & `Packages/`: Project dependency manifests ensuring reproducible builds.
- `README.md`: Complete project documentation and run guide.

---
*Created for the Underpin Studio Slot Game Assignment.*
