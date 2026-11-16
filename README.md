# aquamoon-levelsuite
[Alpha] Modular Unity editor extension for physics-based prop placement, procedural scattering, and alignment. Expect bugs and breaking changes.
# AquaMoon // LevelSuite (`v0.1-alpha`)

A modular Unity editor extension designed to streamline environment design, procedural prop distribution, and physics-driven world building.

> [!WARNING]
> **Project Status: WIP **  
> This tool is currently in active development (`v0.1-alpha`). Some features may be unstable, undergo breaking changes, or produce unexpected physics interactions. Use with caution in production projects and back up your scenes. Feedback and issue reports are welcome!

---

## Overview

AquaMoon LevelSuite brings physical simulation and procedural scattering tools directly into the Unity Scene View:
- **Physics Sandbox:** Live simulation with directional or spherical gravity, dynamic object dragging, and player-collision interactions.
- **Scatter Brush:** Precision multi-prefab surface painting using configurable `ScatterProfile` ScriptableObjects with weighted drop chances and slope filtering.
- **Quick Align & Distribute:** Snapping tools to align coordinates or space out objects evenly along global axes.

---

## Features

### 1. Physics Sandbox & Placement
* **Custom Gravity Modes:** Switch between standard Directional gravity ($Y$-down) and Point/Spherical gravity directed toward any designated center Transform.
* **Pre-Drop Jitter:** Randomize initial rotations (per axis) and scaling factors before the simulation steps begin.
* **Scene View Dynamic Drag:** Hold `Ctrl + Left Click` to grab and throw rigidbodies using directional velocity calculation.
* **Player Collision Pusher:** Runtime `CharacterPhysicsPusher` component to push dynamic scene objects with a `CharacterController`.
* **Safe State Commits:** Finalize placed objects with position baking or revert changes via Unity's internal undo stack.

### 2. Scatter Brush Pro
* **Weighted Multi-Prefab Palettes:** Assign multiple prefabs with custom selection weights inside a single `ScatterProfile`.
* **Constraint Filtering:** Reject placement on surfaces exceeding maximum slope angles or missing target `LayerMask` flags.
* **Zero-Allocation Overlap Check:** Prevents spawned props from overlapping using non-allocating spherical physics queries (`Physics.OverlapSphereNonAlloc`).
* **Surface Normal Alignment:** Aligns the upright vector of instantiated props with underlying collision normals.
* **Atomic Batch Undo:** Every brush stroke is grouped into a single transactional undo operation (`Ctrl + Z`).

### 3. Precision Align & Distribute
* **Axis Snapping:** Match the pivot positions of all selected objects along the $X$, $Y$, or $Z$ axis.
* **Interval Spacing:** Sort and space out three or more objects evenly across selected dimensions.

---

## Installation

### Via Unity Package
1. Open your project in Unity and import the package via **Assets > Import Package > Custom Package...**
2. Launch the window from the top menu: `Tools > AquaMoon > Level Suite Pro`.
