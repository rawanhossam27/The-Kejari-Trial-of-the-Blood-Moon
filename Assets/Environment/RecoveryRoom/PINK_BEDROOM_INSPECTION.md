# Pink Bedroom inspection

Inspection only. The FBX, its textures, scenes, and gameplay were not changed.

**Verdict: Suitable with modifications.**

The room is already a single enclosed bedroom about 10.7 m by 10.7 m by 6.1 m, with separate meshes for the shell and the furniture. It imports on URP Lit, uses real meters, and is light enough to leave in a scene. The pink look is painted into four albedo atlases. A material-color tint can darken it, but it will not turn floral pink into antique wood or burgundy cloth. Keep the layout and the meshes. Replace or repaint the surfaces that must read as gothic, and add moonlight, candles, and a few palace props.

## Asset structure

```
Assets/Environment/RecoveryRoom/
  source/pink.fbx          7.20 MB
  textures/1jn.png         1.30 MB
  textures/2jn.png         1.38 MB
  textures/jn3.png         1.36 MB
  textures/попка.png       0.57 MB
```

No prefab, no extracted material folder, no normal maps, no masks, and no README. The FBX is the whole model. Unity already created the `.meta` files beside it.

`pink.fbx` imports as one root, `pink`, with 28 direct children. Nothing is nested into a bed group or a curtain group. Names are Blender leftovers: `node_0` through `node_0.015`, `Plane` through `Plane.004`, `Cylinder`, `Cylinder.001`, and five lights named `Point`.

| Kind | Count |
|---|---:|
| Mesh objects | 23 |
| Vertices | 80,786 |
| Triangles | 46,508 |
| Materials | 5 |
| Textures wired to materials | 4 |
| Lights | 5 |

## Technical compatibility

| Check | Result |
|---|---|
| Format | Binary FBX. Scale comes from the file (`useFileScale` on, `globalScale` 1). |
| Axis | Floor lies on XZ. Up is Y. The assembled bounds are about 10.92 by 6.27 by 11.04 m. Compatible with Oraya’s scale if the root stays at 1. |
| Local scale | Individual objects have large Blender scales (about 56 to 1366) and tiny mesh bounds. The world size is already correct. Do not scale the root by 100. |
| Shader | All five materials are `Universal Render Pipeline/Lit`. None are the error shader. |
| Pipeline extras | Each light has `UniversalAdditionalLightData`. Rectangle and spot lights are valid URP lights. |
| UVs | Every mesh has UV0. None have UV2. `generateSecondaryUV` is off, so this room is not ready for baked lightmaps. Realtime lights are the intended path. |
| Normals | Present. No normal-map textures. |
| Colliders | `addColliders` is off. The room will not block a character until colliders are added later. |
| Animation | Generic import, no clips, no avatar. Not a character. |
| Index format | 16-bit. The largest mesh is 12,783 vertices, under the 65,535 limit. |
| Read/write | Meshes and textures are not readable. Fine for rendering. Runtime mesh edits would need that flag. |

The hierarchy is editable without opening the FBX. Each furniture piece is its own `MeshRenderer`. A later prefab instance can hide, move, or override one object. `preserveHierarchy` is off, but the children are still separate. Do not edit `pink.fbx` itself.

## Materials and what a recolor can keep

Color is not a material tint. `_BaseColor` is white on every material. The pink, cream, and floral color is inside the PNGs. All four textures import as sRGB default textures, max 1024, mipmaps on, compressed. There is no alpha cutout, no normal, no metallic map.

| Material | Albedo | Smoothness | Metallic | Used by |
|---|---|---:|---:|---|
| `jn1` | `попка.png` | 0.50 | 0 | Floor, ceiling, three wall planes, vanity body, one prop, one round prop |
| `jn2` | `jn3.png` | 0 | 0 | Largest furniture mass, including the bed-sized mesh, plus two tall narrow pieces |
| `jn3` | `2jn.png` | 0 | 0 | Chandelier, full-height wall piece, and several floor pieces |
| `jn4` | `1jn.png` | 0 | 0 | Matching pair, lamps, wall ornaments |
| `зеркало` | none | 1 | 0 | Second submesh of the vanity only. The name means “mirror”. |

`jn3.png`, `2jn.png`, and `1jn.png` are four-panel atlases. Each panel mixes molded shapes, fabric, and floral color. `попка.png` is the cleanest image: pink plaster above white boiserie, with a large empty white area.

