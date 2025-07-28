<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# MEP Connector - Revit Add-in Development Instructions

## Project Overview
This is a Revit add-in project for connecting MEP (Mechanical, Electrical, Plumbing) family instances. The add-in provides functionality to move, align, and connect MEP elements with automatic unpinning.

## Development Guidelines

### Architecture
- **Application.cs**: Main entry point, creates ribbon UI
- **Commands/**: Contains IExternalCommand implementations
- **Utils/**: Helper classes for connection and selection logic
- **Properties/**: Assembly information

### Key Technologies
- Revit API 2024
- .NET Framework 4.8
- C# language features
- WPF for UI (if needed)

### Coding Standards
1. Use Vietnamese comments for user-facing messages
2. Use English for code comments and technical documentation
3. Follow Revit API best practices:
   - Always use transactions for model modifications
   - Check element validity before operations
   - Handle exceptions gracefully
   - Provide meaningful error messages

### MEP-Specific Considerations
1. **Connector Management**: 
   - Use ConnectorManager to access element connectors
   - Check connector compatibility (domain, size, flow direction)
   - Ensure connectors are not already connected

2. **Element Movement**:
   - Use ElementTransformUtils for moving and rotating
   - Calculate proper transformation matrices
   - Consider connector alignment for perfect connections

3. **Unpinning Logic**:
   - Always check if elements are pinned before operations
   - Unpin elements automatically with user notification
   - Handle unpin failures gracefully

### Error Handling
- Use try-catch blocks around Revit API calls
- Provide Vietnamese error messages for users
- Log technical details for debugging
- Always rollback transactions on failure

### Selection Filters
- Implement ISelectionFilter for MEP elements only
- Support multiple MEP categories (mechanical, electrical, plumbing)
- Filter by element capabilities (has connectors, not already connected)

### Performance Considerations
- Minimize connector enumeration
- Cache element properties when possible
- Use filtered element collectors efficiently
- Avoid unnecessary geometry calculations

### Testing
- Test with different MEP element types
- Verify connector compatibility checking
- Test error scenarios (invalid selections, pinned elements)
- Validate undo/redo functionality
