# PXG // PIXEL BY GRID // HARD-GRID COORDINATE SYSTEM

**Operational Order:** PXG-2026-SOMU-0325
**Organization:** State of Mind Union (SoMU) · Studio PxG · WREN
**Event:** UH / Google Collaborative Workshop — 2026.03.25 — Honolulu, HI

---

## Overview

PXG (Pixel By Grid) is an advanced, deterministic UI/UX framework designed for the State of Mind Union (SoMU). It provides a **Hard-Grid Coordinate System (HGCS)** that eliminates "resolution drift" across heterogeneous hardware—from mobile WebGPU runtimes to 8K simulation displays to XR headsets.

Built on **Unity 6's CoreCLR** runtime (4x performance over legacy Mono), PXG delivers:

- **15-22% reduction** in CPU layout overhead via predictive grid-snapping
- **Automated UI "Sanity Checks"** for research data visualization
- **AI-suggested auto-layout refinement** through Unity Sentis integration

Target domains: **Astronomy, Health, and Ocean sciences** research workflows.

## Repository Structure

```
HGCS/
├── README.md
├── .gitignore
├── Artifacts/                          # Workshop submission assets
│   ├── Binh_Phan_Google_Workshop_Abstract.pdf
│   ├── Binh_Phan_Google_Workshop_Cover_Letter.docx
│   ├── Binh_Phan_Google_Workshop_Cover_Letter.txt
│   ├── Binh_Phan_Google_Workshop_Poster_Asset.png
│   └── extract_docx.ps1
├── docs/                               # Documentation
│   ├── poster_transcription.md         # Full verbatim poster transcription
│   ├── operational_abstract.md         # Operational abstract
│   ├── technical_specification.md      # 5-Point Technical Specification
│   └── market_data_synopsis.md         # 2026 Market Data (Unity Report)
└── src/
    ├── HGCS/                           # Core library
    │   ├── DeterministicGrid.cs        # Spec 1 — Grid-snapping engine
    │   ├── MultiResolutionScaler.cs    # Spec 2 — Cross-resolution scaling
    │   ├── ConstraintSolver.cs         # Spec 3 — C#/CoreCLR layout solver
    │   ├── SentisIntegration.cs        # Spec 4 — AI auto-layout hooks
    │   ├── BinaryVersionControl.cs     # Spec 5 — Lightweight binary VCS
    │   ├── AnchorPoint.cs              # ANCHOR_PT data structure
    │   └── SanityCheck.cs              # Automated UI validation
    └── HGCS.Tests/                     # Test stubs
        ├── ResolutionStressTest.cs     # RESOLUTION_STRESS_TEST_FAILSAFE
        ├── LatencyCompensationTest.cs  # LATENCY_COMPENSATION_PROTOCOL_04
        └── GridSnapTest.cs            # DETERMINISTIC_GRID_SNAP_v2.1
```

## 5-Point Technical Specification

| # | Spec | Description |
|---|------|-------------|
| 1 | **Deterministic Grid** | Mathematical accuracy & pixel perfection across all scales |
| 2 | **Multi-Resolution** | Cross-compatible from 8K simulation to Mobile WebGPU runtimes |
| 3 | **Constraint Solver** | C#-driven layout logic optimized for Unity 6's CoreCLR |
| 4 | **Sentis Integration** | Hook points for AI-suggested auto-layout refinement |
| 5 | **Version Control** | Lightweight binary architecture minimizes meta-file bloat |

## Partners

Google · University of Hawaiʻi Cancer Center · Unity 6

## License

Proprietary — Studio PxG / State of Mind Union
