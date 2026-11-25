# Recovery Room art report

The Pink Bedroom is now a gothic prefab and a preview scene. This is an art pass only. Oraya, dialogue, objectives, and the Blood Moon Arena scene were not touched. `source/pink.fbx` and the four original PNGs were not edited.

Preview scene: `Assets/Environment/RecoveryRoom/Scenes/RecoveryRoom_ArtPreview.unity`

Prefab: `Assets/Environment/RecoveryRoom/Prefabs/RecoveryRoom_Gothic.prefab`

The scene camera is left on the wide view from the open side, looking toward the bed.

## What the meshes actually are

Each mesh was opened on its own in the Editor before it was renamed. The old Blender names were not trusted.

| Imported object | What it is | Prefab name | Treatment |
|---|---|---|---|
| `node_0.001` | Tufted bed, linens, pillows | `Bed` | Repainted albedo: burgundy cloth, charcoal linen, walnut frame |
| `node_0.006` | Floor-length curtains | `Curtains` | Repainted to burgundy velvet |
| `node_0.014` | Swag valance over the bed | `Curtain_Valance` | Repainted to burgundy velvet |
| `node_0.002` | Dressing table with a tall mirror | `Vanity` | Repainted to dark wood; flat mirror panel pushed toward dark glass |
| `node_0.007` | Oval wall mirror, two materials | `Mirror_Oval` | Carved frame on a wood albedo; glass is dark and reflective |
| `node_0` | Upholstered armchair | `Armchair` | Pink cloth to burgundy, pale frame to walnut |
| `node_0.004`, `node_0.005` | Matching bombe nightstands | `Nightstand_Left`, `Nightstand_Right` | Shared dark-wood albedo |
| `node_0.003` | Fireplace | `Fireplace` | Grey marble grade shifted to darker stone |
| `node_0.013` | Openwork metal chair | `Chair` | Iron |
| `node_0.012` | Five-arm chandelier | `Chandelier` | Bronze and muted shade color, one material |
| `node_0.008`, `node_0.009` | Floral table lamps | `Lamp_Table`, `Lamp_Mantel` | Shade, stem, and flowers recolored |
| `node_0.010`, `node_0.011` | Tall floor lamps | `FloorLamp_Mirror`, `FloorLamp_West` | Same lamp treatment |
| `Cylinder` | Wall bust with roses | `Bust_Floral` | Cool marble, roses deepened |
| `Cylinder.001` | Round pedestal under the bust | `Bust_Pedestal` | Wood |
| `node_0.015` | Three candles and roses | `Candles` | Ivory wax, dark roses |
| `Plane` | Floor | `Floor` | New walnut plank texture. The wall atlas is not used |
| `Plane.001`, `Plane.003`, `Plane.004` | East, south, and west walls | `Wall_East`, `Wall_South`, `Wall_West` | Boiserie recolored: dark panels, cream-gold molding |
| `Plane.002` | Ceiling | `Ceiling` | Mottled dark plaster |

The open side is now a window wall. All 23 imported renderers are still in the prefab. None of the source meshes were edited. The current look is the polish pass at the end of this report.

## Original versus new files

Unchanged:

- `source/pink.fbx`
- `textures/1jn.png`
- `textures/2jn.png`
- `textures/jn3.png`
- `textures/попка.png`

New, all under `Assets/Environment/RecoveryRoom/`:

- `textures/Gothic/` — repainted atlas copies plus floor, ceiling, rug, and two paintings
- `Materials/` — 24 URP Lit materials
- `Prefabs/RecoveryRoom_Gothic.prefab`
- `Scenes/RecoveryRoom_ArtPreview.unity`

The gothic PNGs were written into `textures/Gothic` because Windows treats `Textures` and `textures` as the same folder. The original four PNGs in that parent folder were not overwritten.

## Materials

Every furniture material is `Universal Render Pipeline/Lit`. Color is in the albedo, not a dark multiply on the pink original.

