# AI Context Packer

**AI Context Packer** turns large, messy repositories into clean, AI-ready context packs.
Instantly prepare your project for **feature generation, refactoring,** or **debugging** with models like ChatGPT, Claude, Gemini. No more manual copy-pasting or hitting token limits.

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-MVVM-0078D4?logo=windows)
![License](https://img.shields.io/badge/License-MIT-green)

---

[![Watch the AI Context Packer Demo](https://img.youtube.com/vi/FS1j3-QtWBs/maxresdefault.jpg)](https://youtu.be/FS1j3-QtWBs)

---

## 🎯 Key Features

### ⚙️ Core
- **Smart File Tree** - intuitive multi-select with hierarchy awareness  
- **Pinned Files Priority** - keep crucial files (e.g. README, Program.cs) always first  
- **Intelligent Splitting** - divides files into parts without breaking structure  
- **Global & Custom Prompts** - built-in AI roles or your own templates  
- **Recent Projects** - instantly reopen your last sessions  
- **Drag & Drop** - just drop your folder and go  

### 🎨 UI / UX
- **Light & Dark themes** with Material-inspired palette  
- **Responsive layout** - file tree + preview panels  
- **Toast notifications** and progress feedback  
- **Preview before copy** for every generated part  

### 🔍 Filtering System
A **3-stage pipeline** keeps your context clean:
1. **Whitelist** - include only allowed file extensions  
2. **Blacklist Filters** - ignore auto-generated or build files (66 presets for major frameworks)  
3. **Local .gitignore** - automatically detected & applied  

[→ Full filtering reference](Docs/Filtering.md)

### 💾 Session Restore
- Remembers opened folders, selected and pinned files, filters, and layout  
- Saves configuration in `%AppData%\AIContextPacker\`  

---

💡 **In short:**  
You pick a folder → it scans, filters, and splits your project → you copy ready-to-paste context parts for AI.

---

## 📥 Quick Start

1. **Download:** [Latest release](../../releases)  
2. **Run:** `AIContextPacker.exe`  
3. **Select project folder**  
4. **Click “Generate Parts”**  
5. **Copy and paste to your AI assistant**

---

## 🧩 More

- [Contributing](CONTRIBUTING.md)  
- [Support](SUPPORT.md)  
- [Roadmap](ROADMAP.md)  
- [Changelog](CHANGELOG.md)  
- [License (MIT)](LICENSE)

---

💖 Created by **Łukasz Capała** - [www.capala.pl](https://www.capala.pl)  
If this tool saves you hours, consider [buying a pizza 🍕](https://buymeacoffee.com/thelukcraft)
