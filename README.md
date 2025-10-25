# AI Context Packer

**AI Context Packer** is a professional WPF application designed for developers who work with AI assistants like ChatGPT, Claude, or Gemini. It streamlines the process of preparing code context by intelligently packaging multiple files into manageable parts that respect character limits.

## Features

### 🎯 Core Functionality
- **Smart File Selection**: Visual tree view with multi-select capabilities
- **Pinned Files**: Priority system for critical files that must appear first
- **Intelligent Partitioning**: Automatically splits content into parts without breaking individual files
- **Global Prompts**: Reusable prompt templates that automatically prepend to your context

### 🔍 Advanced Filtering
- **3-Stage Filter Pipeline**:
  1. **Whitelist**: Only show allowed file extensions (.cs, .js, .py, etc.)
  2. **Blacklist**: Custom ignore filters (.NET, Angular, Python patterns)
  3. **Local .gitignore**: Automatically detected and respected

### 💾 State Management
- Automatically saves and restores your workspace
- Recent projects list
- Persistent settings across sessions
- Remembers pinned files and selections

### 🎨 Modern UI
- Light and Dark themes
- Drag & Drop support for folders
- Real-time character counting
- Copy structure visualization for AI context

## Installation

### Requirements
- Windows 10/11
- .NET 9 Runtime (included in self-contained builds)

### Build from Source
```bash
git clone [repository-url]
cd AIContextPacker/AIContextPacker
dotnet restore
dotnet build
dotnet run
```

## Usage

### Quick Start
1. **Load a Project**:
   - Click "Select Project Folder" or drag & drop a folder onto the window
   - Or use File → Open Project

2. **Select Files**:
   - Check files you want to include in the tree view
   - Use "Select All" / "Deselect All" for bulk operations

3. **Pin Priority Files**:
   - Click the 📌 icon next to important files
   - Pinned files always appear first in generated parts

4. **Configure Filters** (Optional):
   - Enable/disable ignore filters as needed
   - Toggle local .gitignore support

5. **Set Character Limit**:
   - Adjust the "Max Chars Limit" based on your AI model's context window
   - Default: 10,000 characters

6. **Generate Parts**:
   - Click "Generate" to create optimally packed parts
   - Each part respects the character limit and never splits individual files

7. **Copy & Use**:
   - Click "Copy" on any part to copy it to clipboard
   - Use "Preview" to see exact content before copying
   - Use "Copy Structure" to get a tree view of your selection

### Advanced Features

#### Custom Ignore Filters
Settings → Ignore Filters (Blacklist)
- Create named filter sets (e.g., ".NET Build", "Node Modules")
- Use gitignore pattern syntax
- Combine multiple filters simultaneously

#### Global Prompts
Settings → Global Prompts
- Create reusable prompt templates
- Selected prompt automatically prepends to Part 1
- Perfect for consistent AI instructions

#### File Headers
Settings → General → "Include file headers in output"
- Adds `// File: [path]` before each file's content
- Helps AI understand project structure

## Architecture

### Technology Stack
- **.NET 9 WPF**: Modern Windows desktop framework
- **MVVM Pattern**: Clean separation of concerns
- **Dependency Injection**: Professional service architecture
- **CommunityToolkit.Mvvm**: Reduced boilerplate with source generators

### Project Structure
```
AIContextPacker/
├── Models/              # Domain models
├── ViewModels/          # MVVM ViewModels
├── Views/               # XAML UI
├── Services/            # Business logic
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
