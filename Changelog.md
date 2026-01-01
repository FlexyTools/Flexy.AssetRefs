# Changelog

All notable changes to this package will be documented in this file  
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)

## [6.0.0]

### Fixed

- Fixed Unloading of scenes on load dummy scene
- Fixed AssetLoader EditorGetAssetAddress to return only guid for any Prefab
- Fixed unconsistent AssetRef loading in editor vs build. Editor was capable for load any asset on path while build can only load exact asset on address
- Fixed loading incorrect asset in editor and inspector
- Fixed AddRefsFromDirectory now correctly adds scenes when filtered
- Fixed scene loading in editor when simulate runtime behaviour and scene is not in build settings

### Added

- Added SceneList
- Added SceneRef.SceneLoader
- Added ExtractSceneListFromRefList Pipeline Task

### Changed

- Changed version to 6.0.0
- Changed LoadSceneTask now class with chain loading
- Changed better editor menu naming
- Changed Moved SceneLoading from AssetsLoader to SceneLoader
- Changed ResourcesPopulateRefs dont add scenes to BuildSettings (BuildPlayer task set them directly)
- Changed Moved AssetLoader.NewSceneCreatedAndLoadingStarted to LoadSceneTask.NewLoadSceneTaskStarted 

### Removed

- Removed SceneLoadData - merged to LoadSceneTask

## [5.1.0] - 2026-01-01

### Fixed

- Fixed Unloading of scenes on load dummy scene
- Fixed AssetLoader EditorGetAssetAddress to return only guid for any Prefab
- Fixed unconsistent AssetRef loading in editor vs build. Editor was capable for load any asset on path while build can only load exact asset on address
- Fixed loading incorrect asset in editor and inspector

### Added

- Added better exception message for chaning scene list from inside build
- Added better customisation on dummy scene creation
- Added shortcut to load correct dummy scene for urp
- Added auto add URP/HDRP CameraData to camera in dummy scene
- Added SetUniversalVersion pipeline task
- Added AppRevision and reading revision from git repo pipeline task
- Added customisation to SetUniversalVersion
- Added VersionTag and Branch info support
- Added Pipeline.CmdRun static method for batch mode run and aimple BuildPlayer Task
- Added ability to have autorun pipelines and manual run that will autorun from running
- Added logs on running pipeline tasks
- Added Quarters to universal versioning and correct day 31 handling
- Added ability to not update build number while set universal version
- Added DevBuild option into BuildPlayer task
- Added Build player task time and name options for output name

### Changed

- Changed better package naming
- Changed better editor menu naming

## [5.0.3] - 2025-03-30

### Fixed

- Fixed Asset Drawer get filter type for AsetRef<T>
- Fixed getting asset ref for main assets in asset file

### Added

- Added log error with asset ref on fail to load with AssetLoader_Resources

## [5.0.2] - 2025-03-07

### Fixed

- Changed documentation wording

### Added

- Added small roadmap to readme
- Added documentation Third party notices section
- Added documentation FAQ section

## [5.0.1] - 2025-02-25

### Fixed

- Fixed formating in different environments by adding .editorconfig 
- Fixed wording and typos in Documentation.md 

### Changed

- Changed AsmDef names to pass asset store validation

## [5.0.0] - 2025-02-24

### Fixed

- Fixed multiselect support
- Fixed populate resource refs spawn errors in console
- Fixed auto run pipeline can not find pipeline

### Added

- Documentation

## [5.0.0-pre.1] - 2025-02-23

### Changed

- Changed Api to use C# 8 Nullable reference types 
- Updated package to C# 10
- Updated minimal supported Unity version to Unity 2022.3

### Removed

- Removed API bloat
- Removed Bundles related API (moved to Flexy.Bundles)

### Added

- Added C# 8 Nullability warnings as errors 
- Added Generic AssetRef<T>
- Added SceneRef
- Added AssetLoader - Responsible for loading assets from refs
- Added Load Extensions - Extension methods that connect AssetRefs to AssetLoader
- Added Pipeline - Flexible generic task processor. Used to prepare asset refs for build
  - Task AddRefsDirect
  - Task AddRefsFromDirectory
  - Task DistinctRefs
  - Task RunPipeline - Run sub pipeline
  - Task ResourcesPopulateRefs - Make refs work in runtime
  - Task RunOnBuildPreprocess - Autorun pipeline when you press build
- Added RefsCollector - Utility to collect refs from object fields
- Added AssetLoader_Resources - Default AssetLoader backend

## [ . . . ]

## [0.0.1] - 2018-08-11

- Initial version of package (not public). Simpler alternative to addressables

