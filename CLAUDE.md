# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Release build
msbuild ElevatorCabinVisualization.sln /p:Configuration=Release

# Debug build
msbuild ElevatorCabinVisualization.sln /p:Configuration=Debug
```

Or open `ElevatorCabinVisualization.sln` in Visual Studio 2010+.

## Architecture

Windows Forms application (.NET Framework 4.0) for elevator cabin visualization with KOMPAS-3D CAD export.

### Core Components

- **MainForm.cs** — Primary UI rendering isometric 3D cabin preview with 6 surfaces (ceiling, floor, 4 walls). Handles parameter controls (height, width, depth), finish selection, and visibility toggles. Loads configuration from XML on startup.

- **Kompas/KompasExporter.cs** — Orchestrates KOMPAS-3D export: loads Report.xml from "Reports" directory, applies parameter substitutions, and generates CAD assembly. Uses COM interop with `Kompas6API5` and `KompasAPI7`.

- **Kompas/KompasRestartService.cs** — Manages KOMPAS-3D process lifecycle (launch, close, reconnect). Expects `C:\Program Files\ASCON\KOMPAS-3D v22\Bin\KOMPAS.exe`.

- **Data/ObjectAssemblyKompas.cs** — Tree structure representing CAD assembly hierarchy with parent-child relationships for navigating sub-assemblies.

- **CabinDesignForm/** — Secondary form for individual cabin component configuration.

### Configuration (Settings/)

XML files with corresponding C# serialization classes:

| File | Purpose |
|------|---------|
| Finishing.xml | Cabin finish variants (потолок, пол, стенки) with model/image paths |
| Options.xml | Design options (материал, сторона открывания) |
| Params.xml | Parameter constraints (min/max, step values for dimensions) |

Report.xml (in Reports/) stores export data with marks, dimensions, and selected options.

## Dependencies

- .NET Framework 4.0 Client Profile
- KOMPAS-3D v22 SDK assemblies (Kompas6API5.dll, KompasAPI7.dll)

## Notes

- All UI text and code comments are in Russian
- Configuration files use Cyrillic XML elements
- KOMPAS-3D COM ProgID: `KOMPAS.Application.5`
