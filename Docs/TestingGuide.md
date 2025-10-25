# AI Context Packer - Testing Guide

## Manual Testing Checklist

### Basic Functionality

#### ✅ Project Loading
- [ ] Load project via "Select Project Folder" button
- [ ] Load project via File → Open Project menu
- [ ] Load project via drag & drop folder onto window
- [ ] Verify tree structure displays correctly
- [ ] Verify all allowed file extensions are visible

#### ✅ File Selection
- [ ] Select individual files via checkbox
- [ ] Select folder (all children should be selected)
- [ ] Use "Select All" button
- [ ] Use "Deselect All" button
- [ ] Verify selections persist when changing filters

#### ✅ Pinned Files
- [ ] Pin a file using 📌 icon
- [ ] Verify file moves to "Pinned Files" section
- [ ] Unpin a file (should return to tree)
- [ ] Pin multiple files
- [ ] Verify pinned files order is maintained

### Filtering System

#### ✅ Whitelist (Extension Filter)
- [ ] Only allowed extensions visible in tree
- [ ] Settings: Add new extension → verify files appear
- [ ] Settings: Remove extension → verify files disappear
- [ ] Files with unallowed extensions hidden from tree

#### ✅ Blacklist (Ignore Filters)
- [ ] Create new ignore filter in Settings
- [ ] Add patterns (e.g., `bin/`, `obj/`, `*.dll`)
- [ ] Enable filter checkbox → verify files disappear
- [ ] Disable filter → verify files reappear
- [ ] Multiple filters active simultaneously

#### ✅ Local .gitignore
- [ ] Load project with .gitignore file
- [ ] Verify "Use detected .gitignore" checkbox appears
- [ ] Check checkbox → files matching .gitignore hidden
- [ ] Uncheck checkbox → files reappear
- [ ] Project without .gitignore → checkbox disabled

### Part Generation

#### ✅ Basic Generation
- [ ] Select files and click "Generate"
- [ ] Verify parts are created
- [ ] Verify each part shows character count
- [ ] Verify "Generate" button disables after generation
- [ ] Make change → verify "Generate" button re-enables

#### ✅ Part Content Validation
- [ ] Preview part → verify exact content shown
- [ ] Verify no file is split between parts
- [ ] Verify pinned files appear first
- [ ] Verify pinned files fill separate parts
- [ ] Verify global prompt appears at start of Part 1

#### ✅ Character Limit
- [ ] Set limit to 1000 characters
- [ ] Generate with small files → multiple parts created
- [ ] Select file > limit → error dialog shown with filename
- [ ] Verify error prevents generation
- [ ] Increase limit → generation succeeds

#### ✅ File Headers
- [ ] Settings: Enable "Include file headers"
- [ ] Generate parts
- [ ] Verify each file has `// File: [path]` header
- [ ] Settings: Disable headers → verify no headers appear

### Copy Operations

#### ✅ Copy Part
- [ ] Click "Copy" on a part
- [ ] Paste into text editor → verify content matches preview
- [ ] Verify success notification appears
- [ ] Copy multiple parts sequentially

#### ✅ Copy Structure
- [ ] Select various files
- [ ] Click "Copy Structure"
- [ ] Paste → verify tree structure with markers (📌, ✓)
- [ ] Verify hidden files (non-whitelisted) ARE shown
- [ ] Verify filtered files (blacklist/gitignore) are NOT shown

### Global Prompts

#### ✅ Prompt Management
- [ ] Settings → Create new prompt
- [ ] Edit existing prompt
- [ ] Delete prompt
- [ ] Select prompt from dropdown
- [ ] Generate → verify prompt appears at start of Part 1
- [ ] Select "None" → verify no prompt added

### Settings Persistence

#### ✅ State Saving
- [ ] Load project, select files, pin some
- [ ] Close application
- [ ] Reopen → verify project reloaded
- [ ] Verify selections restored
- [ ] Verify pinned files restored
- [ ] Verify filter states restored

#### ✅ Recent Projects
- [ ] Load Project A
- [ ] Load Project B
- [ ] File → Recent Projects → verify A and B listed
- [ ] Click recent project → should load immediately
- [ ] Verify max 10 recent projects retained

### Theme System

#### ✅ Theme Switching
- [ ] Settings → Select Light theme
- [ ] Verify UI colors change
- [ ] Settings → Select Dark theme
- [ ] Verify UI colors change
- [ ] Settings → Select System theme
- [ ] Close and reopen → verify theme persists

### UI/UX

#### ✅ Drag & Drop
- [ ] Drag folder from Explorer onto window
- [ ] Verify visual feedback during drag
- [ ] Verify project loads on drop
- [ ] Drag non-folder → verify rejected

#### ✅ Responsiveness
- [ ] Resize window → verify layout adapts
- [ ] Load large project (1000+ files) → verify performance
- [ ] Generate with many parts → verify UI remains responsive

#### ✅ Error Handling
- [ ] Select folder without permission → verify error message
- [ ] Load empty folder → verify graceful handling
- [ ] Generate with no files selected → verify warning

### Edge Cases

#### ✅ Empty States
- [ ] Launch app with no previous state
- [ ] Open folder with no matching files
- [ ] Generate with only pinned files
- [ ] Generate with only selected files

#### ✅ Large Files
- [ ] Select file larger than limit
- [ ] Verify specific error with filename and sizes
- [ ] Verify generation blocked

#### ✅ Special Characters
- [ ] Files with spaces in name
- [ ] Files with unicode characters
- [ ] Folders with special characters
- [ ] Verify all handled correctly

## Automated Testing Recommendations

### Unit Tests (Priority)
1. **FilterService**
   - Test gitignore pattern matching
   - Test whitelist filtering
   - Test combined filter logic

2. **PartGeneratorService**
   - Test character limit enforcement
   - Test file ordering (pinned first)
   - Test no-split-file rule
   - Test global prompt insertion

3. **SettingsService**
   - Test save/load cycle
   - Test settings migration
   - Test corrupt file handling

### Integration Tests
1. **FileSystemService**
   - Test tree building with various structures
   - Test large directory handling
   - Test permission errors

2. **End-to-End Workflows**
   - Load → Select → Generate → Copy
   - Settings change → Apply filters → Generate
   - State save → Close → Restore

## Performance Benchmarks

### Target Metrics
- Load 1000 files: < 2 seconds
- Apply filters: < 500ms
- Generate 10 parts: < 1 second
- UI remains responsive during all operations

### Memory Usage
- Idle: < 100 MB
- With large project loaded: < 500 MB
- After multiple generations: No memory leaks

## Bug Reporting Template

When reporting bugs, include:
```
**Description**: [What went wrong]
**Steps to Reproduce**:
1. [First step]
2. [Second step]
**Expected Behavior**: [What should happen]
**Actual Behavior**: [What actually happened]
**Environment**:
- OS: Windows [version]
- .NET Version: 9.x
- App Version: [version]
**Logs**: [If available, from %AppData%/AIContextPacker/]
```

## Known Limitations

1. **Windows Only**: No macOS/Linux support (WPF limitation)
2. **Text Files Only**: Binary files not supported
3. **Single Project**: Cannot load multiple projects simultaneously
4. **No Search**: Tree navigation only, no file search feature (yet)

---

**Status**: ✅ All core features implemented and ready for testing
