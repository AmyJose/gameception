# gameception

# 🕺 Groove Galaxy

**"Dance Dance Revolution" meets "Just Dance" — in space!**

Groove Galaxy is a party rhythm game built around a shared performance: one player takes the stage as an interstellar performer, while everyone else watches and cheers as a spectator audience. Players help displaced alien species — each tied to a unique elemental identity (Fire, Water, Earth, Air) — reclaim their home planets by hitting choreographed dance poses in time with the music, using a combination of full-body pose tracking and a custom-built dance mat.

Built by **Team Gameception** for COMS30043 (University of Bristol).

> 📹 Watch the technical demo video — *(https://www.youtube.com/watch?v=R8g3ffu7vaI)*

---

## Gameplay

A single **performer** stands on the dance mat while choreography prompts scroll down planetary lanes toward a judgement zone. Each prompt combines:

- a target **elemental pose** (Fire, Water, Earth, Air, or Idle),
- a **lane**, mapped to a specific planet and dance mat direction, and
- a **timing constraint** relative to the musical beat.

To score a hit, the player must simultaneously stand on the correct mat pad **and** hold the matching pose as the prompt reaches the judgement zone. Correct actions restore alien planets, trigger visual/audio feedback, and ramp up musical intensity.

Difficulty scales dynamically across three stages (**Easy → Medium → Hard**), adjusting BPM, prompt spacing, and sequence length based on a rolling average of the player's recent accuracy. Later stages introduce **"Double Trouble"** — a high-intensity mode where two prompts travel simultaneously, forcing rapid movement across mat positions while holding a pose.

Meanwhile, spectators experience the game passively through reactive alien animations, layered adaptive music, and visual feedback — turning gameplay into a communal performance rather than a solo experience.

<table>
  <tr>
    <td><img width="1600" height="1200" alt="gameplayAmyGamesDay" src="https://github.com/user-attachments/assets/8c922899-0579-44d5-82b9-434a4ef7e8f5" /></td>
    <td><img width="1600" height="1200" alt="viewingAreaGamesDay" src="https://github.com/user-attachments/assets/8daec1c8-ad68-4935-8a0d-961258bb5e8a" /></td>
  </tr>
</table>

## Key Features

- 🩰 **Custom-built dance mat** — designed, wired, and iterated through 7 hardware revisions (Velostat pressure sensing on a Teensy 3.6 microcontroller)
- 🤖 **ML-based pose classification** — a custom-trained MLP replacing an earlier, brittle vector-maths heuristic
- 🎯 **Full-body pose tracking** via Google's MediaPipe / BlazePose, running in real time on a standard webcam
- 📈 **Adaptive difficulty** that scales BPM and prompt density to the player's live performance
- 🏆 **Secure, persistent online leaderboard** backed by Firebase, with a deployed companion website
- 🎨 **100% original art, animation, and music**, hand-drawn and custom-composed
- 🕹️ Fully playable **with or without** the physical dance mat (keyboard fallback)

## Tech Stack

| Technology | Category | Role in the Project |
|---|---|---|
| **Unity (C#)** | Game Engine | Core gameplay, rendering, and ML/hardware integration |
| **MediaPipe Unity Plugin** (BlazePose) | Pose Detection | Real-time, 33-point full-body landmark tracking |
| **PyTorch** | Machine Learning | Training the pose classification model |
| **ONNX + Unity Inference Engine** | Real-Time ML Inference | Running the trained model natively inside Unity |
| **Arduino IDE + Teensy 3.6** | Hardware / Firmware | Programming the custom dance mat controller |
| **Firebase** (Realtime Database, Auth, Cloud Functions) | Backend | Secure, persistent leaderboard |
| **Netlify** | Deployment | Hosting the public leaderboard website |
| **MediBang Paint** | Asset Creation | All 2D art, UI, and concept work |
| **GitHub** (Git LFS) | Version Control | Source control and collaborative development |

## System Architecture

### Pose Detection & Classification

Player pose is tracked in real time via MediaPipe/BlazePose, which returns 33 normalised body landmarks per frame.

An initial rule-based approach computed joint angles and relative positions directly from these landmarks (e.g. detecting a T-pose "Earth" stance via elbow/wrist geometry). This was simple but brittle — fixed thresholds didn't generalise across body types, camera angles, or natural pose variation, producing frequent false positives/negatives.

This was replaced with a **supervised ML approach**:

1. **Custom dataset** — landmark data was collected from team members across multiple sessions and labelled by pose class, with a held-out validation set from unseen individuals to avoid data leakage from temporally-correlated samples.
2. **Feature engineering** — raw landmarks were reduced to 8 engineered features (joint angles, wrist heights, wrist-to-wrist distance, hand-to-hip distance), normalised relative to the hip centre for positional invariance.
3. **Model** — a lightweight Multi-Layer Perceptron (2 hidden layers, 64 → 32 neurons, ReLU, softmax output over 5 classes: Fire / Water / Earth / Air / Idle), trained with cross-entropy loss, Adam, dropout, and early stopping.
4. **Result** — **96% validation accuracy**, exported to ONNX and run natively in Unity via the Unity Inference Engine for low-latency, frame-by-frame inference.

### Custom Dance Mat

Every part of the mat — sensing, circuitry, and enclosure — was built in-house rather than adapting around off-the-shelf hardware, to keep latency low and integration seamless.

- Iterated through **7 hardware revisions**, moving from simple push buttons through Force-Sensitive Resistors before settling on **Velostat** for full-surface, analogue pressure sensing.
- Driven by a **Teensy 3.6**, with 4 identical sensor/LED circuits (one per directional pad), housed in a custom 3D-printed (Fusion 360) PLA enclosure rated to support a player's full weight.
- State is polled every 10 ms and thresholded against a manually calibrated baseline (tuned per-session via the Arduino serial plotter) to reliably distinguish intentional presses from noise.
- Pads are arranged in a 3×3 cross layout, refined over multiple rounds of user testing for natural foot positioning.

### Online Leaderboard

A persistent leaderboard was built on **Firebase Realtime Database**. Initial client-side writes from Unity were replaced with a **Firebase Cloud Function** that validates and sanitises run statistics server-side before writing to the database, with security rules denying direct client writes while still allowing public reads — preventing score forgery while keeping the backend serverless. Players are authenticated anonymously via Firebase Auth. The public leaderboard site is deployed on **Netlify**.

## Look & Feel

Every sprite, animation, and sound effect is original, hand-drawn in MediBang Paint to a consistent cartoon space aesthetic. Each alien species has a distinct visual identity and reacts uniquely as its planet is restored, planets are animated with frame-by-frame stop motion, and a parallax starfield runs throughout. Music and sound design were produced by a three-person composer team, including adaptive, planet-specific tracks that respond to gameplay.

## User Testing

The game went through extensive informal and structured user testing throughout development, directly shaping:

- the dance mat's sensor type, pad size, and layout (7 iterations),
- the adaptive difficulty curve and BPM bounds per stage,
- the onboarding tutorial (3 major iterations, moving from text-heavy instructions to imitation-based pose learning), and
- the physical exhibition space — lighting, partitioning, and screen/speaker placement — to balance reliable pose tracking with an immersive spectator experience.

Player enjoyment was also quantified via **PXI (Player Experience Inventory)** survey responses collected on Games Day.


## Team Members
| Members | Email |
| --- | --- |
| Ezen Tan | mo23274@bristol.ac.uk |
| Dennis Han | jf23616@bristol.ac.uk |
| Elsa Wakfi | xp23980@bristol.ac.uk |
| Marek Janiec | qx23239@bristol.ac.uk |
| Anni Liu | zi23140@bristol.ac.uk |
| Amy Jose | yh23240@bristol.ac.uk |
