# AI Context Packer - Architecture Decisions

## Project Overview
**Created:** October 25, 2025  
**Last Updated:** October 26, 2025  
**Framework:** WPF .NET 9  
**Pattern:** MVVM with full Dependency Injection

## Key Architecture Decisions

### 1. Technology Stack
- **.NET 9 WPF**: Latest stable framework with modern performance improvements
- **CommunityToolkit.Mvvm**: Industry-standard MVVM toolkit for cleaner, maintainable code
- **Microsoft.Extensions.DependencyInjection**: Professional DI container for service management
- **Newtonsoft.Json**: Reliable JSON serialization for settings persistence

**Rationale:** These choices provide a robust, maintainable foundation following modern .NET best practices.

### 2. Project Structure
```
AIContextPacker/
├── Models/              # Domain models and data structures
├── ViewModels/          # MVVM ViewModels with business logic
├── Views/               # XAML views and code-behind
├── Services/            # Business logic services
│   └── Interfaces/      # Service contracts
├── Converters/          # WPF value converters
├── Helpers/             # Utility classes
└── Resources/           # Themes and resources
    └── Themes/          # Light/Dark theme definitions
```

**Rationale:** Clear separation of concerns following SOLID principles, making code maintainable and testable.

### 3. Service Layer Architecture

#### Core Services Implemented:
1. **IFileSystemService**: All file operations, tree building, path management
2. **ISettingsService**: Persistent storage of app settings and session state
3. **INotificationService**: User notifications (errors, success messages)
4. **IClipboardService**: Clipboard operations abstraction

#### Specialized Services:
5. **FilterService**: 3-stage filtering logic (Whitelist → Blacklist → .gitignore)
6. **PartGeneratorService**: Complex algorithm for splitting files into parts

**Rationale:** Each service has a single, well-defined responsibility. Interfaces enable testing and future extensibility.

### 4. MVVM Implementation

**MainViewModel:**
- Single source of truth for application state
- Uses CommunityToolkit.Mvvm for [ObservableProperty] and [RelayCommand]
- Handles all business logic coordination
- Manages file tree, pinned files, filters, and part generation

**Rationale:** Centralized state management prevents bugs from scattered state. MVVM Toolkit eliminates boilerplate code.

### 5. Filtering System (3-Stage Pipeline)

**Stage 1 - Whitelist (Extension Filter):**
- Only files with allowed extensions are shown in tree
- Default: `.cs`, `.html`, `.css`, `.js`, `.ts`, `.json`, `.md`, `.java`, `.py`, etc.
- Completely hides non-whitelisted files from selection

**Stage 2 - Blacklist (Ignore Filters):**
- User-defined filter sets (e.g., ".net", "angular", "python")
- Uses gitignore pattern matching
- Multiple filters can be active simultaneously
- Initially empty - user must create/populate filters

**Stage 3 - Local .gitignore:**
- Automatically detected in project root
- Optional checkbox to enable/disable
- Default: enabled if detected

**Copy Structure Special Behavior:**
- Shows files hidden by Whitelist (so AI knows they exist)
- Still respects Blacklist and .gitignore filters

**Rationale:** Progressive filtering gives users fine-grained control while maintaining good defaults. AI needs visibility of ALL files for context.

### 6. Part Generation Algorithm

**Key Requirements Met:**
1. **No File Splitting**: Files are never divided between parts
2. **Pinned Files Priority**: Pinned files go first, fill parts exclusively
3. **Validation**: Checks if any single file exceeds limit before processing
4. **Global Prompt**: Added to Part 1 if specified

**Algorithm Flow:**
```
1. Validate: Check if any file > maxCharsLimit → Error if true
2. Add global prompt to Part 1 (if provided)
3. Add all pinned files sequentially (create new parts as needed)
4. Add selected files to subsequent parts
5. Each part stays within maxCharsLimit
```

**Global Prompt System:**
- Predefined professional prompts for different AI personas
- 10+ default prompts: Code Review Expert, Refactoring Specialist, Documentation Writer, etc.
- User can create/edit/delete custom prompts
- "(None)" option to skip global prompt
- Persistent storage in AppSettings
- PromptEditorWindow for management

**Rationale:** Ensures predictable, user-controlled output. Pinned files are truly prioritized. Global prompts provide context to AI for specific tasks.

### 7. State Persistence

**Saved on Exit:**
- All settings (limits, theme, filters, extensions, global prompts)
- Session state (last project path, pinned files, selected files, expanded folders)
- Recent projects list (max 10)
- Active filters state
- Selected global prompt ID

**Storage Location:**
```
%AppData%/AIContextPacker/
├── settings.json      # User preferences and configuration
├── session.json       # Current workspace state
└── recent.json        # Recent projects history
```

**Default Settings:**
- MaxCharsLimit: 10,000 characters per part
- Theme: System (auto-detect OS theme)
- IncludeFileHeaders: true
- 13 default file extensions supported
- 8 predefined ignore filter sets (empty by default)
- 10 predefined global prompts