A duplicated material can tint one renderer without tinting its neighbors. That preserves shadows and molding in the albedo, but a dark multiply on a light pink floral map goes muddy. It does not become walnut or velvet. Independent color is possible. A convincing gothic surface needs a copied texture, or a new albedo, for cloth, wood, floor, and walls.

Objects that share `jn2`, `jn3`, or `jn4` are not independently colored until those renderers get their own material copies. The copies can still point at the same PNG.

## Room layout, inferred from bounds

The file has no semantic names. Roles below come from world size and position. Confirm them in a prefab view before replacing any one mesh.

Shell, all on `jn1`:

| Object | World size (m) | Role |
|---|---|---|
| `Plane` | 10.71 × 0 × 10.71 at y 0.02 | Floor |
| `Plane.002` | 10.71 × 0 × 10.71 at y 6.01 | Ceiling |
| `Plane.001` | wall at x +5.35, 6.11 m tall | +X wall |
| `Plane.004` | wall at x −5.35 | −X wall |
| `Plane.003` | wall at z −5.35 | −Z wall |

There is no fourth wall plane on +Z. That side is the natural place for windows and moonlight. `node_0.006` is a thin full-height piece (about 1.3 × 6.3 × 6.1 m) just inside the +X wall, so one long side already has a separate dressing mesh. `node_0.014` is a wide panel high on the −Z wall (about 4.1 × 2.6 × 0.3 m).

Furniture worth keeping as meshes:

| Object | Approx. size (m) | Likely role | Material |
|---|---|---|---|
| `node_0.001` | 4.4 × 3.0 × 4.7 | Bed | `jn2` |
| `node_0.002` | 1.8 × 4.3 × 3.6 | Tall wardrobe or drape | `jn2` |
| `node_0.003` | 1.5 × 2.7 × 3.4 | Side cabinet or seating along −X | `jn2` |
| `node_0` | 2.7 × 2.3 × 2.7 | Corner piece against −Z | `jn3` |
| `node_0.007` | 1.9 × 3.6 × 1.6, 2 materials | Vanity. Second material is the mirror | `jn1` + `зеркало` |
| `node_0.004`, `node_0.005` | 1.4 × 1.8 × 1.0, identical meshes | Pair along the −Z wall. Nightstands or chairs | `jn3` |
| `node_0.008` | 1.0 × 1.5 × 0.8 | Small table. A red light sits on it | `jn4` |
| `node_0.010`, `node_0.011` | about 0.9 × 3.5 × 0.9 | Pair of tall narrow stands | `jn2` |
| `node_0.012` | 1.8 × 1.8 × 1.7, hung at y 5.1 | Ceiling fixture | `jn3` |
| `node_0.013` | 1.3 × 2.4 × 1.2 | Medium chair or cabinet | `jn1` |
| `node_0.009`, `node_0.015`, `Cylinder`, `Cylinder.001` | under 1.7 m, several are wall-height | Lamps, frames, or small ornaments | `jn1` or `jn4` |

Lights already in the file:

| Object | Type | Color | Intensity | Position |
|---|---|---|---:|---|
| `Point` | Rectangle | warm pink | 126 | high on −X |
| `Point.001` | Point | deep pink-red | 49 | on the small table `node_0.008` |
| `Point.002` | Spot | white | 1582 | outside +Z, above the opening |
| `Point.003` | Rectangle | pink | 539 | high, toward +Z |
| `Point.004` | Rectangle | warm pink | 23 | near the vanity wall |

Intensities look like a Blender watt export, especially the spot at 1582. They will need to be brought down into URP ranges. Their positions are still useful: bedside, vanity, and one white spot aimed in from the open side.

## Visual transformation

