> **Note:** This README focuses on technical systems and architecture for
> employers and developers. A player-focused page and release build will be
> available on Steam soon.

This is a game called Mitya and the Castle that I've worked on for several years. This project is a 100% solo effort; I designed and implemented all gameplay
systems, tooling, persistence, telemetry, art, and animations.

<img width="1572" height="858" alt="image" src="https://github.com/user-attachments/assets/af28b226-2242-4ae7-8ff2-3fa8424ec9df" />
<img width="1564" height="873" alt="Screenshot 2025-12-16 164012" src="https://github.com/user-attachments/assets/d87fd2e3-c105-41c8-a654-5fdd9270e461" />

## Project Goals

This project was built not only as a game, but as an exercise in designing
maintainable systems for state management, persistence, and runtime observability.
A major focus was ensuring that complex gameplay state remained consistent across
scene transitions and play sessions, and that failures could be diagnosed using
structured runtime data rather than ad-hoc logging.

Below is an overview of the core systems implemented for this project,
with a focus on persistence, telemetry, and architecture.

### Gameplay / Systems
- Input (Unity Input System, action maps)
- Combat + animation state handling
- UI (health, menus, etc.)
- Dialogue (Ink integration)
- Modular, event-driven gameplay systems with clear separation of concerns

### Persistence
- JSON + binary serialization
- Centralized `GameData` model
- Cross-scene state restoration

To ensure correctness of saved state across scenes and sessions, I built
an internal persistence debugging tool within the Unity editor. This tool
visualizes registered persistent entities, validates unique identifiers,
and detects invalid or duplicate state at runtime.

The debugger was used to catch issues such as duplicate entity IDs,
unexpected instance counts, and invalid activation states before they
could corrupt save data.

<img width="481" height="784" alt="image" src="https://github.com/user-attachments/assets/3d1aa773-9a6e-4ee7-8bbe-eb3b85267d46" />

This tool was particularly useful when refactoring persistence logic and
verifying that scene transitions did not introduce duplicate or orphaned
persistent entities.

### Telemetry
- Structured event schema (JSON payloads)
- REST API ingestion
- SQLite storage for querying/debugging

## Architecture
**High-level flow**
Input → Systems → State (GameData) → Persistence  
Systems → Telemetry events → REST API → SQLite

The `GameData` object serves as the authoritative source of truth for persistent
state. Gameplay systems read from and write to this model through controlled
interfaces, allowing state to be restored deterministically on load and preventing
runtime systems from mutating saved data directly. This design reduced hidden dependencies 
between systems and made load-time state restoration predictable and debuggable.

## Telemetry Pipeline
The application logs structured gameplay and lifecycle events to support
debugging, state validation, and post-session analysis.

**Example recorded events include:**
- Session lifecycle (start, load, end, quit cause)
- Scene transitions (from → to, new game flags)
- Player damage (source, context)
- Item equip/unequip actions
- Enemy destruction events with metadata

Below is a snapshot of the SQLite events table populated during development
and testing:
<img width="1605" height="644" alt="image" src="https://github.com/user-attachments/assets/d83c2e37-0582-40d4-8a81-fdaec0424233" />
>Telemetry was actively used during development to diagnose issues such as
>state desynchronization during scene transitions, duplicate item equip events,
>and unexpected session termination paths. Having a persistent event history
>allowed bugs to be reproduced and validated across play sessions.
>
>**Note:** This screenshot shows only development logs from my personal machine for privacy purposes.

## Tech Stack
- Unity (2D), C#
- Unity Input System, Animator, UGUI
- Ink (dialogue scripting)
- REST APIs, JSON
- SQLite
- Git


## Design Decisions & Tradeoffs

- **Centralized state model:** A single `GameData` object was used to reduce
  hidden coupling between systems and simplify persistence, at the cost of
  stricter access control requirements.

- **Event-based telemetry:** Events are logged at system boundaries rather than
  every frame to balance observability with performance.

- **SQLite for telemetry storage:** Chosen for simplicity, portability, and
  fast local querying during development without requiring external services.

- **Environment-aware identifiers:** Editor and runtime sessions are separated
  to prevent development telemetry from contaminating production data.

- **Ink for dialogue system:** Ink provides an amazing interface to write massive amounts of dialogue easily,
  but is more difficult to integrate into code in Unity and sync with runtime events.  

## Credits
Ink by Inkle