| Material | Surface |
|---|---|
| `M_Bed` | Bed. Burgundy upholstery, charcoal sheets, walnut frame, kept in the original UV islands |
| `M_Curtain_Velvet`, `M_Valance_Velvet` | Curtains and the bed valance |
| `M_Wall_Boiserie` | Three walls. Panel faces darkened, bright molding kept light |
| `M_Floor_Walnut` | Generated dark planks, tiled. Not `попка.png` |
| `M_Ceiling_Plaster` | Dark mottled plaster |
| `M_Vanity_Wood` | Vanity carcass |
| `M_Mirror_Frame`, `M_Mirror_Glass` | Oval mirror. Glass is metallic 0.92, smoothness 0.88, base color about 0.07 |
| `M_Nightstand_Wood` | Both nightstands |
| `M_Armchair` | Chair cloth and frame |
| `M_Fireplace_Stone` | Fireplace |
| `M_Chair_Iron` | Metal chair, metallic 0.72 |
| `M_Chandelier_Bronze` | Chandelier, metallic 0.35, darkened so the shades do not blow out |
| `M_FloorLamp`, `M_TableLamp` | Lamps |
| `M_Bust`, `M_Pedestal`, `M_Candles` | Bust, its stand, the candle cluster |
| `M_Rug` | Rug |
| `M_Painting_Moon`, `M_Painting_Figure` | Two canvases |
| `M_Trim_Wood`, `M_Book_Leather`, `M_Book_Page`, `M_Candle_Wax` | Frames, books, the added candlestick |

Bed, curtains, walls, and floor each have their own texture. Objects that share an atlas quadrant, such as the two nightstands, share one repainted copy. Other quadrants of that copy are unused by those meshes.

## Lighting

The five imported lights were retuned. The default scene directional light is off. Ambient light is a dim warm grey, about `(0.10, 0.075, 0.065)`, so unlit corners stay readable without a second sun.

| Light | Role | Shadows |
|---|---|---|
| `Light_TableLamp` | Warm amber point on the right nightstand. Intensity 3.2, range 3.4. Moved off the floral lamp, which was flaring on the floor | Soft |
| `Light_Mantel` | Warm ember point at the fireplace. Intensity 4.2, range 5.2 | Soft |
| `Light_Moon` | Cool spot outside the open side, aimed into the room. Intensity 11, range 22, spot angle 80 | None |
| `Light_Chandelier` | Small warm point under the chandelier. Intensity 2.2, range 8. Lowered out of the shade mesh | None |
| `Light_Unused` | The extra pink area light | Disabled |

Only the nightstand and the mantel cast shadows.

## Added dressing

Only what the bedroom did not already contain:

- A dark rug with a border and one offset diamond, in front of the bed, rotated slightly.
- Two paintings: one on the west wall by the fireplace, one on the south wall beside the oval mirror. The canvases are flat dark interiors, not portraits.
- Three books and one bronze candlestick on the right nightstand. The imported candle cluster by the bust was kept.
- A box collider on the floor, about 10.6 by 10.6 meters, for the preview.
- One realtime reflection probe, 128 resolution, covering the room.

No new packages. No arena props were moved.

## How it was checked

`RecoveryRoom_ArtPreview` was rendered from four cameras:

1. Wide view from the open side, toward the bed.
2. Closer view of the bed, nightstands, and oval mirror.
3. Vanity, iron chair, and the east curtain.
4. From beside the bed, looking out through the missing wall.

The wide view reads as one bedroom: burgundy bed and curtains, dark paneled walls, a warm pool on the near nightstand, a lamp by the fireplace, and a dark floor. The bed close-up keeps the tufting, the charcoal pillows, and the wood frame. The vanity is dark wood rather than pink lacquer.

## Remaining shortcomings

- The east and side walls stretch the boiserie. Up close, the molding turns into vertical bands. That is the imported plane’s UVs, not a second texture.
- The vanity’s mirror panel and a few flat atlas islands went very dark. The carcass reads as wood; the glass is not a clean rectangle.
- Both mirrors are dark. The room has little in front of them to reflect, and the probe is only 128 pixels.
- Moonlight is a cool tint from the open side, not a window beam. Looking outward shows a black opening because there is no exterior.
- The floor planks are generated. They read as dark boards in the wide view and stay simpler than the carved furniture.
- The chandelier is a single mesh and a single material, so the shades and the arms share a darkened bronze grade.
- There are no normal maps. Relief comes from the albedo and the mesh.
- The floral lamps and the bust still have the original sculpt. The pink was repainted, not remodelled.

## Performance

The imported room is 46,508 triangles and 80,786 vertices. The rug, frames, books, and candlestick add a few hundred triangles. Two shadowed point lights and one unshadowed spot are the lighting cost. The realtime probe is the extra item to drop if this room is later placed in a heavier scene. Textures are 1024 or smaller. Walls, floor, bed, curtains, rug, and chandelier are uncompressed so the dark molding does not block as badly; the rest are compressed.

