# AI Context Packer

**AI Context Packer** is a professional WPF application designed for developers who work with AI assistants like ChatGPT, Claude, or Gemini. It streamlines the process of preparing code context by intelligently packaging multiple files into manageable parts that respect character limits.

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-MVVM-0078D4?logo=windows)
![License](https://img.shields.io/badge/License-MIT-green)

## 🎯 Key Features

### Core Functionality
- **Smart File Selection**: Visual tree view with multi-select and hierarchical selection
- **Pinned Files Priority**: Critical files always appear first in generated parts
- **Intelligent Partitioning**: Automatically splits content into parts without breaking individual files
- **Global Prompts**: 10+ predefined AI persona prompts (Code Review Expert, Refactoring Specialist, etc.)
- **Custom Prompt Editor**: Create and manage your own reusable prompt templates
- **Drag & Drop**: Drop folders directly into the application
- **Recent Projects**: Quick access to your last 10 projects

### 🎨 Modern UI/UX
- **Light & Dark Themes**: Auto-detects Windows system theme or manual selection
- **Material Design Colors**: Carefully chosen palette optimized for extended coding sessions
- **Responsive Layout**: Two-column design with file tree and output panels
- **Toast Notifications**: Non-intrusive success/error messages with auto-dismiss
- **Preview Window**: View part content before copying
- **Loading Indicators**: Progress bar with status text during operations

### 🔍 Advanced Filtering System
**3-Stage Filter Pipeline**:
1. **Whitelist**: Only show allowed file extensions (13 defaults: .cs, .js, .ts, .py, .html, .css, .json, .md, .xml, .txt, .xaml, .tsx, .jsx)
2. **Blacklist**: 8 predefined filter sets (.NET, Node.js, Python, Angular, React, Vue, Java, C++) with gitignore-style patterns
3. **Local .gitignore**: Automatically detected and optionally applied

### 💾 State Persistence
- **Session Restore**: Automatically saves and restores your workspace
- **Expanded Folders**: Remembers which folders were expanded
- **Selected Files**: Restores your file selection
- **Pinned Files**: Maintains pinned files across sessions
- **Active Filters**: Remembers which filters were enabled
- **Settings**: All preferences saved in %AppData%\AIContextPacker\

## 📸 Screenshots

### Light Theme
Modern, clean interface optimized for daytime coding with Windows 11 design language.

### Dark Theme
Eye-friendly dark mode with Material Design Indigo accent, perfect for extended coding sessions.

## � Getting Started

### Requirements
- **Windows 10/11** (64-bit)
- **.NET 9 Runtime** (included in self-contained builds)

### Installation

#### Option 1: Download Release (Recommended)
1. Download the latest release from [Releases](../../releases)
2. Extract the ZIP file
3. Run `AIContextPacker.exe`

#### Option 2: Build from Source
```bash
git clone https://github.com/TheLukCraft/AIContextPacker.git
cd AIContextPacker/AIContextPacker
dotnet restore
dotnet build
dotnet run
```

## 📖 How to Use

### Quick Start Guide

1. **Load a Project**:
   - Click "📁 Select Project Folder" or drag & drop a folder
   - Or use File → Recent Projects for quick access

2. **Select Files**:
   - Browse the file tree and check files to include
   - Click folder checkboxes to select all children
   - Use "Select All" / "Deselect All" for bulk operations

3. **Pin Priority Files** (Optional):
   - Click the 📌 icon next to critical files
   - Pinned files always appear first in generated parts
   - Perfect for README, main entry points, or config files

4. **Choose Global Prompt** (Optional):
   - Select from 10+ predefined AI personas
   - Code Review Expert, Refactoring Specialist, Test Writer, etc.
   - Prompt automatically prepends to Part 1

5. **Configure Filters** (Optional):
   - Enable ignore filters (.NET, Node.js, Python, etc.)
   - Toggle local .gitignore checkbox
   - Files hidden by filters are still shown in "Copy Structure"

6. **Set Character Limit**:
   - Adjust "Max Chars Limit" based on your AI model
   - Default: 10,000 (suitable for most models)
   - Claude: 100,000+ | ChatGPT: 8,000-128,000 depending on model

7. **Generate Parts**:
   - Click "Generate Parts" button
   - Algorithm ensures no file is split between parts
   - Pinned files fill early parts exclusively

8. **Copy & Use**:
   - Click "Copy Part X" to copy to clipboard
   - Use "Preview" to see exact content
   - Use "Copy Structure" for project tree overview
   - Button turns orange after copying for visual feedback

### Advanced Features

#### Custom Ignore Filters
**Settings → Ignore Filters (Blacklist)**
- Create custom filter sets with descriptive names
- Use gitignore-style patterns:
  - `*.log` - all log files
  - `**/bin/` - bin folders at any level
  - `node_modules/` - specific folder names
- Activate multiple filters simultaneously
- Example filters included: .NET, Node.js, Python, Angular, React, Vue, Java, C++

#### Global Prompts Management
**Settings → Global Prompts**
- Create reusable prompt templates for different tasks
- Edit or delete existing prompts
- Predefined prompts include:
  - 🔍 Code Review Expert
  - 🔧 Refactoring Specialist
  - 🧪 Test Writing Expert
  - 📚 Documentation Writer
  - 🎨 UI/UX Specialist
  - ⚡ Performance Optimizer
  - 🔒 Security Auditor
  - 🏗️ Architecture Designer
  - 🐛 Debugging Assistant
  - And more...

#### Theme Customization
**Settings → Theme**
- **Light Mode**: Clean, professional Windows 11 style
- **Dark Mode**: Eye-friendly with Material Design Indigo accents
- **System**: Automatically follows Windows theme

#### Tutorial & Support
- **Help → Tutorial**: 7-step interactive guide for new users
- **Help → About**: Developer information and social links
- **❤️ Support Me**: Buy me a slice of pizza if the tool saved you time!

## Architecture

### Technology Stack
- **.NET 9 WPF**: Latest Windows desktop framework with modern performance
- **MVVM Pattern**: Clean separation of concerns with CommunityToolkit.Mvvm
- **Dependency Injection**: Professional service architecture with Microsoft.Extensions.DependencyInjection
- **Async/Await**: All I/O operations are non-blocking
- **Newtonsoft.Json**: Reliable settings serialization

### Project Structure
```
AIContextPacker/
├── Models/              # Domain models (FileTreeNode, AppSettings, etc.)
├── ViewModels/          # MVVM ViewModels with business logic
├── Views/               # XAML windows (Main, Settings, Tutorial, About, Support)
├── Services/            # Business logic services
│   ├── FileSystemService      # File operations and tree building
│   ├── SettingsService        # Persistent configuration
│   ├── FilterService          # 3-stage filtering pipeline
│   ├── PartGeneratorService   # Smart content splitting
│   ├── ClipboardService       # Clipboard abstraction
│   └── NotificationService    # User notifications
├── Converters/          # WPF value converters
├── Helpers/             # Utility classes
├── Resources/           # Themes and assets
│   └── Themes/          # LightTheme.xaml, DarkTheme.xaml
└── Controls/            # Custom controls (ToastNotification)
```

### Key Design Decisions

**3-Stage Filtering Algorithm:**
1. **Whitelist** (Extension filter) - Only allowed file types shown
2. **Blacklist** (Ignore filters) - User-defined gitignore patterns
3. **Local .gitignore** - Project-specific exclusions

**Part Generation Algorithm:**
- Validates no single file exceeds limit before processing
- Pinned files fill parts first, sequentially
- Regular files fill remaining space
- Never splits a file between parts
- Global prompt prepended to Part 1 if selected

**State Persistence:**
- Settings: `%AppData%\AIContextPacker\settings.json`
- Session: `%AppData%\AIContextPacker\session.json`
- Recent: `%AppData%\AIContextPacker\recent.json`

**Performance Optimizations:**
- Tree building on background thread (Task.Run) prevents UI freeze
- Async file I/O throughout
- Lazy tree rendering
- Proper resource cleanup (timers, event handlers)

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Setup
1. Clone the repository
2. Open in Visual Studio 2022 or VS Code with C# extension
3. Build with `dotnet build`
4. Run with `dotnet run`

### Code Style
- Follow C# naming conventions
- Use nullable reference types
- Keep methods focused and testable
- Document complex algorithms
- MVVM pattern throughout

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 💖 Support

If this tool saves you time and makes your work easier, consider supporting the project:

**[🍕 Buy Me a Slice of Pizza](https://buymeacoffee.com/thelukcraft)**

Your support helps:
- Add new features you request
- Fix bugs and improve performance
- Create detailed tutorials
- Maintain compatibility with new technologies

### Other Ways to Support
- ⭐ Star the project on GitHub
- 🐛 Report bugs and suggest features
- 📢 Share with fellow developers
- 📝 Write a review or testimonial

## 👨‍💻 About the Developer

**Łukasz Capała** - Full Stack Developer

I turn complex challenges into intuitive software that drives your business. Specializing in custom web applications, from strategy and prototype through development to deployment and support.

### Connect With Me
- 🌐 Website: [www.capala.pl](https://www.capala.pl/)
- 💻 GitHub: [@TheLukCraft](https://github.com/TheLukCraft)
- 📺 YouTube: [@capalapl](https://www.youtube.com/@capalapl)
- 🎵 TikTok: [@capalapl](https://www.tiktok.com/@capalapl)

## 🗺️ Roadmap

### Completed ✅
- Core file selection and filtering
- 3-stage filtering pipeline
- Part generation algorithm
- Light/Dark theme system
- Global prompts management
- State persistence
- Recent projects
- Tutorial and About windows
- Support integration
- Toast notifications
- Drag & drop support

### Planned 🚧
- Application icon
- Cloud sync for settings
- Export formats (Markdown, JSON)
- Plugin architecture
- Multi-language support
- File content search
- Diff view for changes
- Team collaboration features

## 📚 Documentation

For detailed architecture decisions and development notes, see [Docs/Decisions.md](Docs/Decisions.md).

For testing guidelines, see [Docs/TestingGuide.md](Docs/TestingGuide.md).

## 🐛 Known Issues

All major bugs have been resolved in version 1.0.0. If you encounter any issues, please [open an issue](../../issues).

## ⚡ Version History

### Version 1.0.0 (October 26, 2025)
- Initial release
- Complete UI/UX implementation
- Full theme system (Light/Dark/System)
- 10+ predefined global prompts
- 8 predefined ignore filter sets
- State persistence and session restore
- Tutorial and About windows
- Support integration
- All major bugs fixed

---

**Made with ❤️ by TheLukCraft**

If AI Context Packer saved you time, [buy me a slice of pizza](https://buymeacoffee.com/thelukcraft)! 🍕
│   └── Interfaces/      # Service contracts
├── Converters/          # WPF value converters
├── Helpers/             # Utilities
└── Resources/           # Themes & resources
```

### Key Design Decisions
- **SOLID Principles**: Single responsibility, dependency inversion
- **Async/Await**: All I/O operations are non-blocking
- **Service Layer**: Testable business logic isolated from UI
- **State Persistence**: Settings saved in `%AppData%/AIContextPacker`

## Configuration

### Settings Location
All settings are stored in:
```
%AppData%/AIContextPacker/
├── settings.json      # User preferences
├── session.json       # Last workspace state
└── recent.json        # Recent projects
```

### Default Allowed Extensions
`.cs`, `.html`, `.css`, `.js`, `.ts`, `.json`, `.md`, `.java`, `.py`, `.xml`, `.txt`, `.xaml`, `.tsx`, `.jsx`

## Troubleshooting

### "File exceeds maximum character limit" Error
**Cause**: A single file is larger than your configured limit.  
**Solution**: Increase the Max Chars Limit in the toolbar, or exclude the large file.

### Files Not Appearing in Tree
**Cause**: Extension not in whitelist, or filtered by ignore rules.  
**Solution**: 
1. Check Settings → Allowed Extensions
2. Disable active ignore filters temporarily
3. Uncheck "Use detected .gitignore"

### Application Doesn't Save State
**Cause**: Permissions issue with %AppData% folder.  
**Solution**: Ensure the application has write permissions to `%AppData%/AIContextPacker`

## Contributing

This project follows industry-standard .NET development practices:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Code Standards
- Follow SOLID principles
- Use async/await for I/O operations
- Maintain MVVM separation
- Add XML documentation for public APIs
- Write clean, self-documenting code

## License

[Add your license here]

## Acknowledgments

- Built with [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- Inspired by the needs of AI-assisted development workflows

---

**Made with ❤️ for developers working with AI assistants**