**Window Closing Behavior:**
- Async save with proper cancellation handling
- Flag-based mechanism prevents double-closing
- Unsubscribes from events to prevent memory leaks
- Calls Application.Current.Shutdown() after save completes
- DispatcherTimer cleanup in ToastNotification on Unloaded event

**Rationale:** Users expect their workspace to be restored. Professional UX standard. Proper cleanup prevents processes from lingering after close.

### 8. UI/UX Decisions

**Theme System:**
- Light, Dark, and System themes with dynamic runtime switching
- System theme automatically detects Windows theme preference
- Themes defined in separate ResourceDictionaries (LightTheme.xaml, DarkTheme.xaml)
- Complete color palette with DynamicResource bindings for instant updates
- Material Design inspired colors: Indigo (#5C6BC0) for Dark, Blue (#0078D4) for Light

**Dark Mode Color Palette:**
- Background: #1E1E1E (VS Code dark)
- Surface: #252526 (elevated elements)
- Secondary Background: #2D2D30 (borders, separators)
- Hover: #3E3E42 (interactive element hover)
- Text: #E0E0E0 (high contrast, reduced eye strain)
- Secondary Text: #A0A0A0 (labels, hints)
- Primary: #5C6BC0 (Material Design Indigo - less aggressive than bright blue)

**Light Mode Color Palette:**
- Background: #FFFFFF (pure white)
- Surface: #F3F3F3 (subtle elevation)
- Secondary Background: #E8E8E8 (borders)
- Hover: #D0D0D0 (interactive hover)
- Text: #000000 (maximum contrast)
- Secondary Text: #666666 (reduced emphasis)
- Primary: #0078D4 (Windows accent blue)

**Custom Control Styling:**
- **MenuItem:** Complete ControlTemplate override to prevent system blue highlight in Dark Mode
- **ComboBox:** Custom ItemContainerStyle with IsHighlighted and IsSelected triggers
- **TreeView:** ItemContainerStyle with DynamicResource Foreground for text visibility
- **CheckBox:** Global style ensuring text uses TextBrush in all themes
- **ListBox:** Pinned files styled with proper foreground colors
- **Button Emoji Icons:** Explicit Foreground binding for visibility (📌 pin icon)

**Layout:**
- Two-column design: Selection (left) | Output (right)
- Pinned files shown above tree for visibility
- Filter checkboxes adjacent to tree for easy access
- Part preview with read-only modal window
- Toolbar with project selection, settings, and support button

**Visual Feedback:**
- Generate button disables after generation until changes made
- Copy buttons show success notifications via Toast system
- Part buttons change color after copying (WasCopiedToColorConverter)
- Tree items show pin status with emoji icons
- Loading indicator during project load
- Progress bar with current status text

**Toast Notification System:**
- Custom UserControl with slide-in animation
- Auto-dismiss after configurable duration (default 3 seconds)
- Four types: Success (green), Info (blue), Warning (orange), Error (red)
- Positioned in top-right corner, stacks multiple toasts
- Clean-up on window unload prevents memory leaks

**Support Integration:**
- "❤️ Support Me" button in main toolbar (purple #FF6351B5)
- Dedicated SupportWindow with compelling value proposition
- Integrated into About window as highlighted section
- Buy Me a Coffee link: https://buymeacoffee.com/thelukcraft
- Emphasizes time saved and productivity gains

**Rationale:** Clean, professional interface following Windows 11 design language. Dark mode optimized for extended coding sessions. Support integration encourages sustainability without being intrusive.

### 9. Data Models

**FileTreeNode:**
- ObservableObject with parent/children relationships
- Tracks visibility, selection, pinned state
- Hierarchical checkbox behavior (selecting folder selects children)

**GeneratedPart:**
- Immutable output of generation process
- Tracks character count, copy state, content

**AppSettings:**
- Central configuration object
- Includes all user preferences and filter definitions

**Rationale:** Rich domain models reduce complexity in ViewModels and Services.

### 10. Error Handling Strategy

**File Too Large:**
- Modal error dialog with specific file name and sizes
- Blocks generation to prevent data loss
- Clear guidance on increasing limit or removing files

**Project Load Failures:**
- User-friendly error messages
- No crash, graceful degradation
- Recent projects validates existence before loading

**File System Access:**
- Catches UnauthorizedAccessException (system folders)
- Catches DirectoryNotFoundException (disappeared during scan)
- Catches FileNotFoundException (deleted files)
- Debug output for diagnostics without crashing UI
- Synchronous tree building in background thread (Task.Run) prevents freezing

**General Principle:**
- User always knows what went wrong and why
- No silent failures
- All catch blocks include diagnostic output
- Async operations properly handled with cancellation

**Rationale:** Professional applications must handle errors gracefully with clear communication. File system operations are unpredictable and must be robust.

### 11. Performance Considerations

**Lazy Loading:**
- Tree is built asynchronously on background thread (Task.Run)
- Filtering applied on-demand during generation
- Only visible nodes rendered in TreeView

**Efficient File Reading:**
- Files only read during generation phase
- Async I/O throughout (File.ReadAllTextAsync)
- Stream-based reading for large files

**Memory Management:**
- Large content stored temporarily during generation
- Cleared after parts created and copied
- GeneratedPart objects kept in memory for re-copy

**UI Responsiveness:**
- All file operations on background threads
- Progress indicators during long operations
- Loading status text updated via binding
- Tree building doesn't block UI thread

**Resource Cleanup:**
- DispatcherTimer stopped and nulled on Unloaded
- Event handlers unsubscribed on window close
- Proper async/await patterns prevent deadlocks
- Application.Current.Shutdown() ensures clean exit

**Rationale:** Handles large projects (1000+ files) without performance degradation. Prevents memory leaks and hanging processes.

### 12. Future Extensibility Points

**Plugin Architecture Possibility:**
- Services use interfaces → can be replaced/extended
- Filter patterns can be extended with custom matchers

**Export Formats:**
- PartGeneratorService can be extended for different output formats
- Currently text-based, could support Markdown, JSON, XML

**Cloud Integration:**
- ISettingsService could be backed by cloud storage
- Recent projects could sync across devices
- Global prompts shared across team

**Multi-Language Support:**
- Resource files structure ready for localization
- All UI strings can be externalized
- Current: English (Tutorial, About, Support windows)

**Additional Windows:**
- TutorialWindow: 7-step guide for new users
- AboutWindow: Developer information and social links
- SupportWindow: Compelling monetization without being pushy
- SettingsWindow: Comprehensive preferences management
- FilterEditorWindow: Custom filter creation
- PromptEditorWindow: Custom prompt management
- PreviewWindow: Read-only part content view

**Rationale:** Clean architecture enables future enhancements without major refactoring. Monetization built-in early promotes sustainability.

## Code Quality Standards

### Applied Throughout:
- **SOLID Principles**: Every class has single responsibility
- **DRY**: No code duplication, shared logic in services
- **KISS**: Simple, readable solutions preferred
- **Minimal Comments**: Code is self-documenting via clear naming
- **Async/Await**: All I/O operations are asynchronous
- **Null Safety**: Careful null checking, nullable reference types enabled
- **Resource Management**: Proper disposal of timers and event handlers
- **Memory Leak Prevention**: Event unsubscription on window close

### Testing Readiness:
- Service interfaces enable easy mocking
- ViewModels testable without UI
- Business logic isolated from presentation
- No static dependencies except for theme application

### Known Issues Resolved:
1. ✅ Invisible directories in tree (exists check before adding)
2. ✅ Recent projects not showing (ObservableCollection binding)
3. ✅ Progress bar not displaying (IsLoadingProject binding)
4. ✅ Scroll not working in tree (ScrollViewer.CanContentScroll="False")
5. ✅ Toast notifications not showing (proper container setup)
6. ✅ Part colors not changing after copy (WasCopied observable property)
7. ✅ UI freeze during project load (Task.Run for tree building)
8. ✅ Global prompt options missing (None option, proper ItemsSource)
9. ✅ Settings not persisting (proper async save)
10. ✅ Dark mode text invisible (complete DynamicResource styling)
11. ✅ Application not closing (proper async shutdown with flag)
12. ✅ ComboBox dropdown unreadable (ItemContainerStyle triggers)
13. ✅ Timer leaks (Unloaded event cleanup)

**Rationale:** Professional-grade code that's maintainable by teams and AI assistants. All major bugs identified and fixed.

## Build & Deployment

**Target Platform:** Windows 11 (Windows 10 compatible)  
**Framework:** .NET 9 with nullable reference types  
**Deployment:** Self-contained executable with .NET 9 runtime  
**First Run:** Creates %AppData% folder and default settings  
**Icon:** app_icon.ico in Resources folder (placeholder - not yet created)

### Default Configuration:
- 13 supported file extensions
- 8 predefined ignore filter templates
- 10 professional global prompt templates
- MaxCharsLimit: 10,000 characters
- Theme: System (auto-detect)

### Build Configuration:
```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net9.0-windows</TargetFramework>
  <Nullable>enable</Nullable>
  <UseWPF>true</UseWPF>
  <ApplicationIcon>Resources\app_icon.ico</ApplicationIcon>
</PropertyGroup>
```

### Package Dependencies:
- CommunityToolkit.Mvvm 8.4.0
- Microsoft.Extensions.DependencyInjection 9.0.10
- Microsoft.Extensions.Hosting 9.0.10
- Newtonsoft.Json 13.0.4

---

**Status:** ✅ Core architecture complete and production-ready  
**Version:** 1.0.0  
**Phase:** Bug-fixing and polish completed  
**Next:** Icon creation, potential feature additions based on user feedback