| Original element | Proposed appearance | Required modification | Difficulty |
|---|---|---|---|
| Bed `node_0.001` | Dark wood frame, charcoal or burgundy bedding | Duplicate `jn2`. Tint is only a first pass. Repaint or replace the bed’s albedo islands so flowers do not remain | Hard |
| Bedding specifically | Matte dark cloth, low smoothness | Same mesh. New albedo. Smoothness stays near 0, which already reads as cloth | Hard |
| Curtain-scale meshes `node_0.006`, `node_0.014`, and the tall `node_0.002` if it is drapery | Heavy burgundy or near-black curtains | Confirm identity in a view. Then a duplicated material and a new cloth albedo. Add a separate curtain only if these meshes are windows or walls | Medium if they are cloth meshes. Hard if new drapery must be modeled |
| Floor `Plane` | Dark antique boards or a muted rug, not boiserie | Do not keep `попка` on the floor. New URP Lit material on this plane only. Optional rug mesh on top | Medium |
| Ceiling `Plane.002` | Dark plaster, slightly warmer than the walls | Own material. A darkened copy of the plaster half of `попка`, not the whole atlas | Easy |
| Walls `Plane.001`, `Plane.003`, `Plane.004` | Charcoal or stained plaster, cream or aged-gilt molding | Copy `попка.png`. Recolor the pink upper field. Leave the white molding light, or it turns grey when the whole texture is multiplied | Medium |
| Vanity `node_0.007` | Dark wood case | Own copy of `jn1`, or a wood albedo if the baked pink is too strong | Medium |
| Mirror submesh `зеркало` | Dark glass, slight reflection | No texture to protect. Lower the base color, set metallic near 1, keep smoothness at 1. A reflection probe later sells it | Easy |
| Pair `node_0.004` and `node_0.005` | Matching nightstands or side chairs in dark wood | One duplicated material for the pair. New wood albedo if the atlas stays floral | Medium |
| Small lamp table `node_0.008` | Bedside table, dark wood, warm practical light | Recolor mesh. Retune `Point.001` to candle warmth and a short range | Easy |
| Tall pair `node_0.010`, `node_0.011` | Candlestands or narrow cabinets | Duplicate `jn2` so they are not tied to the bed tint | Medium |
| Chandelier `node_0.012` | Dark metal, weak warm emission | Duplicate `jn3`. Add an emission color only on a copy. Real candle flames are not in the mesh | Medium |
| Wall ornaments and cylinders | Antique frames, kept only where the silhouette is already decorative | Tint first. Replace any piece whose atlas island is an obvious pink print | Medium |
| Existing lights | Warm practicals plus one cool moon source | Reuse transforms. Cut intensities. Shift pink lights toward amber. Keep `Point.002` as the cool window spot and lower it sharply | Easy |
| Missing gothic layer | Rug, a few paintings, candlesticks, maybe a heavier bedspread | New props. Do not rebuild the shell | Medium, and only as dressing |

## What to retain, recolor, replace, or add

**Retain the meshes and the layout.** Floor, ceiling, three walls, bed mass, vanity, the matching pair, the ceiling fixture, and the light transforms. The open +Z side should stay open or become the window wall.

**Recolor with copied materials, not by editing the FBX.** Mirror, ceiling, metal-looking small parts, and any prop that still reads correctly after a dark tint.

**Replace the albedo, keep the mesh.** Walls (edited copy of `попка.png`), floor, bed and bedding, and anything that is still recognizably pink after a tint. Floor must not keep the wall texture.

**Supplement, do not swap the room.** Cool directional or tightened spot for moonlight, candle emission or a small flame, one dark rug, two or three framed pictures if the baked frames are unusable, and colliders when the room becomes playable. Normal maps are optional. The molding in `попка.png` already carries relief.

## Missing assets

- Normal, roughness, and metallic maps. Surfaces are flat albedo only.
- A floor material. The floor currently shows the wall atlas.
- Separate cloth and wood textures. Four atlases mix both.
- Named groups. A future prefab should rename instances. Leave `pink.fbx` untouched.
- Lightmap UVs, only if baked lighting is required later.
- Colliders.
- A fourth wall, only if the open side should not be the window.
- Gothic art, candle flames, and a rug. The project’s arena candles and warm practicals can be references. They should not be moved into this folder as part of this inspection.
- No shader package is missing. Do not install one.

`попка` and `зеркало` are Cyrillic filenames. They resolved on this machine. Keep those names stable so the imported material slots do not break.

## Performance

46.5k triangles for a whole dressed bedroom is comfortable for this URP scene. One material slot per object, four textures at 1024, and no transparency.

The cost to watch is lighting, not polygons. Five lights imported with hard shadows, three of them area lights, will be heavier than the meshes. For a recovery room, two shadowed warm practicals and one unshadowed cool window light are enough. The chandelier does not need a shadow-casting light on every candle.

There is no LOD. At this count it does not need one unless many copies of the room are visible.

## Recommendation

**Suitable with modifications.**

Use this asset as the Recovery Room shell and furniture. Do not construct a new room, and do not rewrite the imported FBX. Instance it later, override materials per object, repaint wall and cloth textures where a tint stays pink, retune the lights, and add only the gothic pieces the atlases cannot provide: floor, bedding color, curtains if the tall meshes are not already drapes, moonlight, and a few props.

A tint-only pass will look like a darkened pink bedroom. That is not the palace room. The mesh layout is the part worth keeping.
