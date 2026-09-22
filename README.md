# Blood Moon Trial

A dark-fantasy arena environment built in **Unity 6** (URP). A condemned assassin, **Oraya**, stands in a sealed ritual ring under a blood moon — cobbled floor, iron gates, crescent banners, gargoyles, and firelight.

![Blood Moon arena](Assets/Screenshots/oraya_lit.png)

## Open the project

1. Install **Unity 6000.0.83f1** (matching `ProjectSettings/ProjectVersion.txt`).
2. Clone this repository.
3. Open the folder in Unity Hub.
4. Load `Assets/Scenes/SampleScene.unity`.
5. Enter Play Mode to view the scene.

The scene is an authored environment / vertical slice, not a full playable campaign.

## Scene

| | |
|---|---|
| **Engine** | Unity 6000.0.83f1 · URP |
| **Scene** | `Assets/Scenes/SampleScene.unity` |
| **Character** | Oraya — moon sword (right hand) and reverse-grip ruby dagger (left) |
| **Setting** | Circular stone arena at night, blood moon sky |

The arena includes:

- Stone walls, classical pillars, and iron entry gates
- Crescent-moon banners and hanging chains
- Gargoyle sentinels, braziers with fire VFX
- Floor and wall blood, skulls, stone piles, buckets, and weapon props

Hand poses are driven by `Assets/Scripts/OrayaSwordGrip.cs` so the dual-wield grip holds in the editor and in Play Mode.

## Requirements

- Windows (editor and play)
- Unity **6000.0.83f1**
- Universal Render Pipeline (already in `Packages/manifest.json`)

Third-party meshes, textures, and VFX in `Assets/` are used as imported packs. Keep those licenses if you redistribute.
