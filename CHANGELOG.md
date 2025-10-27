## ⚡ Version History

## Version 1.1.3 (October 27, 2025)

- Added responsive, collapsible layout with dynamic GridSplitters.
- Refactored search to be an instant file/folder name filter (no file content I/O).
- Fixed "Generate" button state logic (now re-enables on any selection change).
- Fixed UpdateService JSON mapping bug for reliable update checks.
- Polished Prompt/Filter editor UI (scrolling and text wrapping).

### Version 1.1.0 (October 26, 2025)
- Complete refactoring to SOLID principles
- Service layer architecture with dependency injection
- Comprehensive test coverage (114 tests, 100% pass rate)
- MainViewModel reduced by 31.9% (589 → 401 lines)
- 6 fully-tested services extracted
- Structured logging with Serilog
- Improved maintainability and testability

### Version 1.0.1 (October 25, 2025)
- Initial release
- Complete UI/UX implementation
- Full theme system (Light/Dark/System)
- 10+ predefined global prompts
- 8 predefined ignore filter sets
- State persistence and session restore
- Tutorial and About windows
- Support integration
- All major bugs fixed