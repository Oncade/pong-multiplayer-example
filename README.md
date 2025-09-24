# Pong Multiplayer Demo (Unity + Namazu Elements + Crossfire)

## Overview

This is a **Unity-based multiplayer Pong demo** that demonstrates:  

- Integration with **Namazu Elements** for user management and matchmaking.  
- Use of **Unity Netcode for GameObjects (NGO)** with WebRTC for networked gameplay.  
- CPU player simulation using **ParrelSync** for cloned projects.  
- Scene management and pre-placed vs spawned network objects.  

The project is designed to let the host player login manually, while a cloned instance automatically logs in as a CPU player.

---

## Features

- **Manual Player Login** (create account or log back in)  
- **CPU Player Auto Login** (detected in cloned projects via ParrelSync)  
- **Matchmaking** using Namazu Elements + Crossfire plugin  
- **WebRTC Peer-to-Peer Connection** via Unity Netcode  
- **Networked Pong Gameplay** (paddles, ball, scoring)  
- **Scene flow:** `Login` → `PongMulti` → gameplay  

---

## Project Setup

### Requirements

1. Unity 2021.3+ (or compatible LTS version)  
2. **Namazu Elements** + **Crossfire** services running  
3. A **created application in Elements**, in this case named `Pong`  
4. A **matchmaking configuration** in Elements (FIFO recommended), named `default`  
5. **ParrelSync** (for CPU player detection in cloned projects)  

### Setup Steps

1. **Clone the Project with ParrelSync**  
   - ParrelSync allows creating a "clone" Unity instance.  
   - The cloned project will automatically detect itself and login as the CPU player.  

2. **Configure Elements Client**  
   - Ensure `ElementsProperties.ELEMENTS_ROOT_URL` points to your running Namazu Elements instance.  
   - Ensure the application name (`Pong`) matches your Elements setup.  

3. **Network Prefabs & Scenes**  
   - Pre-place **NetworkObjects** (ball, host paddle, client paddle) in the `PongMulti` scene.  
   - **Do NOT call `Spawn()` manually** on pre-placed scene objects — NGO will spawn them automatically.  
   - Add any dynamically spawned prefabs (e.g., projectiles, power-ups) to **NetworkManager → NetworkPrefabs**.  

4. **Startup Flow**  
   - The project starts in the `Login` scene.  
   - Host player logs in manually (create account or sign in).  
   - CPU player clone logs in automatically using `CPUPlayerLoader`.  
   - After login, players can **find or join a match** using the default matchmaking configuration.  
   - Once connected, a **WebRTC P2P connection** is established via NGO + Crossfire plugin.  
   - The `PongMulti` scene is loaded automatically, and gameplay begins.  

---

## NGO Gotchas (Important Notes)

1. **Scene Objects vs Prefabs**  
   - **Scene objects** (pre-placed in the scene) are automatically spawned by NGO. Do **not** call `Spawn()` on them.  
   - Only dynamically instantiated objects should be spawned manually.  

2. **NetworkVariables**  
   - Do not write to `NetworkVariables` before the `NetworkObject` has spawned.  
   - Initialize variables and subscribe to `OnValueChanged` **in `OnNetworkSpawn()`**.  

3. **Ownership**  
   - For pre-placed paddles or ball objects, use `ChangeOwnership(clientId)` to assign control.  
   - Do **not** try to `InstantiateAndSpawn` scene objects — this causes hash mismatches and spawn failures.  

4. **WebRTC / DataChannel Timing**  
   - Ensure `NetworkManager.Singleton.StartHost()` or `StartClient()` completes before sending any network messages.  
   - For proper synchronization, all initial game setup should happen in `OnNetworkSpawn()` on both host and clients.  

5. **Score & UI Updates**  
   - Use `NetworkVariable` hooks to update UI across clients.  
   - Do not attempt to directly set UI text from server logic — always update through `OnValueChanged` callbacks.  

---

## Scenes

| Scene Name | Description |
|------------|-------------|
| `Login` | Player login / account creation / CPU player detection |
| `PongMulti` | Multiplayer Pong gameplay |

---

## Code Structure

- **LoginViewController** – Handles manual login, account creation, and profile management.  
- **CPUPlayerLoader** – Automatically logs in CPU player in cloned projects.  
- **GameManager** – Manages gameplay: networked paddles, ball, score, round logic.  
- **ElementsAuthService** – Utility class to handle shared authentication logic (login, signup, profile fetching).  

---

## Running the Demo

1. Open the main project → Start Unity.  
2. Open the `Login` scene.  
3. For CPU vs host testing:  
   - Use ParrelSync → Create Clone.  
   - Start clone → CPU automatically logs in.  
4. Log in manually in the main project.  
5. Matchmaking starts automatically.  
6. Once connection established → `PongMulti` scene loads → game starts.  

---

## References

[Unity Netcode for GameObjects](https://docs-multiplayer.unity3d.com/)

[Namazu Elements Docs](https://namazustudios.com/docs)

[ParrelSync GitHub](https://github.com/VeriorPies/ParrelSync)

---

## Mermaid Flow Diagram

```mermaid
flowchart TD
    A[Main Project (Host Player)] -- "Manual login / signup" --> B[Fetch Profile]
    C[Cloned Project (CPU Player)] -- "Auto login as CPU" --> D[Ensure Profile Exists]

    B --> E[Matchmaking (Elements + Crossfire)]
    D --> E

    E --> F[WebRTC P2P Connection via Unity Netcode]

    F --> G[PongMulti Scene]
    G --> H[NetworkObjects auto-spawned (ball, paddles)]
    G --> I[Ownership assigned: Host & CPU paddles]
    G --> J[Gameplay begins: ball movement, scoring, UI updates via NetworkVariables]

    %% Notes
    classDef notes fill:#f9f,stroke:#333,stroke-width:1px,color:#000;
    K[NGO Gotchas: Pre-placed objects auto-spawn, assign ownership with ChangeOwnership, NetworkVariables in OnNetworkSpawn, UI via OnValueChanged]:::notes

