# Changelog

All notable changes to this package will be documented in this file  
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)

## [Unreleased]

### Fixed

- 


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
  - Task MakeSpritesInAtlasesUncompressed
  - Task DistinctRefs
  - Task RunPipeline - Run sub pipeline
- Added RefsCollector - Utility to collect refs from object fields
- Added AssetLoader_Resources - Default AssetLoader backend

## [ . . ]

## [0.0.1] - 2018-08-11

- Initial version of package (not public). Simpler alternative to addressables

