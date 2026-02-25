# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.5.0] - 2026-01-28

### Added

- **Marker Tracking** - Track ArUco markers in the real world and place virtual objects on top of them. The virtual and real spaces are now bridged! 
- **New Hand Model** - Updated the default hand model with sleeker visuals.
- **Adjustable Hand Filter Mode** - You can now choose between responsive (fast, lower latency) and stable (smooth, filtered) hand tracking modes.
- **Building Blocks** - One-click setup for common VITURE features. Automatically handles package dependencies, sample imports, and scene configuration. Includes XR Origin, Hands, Quick Actions, Canvas Interaction, and Marker Tracking.

### Fixed

- **Longer Recordings** - No more 5-minute recording limit. Plus, fixed the freeze issue when completing long sessions.

### Removed

- **Hand State Demo** - Not particularly useful.
- **Error Codes and Hand Tracking Callbacks** - Simplified. The system now handles failures internally, so you don't need to.

## [0.4.1] - 2025-12-17

### Fixed

- Fixed compile error when Unity XR Hands package is not installed in the project.

## [0.4.0] - 2025-12-16

### Compatibility Notice

This is a beta release. Apps built with SDK 0.4.0 require VITURE Neckband OS 0.1.0 or later. OS 0.1.0 cannot run apps built with SDK older than 0.4.0.

### Added

- **Supported Glasses Setting** - Configure which VITURE glasses your app supports (6DoF only or both 3DoF and 6DoF) in project settings. The system prevents incompatible apps from launching.
- **SDK & OS Version Validation** - System-level version compatibility checks prevent apps with incompatible SDK versions from launching.

### Changed

- **Hand Tracking API** - `VitureXR.HandTracking.Start()` now uses callbacks with error codes and messages instead of boolean return value for better async handling.
- **Recording Callbacks** - Now also include error codes and messages for improved error diagnostics.

## [0.3.0] - 2025-11-28

### Added

- **Capture API** - Record first-person mixed reality experiences with both virtual and real-world layers. Capture and share the experiences you create with others!
- **Setup Wizard** - Streamlined project configuration tool.
- **Project Validation System** - Checks required and recommended settings and provides one-click fix.
- **Glasses Electrochromic Control** - Programmatically adjust glasses lens darkness level.
- **Quick Actions UI Panel** - System-level UI that activates when looking up, providing intuitive hand-tracked controls for recording and navigation.

### Removed

- **Deprecated UI Components** - Removed Viture Hand Menu and Recenter Indicator prefabs in favor of the new Quick Actions prefab.

### Compatibility Notice

This is a beta release. SDK 0.3.0 requires VITURE Neckband OS 2.0.5.21127 or later.

- Apps built with SDK 0.3.0 cannot run on older OS versions
- Apps built with older SDKs cannot run on OS 2.0.5.21127 or later

We're working to establish stable cross-version compatibility in upcoming releases.

## [0.2.1] - 2025-10-09

### Removed

- Remove XR Hands HandVisualizer sample dependency for Starter Asset and Hand State Demo samples.

## [0.2.0] - 2025-09-30

### Added

- New 3D hand models for hand tracking visualization.
- `VitureHandVisualizer` component providing basic hand tracking visualization functionality.
- `VitureHandRayController` component to automatically control XRI `NearFarInteractor` hand ray visibility.
- Boolean return value for `VitureXR.HandTracking.Start()` method to indicate whether hand tracking started successfully.

### Changed

- Hand tracking joint count changed to 21.

### Fixed

- Fixed hand tracking joint local rotations, which caused incorrect hand ray direction and hand menu issues.

## [0.1.0] - 2025-09-19

### Added

- 3DoF and 6DoF head tracking.
- Hand tracking with Unity XR Hand Subsystem integration.
- VitureXR API.
- Starter Assets sample with pre-configured assets.
- Hand State Demo sample.
