# piper-unity

A Fast, Local Neural Text-to-Speech System: Piper in Unity for Multi-Platform

## Overview

Piper is a lightweight, on-device text-to-speech system designed for real-time performance, which is one of the [Open Home Foundation Projects](https://www.openhomefoundation.org/projects/). The neural models are optimized for fast inference while maintaining high-quality speech synthesis. By using pre-trained voices, you can easily switch between different voice nationalities through [Piper Voices](https://rhasspy.github.io/piper-samples/).

This repository provides Unity integration for Piper, enabling cross-platform text-to-speech capabilities with real-time performance on Windows, macOS, and Android mobile devices.

## Features

- ✅ Multi-platform support: Windows, macOS, and Android
- ✅ 100+ languages and accents: Powered by espeak-ng phoneme generation
- ✅ On-device processing: No internet connection required
- ✅ Lightweight models: Compact voice models (20~60MB) for efficient deployment
- ✅ Pre-built libraries: Ready-to-use binaries included
- ✅ Extensive voice collection: Access to various pre-trained voice models

## Requirements

- **Unity**: `6000.0.50f1`
- **Inference Engine**: `2.2.1`
- **espeak-ng**: `1.5.2`

## Architecture

### 1. Phoneme Generation (espeak-ng)

This repository includes a Unity port of [espeak-ng](https://github.com/espeak-ng/espeak-ng) for multilingual phoneme generation. The library supports more than 100 languages and accents.

**Pre-built libraries included:**
- Windows: `.dll`
- macOS: `.dylib` 
- Android: `.so`

### 2. Pre-trained Voice Models

The system uses pre-trained neural voice models that can be easily downloaded and integrated into your Unity project.

## Getting Started

### 1. Project Setup

- Clone or download this repository
- Unzip the provided [StreamingAssets.zip](https://drive.google.com/file/d/1Wir241YUVQgDu11T9rwMWgsrL614GhVt/view?usp=sharing) file and place its contents into the `/Assets/StreamingAssets` directory in your project

### 2. Run the Demo Scene

- Open the `/Assets/Scenes/PiperScene.unity` scene in the Unity Editor
- Run the scene to see the piper tts tests in action

### 3. Voice Models

To find and download additional pre-trained voices:

1. Browse available voices: [Piper Voice Samples](https://rhasspy.github.io/piper-samples/)
2. Check the complete voice list: [VOICES.md](https://github.com/rhasspy/piper/blob/master/VOICES.md)
3. Import the `.onnx` and `.json` files into your Unity project's Assets folder
(A few voice models are not compatible with the current version of the Inference Engine and cannot be imported.)

### 4. Custom Voice Training

Want to train your own voice model? Follow the official training guide:
[TRAINING.md](https://github.com/rhasspy/piper/blob/master/TRAINING.md)

## Platform Support

| Platform | Status | Library Format |
|----------|--------|----------------|
| Windows  | ✅     | `.dll`         |
| macOS    | ✅     | `.dylib`       |
| Android  | ✅     | `.so`          |

* If not executable on Windows, install [espeak-ng.msi](https://github.com/espeak-ng/espeak-ng/releases/tag/1.52.0) manually.

## Installation

1. Follow the Project Setup steps in the [Getting Started](#-getting-started) section
2. Import the appropriate platform libraries
3. Configure the TTS system in your Unity scenes

## Demo

Experience piper-unity in action! Check out our demo showcasing:

[![Piper Unity](https://img.youtube.com/vi/i2LvqWICb40/0.jpg)](https://www.youtube.com/watch?v=i2LvqWICb40)

## Links

- [Piper Official Repository](https://github.com/rhasspy/piper)
- [Piper Voice Samples](https://rhasspy.github.io/piper-samples/)
- [espeak-ng](https://github.com/espeak-ng/espeak-ng)
- [Open Home Foundation](https://www.openhomefoundation.org/projects/)

## License

This project follows the licensing terms of its underlying components. Please refer to the original Piper and espeak-ng repositories for detailed license information.
