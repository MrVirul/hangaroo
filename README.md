<p align="center">
  <img src="docs/images/HANGAroo.png" alt="HANGAROO Logo" width="380" /><br>
  <img src="docs/images/kangaroo.png" alt="Hangaroo Mascot" width="160" />
</p>

<h3 align="center">Classic Word Guessing Game built with Unity</h3>

<p align="center">
  A modern, polished reimagining of the classic word-guessing game. Guess the secret phrase letter by letter before running out of hearts and leaving our kangaroo friend hanging!
</p>

---

## 🦘 Features

- **Classic Word Guessing**: Guess letters to reveal hidden words and phrases across dozens of categories.
- **Difficulty Modes**: Choose between **Easy**, **Medium**, and **Hard** tiers with tailored word lengths and hints.
- **Helpful Clues & Hints**: Every phrase includes a category clue, with unlockable hints when you get stuck.
- **Responsive Design**: Adaptive UI layout and notch/safe-area handling across different screen aspect ratios.
- **Audio & Sound Controls**: Background music and SFX with customizable volume and audio settings.
- **Automated Test Coverage**: 140+ headless EditMode and PlayMode tests to ensure rock-solid stability.
- **Cloud & Content Pipeline**: Extensible JSON word repository with automated content validation and Firebase Firestore readiness.

---

## 🎮 How to Play

1. **Select Difficulty**: Choose Easy, Medium, or Hard from the settings.
2. **Read the Clue**: Use the clue to figure out the secret word or phrase.
3. **Guess Letters**: Tap the on-screen keyboard or press keys on your keyboard.
4. **Watch Your Lives**: Each wrong guess loses a heart. Solve the puzzle before you run out of lives!

---

## 🛠️ Tech Stack & Architecture

- **Engine**: Unity 6 (`6000.3.10f1`)
- **Language**: C# (.NET / Unity Engine)
- **UI Framework**: Unity UI (uGUI) with TextMeshPro and responsive scalers
- **Data & Backend**: Shipped JSON word bank with Firebase Firestore sync pipeline
- **Testing**: Unity Test Framework (EditMode & PlayMode) with headless CLI runners

---

## 🚀 Getting Started

### Prerequisites

- [Unity Hub](https://unity.com/download)
- Unity Editor version **`6000.3.10f1`**

### Setup & Play

1. Clone the repository:
   ```bash
   git clone https://github.com/MrVirul/hangaroo.git
   cd hangaroo
   ```
2. Open the project folder in **Unity Hub** using Unity `6000.3.10f1`.
3. In the Unity Editor Project window, open one of the scenes under `Assets/Scenes/`:
   - `splash.unity` — Splash screen
   - `MainMenu.unity` — Main menu, difficulty selector, and settings
   - `GamePlay.unity` — Core interactive gameplay scene
4. Press the **Play** button in the Unity Editor toolbar.

---

## 🧪 Testing & Validation

### Automated Unity Tests

Run tests inside the Unity Editor via **Window > General > Test Runner**, or run them headlessly:

```bash
# Run headless EditMode test suite
scripts/run-tests.sh

# Run both EditMode and PlayMode suites
scripts/run-tests.sh EditMode PlayMode
```

### Content Validation

Validate the word bank structure, lengths, categories, and asset metadata without opening Unity:

```bash
python3 scripts/validate-content.py
```

---

## 📚 Documentation

For detailed design, testing rules, and implementation specs, check out the documentation:

- [Delivery Roadmap](docs/ROADMAP.md) — Open issues, epics, and milestone dependencies.
- [Testing Guide](docs/TESTING.md) — Test suite architecture, headless execution, and conventions.
- [CI Pipeline](docs/CI.md) — GitHub Actions workflows and automated gates.
- [Word Content Policy](docs/WORD_CONTENT_POLICY.md) — Word length bands, categories, and authoring guidelines.
- [Firestore Schema](docs/FIRESTORE_SCHEMA.md) — Cloud content backend schema and security rules.

---

## 📄 License

This project is licensed under the terms described in the repository. All rights reserved.
