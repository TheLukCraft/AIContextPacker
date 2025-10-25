# AI Context Packer - Architecture Decisions

## Project Overview
**Created:** October 25, 2025  
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

**Rationale:** Ensures predictable, user-controlled output. Pinned files are truly prioritized.

### 7. State Persistence

**Saved on Exit:**
- All settings (limits, theme, filters, extensions)
- Session state (last project path, pinned files, selected files)
- Recent projects list (max 10)

**Storage Location:**
```
%AppData%/AIContextPacker/
├── settings.json
├── session.json
└── recent.json
```

**Rationale:** Users expect their workspace to be restored. Professional UX standard.

### 8. UI/UX Decisions

**Theme System:**
- Light and Dark themes with dynamic switching capability
- System theme option for OS integration
- Themes defined in ResourceDictionaries for maintainability

**Layout:**
- Two-column design: Selection (left) | Output (right)
- Pinned files shown above tree for visibility
- Filter checkboxes adjacent to tree for easy access
- Part preview with read-only modal window

**Visual Feedback:**
- Generate button disables after generation until changes made
- Copy buttons show success notifications
- Part buttons can indicate "copied" state
- Tree items show pin status with emoji icons

**Rationale:** Clean, professional interface following Windows 11 design language.

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

**Project Load Failures:**
- User-friendly error messages
- No crash, graceful degradation

**General Principle:**
- User always knows what went wrong and why
- No silent failures

**Rationale:** Professional applications must handle errors gracefully with clear communication.

### 11. Performance Considerations

**Lazy Loading:**
- Tree is built asynchronously
- Filtering applied on-demand

**Efficient File Reading:**
- Files only read during generation
- Async I/O throughout

**Memory Management:**
- Large content stored temporarily during generation
- Cleared after parts created

**Rationale:** Handles large projects (1000+ files) without performance degradation.

### 12. Future Extensibility Points

**Plugin Architecture Possibility:**
- Services use interfaces → can be replaced/extended
- Filter patterns can be extended with custom matchers

**Export Formats:**
- PartGeneratorService can be extended for different output formats
- Currently text-based, could support Markdown, JSON, etc.

**Cloud Integration:**
- ISettingsService could be backed by cloud storage
- Recent projects could sync across devices

**Rationale:** Clean architecture enables future enhancements without major refactoring.

## Code Quality Standards

### Applied Throughout:
- **SOLID Principles**: Every class has single responsibility
- **DRY**: No code duplication, shared logic in services
- **KISS**: Simple, readable solutions preferred
- **Minimal Comments**: Code is self-documenting via clear naming
- **Async/Await**: All I/O operations are asynchronous
- **Null Safety**: Careful null checking, nullable reference types

### Testing Readiness:
- Service interfaces enable easy mocking
- ViewModels testable without UI
- Business logic isolated from presentation

**Rationale:** Professional-grade code that's maintainable by teams and AI assistants.

## Build & Deployment

**Target Platform:** Windows 11 (Windows 10 compatible)  
**Deployment:** Self-contained executable with .NET 9 runtime  
**First Run:** Creates %AppData% folder and default settings

---

**Status:** ✅ Core architecture complete and production-ready