## Not in the first pass

No gameplay, no Oraya, no objectives, and no change to `Assets/Scenes/SampleScene.unity`.

## Polish pass

The layout and furniture were kept. This pass only changes light, the rug, the mirrors, the open side, and the wall surfaces.

### Chandelier

`Light_Chandelier` is now the main warm source: a soft-shadow point just under the fixture, color about `(1.00, 0.86, 0.72)`, intensity 14, range 13. The bronze material is not emissive. Five small flame spheres sit under the shades and use `M_Flame` only. The nightstand light and the mantel light are unchanged. Corners fall off outside the chandelier’s range.

### Rug

`T_Rug.png` is a deep burgundy field with a diamond lattice, a gold and charcoal border, and one offset medallion. `M_Rug` smoothness is 0.06. The rug sits at y 0.028, under the foot of the bed, and runs forward into the middle of the floor. It is rotated a few degrees. It does not use the wall texture.

### Mirrors

`M_Mirror_Glass` is aged silver: base color about `(0.46, 0.48, 0.50)`, metallic 0.70, smoothness 0.66. That material is on the oval mirror’s glass submesh and on a separate quad, `Vanity_MirrorGlass`, placed on the vanity’s mirror panel so the wood material stays non-metal. The reflection probe is realtime, 256 resolution, box projection, covering the room. The glass is no longer a black hole. It is still dim, because the room in front of the mirrors is dark.

### Windows

The open side has a plaster pier wall, a wainscot, and three tall openings with mullions and shallow pointed heads. A transparent `M_WindowGlass` sits in each opening. Behind them, `NightSky` is a self-lit night backdrop with the moon set off-center. `Light_Moon` is an unshadowed cool spot outside those windows, aimed across the bed. A collider on the window wall keeps the interior walkable and stops a future player from walking out through the glass. No exterior level was built.

### Walls

The three original wall planes no longer wear the stretched boiserie. They use `M_Plaster`. A single row of `T_Wainscot` quads runs along the bottom of each wall, including the window wall, at the texture’s real proportion. A cream chair rail and a ceiling cornice (`M_Molding`) finish the joint. The vertical bands are gone. The upper wall is plain dark plaster rather than full-height carved panels.

### Bed and hearth

The bed albedo was graded from the original atlas copy, then the crimson quilt was shifted to warm stone grey so the bedding is not a second red next to the curtains and rug. The remaining brown cloth and the frame were pushed to a near-black brown. One mesh still carries the frame, quilt, and pillows, so the split is by color in the atlas, not by separate materials.

The hearth has three flattened coals (`M_Ember`) and a small unshadowed point, `Light_Ember`, intensity 0.85, range 1.8. They warm the opening. They are not a fire simulation.

### Checked views

Rendered in the preview scene after the changes:

1. Wide view from inside the windows, toward the bed. The quilt, wood frame, rug, wainscot, and nightstand light read together.
2. Bed and chandelier. The fixture is lit from below by its own light, with small flames, and the bed head is in the same view.
3. Vanity. The added glass plane reads as dull silver in the carved frame. The oval mirror is the same material.
4. Window wall. Night sky and the moon are visible through the three openings. The wainscot under them is not stretched.

### Still limited

- Upper walls are plain plaster. The carved paneling exists only as the lower wainscot.
- Window heads are shallow peaks made from straight pieces, not carved gothic tracery.
- The moon backdrop is one quad, not a skybox or a palace exterior.
- Mirrors reflect a dark room. They will not look bright until something luminous sits in front of them.
- Bed frame, quilt, and pillows still share one UV island set. The walnut and the linen are a color split, and some pillow faces stay close to the quilt.
- Hearth coals are simple meshes. The glow on the stone is the part that reads.
- Chandelier shades and arms still share `M_Chandelier_Bronze`. Only the added flames emit.
- No normal maps.

### Ready for gameplay

The prefab can be placed as the Recovery Room shell. It has a floor collider, a window-wall collider, a lit interior, and a closed boundary on all four sides. It does not contain Oraya, dialogue, objectives, or a door script. `Assets/Scenes/SampleScene.unity` was not modified. The original `pink.fbx` and the four source PNGs were not modified.
