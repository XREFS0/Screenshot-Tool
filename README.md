# MASA Screenshot

A lightweight, high-performance Windows screen capture, annotation, and redaction tool built with **C# (.NET 8)**, **WPF**, and **Clean Architecture**.

---

## Screenshots

| MainWindow Dashboard | Image Editor & Annotator |
| :---: | :---: |
| ![Dashboard](Screenshots/01_MainWindow_Dashboard.png) | ![Editor](Screenshots/04_EditorWindow_Annotator.png) |

| Capture History | Preferences & Settings |
| :---: | :---: |
| ![History](Screenshots/03_HistoryWindow.png) | ![Settings](Screenshots/02_SettingsWindow.png) |

---

## Features

- **Capture Modes**:
  - Selected rectangular screen region with interactive magnifier HUD & crosshair
  - Full desktop (single or virtual multi-monitor bounds)
  - Active focused window (with optional DWM frame/shadow handling)
  - Configurable capture countdown delay (0s, 3s, 5s, 10s)
- **Annotations & Drawing**:
  - Smooth pen, alpha highlighter, arrows, rectangles, ellipses, straight lines, and customizable text
  - Auto-incrementing step counter badges (`1`, `2`, `3`...)
  - Undo & Redo stack
- **Local Privacy & Redaction**:
  - Box Blur and Pixelation filters
  - Smart redaction heuristics (detecting high-density text bands and credentials)
  - Strict Privacy Mode: Zero disk logging or local capture persistence when active
- **Multi-Monitor & DPI Awareness**:
  - Supports Per-Monitor V2 DPI scaling and negative coordinate virtual display spaces
- **Color Inspector**:
  - Live pixel color inspector reporting HEX, RGB, and HSL values with one-click clipboard copy
- **History & Management**:
  - Searchable thumbnail history gallery with disk caching and direct Explorer integration
  - Global background hotkeys and System Tray support

---

## Architecture

The solution follows Clean Architecture principles with explicit layer boundaries and Dependency Injection:

```
MASA.Screenshot.sln
├── MASA.Screenshot.Core            # Domain entities, enums, interfaces, and constants
├── MASA.Screenshot.Application     # Business logic, annotations, utilities, and redaction heuristics
├── MASA.Screenshot.Infrastructure  # Win32 P/Invoke, GDI+ capture engine, storage, hotkeys, and logging
├── MASA.Screenshot.App             # WPF MVVM presentation layer with dark Fluent theme
└── MASA.Screenshot.Tests           # xUnit test suite with FluentAssertions
```

---

## Getting Started

### Prerequisites
- Windows 10 (Build 19041+) or Windows 11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build and Run

1. Clone the repository:
   ```bash
   git clone https://github.com/XREFS0/Screenshot-Tool.git
   cd Screenshot-Tool
   ```

2. Restore dependencies and build the solution:
   ```bash
   dotnet build
   ```

3. Run unit tests:
   ```bash
   dotnet test
   ```

4. Launch the application:
   ```bash
   dotnet run --project MASA.Screenshot.App
   ```

---

## Default Keyboard Shortcuts

| Shortcut | Action |
| :--- | :--- |
| `PrintScreen` | Capture Selected Area |
| `Ctrl + PrintScreen` | Capture Full Screen |
| `Alt + PrintScreen` | Capture Active Window |
| `Win + Shift + S` | Quick Area Capture |

---

## License

This project is licensed under the [MIT License](LICENSE).

