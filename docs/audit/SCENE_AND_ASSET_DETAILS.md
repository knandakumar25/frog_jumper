# Axiom Force scenes and assets audit

Target engine: Godot 4.7.2. No engine, import, build, project edit, or deletion performed.

Read-only scene and asset audit. Serialized scene hierarchy does not include nodes created dynamically by scripts or internal imported GLB nodes. Exact source references, not a deletion-safe reachability proof. Excludes .godot, counts .import/.uid and generated .translation separately.

## Asset inventory

Raw/support files under assets/: **434**, **3,520,195,637 bytes**. Placeholders excluded and listed in JSON.

| Category | Files | Bytes |
|---|---:|---:|
| audio | 2 | 1,484,832 |
| fonts | 17 | 4,007,318 |
| images | 60 | 84,435,059 |
| models | 177 | 3,407,492,198 |
| music | 5 | 9,192,018 |
| narrative | 16 | 1,471,506 |
| shaders | 1 | 707 |
| speech | 153 | 11,947,104 |
| themes | 2 | 5,749 |
| translations | 1 | 159,146 |

| Format | Files | Bytes |
|---|---:|---:|
| .csv | 1 | 159,146 |
| .gdshader | 1 | 707 |
| .glb | 50 | 2,038,447,188 |
| .jpg | 135 | 700,694,260 |
| .js | 1 | 12,655 |
| .json | 13 | 1,402,977 |
| .log | 1 | 53,499 |
| .md | 1 | 2,375 |
| .mp3 | 160 | 22,623,954 |
| .png | 52 | 752,785,809 |
| .tres | 4 | 6,439 |
| .ttf | 15 | 4,006,628 |

Import metadata: 414 files (362,941 bytes); generated translations: 17 files (258,894 bytes). These are not counted as raw assets.

## Every serialized scene

### scenes/gameplay/collectable.tscn

Root `Collectable`: `Area2D` at `scenes/gameplay/collectable.tscn:137`; 4 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `.` -> `res://scripts/gameplay/collectable.gd` (GDScript); `scenes/gameplay/collectable.tscn:138` (resource declaration line 4).

Instanced subscenes/models:

- None serialized.

Serialized signal connections:

- `..body_entered` -> `.._on_body_entered`; `scenes/gameplay/collectable.tscn:153`.

Complete serialized hierarchy (path, type, declaration line):

```text
. [Area2D] :137
AnimatedSprite2D [AnimatedSprite2D] :140
CollisionShape2D [CollisionShape2D] :145
CoinSound [AudioStreamPlayer] :149
```

### scenes/gameplay/cyber_shogun.tscn

Root `CyberShogun`: `CharacterBody2D` at `scenes/gameplay/cyber_shogun.tscn:201`; 4 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `.` -> `res://scripts/actors/CyberShogun.cs` (C#); `scenes/gameplay/cyber_shogun.tscn:203` (resource declaration line 3).

Instanced subscenes/models:

- None serialized.

Serialized signal connections:

- None serialized; runtime connections are outside this audit.

Complete serialized hierarchy (path, type, declaration line):

```text
. [CharacterBody2D] :201
Sprite2D [AnimatedSprite2D] :205
CollisionShape2D [CollisionShape2D] :211
JumpSound [AudioStreamPlayer] :215
```

### scenes/gameplay/finish.tscn

Root `Finish`: `Area2D` at `scenes/gameplay/finish.tscn:10`; 4 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `.` -> `res://scripts/gameplay/finish.gd` (GDScript); `scenes/gameplay/finish.tscn:11` (resource declaration line 4).

Instanced subscenes/models:

- None serialized.

Serialized signal connections:

- `..body_entered` -> `.._on_body_entered`; `scenes/gameplay/finish.tscn:25`.

Complete serialized hierarchy (path, type, declaration line):

```text
. [Area2D] :10
Sprite2D [Sprite2D] :13
CollisionShape2D [CollisionShape2D] :17
FinishAudio [AudioStreamPlayer] :21
```

### scenes/gameplay/level1.tscn

Root `Node`: `Node` at `scenes/gameplay/level1.tscn:341`; 38 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `GameManager` -> `res://scripts/gameplay/GameManager.cs` (C#); `scenes/gameplay/level1.tscn:345` (resource declaration line 3).

Instanced subscenes/models:

- `SceneObjects/CyberShogun` -> `res://scenes/gameplay/main_character.tscn`; `scenes/gameplay/level1.tscn:369`.
- `SceneObjects/Collectables group/Collectable` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:380`.
- `SceneObjects/Collectables group/Collectable2` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:384`.
- `SceneObjects/Collectables group/Collectable3` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:388`.
- `SceneObjects/Collectables group/Collectable4` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:392`.
- `SceneObjects/Collectables group/Collectable5` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:396`.
- `SceneObjects/Collectables group/Collectable6` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:400`.
- `SceneObjects/Collectables group/Collectable7` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:404`.
- `SceneObjects/Collectables group/Collectable8` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:408`.
- `SceneObjects/Collectables group/Collectable9` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:412`.
- `SceneObjects/Collectables group/Collectable10` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:416`.
- `SceneObjects/Collectables group/Collectable11` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:420`.
- `SceneObjects/Collectables group/Collectable12` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:424`.
- `SceneObjects/Collectables group/Collectable13` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:428`.
- `SceneObjects/Collectables group/Collectable14` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:432`.
- `SceneObjects/Collectables group/Collectable15` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:436`.
- `SceneObjects/Collectables group/Collectable16` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:440`.
- `SceneObjects/Collectables group/Collectable17` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:444`.
- `SceneObjects/Collectables group/Collectable18` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:448`.
- `SceneObjects/Collectables group/Collectable19` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:452`.
- `SceneObjects/Collectables group/Collectable20` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:456`.
- `SceneObjects/Collectables group/Collectable21` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level1.tscn:460`.
- `SceneObjects/Finish` -> `res://scenes/gameplay/finish.tscn`; `scenes/gameplay/level1.tscn:464`.
- `CanvasLayer/PauseMenu` -> `res://scenes/gameplay/pause_menu.tscn`; `scenes/gameplay/level1.tscn:505`.

Serialized signal connections:

- `GameManager/Water.body_entered` -> `GameManager._on_water_body_entered`; `scenes/gameplay/level1.tscn:511`.

Complete serialized hierarchy (path, type, declaration line):

```text
. [Node] :341
GameManager [Node] :343
GameManager/Water [Area2D] :347
GameManager/Water/CollisionShape2D [CollisionShape2D] :350
SceneObjects [Node] :354
SceneObjects/TextureRect [TextureRect] :356
SceneObjects/TileMap [TileMapLayer] :363
SceneObjects/CyberShogun [instance: res://scenes/gameplay/main_character.tscn] :369
SceneObjects/CyberShogun/Camera2D [Camera2D] :372
SceneObjects/Collectables group [Node] :378
SceneObjects/Collectables group/Collectable [instance: res://scenes/gameplay/collectable.tscn] :380
SceneObjects/Collectables group/Collectable2 [instance: res://scenes/gameplay/collectable.tscn] :384
SceneObjects/Collectables group/Collectable3 [instance: res://scenes/gameplay/collectable.tscn] :388
SceneObjects/Collectables group/Collectable4 [instance: res://scenes/gameplay/collectable.tscn] :392
SceneObjects/Collectables group/Collectable5 [instance: res://scenes/gameplay/collectable.tscn] :396
SceneObjects/Collectables group/Collectable6 [instance: res://scenes/gameplay/collectable.tscn] :400
SceneObjects/Collectables group/Collectable7 [instance: res://scenes/gameplay/collectable.tscn] :404
SceneObjects/Collectables group/Collectable8 [instance: res://scenes/gameplay/collectable.tscn] :408
SceneObjects/Collectables group/Collectable9 [instance: res://scenes/gameplay/collectable.tscn] :412
SceneObjects/Collectables group/Collectable10 [instance: res://scenes/gameplay/collectable.tscn] :416
SceneObjects/Collectables group/Collectable11 [instance: res://scenes/gameplay/collectable.tscn] :420
SceneObjects/Collectables group/Collectable12 [instance: res://scenes/gameplay/collectable.tscn] :424
SceneObjects/Collectables group/Collectable13 [instance: res://scenes/gameplay/collectable.tscn] :428
SceneObjects/Collectables group/Collectable14 [instance: res://scenes/gameplay/collectable.tscn] :432
SceneObjects/Collectables group/Collectable15 [instance: res://scenes/gameplay/collectable.tscn] :436
SceneObjects/Collectables group/Collectable16 [instance: res://scenes/gameplay/collectable.tscn] :440
SceneObjects/Collectables group/Collectable17 [instance: res://scenes/gameplay/collectable.tscn] :444
SceneObjects/Collectables group/Collectable18 [instance: res://scenes/gameplay/collectable.tscn] :448
SceneObjects/Collectables group/Collectable19 [instance: res://scenes/gameplay/collectable.tscn] :452
SceneObjects/Collectables group/Collectable20 [instance: res://scenes/gameplay/collectable.tscn] :456
SceneObjects/Collectables group/Collectable21 [instance: res://scenes/gameplay/collectable.tscn] :460
SceneObjects/Finish [instance: res://scenes/gameplay/finish.tscn] :464
UI [CanvasLayer] :469
UI/Panel [Panel] :471
UI/Panel/PointsLabel [Label] :479
AudioStreamPlayer [AudioStreamPlayer] :497
CanvasLayer [CanvasLayer] :503
CanvasLayer/PauseMenu [instance: res://scenes/gameplay/pause_menu.tscn] :505
```

### scenes/gameplay/level2.tscn

Root `Node`: `Node` at `scenes/gameplay/level2.tscn:363`; 42 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `GameManager` -> `res://scripts/gameplay/GameManager.cs` (C#); `scenes/gameplay/level2.tscn:367` (resource declaration line 3).

Instanced subscenes/models:

- `SceneObjects/CyberShogun` -> `res://scenes/gameplay/main_character.tscn`; `scenes/gameplay/level2.tscn:385`.
- `SceneObjects/Collectables group/Collectable` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:396`.
- `SceneObjects/Collectables group/Collectable2` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:400`.
- `SceneObjects/Collectables group/Collectable3` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:404`.
- `SceneObjects/Collectables group/Collectable4` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:408`.
- `SceneObjects/Collectables group/Collectable5` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:412`.
- `SceneObjects/Collectables group/Collectable6` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:416`.
- `SceneObjects/Collectables group/Collectable7` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:420`.
- `SceneObjects/Collectables group/Collectable8` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:424`.
- `SceneObjects/Collectables group/Collectable9` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:428`.
- `SceneObjects/Collectables group/Collectable10` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:432`.
- `SceneObjects/Collectables group/Collectable11` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:436`.
- `SceneObjects/Collectables group/Collectable12` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:440`.
- `SceneObjects/Collectables group/Collectable13` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:444`.
- `SceneObjects/Collectables group/Collectable14` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:448`.
- `SceneObjects/Collectables group/Collectable15` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:452`.
- `SceneObjects/Collectables group/Collectable16` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:456`.
- `SceneObjects/Collectables group/Collectable17` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:460`.
- `SceneObjects/Collectables group/Collectable18` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:464`.
- `SceneObjects/Collectables group/Collectable19` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:468`.
- `SceneObjects/Collectables group/Collectable20` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:472`.
- `SceneObjects/Collectables group/Collectable21` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:476`.
- `SceneObjects/Collectables group/Collectable22` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:480`.
- `SceneObjects/Collectables group/Collectable23` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:484`.
- `SceneObjects/Collectables group/Collectable24` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:488`.
- `SceneObjects/Collectables group/Collectable25` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:492`.
- `SceneObjects/Collectables group/Collectable26` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:496`.
- `SceneObjects/Collectables group/Collectable27` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level2.tscn:500`.
- `SceneObjects/Finish` -> `res://scenes/gameplay/finish.tscn`; `scenes/gameplay/level2.tscn:504`.
- `CanvasLayer/PauseMenu` -> `res://scenes/gameplay/pause_menu.tscn`; `scenes/gameplay/level2.tscn:545`.

Serialized signal connections:

- None serialized; runtime connections are outside this audit.

Complete serialized hierarchy (path, type, declaration line):

```text
. [Node] :363
GameManager [Node] :365
SceneObjects [Node] :369
SceneObjects/TextureRect [TextureRect] :371
SceneObjects/TileMap [TileMapLayer] :378
SceneObjects/CyberShogun [instance: res://scenes/gameplay/main_character.tscn] :385
SceneObjects/CyberShogun/Camera2D [Camera2D] :388
SceneObjects/Collectables group [Node] :394
SceneObjects/Collectables group/Collectable [instance: res://scenes/gameplay/collectable.tscn] :396
SceneObjects/Collectables group/Collectable2 [instance: res://scenes/gameplay/collectable.tscn] :400
SceneObjects/Collectables group/Collectable3 [instance: res://scenes/gameplay/collectable.tscn] :404
SceneObjects/Collectables group/Collectable4 [instance: res://scenes/gameplay/collectable.tscn] :408
SceneObjects/Collectables group/Collectable5 [instance: res://scenes/gameplay/collectable.tscn] :412
SceneObjects/Collectables group/Collectable6 [instance: res://scenes/gameplay/collectable.tscn] :416
SceneObjects/Collectables group/Collectable7 [instance: res://scenes/gameplay/collectable.tscn] :420
SceneObjects/Collectables group/Collectable8 [instance: res://scenes/gameplay/collectable.tscn] :424
SceneObjects/Collectables group/Collectable9 [instance: res://scenes/gameplay/collectable.tscn] :428
SceneObjects/Collectables group/Collectable10 [instance: res://scenes/gameplay/collectable.tscn] :432
SceneObjects/Collectables group/Collectable11 [instance: res://scenes/gameplay/collectable.tscn] :436
SceneObjects/Collectables group/Collectable12 [instance: res://scenes/gameplay/collectable.tscn] :440
SceneObjects/Collectables group/Collectable13 [instance: res://scenes/gameplay/collectable.tscn] :444
SceneObjects/Collectables group/Collectable14 [instance: res://scenes/gameplay/collectable.tscn] :448
SceneObjects/Collectables group/Collectable15 [instance: res://scenes/gameplay/collectable.tscn] :452
SceneObjects/Collectables group/Collectable16 [instance: res://scenes/gameplay/collectable.tscn] :456
SceneObjects/Collectables group/Collectable17 [instance: res://scenes/gameplay/collectable.tscn] :460
SceneObjects/Collectables group/Collectable18 [instance: res://scenes/gameplay/collectable.tscn] :464
SceneObjects/Collectables group/Collectable19 [instance: res://scenes/gameplay/collectable.tscn] :468
SceneObjects/Collectables group/Collectable20 [instance: res://scenes/gameplay/collectable.tscn] :472
SceneObjects/Collectables group/Collectable21 [instance: res://scenes/gameplay/collectable.tscn] :476
SceneObjects/Collectables group/Collectable22 [instance: res://scenes/gameplay/collectable.tscn] :480
SceneObjects/Collectables group/Collectable23 [instance: res://scenes/gameplay/collectable.tscn] :484
SceneObjects/Collectables group/Collectable24 [instance: res://scenes/gameplay/collectable.tscn] :488
SceneObjects/Collectables group/Collectable25 [instance: res://scenes/gameplay/collectable.tscn] :492
SceneObjects/Collectables group/Collectable26 [instance: res://scenes/gameplay/collectable.tscn] :496
SceneObjects/Collectables group/Collectable27 [instance: res://scenes/gameplay/collectable.tscn] :500
SceneObjects/Finish [instance: res://scenes/gameplay/finish.tscn] :504
UI [CanvasLayer] :509
UI/Panel [Panel] :511
UI/Panel/PointsLabel [Label] :519
AudioStreamPlayer [AudioStreamPlayer] :537
CanvasLayer [CanvasLayer] :543
CanvasLayer/PauseMenu [instance: res://scenes/gameplay/pause_menu.tscn] :545
```

### scenes/gameplay/level3.tscn

Root `Node`: `Node` at `scenes/gameplay/level3.tscn:376`; 55 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `GameManager` -> `res://scripts/gameplay/GameManager.cs` (C#); `scenes/gameplay/level3.tscn:380` (resource declaration line 3).

Instanced subscenes/models:

- `SceneObjects/CyberShogun` -> `res://scenes/gameplay/main_character.tscn`; `scenes/gameplay/level3.tscn:398`.
- `SceneObjects/Collectables group/Collectable27` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:409`.
- `SceneObjects/Collectables group/Collectable44` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:413`.
- `SceneObjects/Collectables group/Collectable66` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:417`.
- `SceneObjects/Collectables group/Collectable62` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:421`.
- `SceneObjects/Collectables group/Collectable63` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:425`.
- `SceneObjects/Collectables group/Collectable64` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:429`.
- `SceneObjects/Collectables group/Collectable65` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:433`.
- `SceneObjects/Collectables group/Collectable55` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:437`.
- `SceneObjects/Collectables group/Collectable56` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:441`.
- `SceneObjects/Collectables group/Collectable57` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:445`.
- `SceneObjects/Collectables group/Collectable58` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:449`.
- `SceneObjects/Collectables group/Collectable59` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:453`.
- `SceneObjects/Collectables group/Collectable60` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:457`.
- `SceneObjects/Collectables group/Collectable61` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:461`.
- `SceneObjects/Collectables group/Collectable45` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:465`.
- `SceneObjects/Collectables group/Collectable46` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:469`.
- `SceneObjects/Collectables group/Collectable47` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:473`.
- `SceneObjects/Collectables group/Collectable48` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:477`.
- `SceneObjects/Collectables group/Collectable49` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:481`.
- `SceneObjects/Collectables group/Collectable50` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:485`.
- `SceneObjects/Collectables group/Collectable51` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:489`.
- `SceneObjects/Collectables group/Collectable52` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:493`.
- `SceneObjects/Collectables group/Collectable53` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:497`.
- `SceneObjects/Collectables group/Collectable54` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:501`.
- `SceneObjects/Collectables group/Collectable28` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:505`.
- `SceneObjects/Collectables group/Collectable29` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:509`.
- `SceneObjects/Collectables group/Collectable30` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:513`.
- `SceneObjects/Collectables group/Collectable31` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:517`.
- `SceneObjects/Collectables group/Collectable32` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:521`.
- `SceneObjects/Collectables group/Collectable33` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:525`.
- `SceneObjects/Collectables group/Collectable34` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:529`.
- `SceneObjects/Collectables group/Collectable35` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:533`.
- `SceneObjects/Collectables group/Collectable36` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:537`.
- `SceneObjects/Collectables group/Collectable37` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:541`.
- `SceneObjects/Collectables group/Collectable38` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:545`.
- `SceneObjects/Collectables group/Collectable39` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:549`.
- `SceneObjects/Collectables group/Collectable40` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:553`.
- `SceneObjects/Collectables group/Collectable41` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:557`.
- `SceneObjects/Collectables group/Collectable42` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:561`.
- `SceneObjects/Collectables group/Collectable43` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level3.tscn:565`.
- `SceneObjects/Finish` -> `res://scenes/gameplay/finish.tscn`; `scenes/gameplay/level3.tscn:569`.
- `CanvasLayer/PauseMenu` -> `res://scenes/gameplay/pause_menu.tscn`; `scenes/gameplay/level3.tscn:610`.

Serialized signal connections:

- None serialized; runtime connections are outside this audit.

Complete serialized hierarchy (path, type, declaration line):

```text
. [Node] :376
GameManager [Node] :378
SceneObjects [Node] :382
SceneObjects/TextureRect [TextureRect] :384
SceneObjects/TileMap [TileMapLayer] :391
SceneObjects/CyberShogun [instance: res://scenes/gameplay/main_character.tscn] :398
SceneObjects/CyberShogun/Camera2D [Camera2D] :401
SceneObjects/Collectables group [Node] :407
SceneObjects/Collectables group/Collectable27 [instance: res://scenes/gameplay/collectable.tscn] :409
SceneObjects/Collectables group/Collectable44 [instance: res://scenes/gameplay/collectable.tscn] :413
SceneObjects/Collectables group/Collectable66 [instance: res://scenes/gameplay/collectable.tscn] :417
SceneObjects/Collectables group/Collectable62 [instance: res://scenes/gameplay/collectable.tscn] :421
SceneObjects/Collectables group/Collectable63 [instance: res://scenes/gameplay/collectable.tscn] :425
SceneObjects/Collectables group/Collectable64 [instance: res://scenes/gameplay/collectable.tscn] :429
SceneObjects/Collectables group/Collectable65 [instance: res://scenes/gameplay/collectable.tscn] :433
SceneObjects/Collectables group/Collectable55 [instance: res://scenes/gameplay/collectable.tscn] :437
SceneObjects/Collectables group/Collectable56 [instance: res://scenes/gameplay/collectable.tscn] :441
SceneObjects/Collectables group/Collectable57 [instance: res://scenes/gameplay/collectable.tscn] :445
SceneObjects/Collectables group/Collectable58 [instance: res://scenes/gameplay/collectable.tscn] :449
SceneObjects/Collectables group/Collectable59 [instance: res://scenes/gameplay/collectable.tscn] :453
SceneObjects/Collectables group/Collectable60 [instance: res://scenes/gameplay/collectable.tscn] :457
SceneObjects/Collectables group/Collectable61 [instance: res://scenes/gameplay/collectable.tscn] :461
SceneObjects/Collectables group/Collectable45 [instance: res://scenes/gameplay/collectable.tscn] :465
SceneObjects/Collectables group/Collectable46 [instance: res://scenes/gameplay/collectable.tscn] :469
SceneObjects/Collectables group/Collectable47 [instance: res://scenes/gameplay/collectable.tscn] :473
SceneObjects/Collectables group/Collectable48 [instance: res://scenes/gameplay/collectable.tscn] :477
SceneObjects/Collectables group/Collectable49 [instance: res://scenes/gameplay/collectable.tscn] :481
SceneObjects/Collectables group/Collectable50 [instance: res://scenes/gameplay/collectable.tscn] :485
SceneObjects/Collectables group/Collectable51 [instance: res://scenes/gameplay/collectable.tscn] :489
SceneObjects/Collectables group/Collectable52 [instance: res://scenes/gameplay/collectable.tscn] :493
SceneObjects/Collectables group/Collectable53 [instance: res://scenes/gameplay/collectable.tscn] :497
SceneObjects/Collectables group/Collectable54 [instance: res://scenes/gameplay/collectable.tscn] :501
SceneObjects/Collectables group/Collectable28 [instance: res://scenes/gameplay/collectable.tscn] :505
SceneObjects/Collectables group/Collectable29 [instance: res://scenes/gameplay/collectable.tscn] :509
SceneObjects/Collectables group/Collectable30 [instance: res://scenes/gameplay/collectable.tscn] :513
SceneObjects/Collectables group/Collectable31 [instance: res://scenes/gameplay/collectable.tscn] :517
SceneObjects/Collectables group/Collectable32 [instance: res://scenes/gameplay/collectable.tscn] :521
SceneObjects/Collectables group/Collectable33 [instance: res://scenes/gameplay/collectable.tscn] :525
SceneObjects/Collectables group/Collectable34 [instance: res://scenes/gameplay/collectable.tscn] :529
SceneObjects/Collectables group/Collectable35 [instance: res://scenes/gameplay/collectable.tscn] :533
SceneObjects/Collectables group/Collectable36 [instance: res://scenes/gameplay/collectable.tscn] :537
SceneObjects/Collectables group/Collectable37 [instance: res://scenes/gameplay/collectable.tscn] :541
SceneObjects/Collectables group/Collectable38 [instance: res://scenes/gameplay/collectable.tscn] :545
SceneObjects/Collectables group/Collectable39 [instance: res://scenes/gameplay/collectable.tscn] :549
SceneObjects/Collectables group/Collectable40 [instance: res://scenes/gameplay/collectable.tscn] :553
SceneObjects/Collectables group/Collectable41 [instance: res://scenes/gameplay/collectable.tscn] :557
SceneObjects/Collectables group/Collectable42 [instance: res://scenes/gameplay/collectable.tscn] :561
SceneObjects/Collectables group/Collectable43 [instance: res://scenes/gameplay/collectable.tscn] :565
SceneObjects/Finish [instance: res://scenes/gameplay/finish.tscn] :569
UI [CanvasLayer] :574
UI/Panel [Panel] :576
UI/Panel/PointsLabel [Label] :584
AudioStreamPlayer [AudioStreamPlayer] :602
CanvasLayer [CanvasLayer] :608
CanvasLayer/PauseMenu [instance: res://scenes/gameplay/pause_menu.tscn] :610
```

### scenes/gameplay/level4.tscn

Root `Node`: `Node` at `scenes/gameplay/level4.tscn:392`; 37 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `GameManager` -> `res://scripts/gameplay/GameManager.cs` (C#); `scenes/gameplay/level4.tscn:396` (resource declaration line 3).

Instanced subscenes/models:

- `SceneObjects/CyberShogun` -> `res://scenes/gameplay/main_character.tscn`; `scenes/gameplay/level4.tscn:422`.
- `SceneObjects/Collectables group/Collectable27` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:433`.
- `SceneObjects/Collectables group/Collectable45` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:437`.
- `SceneObjects/Collectables group/Collectable46` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:441`.
- `SceneObjects/Collectables group/Collectable42` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:445`.
- `SceneObjects/Collectables group/Collectable43` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:449`.
- `SceneObjects/Collectables group/Collectable44` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:453`.
- `SceneObjects/Collectables group/Collectable37` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:457`.
- `SceneObjects/Collectables group/Collectable38` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:461`.
- `SceneObjects/Collectables group/Collectable39` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:465`.
- `SceneObjects/Collectables group/Collectable40` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:469`.
- `SceneObjects/Collectables group/Collectable41` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:473`.
- `SceneObjects/Collectables group/Collectable28` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:477`.
- `SceneObjects/Collectables group/Collectable29` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:481`.
- `SceneObjects/Collectables group/Collectable30` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:485`.
- `SceneObjects/Collectables group/Collectable31` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:489`.
- `SceneObjects/Collectables group/Collectable32` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:493`.
- `SceneObjects/Collectables group/Collectable33` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:497`.
- `SceneObjects/Collectables group/Collectable34` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:501`.
- `SceneObjects/Collectables group/Collectable35` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:505`.
- `SceneObjects/Collectables group/Collectable36` -> `res://scenes/gameplay/collectable.tscn`; `scenes/gameplay/level4.tscn:509`.
- `SceneObjects/Finish` -> `res://scenes/gameplay/finish.tscn`; `scenes/gameplay/level4.tscn:513`.
- `CanvasLayer/PauseMenu` -> `res://scenes/gameplay/pause_menu.tscn`; `scenes/gameplay/level4.tscn:554`.

Serialized signal connections:

- `GameManager/Water.body_entered` -> `GameManager._on_water_body_entered`; `scenes/gameplay/level4.tscn:560`.

Complete serialized hierarchy (path, type, declaration line):

```text
. [Node] :392
GameManager [Node] :394
GameManager/Water [Area2D] :398
GameManager/Water/CollisionShape2D [CollisionShape2D] :401
SceneObjects [Node] :405
SceneObjects/TextureRect [TextureRect] :407
SceneObjects/TileMap [TileMapLayer] :414
SceneObjects/CyberShogun [instance: res://scenes/gameplay/main_character.tscn] :422
SceneObjects/CyberShogun/Camera2D [Camera2D] :425
SceneObjects/Collectables group [Node] :431
SceneObjects/Collectables group/Collectable27 [instance: res://scenes/gameplay/collectable.tscn] :433
SceneObjects/Collectables group/Collectable45 [instance: res://scenes/gameplay/collectable.tscn] :437
SceneObjects/Collectables group/Collectable46 [instance: res://scenes/gameplay/collectable.tscn] :441
SceneObjects/Collectables group/Collectable42 [instance: res://scenes/gameplay/collectable.tscn] :445
SceneObjects/Collectables group/Collectable43 [instance: res://scenes/gameplay/collectable.tscn] :449
SceneObjects/Collectables group/Collectable44 [instance: res://scenes/gameplay/collectable.tscn] :453
SceneObjects/Collectables group/Collectable37 [instance: res://scenes/gameplay/collectable.tscn] :457
SceneObjects/Collectables group/Collectable38 [instance: res://scenes/gameplay/collectable.tscn] :461
SceneObjects/Collectables group/Collectable39 [instance: res://scenes/gameplay/collectable.tscn] :465
SceneObjects/Collectables group/Collectable40 [instance: res://scenes/gameplay/collectable.tscn] :469
SceneObjects/Collectables group/Collectable41 [instance: res://scenes/gameplay/collectable.tscn] :473
SceneObjects/Collectables group/Collectable28 [instance: res://scenes/gameplay/collectable.tscn] :477
SceneObjects/Collectables group/Collectable29 [instance: res://scenes/gameplay/collectable.tscn] :481
SceneObjects/Collectables group/Collectable30 [instance: res://scenes/gameplay/collectable.tscn] :485
SceneObjects/Collectables group/Collectable31 [instance: res://scenes/gameplay/collectable.tscn] :489
SceneObjects/Collectables group/Collectable32 [instance: res://scenes/gameplay/collectable.tscn] :493
SceneObjects/Collectables group/Collectable33 [instance: res://scenes/gameplay/collectable.tscn] :497
SceneObjects/Collectables group/Collectable34 [instance: res://scenes/gameplay/collectable.tscn] :501
SceneObjects/Collectables group/Collectable35 [instance: res://scenes/gameplay/collectable.tscn] :505
SceneObjects/Collectables group/Collectable36 [instance: res://scenes/gameplay/collectable.tscn] :509
SceneObjects/Finish [instance: res://scenes/gameplay/finish.tscn] :513
UI [CanvasLayer] :518
UI/Panel [Panel] :520
UI/Panel/PointsLabel [Label] :528
AudioStreamPlayer [AudioStreamPlayer] :546
CanvasLayer [CanvasLayer] :552
CanvasLayer/PauseMenu [instance: res://scenes/gameplay/pause_menu.tscn] :554
```

### scenes/gameplay/main_character.tscn

Root `CharacterBody2D`: `CharacterBody2D` at `scenes/gameplay/main_character.tscn:9`; 4 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `.` -> `res://scripts/actors/MainCharacter.cs` (C#); `scenes/gameplay/main_character.tscn:11` (resource declaration line 3).

Instanced subscenes/models:

- None serialized.

Serialized signal connections:

- None serialized; runtime connections are outside this audit.

Complete serialized hierarchy (path, type, declaration line):

```text
. [CharacterBody2D] :9
Sprite2D [AnimatedSprite2D] :13
CollisionShape2D [CollisionShape2D] :16
JumpSound [AudioStreamPlayer] :20
```

### scenes/gameplay/pause_menu.tscn

Root `PauseMenu`: `Control` at `scenes/gameplay/pause_menu.tscn:50`; 11 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `.` -> `res://scripts/gameplay/pause_menu.gd` (GDScript); `scenes/gameplay/pause_menu.tscn:59` (resource declaration line 5).

Instanced subscenes/models:

- None serialized.

Serialized signal connections:

- `ColorRect2/VBoxContainer/Resume.pressed` -> `.._on_resume_pressed`; `scenes/gameplay/pause_menu.tscn:134`.
- `ColorRect2/VBoxContainer/Restart.pressed` -> `.._on_restart_pressed`; `scenes/gameplay/pause_menu.tscn:135`.
- `ColorRect2/VBoxContainer/LevelSelection.pressed` -> `.._on_level_selection_pressed`; `scenes/gameplay/pause_menu.tscn:136`.
- `ColorRect2/VBoxContainer/MainMenu.pressed` -> `.._on_main_menu_pressed`; `scenes/gameplay/pause_menu.tscn:137`.

Complete serialized hierarchy (path, type, declaration line):

```text
. [Control] :50
WorldEnvironment [WorldEnvironment] :61
ColorRect [ColorRect] :64
ColorRect2 [PanelContainer] :74
ColorRect2/VBoxContainer [VBoxContainer] :89
ColorRect2/VBoxContainer/Title [Label] :94
ColorRect2/VBoxContainer/Resume [Button] :105
ColorRect2/VBoxContainer/Restart [Button] :111
ColorRect2/VBoxContainer/LevelSelection [Button] :117
ColorRect2/VBoxContainer/MainMenu [Button] :123
AnimationPlayer [AnimationPlayer] :129
```

### scenes/gameplay/training_protocol.tscn

Root `TrainingLevel`: `Node` at `scenes/gameplay/training_protocol.tscn:68`; 11 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `GameManager` -> `res://scripts/gameplay/GameManager.cs` (C#); `scenes/gameplay/training_protocol.tscn:72` (resource declaration line 3).

Instanced subscenes/models:

- `SceneObjects/MainCharacter` -> `res://scenes/gameplay/main_character.tscn`; `scenes/gameplay/training_protocol.tscn:89`.
- `UI/PauseMenu` -> `res://scenes/gameplay/pause_menu.tscn`; `scenes/gameplay/training_protocol.tscn:123`.

Serialized signal connections:

- None serialized; runtime connections are outside this audit.

Complete serialized hierarchy (path, type, declaration line):

```text
. [Node] :68
GameManager [Node] :70
SceneObjects [Node] :74
SceneObjects/TextureRect [TextureRect] :76
SceneObjects/TileMap [TileMapLayer] :83
SceneObjects/MainCharacter [instance: res://scenes/gameplay/main_character.tscn] :89
SceneObjects/MainCharacter/Camera2D [Camera2D] :92
UI [CanvasLayer] :98
UI/Panel [Panel] :100
UI/Panel/PointsLabel [Label] :107
UI/PauseMenu [instance: res://scenes/gameplay/pause_menu.tscn] :123
```

### scenes/main_menu/3d_menu_experience.tscn

Root `3DMenuExperience`: `Control` at `scenes/main_menu/3d_menu_experience.tscn:10`; 139 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `.` -> `res://scripts/main_menu/ThreeDMenuExperience.cs` (C#); `scenes/main_menu/3d_menu_experience.tscn:18` (resource declaration line 3).
- `ButtonContainer/Play` -> `res://scripts/ui/CyberButton.cs` (C#); `scenes/main_menu/3d_menu_experience.tscn:142` (resource declaration line 5).
- `ButtonContainer/ContinueButton` -> `res://scripts/ui/CyberButton.cs` (C#); `scenes/main_menu/3d_menu_experience.tscn:151` (resource declaration line 5).
- `ButtonContainer/Instructions` -> `res://scripts/ui/CyberButton.cs` (C#); `scenes/main_menu/3d_menu_experience.tscn:159` (resource declaration line 5).
- `ButtonContainer/Settings` -> `res://scripts/ui/CyberButton.cs` (C#); `scenes/main_menu/3d_menu_experience.tscn:167` (resource declaration line 5).
- `ButtonContainer/QuitButton` -> `res://scripts/ui/CyberButton.cs` (C#); `scenes/main_menu/3d_menu_experience.tscn:175` (resource declaration line 5).
- `SettingsPopup/VBoxContainer/AudioOptions` -> `res://scripts/ui/audio_options.gd` (GDScript); `scenes/main_menu/3d_menu_experience.tscn:375` (resource declaration line 7).
- `SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/LanguageDropdown` -> `res://scripts/ui/CyberDropdown.cs` (C#); `scenes/main_menu/3d_menu_experience.tscn:414` (resource declaration line 8).
- `SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/DisplayDropdown` -> `res://scripts/ui/CyberDropdown.cs` (C#); `scenes/main_menu/3d_menu_experience.tscn:428` (resource declaration line 8).

Instanced subscenes/models:

- None serialized.

Serialized signal connections:

- `QuitPopup/VBoxContainer/ButtonContainer/YesButton.pressed` -> `.._on_yes_pressed`; `scenes/main_menu/3d_menu_experience.tscn:874`.
- `QuitPopup/VBoxContainer/ButtonContainer/NoButton.pressed` -> `.._on_no_pressed`; `scenes/main_menu/3d_menu_experience.tscn:875`.
- `SettingsPopup/VBoxContainer/ButtonContainer/ApplyButton.pressed` -> `.._on_apply_settings_pressed`; `scenes/main_menu/3d_menu_experience.tscn:876`.
- `SettingsPopup/VBoxContainer/ButtonContainer/CloseButton.pressed` -> `.._on_close_settings_pressed`; `scenes/main_menu/3d_menu_experience.tscn:877`.
- `InstructionsPopup/VBoxContainer/CloseButton.pressed` -> `.._on_close_instructions_pressed`; `scenes/main_menu/3d_menu_experience.tscn:878`.
- `NewGamePopup/VBoxContainer/ButtonContainer/ConfirmButton.pressed` -> `.._on_new_game_confirm_pressed`; `scenes/main_menu/3d_menu_experience.tscn:879`.
- `NewGamePopup/VBoxContainer/ButtonContainer/CancelButton.pressed` -> `.._on_new_game_cancel_pressed`; `scenes/main_menu/3d_menu_experience.tscn:880`.
- `ContinuePopup/VBoxContainer/ButtonContainer/CancelButton.pressed` -> `.._on_continue_back_pressed`; `scenes/main_menu/3d_menu_experience.tscn:881`.
- `ConfirmPopup/VBoxContainer/ButtonContainer/YesButton.pressed` -> `.._on_confirm_yes_pressed`; `scenes/main_menu/3d_menu_experience.tscn:882`.
- `ConfirmPopup/VBoxContainer/ButtonContainer/NoButton.pressed` -> `.._on_confirm_no_pressed`; `scenes/main_menu/3d_menu_experience.tscn:883`.

Complete serialized hierarchy (path, type, declaration line):

```text
. [Control] :10
WorldEnvironment [WorldEnvironment] :20
SubViewportContainer [SubViewportContainer] :23
SubViewportContainer/SubViewport3D [SubViewport] :32
SubViewportContainer/SubViewport3D/World3D [Node3D] :39
SubViewportContainer/SubViewport3D/World3D/WorldEnvironment3D [WorldEnvironment] :41
SubViewportContainer/SubViewport3D/World3D/Camera3D [Camera3D] :44
SubViewportContainer/SubViewport3D/World3D/DirectionalLight3D [DirectionalLight3D] :49
SubViewportContainer/SubViewport3D/World3D/EntranceAccentLight [OmniLight3D] :54
HomeOverlay [Control] :60
HomeOverlay/DarkOverlay [ColorRect] :68
HomeOverlay/CenterContainer [CenterContainer] :77
HomeOverlay/CenterContainer/VBox [VBoxContainer] :85
HomeOverlay/CenterContainer/VBox/TitleLogo [TextureRect] :89
HomeOverlay/CenterContainer/VBox/Spacer [Control] :95
HomeOverlay/CenterContainer/VBox/PromptLabel [Label] :99
HomeOverlay/BottomMargin [MarginContainer] :105
HomeOverlay/BottomMargin/VersionLabel [Label] :115
TitleContainer [HBoxContainer] :121
ButtonContainer [VBoxContainer] :130
ButtonContainer/Play [Control] :139
ButtonContainer/ContinueButton [Control] :147
ButtonContainer/Instructions [Control] :156
ButtonContainer/Settings [Control] :164
ButtonContainer/QuitButton [Control] :172
ProfileContainer [Control] :180
StatusTicker [Label] :188
QuitBackground [ColorRect] :199
QuitPopup [Panel] :210
QuitPopup/VBoxContainer [VBoxContainer] :226
QuitPopup/VBoxContainer/TopSpacer [Control] :234
QuitPopup/VBoxContainer/Message [Label] :238
QuitPopup/VBoxContainer/Spacer [Control] :245
QuitPopup/VBoxContainer/ButtonContainer [HBoxContainer] :249
QuitPopup/VBoxContainer/ButtonContainer/YesButton [Button] :253
QuitPopup/VBoxContainer/ButtonContainer/ButtonSpacer [Control] :259
QuitPopup/VBoxContainer/ButtonContainer/NoButton [Button] :263
QuitPopup/VBoxContainer/BottomSpacer [Control] :269
ConfirmPopup [Panel] :273
ConfirmPopup/VBoxContainer [VBoxContainer] :289
ConfirmPopup/VBoxContainer/TopSpacer [Control] :297
ConfirmPopup/VBoxContainer/ConfirmLabel [Label] :301
ConfirmPopup/VBoxContainer/Spacer [Control] :308
ConfirmPopup/VBoxContainer/ButtonContainer [HBoxContainer] :312
ConfirmPopup/VBoxContainer/ButtonContainer/YesButton [Button] :316
ConfirmPopup/VBoxContainer/ButtonContainer/ButtonSpacer [Control] :322
ConfirmPopup/VBoxContainer/ButtonContainer/NoButton [Button] :326
SettingsPopup [Panel] :332
SettingsPopup/VBoxContainer [VBoxContainer] :349
SettingsPopup/VBoxContainer/TopSpacer [Control] :358
SettingsPopup/VBoxContainer/Title [Label] :362
SettingsPopup/VBoxContainer/Spacer1 [Control] :368
SettingsPopup/VBoxContainer/AudioOptions [Control] :372
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer [VBoxContainer] :377
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/VolumeLabel [Label] :391
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/MasterSlider [HSlider] :397
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/Spacer [Control] :402
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/LanguageLabel [Label] :406
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/LanguageDropdown [Control] :412
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/Spacer3 [Control] :416
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/DisplayLabel [Label] :420
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/DisplayDropdown [Control] :426
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/Spacer4 [Control] :430
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/ControlsButton [Button] :434
SettingsPopup/VBoxContainer/Spacer2 [Control] :441
SettingsPopup/VBoxContainer/ButtonContainer [HBoxContainer] :445
SettingsPopup/VBoxContainer/ButtonContainer/ApplyButton [Button] :449
SettingsPopup/VBoxContainer/ButtonContainer/Spacer [Control] :455
SettingsPopup/VBoxContainer/ButtonContainer/CloseButton [Button] :459
SettingsPopup/VBoxContainer/BottomSpacer [Control] :465
KeybindingsPopup [Panel] :469
KeybindingsPopup/VBoxContainer [VBoxContainer] :484
KeybindingsPopup/VBoxContainer/TopSpacer [Control] :492
KeybindingsPopup/VBoxContainer/Title [Label] :496
KeybindingsPopup/VBoxContainer/Spacer1 [Control] :502
KeybindingsPopup/VBoxContainer/ScrollContainer [ScrollContainer] :506
KeybindingsPopup/VBoxContainer/ScrollContainer/RowsContainer [VBoxContainer] :510
KeybindingsPopup/VBoxContainer/Spacer2 [Control] :514
KeybindingsPopup/VBoxContainer/ButtonContainer [HBoxContainer] :518
KeybindingsPopup/VBoxContainer/ButtonContainer/ResetDefaultsButton [Button] :522
KeybindingsPopup/VBoxContainer/ButtonContainer/Spacer [Control] :528
KeybindingsPopup/VBoxContainer/ButtonContainer/CloseKeybindingsButton [Button] :532
KeybindingsPopup/VBoxContainer/BottomSpacer [Control] :538
KeyDetectModal [Panel] :542
KeyDetectModal/VBoxContainer [VBoxContainer] :557
KeyDetectModal/VBoxContainer/PromptLabel [Label] :566
KeyDetectModal/VBoxContainer/DetectedKeyLabel [Label] :572
KeyDetectModal/VBoxContainer/Spacer [Control] :578
KeyDetectModal/VBoxContainer/ButtonContainer [HBoxContainer] :582
KeyDetectModal/VBoxContainer/ButtonContainer/AcceptButton [Button] :586
KeyDetectModal/VBoxContainer/ButtonContainer/Spacer [Control] :592
KeyDetectModal/VBoxContainer/ButtonContainer/CancelButton [Button] :596
InstructionsPopup [Panel] :602
InstructionsPopup/VBoxContainer [VBoxContainer] :618
InstructionsPopup/VBoxContainer/TopSpacer [Control] :626
InstructionsPopup/VBoxContainer/Title [Label] :630
InstructionsPopup/VBoxContainer/Spacer1 [Control] :636
InstructionsPopup/VBoxContainer/ScrollContainer [ScrollContainer] :640
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer [MarginContainer] :644
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer [VBoxContainer] :647
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/ObjectiveTitle [Label] :651
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/ObjectiveText [Label] :656
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/Spacer2 [Control] :661
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/ControlsTitle [Label] :665
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/ControlMove [Label] :670
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/ControlJump [Label] :675
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/ControlPause [Label] :680
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/Spacer3 [Control] :685
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/TipsTitle [Label] :689
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/Tip1 [Label] :694
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/Tip2 [Label] :700
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/Tip3 [Label] :706
InstructionsPopup/VBoxContainer/Spacer4 [Control] :712
InstructionsPopup/VBoxContainer/CloseButton [Button] :716
InstructionsPopup/VBoxContainer/BottomSpacer [Control] :723
NewGamePopup [Panel] :727
NewGamePopup/VBoxContainer [VBoxContainer] :742
NewGamePopup/VBoxContainer/TopSpacer [Control] :750
NewGamePopup/VBoxContainer/Message [Label] :754
NewGamePopup/VBoxContainer/Spacer [Control] :761
NewGamePopup/VBoxContainer/ButtonContainer [HBoxContainer] :765
NewGamePopup/VBoxContainer/ButtonContainer/ConfirmButton [Button] :769
NewGamePopup/VBoxContainer/ButtonContainer/ButtonSpacer [Control] :775
NewGamePopup/VBoxContainer/ButtonContainer/CancelButton [Button] :779
NewGamePopup/VBoxContainer/BottomSpacer [Control] :785
ContinuePopup [Panel] :790
ContinuePopup/VBoxContainer [VBoxContainer] :805
ContinuePopup/VBoxContainer/TopSpacer [Control] :813
ContinuePopup/VBoxContainer/Title [Label] :817
ContinuePopup/VBoxContainer/Spacer1 [Control] :823
ContinuePopup/VBoxContainer/ScrollContainer [ScrollContainer] :827
ContinuePopup/VBoxContainer/ScrollContainer/SessionsContainer [VBoxContainer] :831
ContinuePopup/VBoxContainer/Spacer2 [Control] :835
ContinuePopup/VBoxContainer/ButtonContainer [HBoxContainer] :839
ContinuePopup/VBoxContainer/ButtonContainer/LoadButton [Button] :843
ContinuePopup/VBoxContainer/ButtonContainer/Spacer [Control] :849
ContinuePopup/VBoxContainer/ButtonContainer/CancelButton [Button] :853
ContinuePopup/VBoxContainer/BottomSpacer [Control] :859
FadeOverlay [ColorRect] :863
```

### scenes/main_menu/3d_story_intro.tscn

Root `ThreeDStoryIntro`: `Node3D` at `scenes/main_menu/3d_story_intro.tscn:20`; 15 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `.` -> `res://scripts/main_menu/ThreeDStoryIntro.cs` (C#); `scenes/main_menu/3d_story_intro.tscn:21` (resource declaration line 3).

Instanced subscenes/models:

- None serialized.

Serialized signal connections:

- None serialized; runtime connections are outside this audit.

Complete serialized hierarchy (path, type, declaration line):

```text
. [Node3D] :20
WorldEnvironment [WorldEnvironment] :23
Camera3D [Camera3D] :26
DirectionalLight3D_Key [DirectionalLight3D] :31
DirectionalLight3D_Fill [DirectionalLight3D] :36
CityBackgroundQuad [MeshInstance3D] :41
CanvasLayer [CanvasLayer] :45
CanvasLayer/UIOverlay [Control] :48
CanvasLayer/UIOverlay/DarkOverlay [ColorRect] :57
CanvasLayer/UIOverlay/TitleBanner [Label] :66
CanvasLayer/UIOverlay/DialogueBox [PanelContainer] :84
CanvasLayer/UIOverlay/DialogueBox/MarginContainer [MarginContainer] :98
CanvasLayer/UIOverlay/DialogueBox/MarginContainer/DialogueLabel [RichTextLabel] :105
CanvasLayer/UIOverlay/ContinueButton [Button] :115
CanvasLayer/UIOverlay/SkipButton [Button] :132
```

### scenes/main_menu/character_selection.tscn

Root `CharacterSelection`: `Control` at `scenes/main_menu/character_selection.tscn:10`; 35 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `.` -> `res://scripts/main_menu/CharacterSelection.cs` (C#); `scenes/main_menu/character_selection.tscn:18` (resource declaration line 5).
- `MainLayout/RightShowcasePanel/VBox/StatsBox/SpeedMeter` -> `res://scripts/ui/SegmentedStatBar.cs` (C#); `scenes/main_menu/character_selection.tscn:194` (resource declaration line 8).
- `MainLayout/RightShowcasePanel/VBox/StatsBox/AgilityMeter` -> `res://scripts/ui/SegmentedStatBar.cs` (C#); `scenes/main_menu/character_selection.tscn:201` (resource declaration line 8).

Instanced subscenes/models:

- None serialized.

Serialized signal connections:

- `MainLayout/LeftListPanel/VBox/CharacterGrid/CyberShogun.pressed` -> `.._on_cyber_shogun_pressed`; `scenes/main_menu/character_selection.tscn:236`.
- `MainLayout/LeftListPanel/VBox/CharacterGrid/Solaria.pressed` -> `.._on_solaria_pressed`; `scenes/main_menu/character_selection.tscn:237`.
- `MainLayout/LeftListPanel/VBox/CharacterGrid/Oceanus.pressed` -> `.._on_oceanus_pressed`; `scenes/main_menu/character_selection.tscn:238`.
- `MainLayout/LeftListPanel/VBox/CharacterGrid/SuryaPrime.pressed` -> `.._on_surya_prime_pressed`; `scenes/main_menu/character_selection.tscn:239`.
- `MainLayout/LeftListPanel/VBox/CharacterGrid/MirageBlade.pressed` -> `.._on_mirage_blade_pressed`; `scenes/main_menu/character_selection.tscn:240`.
- `MainLayout/LeftListPanel/VBox/CharacterGrid/ApexTitan.pressed` -> `.._on_apex_titan_pressed`; `scenes/main_menu/character_selection.tscn:241`.
- `MainLayout/RightShowcasePanel/VBox/DeployButton.pressed` -> `.._on_deploy_pressed`; `scenes/main_menu/character_selection.tscn:242`.
- `MainLayout/RightShowcasePanel/VBox/RotateRow/RotateLeftBtn.button_down` -> `.._on_rotate_left_down`; `scenes/main_menu/character_selection.tscn:243`.
- `MainLayout/RightShowcasePanel/VBox/RotateRow/RotateLeftBtn.button_up` -> `.._on_rotate_left_up`; `scenes/main_menu/character_selection.tscn:244`.
- `MainLayout/RightShowcasePanel/VBox/RotateRow/RotateRightBtn.button_down` -> `.._on_rotate_right_down`; `scenes/main_menu/character_selection.tscn:245`.
- `MainLayout/RightShowcasePanel/VBox/RotateRow/RotateRightBtn.button_up` -> `.._on_rotate_right_up`; `scenes/main_menu/character_selection.tscn:246`.

Complete serialized hierarchy (path, type, declaration line):

```text
. [Control] :10
WorldEnvironment [WorldEnvironment] :20
BackgroundTexture [TextureRect] :23
DarkOverlay [ColorRect] :34
SubViewportContainer [SubViewportContainer] :43
SubViewportContainer/SubViewport3D [SubViewport] :52
SubViewportContainer/SubViewport3D/World3D [Node3D] :59
SubViewportContainer/SubViewport3D/World3D/Camera3D [Camera3D] :61
SubViewportContainer/SubViewport3D/World3D/DirectionalLight3D [DirectionalLight3D] :65
SubViewportContainer/SubViewport3D/World3D/WorldEnvironment3D [WorldEnvironment] :69
Header [Label] :72
MainLayout [HBoxContainer] :90
MainLayout/LeftListPanel [PanelContainer] :101
MainLayout/LeftListPanel/VBox [VBoxContainer] :105
MainLayout/LeftListPanel/VBox/ListTitle [Label] :109
MainLayout/LeftListPanel/VBox/CharacterGrid [VBoxContainer] :116
MainLayout/LeftListPanel/VBox/CharacterGrid/CyberShogun [Button] :120
MainLayout/LeftListPanel/VBox/CharacterGrid/Solaria [Button] :125
MainLayout/LeftListPanel/VBox/CharacterGrid/Oceanus [Button] :130
MainLayout/LeftListPanel/VBox/CharacterGrid/SuryaPrime [Button] :135
MainLayout/LeftListPanel/VBox/CharacterGrid/MirageBlade [Button] :140
MainLayout/LeftListPanel/VBox/CharacterGrid/ApexTitan [Button] :145
MainLayout/RightShowcasePanel [PanelContainer] :150
MainLayout/RightShowcasePanel/VBox [VBoxContainer] :155
MainLayout/RightShowcasePanel/VBox/SelectedName [Label] :160
MainLayout/RightShowcasePanel/VBox/CivilianName [Label] :170
MainLayout/RightShowcasePanel/VBox/ClassTag [Label] :180
MainLayout/RightShowcasePanel/VBox/StatsBox [VBoxContainer] :187
MainLayout/RightShowcasePanel/VBox/StatsBox/SpeedMeter [Control] :191
MainLayout/RightShowcasePanel/VBox/StatsBox/AgilityMeter [Control] :198
MainLayout/RightShowcasePanel/VBox/DeployButton [Button] :205
MainLayout/RightShowcasePanel/VBox/RotateRow [HBoxContainer] :211
MainLayout/RightShowcasePanel/VBox/RotateRow/RotateLeftBtn [Button] :217
MainLayout/RightShowcasePanel/VBox/RotateRow/RotateHint [Label] :223
MainLayout/RightShowcasePanel/VBox/RotateRow/RotateRightBtn [Button] :230
```

### scenes/main_menu/falling_character.tscn

Root `FallingCharacter`: `AnimatedSprite2D` at `scenes/main_menu/falling_character.tscn:5`; 1 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `.` -> `res://scripts/actors/FallingCharacter.cs` (C#); `scenes/main_menu/falling_character.tscn:7` (resource declaration line 3).

Instanced subscenes/models:

- None serialized.

Serialized signal connections:

- None serialized; runtime connections are outside this audit.

Complete serialized hierarchy (path, type, declaration line):

```text
. [AnimatedSprite2D] :5
```

### scenes/main_menu/game_home.tscn

Root `GameHome`: `Control` at `scenes/main_menu/game_home.tscn:5`; 11 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `.` -> `res://scripts/main_menu/GameHome.cs` (C#); `scenes/main_menu/game_home.tscn:12` (resource declaration line 3).

Instanced subscenes/models:

- None serialized.

Serialized signal connections:

- None serialized; runtime connections are outside this audit.

Complete serialized hierarchy (path, type, declaration line):

```text
. [Control] :5
Background [TextureRect] :14
DarkOverlay [ColorRect] :24
CenterContainer [CenterContainer] :33
CenterContainer/VBoxContainer [VBoxContainer] :41
CenterContainer/VBoxContainer/TitleLogo [TextureRect] :45
CenterContainer/VBoxContainer/Spacer [Control] :51
CenterContainer/VBoxContainer/PromptLabel [Label] :55
BottomMarginContainer [MarginContainer] :61
BottomMarginContainer/VersionLabel [Label] :71
FadeOverlay [ColorRect] :77
```

### scenes/main_menu/leaderboard.tscn

Root `Leaderboard`: `Control` at `scenes/main_menu/leaderboard.tscn:11`; 11 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `.` -> `res://scripts/main_menu/leaderboard.gd` (GDScript); `scenes/main_menu/leaderboard.tscn:19` (resource declaration line 5).

Instanced subscenes/models:

- None serialized.

Serialized signal connections:

- `VBoxContainer/BackButton.pressed` -> `.._on_back_button_pressed`; `scenes/main_menu/leaderboard.tscn:82`.

Complete serialized hierarchy (path, type, declaration line):

```text
. [Control] :11
WorldEnvironment [WorldEnvironment] :21
TextureRect [TextureRect] :24
VBoxContainer [VBoxContainer] :35
VBoxContainer/Title [Label] :49
VBoxContainer/Spacer1 [Control] :57
VBoxContainer/Spacer2 [Control] :61
VBoxContainer/ScrollContainer [ScrollContainer] :65
VBoxContainer/ScrollContainer/ScoresTable [VBoxContainer] :69
VBoxContainer/Spacer3 [Control] :73
VBoxContainer/BackButton [Button] :77
```

### scenes/main_menu/level_selection.tscn

Root `Node`: `Control` at `scenes/main_menu/level_selection.tscn:9`; 29 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `.` -> `res://scripts/main_menu/level_selection.gd` (GDScript); `scenes/main_menu/level_selection.tscn:17` (resource declaration line 5).

Instanced subscenes/models:

- None serialized.

Serialized signal connections:

- `MainLayout/SectorGrid/Sector1/VBox/Level1.pressed` -> `.._on_level_1_pressed`; `scenes/main_menu/level_selection.tscn:185`.
- `MainLayout/SectorGrid/Sector2/VBox/Level2.pressed` -> `.._on_level_2_pressed`; `scenes/main_menu/level_selection.tscn:186`.
- `MainLayout/SectorGrid/Sector3/VBox/Level3.pressed` -> `.._on_level_3_pressed`; `scenes/main_menu/level_selection.tscn:187`.
- `MainLayout/SectorGrid/Sector4/VBox/Level4.pressed` -> `.._on_level_4_pressed`; `scenes/main_menu/level_selection.tscn:188`.
- `MainLayout/CenterContainer/BackButton.pressed` -> `.._on_back_button_pressed`; `scenes/main_menu/level_selection.tscn:189`.

Complete serialized hierarchy (path, type, declaration line):

```text
. [Control] :9
WorldEnvironment [WorldEnvironment] :19
BackgroundTexture [TextureRect] :22
DarkOverlay [ColorRect] :33
Header [Label] :42
MainLayout [VBoxContainer] :60
MainLayout/SectorGrid [GridContainer] :70
MainLayout/SectorGrid/Sector1 [PanelContainer] :77
MainLayout/SectorGrid/Sector1/VBox [VBoxContainer] :81
MainLayout/SectorGrid/Sector1/VBox/Title [Label] :85
MainLayout/SectorGrid/Sector1/VBox/Desc [Label] :92
MainLayout/SectorGrid/Sector1/VBox/Level1 [Button] :97
MainLayout/SectorGrid/Sector2 [PanelContainer] :102
MainLayout/SectorGrid/Sector2/VBox [VBoxContainer] :106
MainLayout/SectorGrid/Sector2/VBox/Title [Label] :110
MainLayout/SectorGrid/Sector2/VBox/Desc [Label] :117
MainLayout/SectorGrid/Sector2/VBox/Level2 [Button] :122
MainLayout/SectorGrid/Sector3 [PanelContainer] :127
MainLayout/SectorGrid/Sector3/VBox [VBoxContainer] :131
MainLayout/SectorGrid/Sector3/VBox/Title [Label] :135
MainLayout/SectorGrid/Sector3/VBox/Desc [Label] :142
MainLayout/SectorGrid/Sector3/VBox/Level3 [Button] :147
MainLayout/SectorGrid/Sector4 [PanelContainer] :152
MainLayout/SectorGrid/Sector4/VBox [VBoxContainer] :156
MainLayout/SectorGrid/Sector4/VBox/Title [Label] :160
MainLayout/SectorGrid/Sector4/VBox/Desc [Label] :167
MainLayout/SectorGrid/Sector4/VBox/Level4 [Button] :172
MainLayout/CenterContainer [CenterContainer] :177
MainLayout/CenterContainer/BackButton [Button] :180
```

### scenes/main_menu/loading_screen.tscn

Root `LoadingScreen`: `Control` at `scenes/main_menu/loading_screen.tscn:11`; 13 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `.` -> `res://scripts/ui/LoadingScreen.cs` (C#); `scenes/main_menu/loading_screen.tscn:19` (resource declaration line 5).
- `CenterContainer/MainPanel/VBoxContainer/SpinnerContainer` -> `res://scripts/ui/CyberCircularSpinner.cs` (C#); `scenes/main_menu/loading_screen.tscn:80` (resource declaration line 9).

Instanced subscenes/models:

- None serialized.

Serialized signal connections:

- None serialized; runtime connections are outside this audit.

Complete serialized hierarchy (path, type, declaration line):

```text
. [Control] :11
WorldEnvironment [WorldEnvironment] :21
BackgroundTexture [TextureRect] :24
DarkOverlay [ColorRect] :35
FadeOverlay [ColorRect] :44
CenterContainer [CenterContainer] :53
CenterContainer/MainPanel [PanelContainer] :61
CenterContainer/MainPanel/VBoxContainer [VBoxContainer] :65
CenterContainer/MainPanel/VBoxContainer/TitleLogoBanner [TextureRect] :69
CenterContainer/MainPanel/VBoxContainer/SpinnerContainer [Control] :76
CenterContainer/MainPanel/VBoxContainer/LoadingLabel [Label] :82
CenterContainer/MainPanel/VBoxContainer/ProgressBar [ProgressBar] :92
CenterContainer/MainPanel/VBoxContainer/StatusLabel [Label] :99
```

### scenes/main_menu/login_scene.tscn

Root `LoginScreen`: `Control` at `scenes/main_menu/login_scene.tscn:9`; 32 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `.` -> `res://scripts/main_menu/LoginScene.cs` (C#); `scenes/main_menu/login_scene.tscn:17` (resource declaration line 5).

Instanced subscenes/models:

- None serialized.

Serialized signal connections:

- None serialized; runtime connections are outside this audit.

Complete serialized hierarchy (path, type, declaration line):

```text
. [Control] :9
WorldEnvironment [WorldEnvironment] :19
BackgroundTexture [TextureRect] :22
DarkOverlay [ColorRect] :33
SplitGrid [Control] :42
SplitGrid/LeftInfoColumn [VBoxContainer] :50
SplitGrid/LeftInfoColumn/BrandingTitle [Label] :60
SplitGrid/LeftInfoColumn/SubBranding [Label] :69
SplitGrid/LeftInfoColumn/Description [Label] :75
SplitGrid/LeftInfoColumn/StatusBadge [Label] :82
SplitGrid/RightAuthColumn [Control] :88
SplitGrid/RightAuthColumn/CenterContainer [CenterContainer] :96
SplitGrid/RightAuthColumn/CenterContainer/AuthPanel [PanelContainer] :104
SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox [VBoxContainer] :108
SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/Title [Label] :112
SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/InputGroup [VBoxContainer] :122
SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/InputGroup/EmailInput [LineEdit] :126
SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/InputGroup/UsernameInput [LineEdit] :131
SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/InputGroup/PasswordInput [LineEdit] :137
SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/SubmitButton [Button] :143
SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/ErrorLabel [Label] :148
SplitGrid/RightAuthColumn/CenterContainer/AuthPanel/VBox/ToggleModeButton [Button] :156
WebsiteButton [Button] :161
LanguageSelector [OptionButton] :173
NetworkErrorPopup [Control] :187
NetworkErrorPopup/Overlay [ColorRect] :197
NetworkErrorPopup/CenterContainer [CenterContainer] :206
NetworkErrorPopup/CenterContainer/DialogPanel [PanelContainer] :214
NetworkErrorPopup/CenterContainer/DialogPanel/VBox [VBoxContainer] :218
NetworkErrorPopup/CenterContainer/DialogPanel/VBox/PopupTitle [Label] :222
NetworkErrorPopup/CenterContainer/DialogPanel/VBox/PopupMessage [Label] :230
NetworkErrorPopup/CenterContainer/DialogPanel/VBox/PopupOkButton [Button] :236
```

### scenes/main_menu/main_menu.tscn

Root `MainMenu`: `Control` at `scenes/main_menu/main_menu.tscn:15`; 126 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `.` -> `res://scripts/main_menu/MainMenu.cs` (C#); `scenes/main_menu/main_menu.tscn:23` (resource declaration line 5).
- `ButtonContainer/Play` -> `res://scripts/ui/CyberButton.cs` (C#); `scenes/main_menu/main_menu.tscn:61` (resource declaration line 12).
- `ButtonContainer/ContinueButton` -> `res://scripts/ui/CyberButton.cs` (C#); `scenes/main_menu/main_menu.tscn:70` (resource declaration line 12).
- `ButtonContainer/Instructions` -> `res://scripts/ui/CyberButton.cs` (C#); `scenes/main_menu/main_menu.tscn:78` (resource declaration line 12).
- `ButtonContainer/Settings` -> `res://scripts/ui/CyberButton.cs` (C#); `scenes/main_menu/main_menu.tscn:86` (resource declaration line 12).
- `ButtonContainer/QuitButton` -> `res://scripts/ui/CyberButton.cs` (C#); `scenes/main_menu/main_menu.tscn:94` (resource declaration line 12).
- `SettingsPopup/VBoxContainer/AudioOptions` -> `res://scripts/ui/audio_options.gd` (GDScript); `scenes/main_menu/main_menu.tscn:241` (resource declaration line 7).
- `SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/LanguageDropdown` -> `res://scripts/ui/CyberDropdown.cs` (C#); `scenes/main_menu/main_menu.tscn:280` (resource declaration line 13).
- `SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/DisplayDropdown` -> `res://scripts/ui/CyberDropdown.cs` (C#); `scenes/main_menu/main_menu.tscn:294` (resource declaration line 13).

Instanced subscenes/models:

- None serialized.

Serialized signal connections:

- `QuitPopup/VBoxContainer/ButtonContainer/YesButton.pressed` -> `.._on_yes_pressed`; `scenes/main_menu/main_menu.tscn:811`.
- `QuitPopup/VBoxContainer/ButtonContainer/NoButton.pressed` -> `.._on_no_pressed`; `scenes/main_menu/main_menu.tscn:812`.
- `SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/LanguageButton.pressed` -> `.._on_language_button_pressed`; `scenes/main_menu/main_menu.tscn:813`.
- `SettingsPopup/VBoxContainer/ButtonContainer/ApplyButton.pressed` -> `.._on_apply_settings_pressed`; `scenes/main_menu/main_menu.tscn:814`.
- `SettingsPopup/VBoxContainer/ButtonContainer/CloseButton.pressed` -> `.._on_close_settings_pressed`; `scenes/main_menu/main_menu.tscn:815`.
- `InstructionsPopup/VBoxContainer/CloseButton.pressed` -> `.._on_close_instructions_pressed`; `scenes/main_menu/main_menu.tscn:816`.
- `LanguagePopup/VBoxContainer/CloseButton.pressed` -> `.._on_close_language_popup_pressed`; `scenes/main_menu/main_menu.tscn:817`.
- `NewGamePopup/VBoxContainer/ButtonContainer/ConfirmButton.pressed` -> `.._on_new_game_confirm_pressed`; `scenes/main_menu/main_menu.tscn:818`.
- `NewGamePopup/VBoxContainer/ButtonContainer/CancelButton.pressed` -> `.._on_new_game_cancel_pressed`; `scenes/main_menu/main_menu.tscn:819`.
- `ContinuePopup/VBoxContainer/BackButton.pressed` -> `.._on_continue_back_pressed`; `scenes/main_menu/main_menu.tscn:820`.
- `ConfirmPopup/VBoxContainer/ButtonContainer/YesButton.pressed` -> `.._on_confirm_yes_pressed`; `scenes/main_menu/main_menu.tscn:821`.
- `ConfirmPopup/VBoxContainer/ButtonContainer/NoButton.pressed` -> `.._on_confirm_no_pressed`; `scenes/main_menu/main_menu.tscn:822`.

Complete serialized hierarchy (path, type, declaration line):

```text
. [Control] :15
WorldEnvironment [WorldEnvironment] :25
TextureRect2 [TextureRect] :29
TitleContainer [HBoxContainer] :40
ButtonContainer [VBoxContainer] :49
ButtonContainer/Play [Control] :58
ButtonContainer/ContinueButton [Control] :66
ButtonContainer/Instructions [Control] :75
ButtonContainer/Settings [Control] :83
ButtonContainer/QuitButton [Control] :91
ProfileContainer [Control] :99
StatusTicker [Label] :107
Control [Control] :118
QuitBackground [ColorRect] :124
QuitPopup [Panel] :135
QuitPopup/VBoxContainer [VBoxContainer] :151
QuitPopup/VBoxContainer/TopSpacer [Control] :159
QuitPopup/VBoxContainer/Message [Label] :163
QuitPopup/VBoxContainer/Spacer [Control] :170
QuitPopup/VBoxContainer/ButtonContainer [HBoxContainer] :174
QuitPopup/VBoxContainer/ButtonContainer/YesButton [Button] :178
QuitPopup/VBoxContainer/ButtonContainer/ButtonSpacer [Control] :184
QuitPopup/VBoxContainer/ButtonContainer/NoButton [Button] :188
QuitPopup/VBoxContainer/BottomSpacer [Control] :194
SettingsPopup [Panel] :198
SettingsPopup/VBoxContainer [VBoxContainer] :215
SettingsPopup/VBoxContainer/TopSpacer [Control] :224
SettingsPopup/VBoxContainer/Title [Label] :228
SettingsPopup/VBoxContainer/Spacer1 [Control] :234
SettingsPopup/VBoxContainer/AudioOptions [Control] :238
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer [VBoxContainer] :243
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/VolumeLabel [Label] :257
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/MasterSlider [HSlider] :263
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/Spacer [Control] :268
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/LanguageLabel [Label] :272
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/LanguageDropdown [Control] :278
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/Spacer3 [Control] :282
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/DisplayLabel [Label] :286
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/DisplayDropdown [Control] :292
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/Spacer4 [Control] :296
SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/ControlsButton [Button] :300
SettingsPopup/VBoxContainer/Spacer2 [Control] :307
SettingsPopup/VBoxContainer/ButtonContainer [HBoxContainer] :311
SettingsPopup/VBoxContainer/ButtonContainer/ApplyButton [Button] :315
SettingsPopup/VBoxContainer/ButtonContainer/Spacer [Control] :321
SettingsPopup/VBoxContainer/ButtonContainer/CloseButton [Button] :325
SettingsPopup/VBoxContainer/BottomSpacer [Control] :331
KeybindingsPopup [Panel] :335
KeybindingsPopup/VBoxContainer [VBoxContainer] :350
KeybindingsPopup/VBoxContainer/TopSpacer [Control] :358
KeybindingsPopup/VBoxContainer/Title [Label] :362
KeybindingsPopup/VBoxContainer/Spacer1 [Control] :368
KeybindingsPopup/VBoxContainer/ScrollContainer [ScrollContainer] :372
KeybindingsPopup/VBoxContainer/ScrollContainer/RowsContainer [VBoxContainer] :376
KeybindingsPopup/VBoxContainer/Spacer2 [Control] :380
KeybindingsPopup/VBoxContainer/ButtonContainer [HBoxContainer] :384
KeybindingsPopup/VBoxContainer/ButtonContainer/ResetDefaultsButton [Button] :388
KeybindingsPopup/VBoxContainer/ButtonContainer/Spacer [Control] :394
KeybindingsPopup/VBoxContainer/ButtonContainer/CloseKeybindingsButton [Button] :398
KeybindingsPopup/VBoxContainer/BottomSpacer [Control] :404
KeyDetectModal [Panel] :408
KeyDetectModal/VBoxContainer [VBoxContainer] :423
KeyDetectModal/VBoxContainer/PromptLabel [Label] :432
KeyDetectModal/VBoxContainer/DetectedKeyLabel [Label] :438
KeyDetectModal/VBoxContainer/Spacer [Control] :444
KeyDetectModal/VBoxContainer/ButtonContainer [HBoxContainer] :448
KeyDetectModal/VBoxContainer/ButtonContainer/AcceptButton [Button] :452
KeyDetectModal/VBoxContainer/ButtonContainer/Spacer [Control] :458
KeyDetectModal/VBoxContainer/ButtonContainer/CancelButton [Button] :462
InstructionsPopup [Panel] :468
InstructionsPopup/VBoxContainer [VBoxContainer] :484
InstructionsPopup/VBoxContainer/TopSpacer [Control] :492
InstructionsPopup/VBoxContainer/Title [Label] :496
InstructionsPopup/VBoxContainer/Spacer1 [Control] :502
InstructionsPopup/VBoxContainer/ScrollContainer [ScrollContainer] :506
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer [MarginContainer] :510
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer [VBoxContainer] :513
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/ObjectiveTitle [Label] :517
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/ObjectiveText [Label] :522
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/Spacer2 [Control] :527
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/ControlsTitle [Label] :531
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/ControlMove [Label] :536
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/ControlJump [Label] :541
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/ControlPause [Label] :546
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/Spacer3 [Control] :551
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/TipsTitle [Label] :555
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/Tip1 [Label] :560
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/Tip2 [Label] :566
InstructionsPopup/VBoxContainer/ScrollContainer/MarginContainer/ContentContainer/Tip3 [Label] :572
InstructionsPopup/VBoxContainer/Spacer4 [Control] :578
InstructionsPopup/VBoxContainer/CloseButton [Button] :582
InstructionsPopup/VBoxContainer/BottomSpacer [Control] :589
NewGamePopup [Panel] :595
NewGamePopup/VBoxContainer [VBoxContainer] :610
NewGamePopup/VBoxContainer/TopSpacer [Control] :618
NewGamePopup/VBoxContainer/Message [Label] :622
NewGamePopup/VBoxContainer/Spacer [Control] :629
NewGamePopup/VBoxContainer/ButtonContainer [HBoxContainer] :633
NewGamePopup/VBoxContainer/ButtonContainer/ConfirmButton [Button] :637
NewGamePopup/VBoxContainer/ButtonContainer/ButtonSpacer [Control] :643
NewGamePopup/VBoxContainer/ButtonContainer/CancelButton [Button] :647
NewGamePopup/VBoxContainer/BottomSpacer [Control] :653
ContinuePopup [Panel] :657
ContinuePopup/VBoxContainer [VBoxContainer] :672
ContinuePopup/VBoxContainer/TopSpacer [Control] :680
ContinuePopup/VBoxContainer/Title [Label] :684
ContinuePopup/VBoxContainer/Spacer1 [Control] :691
ContinuePopup/VBoxContainer/ScrollContainer [ScrollContainer] :695
ContinuePopup/VBoxContainer/ScrollContainer/SessionsContainer [VBoxContainer] :699
ContinuePopup/VBoxContainer/Spacer2 [Control] :703
ContinuePopup/VBoxContainer/BackButton [Button] :707
ContinuePopup/VBoxContainer/BottomSpacer [Control] :714
ConfirmPopup [Panel] :718
ConfirmPopup/VBoxContainer [VBoxContainer] :733
ConfirmPopup/VBoxContainer/TopSpacer [Control] :741
ConfirmPopup/VBoxContainer/ConfirmLabel [Label] :745
ConfirmPopup/VBoxContainer/Spacer [Control] :752
ConfirmPopup/VBoxContainer/ButtonContainer [HBoxContainer] :756
ConfirmPopup/VBoxContainer/ButtonContainer/YesButton [Button] :760
ConfirmPopup/VBoxContainer/ButtonContainer/ButtonSpacer [Control] :766
ConfirmPopup/VBoxContainer/ButtonContainer/NoButton [Button] :770
ConfirmPopup/VBoxContainer/BottomSpacer [Control] :776
VBoxContainer [VBoxContainer] :780
VBoxContainer/Label [Label] :795
VBoxContainer/Yes [Button] :801
VBoxContainer/No [Button] :806
```

### scenes/main_menu/story_intro.tscn

Root `StoryIntro`: `Control` at `scenes/main_menu/story_intro.tscn:62`; 12 serialized node declarations.

Direct scripts (node -> script; attachment line):

- `.` -> `res://scripts/main_menu/StoryIntro.cs` (C#); `scenes/main_menu/story_intro.tscn:70` (resource declaration line 5).

Instanced subscenes/models:

- None serialized.

Serialized signal connections:

- None serialized; runtime connections are outside this audit.

Complete serialized hierarchy (path, type, declaration line):

```text
. [Control] :62
WorldEnvironment [WorldEnvironment] :72
BackgroundTexture [TextureRect] :75
DarkOverlay [ColorRect] :86
TitleBanner [Label] :95
HBoxContainer [HBoxContainer] :113
HBoxContainer/Spacer [Control] :128
HBoxContainer/DialogueBox [PanelContainer] :132
HBoxContainer/DialogueBox/MarginContainer [MarginContainer] :138
HBoxContainer/DialogueBox/MarginContainer/DialogueLabel [RichTextLabel] :145
ContinueButton [Button] :155
SkipButton [Button] :172
```

## GLB material and animation inventory

No engine import inspection performed. Facts below come from each binary GLB JSON chunk; this proves available source channels, not final rendered appearance.

### assets/models/hq/cyber_shogun.glb

29,135,652 bytes; 1 meshes; 1 skins; animations: Armature|Idle_02|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=0.4100847542285919; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].
- Explicit source reference: `scripts/main_menu/CharacterSelection.cs:238`.

### assets/models/hq/Meshy_AI__0807164603_texture.glb

19,295,304 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `material`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/actors/FloatingRobotBuddy3D.cs:36`.

### assets/models/hq/Meshy_AI_Aegis_Defense_Bastion_0807061844_image-to-3d-texture.glb

48,350,432 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:319`.

### assets/models/hq/Meshy_AI_arab_superhero_crysta_0804155043_image-to-3d-texture.glb

46,883,072 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].

### assets/models/hq/Meshy_AI_arab_superhero_rig_biped_Animation_Double_Blade_Spin_withSkin.glb

29,884,948 bytes; 1 meshes; 1 skins; animations: Armature|Double_Blade_Spin|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=0.4100847542285919; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_Arab_Superhero_Rig_biped_Animation_Idle_02_withSkin.glb

31,245,420 bytes; 1 meshes; 1 skins; animations: Armature|Idle_02|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=0.4100847542285919; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].
- Explicit source reference: `scripts/main_menu/CharacterSelection.cs:290`.

### assets/models/hq/Meshy_AI_arab_superhero_rig_biped_Animation_Running_withSkin.glb

29,801,840 bytes; 1 meshes; 1 skins; animations: Armature|running|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_arab_superhero_rig_biped_Animation_Walking_withSkin.glb

29,806,456 bytes; 1 meshes; 1 skins; animations: Armature|walking_man|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_Armored_Perimeter_Bar_0807074359_image-to-3d-texture.glb

40,915,560 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:409`.

### assets/models/hq/Meshy_AI_Automated_Defense_Tur_0807075515_image-to-3d-texture.glb

75,797,056 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:427`.

### assets/models/hq/Meshy_AI_Axiom_Force_HQ_Buildi_0806151721_image-to-3d-texture.glb

59,793,088 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:279`.

### assets/models/hq/Meshy_AI_Axiom_Resistance_VTOL_0807065619_image-to-3d-texture.glb

43,643,276 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:359`.

### assets/models/hq/Meshy_AI_Bio_Gen_Corporate_Spi_0807054611_image-to-3d-texture.glb

53,697,272 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:289`.

### assets/models/hq/Meshy_AI_Cargo_Transport_Hover_0807071108_image-to-3d-texture.glb

95,877,948 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:381`.

### assets/models/hq/Meshy_AI_Cyber_Billboard_Frame_0807082322_image-to-3d-texture.glb

63,246,568 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:466`.

### assets/models/hq/Meshy_AI_cyber_shogun_rigged_biped_Animation_Running_withSkin.glb

30,608,188 bytes; 1 meshes; 1 skins; animations: Armature|running|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_cyber_shogun_rigged_biped_Animation_Thrust_Slash_withSkin.glb

30,636,952 bytes; 1 meshes; 1 skins; animations: Armature|Thrust_Slash|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=0.4100847542285919; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_cyber_shogun_rigged_biped_Animation_Walking_withSkin.glb

30,612,812 bytes; 1 meshes; 1 skins; animations: Armature|walking_man|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_cyber_superhero_0804161932_image-to-3d-texture.glb

41,057,852 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].

### assets/models/hq/Meshy_AI_cyber_superhero_rig_biped_Animation_Heavy_Hammer_Swing_withSkin.glb

30,791,640 bytes; 1 meshes; 1 skins; animations: Armature|Heavy_Hammer_Swing|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=0.4100847542285919; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_Cyber_Superhero_Rig_biped_Animation_Idle_02_withSkin.glb

29,088,716 bytes; 1 meshes; 1 skins; animations: Armature|Idle_02|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=0.4100847542285919; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].
- Explicit source reference: `scripts/main_menu/CharacterSelection.cs:303`.

### assets/models/hq/Meshy_AI_cyber_superhero_rig_biped_Animation_Running_withSkin.glb

30,778,624 bytes; 1 meshes; 1 skins; animations: Armature|running|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_cyber_superhero_rig_biped_Animation_Walking_withSkin.glb

30,783,588 bytes; 1 meshes; 1 skins; animations: Armature|walking_man|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_Cyberpunk_Police_Spin_0807070257_image-to-3d-texture.glb

40,317,736 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:370`.

### assets/models/hq/Meshy_AI_Elevated_Sky_Highway__0807062447_image-to-3d-texture.glb

34,625,196 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:329`.

### assets/models/hq/Meshy_AI_Energy_Shield_Pylon_0807073859_image-to-3d-texture.glb

38,630,832 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:392`.

### assets/models/hq/Meshy_AI_Holo_Projector_Pedest_0807080445_image-to-3d-texture.glb

60,815,576 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:446`.

### assets/models/hq/Meshy_AI_HQ_Gunship_Landing_Pa_0807064955_image-to-3d-texture.glb

94,615,508 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:349`.

### assets/models/hq/Meshy_AI_Metallic_Floor_Grid_0807073249_image-to-3d-texture.glb

50,208,560 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:269`.

### assets/models/hq/Meshy_AI_Monorail_Support_Gant_0807063542_image-to-3d-texture.glb

55,745,736 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `material`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:339`.

### assets/models/hq/Meshy_AI_Neuro_Link_Comm_Tower_0807055157_image-to-3d-texture.glb

41,425,584 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:299`.

### assets/models/hq/Meshy_AI_polynesian_superheroi_0804064602_image-to-3d-texture.glb

54,200,692 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].

### assets/models/hq/Meshy_AI_Polynesian_Superheroi_biped_Animation_Idle_02_withSkin.glb

31,902,036 bytes; 1 meshes; 1 skins; animations: Armature|Idle_02|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=0.4100847542285919; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].
- Explicit source reference: `scripts/main_menu/CharacterSelection.cs:264`.

### assets/models/hq/Meshy_AI_polynesian_superheroi_biped_Animation_mage_soell_cast_4_withSkin.glb

32,298,572 bytes; 1 meshes; 1 skins; animations: Armature|mage_soell_cast_4|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=0.4100847542285919; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_polynesian_superheroi_biped_Animation_Running_withSkin.glb

32,280,060 bytes; 1 meshes; 1 skins; animations: Armature|running|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_polynesian_superheroi_biped_Animation_Walking_withSkin.glb

32,284,664 bytes; 1 meshes; 1 skins; animations: Armature|walking_man|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_Resistance_Shield_Emb_0807081351_image-to-3d-texture.glb

47,515,640 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:456`.

### assets/models/hq/Meshy_AI_scifi_selection_platf_0805042413_image-to-3d-texture.glb

36,415,488 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/CharacterSelection.cs:325`.

### assets/models/hq/Meshy_AI_solar_guardian_0804150949_image-to-3d-texture.glb

34,654,588 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].

### assets/models/hq/Meshy_AI_Solar_Guardian_Rig_biped_Animation_Idle_02_withSkin.glb

26,625,812 bytes; 1 meshes; 1 skins; animations: Armature|Idle_02|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=0.4100847542285919; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].
- Explicit source reference: `scripts/main_menu/CharacterSelection.cs:277`.

### assets/models/hq/Meshy_AI_solar_guardian_rigged_biped_Animation_Charged_Ground_Slam_withSkin.glb

28,727,840 bytes; 1 meshes; 1 skins; animations: Armature|Charged_Ground_Slam|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=0.4100847542285919; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_solar_guardian_rigged_biped_Animation_Running_withSkin.glb

28,699,068 bytes; 1 meshes; 1 skins; animations: Armature|running|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_solar_guardian_rigged_biped_Animation_Walking_withSkin.glb

28,703,676 bytes; 1 meshes; 1 skins; animations: Armature|walking_man|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_solaris_heroine_remes_0804052921_texture.glb

40,011,020 bytes; 1 meshes; 0 skins; animations: none; 3 embedded/referenced images.

- Material `Material_0.001`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_Solaris_Heroine_Rig_biped_Animation_Idle_02_withSkin.glb

26,001,920 bytes; 1 meshes; 1 skins; animations: Armature|Idle_02|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=0.4100847542285919; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].
- Explicit source reference: `scripts/main_menu/CharacterSelection.cs:251`.

### assets/models/hq/Meshy_AI_solaris_heroine_rig_biped_Animation_Jump_Rope_withSkin.glb

26,617,088 bytes; 1 meshes; 1 skins; animations: Armature|Jump_Rope|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=0.4100847542285919; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_solaris_heroine_rig_biped_Animation_Running_withSkin.glb

26,523,584 bytes; 1 meshes; 1 skins; animations: Armature|running|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_solaris_heroine_rig_biped_Animation_Walking_withSkin.glb

26,528,200 bytes; 1 meshes; 1 skins; animations: Armature|walking_man|baselayer; 1 embedded/referenced images.

- Material `Material_1`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=False; normal=False; emissive texture=True; emissive factor=[1, 1, 1].

### assets/models/hq/Meshy_AI_Syndicate_Hab_Block_0807060633_image-to-3d-texture.glb

71,559,152 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:309`.

### assets/models/hq/Meshy_AI_Terminal_Kiosk_Node_0807083122_image-to-3d-texture.glb

38,781,096 bytes; 1 meshes; 0 skins; animations: none; 4 embedded/referenced images.

- Material `None`: PBR declared=True; metallic=1.0; roughness=1.0; base texture=True; metallic/roughness texture=True; normal=True; emissive texture=True; emissive factor=[1.0, 1.0, 1.0].
- Explicit source reference: `scripts/main_menu/ThreeDMenuExperience.cs:477`.

## Reuse and deletion constraints

- JSON includes every asset path, byte size, and statically discoverable explicit scene/script/theme/project reference. Absence of an explicit reference does not prove unused: dynamically constructed paths and imported material dependencies remain possible.
- Keep source GLBs and their referenced texture sidecars together. The migrated model folder contains both geometry and extracted texture assets; import metadata is separate.
- Preserve assets referenced by existing scenes until the corresponding scene/script dependency is intentionally removed.
- Generated translation resources derive from CSV but may also be referenced in project settings; do not treat them as source-language content or delete them casually.
- This audit performs no deletions.

## Architecture summary and findings

- All **21 scenes** accounted for: 10 gameplay and 11 main_menu; **634 serialized nodes**, **40 script attachments** referencing **22 distinct scripts**, and **47 serialized signal connections**.
- No missing scene external-resource file paths and no unattached declared scripts found.
- `scenes/main_menu/3d_menu_experience.tscn:10` is a Control-root UI with a SubViewportContainer -> SubViewport3D -> World3D branch (`:23`, `:32`, `:39`), camera/lights and WorldEnvironment, plus menus/modal panels. No GLB is serialized directly into the scene: explicit Citadel dependency appears in `scripts/main_menu/ThreeDMenuExperience.cs:279`.
- `scenes/main_menu/character_selection.tscn:10` is a Control-root selection UI with a 3D SubViewport and left list/right showcase panels; two SegmentedStatBar script attachments are at `:194` and `:201`.
- `scenes/main_menu/3d_story_intro.tscn:20` is a Node3D root with camera, two directional lights, CityBackgroundQuad and CanvasLayer narrative UI. The separate `story_intro.tscn:62` remains a Control-root 2D narrative scene.
- `scenes/gameplay/level1.tscn:343`, level2 `:365`, level3 `:378`, level4 `:394`, and training `:70` each declare a local `GameManager` Node using `res://scripts/gameplay/GameManager.cs`; this is separate from the persistent GDScript singleton.
- Levels 1-4 instance `main_character.tscn`, `finish.tscn`, `pause_menu.tscn`, and respectively 21/27/40/20 collectible scenes. Training instances main_character and pause_menu only. The separate cyber_shogun scene is not serialized as an instance in these levels.

### Serialized connection issues

- `scenes/main_menu/main_menu.tscn:813` connects from `SettingsPopup/VBoxContainer/AudioOptions/VBoxContainer/LanguageButton`, which has no matching serialized node. Target method: `_on_language_button_pressed`. This is a concrete stale scene declaration; runtime script behavior was not audited.
- `scenes/main_menu/main_menu.tscn:817` connects from `LanguagePopup/VBoxContainer/CloseButton`, which has no matching serialized node. Target method: `_on_close_language_popup_pressed`. This is a concrete stale scene declaration; runtime script behavior was not audited.

## Reusable visual and localization assets

- **Model source library:** 50 GLBs and 127 texture sidecars in `assets/models/hq/`; 24 GLBs contain animations. All 50 contain a PBR metallic/roughness material declaration and an emissive texture slot; 26 have metallic/roughness maps and 25 have normal maps. These are source capabilities, not proof of artistic quality or correct final shading.
- **Citadel:** `Meshy_AI_Axiom_Force_HQ_Buildi_0806151721_image-to-3d-texture.glb` (59,793,088 bytes) has base-color, metallic/roughness, normal and emissive textures; it is the HQ model referenced at `scripts/main_menu/ThreeDMenuExperience.cs:279`.
- **Cyber Shogun:** `cyber_shogun.glb` (29,135,652 bytes) contains one skinned mesh and Idle_02 animation, base-color and emissive slots, metallic factor 1.0, roughness 0.4100847542, and KHR_materials_specular/ior. Its source GLB lacks separate metallic/roughness and normal texture slots. Additional baked/base-color/normal/metallic-roughness/emissive source textures exist under `assets/images/Cyber Shogun/`; additional running/walking/thrust-slash GLBs exist under models/hq. `scripts/main_menu/CharacterSelection.cs:238` references the main model.
- Both Citadel and Cyber Shogun `.glb.import:38` reference `res://scripts/importers/NeonMaterialImport.gd`; `.import` is import configuration, not raw geometry. No import was run by this audit.
- **Neon foundation:** `assets/themes/cyber_glow.tres:8` configures tonemap 3 (ACES), `:9` enables glow, `:10` additive mode, `:11` threshold 0.8, `:13` intensity 0.6. Ten scenes reference this shared Environment.
- **UI theme:** `assets/themes/cyber_shogun.tres:1` is a Theme, not a 3D Material. It uses Orbitron and Rajdhani fonts (`:3-5`), dark cyan/magenta/green style boxes, default Rajdhani Medium (`:138`). Ten scenes reference it. `assets/fonts/default_theme.tres` and `multilingual_theme.tres` are additional Theme resources with no explicit reference found in the scanned scene/script/project paths.
- **Shader:** `assets/shaders/cyber_scanlines.gdshader:1` is a canvas_item scanline/vignette shader with cyan uniform (`:6`). No explicit static scene/script reference found; existence alone does not prove it is active.
- **Fonts:** 15 TTFs: Orbitron variable; Rajdhani Regular/Medium/Bold; NotoSans Regular and Arabic; Cairo; Amiri Bold; Tajawal Bold; and six ComicNeue faces. Keep Arabic/multilingual fallback files until runtime language dependencies are checked.
- **Backgrounds:** cyber_city_dusk_background is declared in six menu/story scenes; training_dojo_background is declared in training and referenced by GameHome. `assets/textures/` currently contains only `.gitkeep`; the usable backgrounds remain under assets/images.
- **Logos and portraits:** assets/images contains localized Axiom Force logos plus AXIOM-7 portrait/emblem images. Both language-name and short-code variants coexist; dynamic language resolution must be checked before deduplication.
- **Audio:** 153 AXIOM-7 voice MP3s = 17 locale folders x 9 clips. Two additional audio tracks (Street_Level_Siege, Sub_Level_Three) and five music/effect MP3s give 160 MP3 total.
- **Localization:** CSV source has 17 locale columns and 220 data records. The 17 generated .translation resources are counted separately. There are 13 narrative-language JSON files plus README, translator JS and progress log; missing narrative-language names compared with UI/voice are traditionalchinese, indonesian, vietnamese and thai. The voice folders french/portuguese differ from CSV eurofrench/brazilianportuguese, requiring the runtime mapping to remain intact.

## Legacy sprite dependencies and reuse limits

- Remaining legacy platformer art is actively referenced: `Cherries.png` by `collectable.tscn:3`, `End (Idle).png` by `finish.tscn:3`, `Green.png` by level1-4 `:4`, `Terrain (16x16).png` and `world_tileset.png` by all four levels and training, and `Gray.png` by `leaderboard.tscn:6`. These cannot be deleted while those scenes are retained. The filenames and character names match the legacy Pixel Adventure family; no separate source-pack/licensing metadata was found in this scoped audit.
- Legacy character sheets are still expected through constructed paths in `scripts/actors/MainCharacter.cs:76` and `:97-106`, and `scripts/actors/FallingCharacter.cs:71-72`. Expected folders `assets/images/Ninja Frog`, `Mask Dude`, `Pink Man`, and `Virtual Guy` are currently absent. `scripts/main_menu/MainMenu.cs:378` still lists Mask Dude/Pink Man/Virtual Guy. This is a missing dynamic-asset dependency, not a safe deletion candidate.
- The shared generated Cyber Shogun sprite sheet is declared by `scenes/gameplay/cyber_shogun.tscn:5` and explicitly referenced at `scripts/actors/MainCharacter.cs:86` and `scripts/actors/FallingCharacter.cs:52`. Keep it while the 2D actor scenes remain.
- 434 is the broad source/support inventory; 431 are game media/data/resource files after excluding narrative README.md, translate_all.js and translate_progress.log. No files live directly at assets/ root. Four .gitkeep placeholders are counted separately.

## Compact scene-to-script map

Exact attachment nodes, lines, instances and all connection methods are listed in each scene section above; this table is for scanning.

| Scene | Root | Main children | Direct script files | Instances |
|---|---|---|---|---|
| scenes/gameplay/collectable.tscn | Collectable (Area2D) | AnimatedSprite2D, CollisionShape2D, CoinSound | collectable.gd | none serialized |
| scenes/gameplay/cyber_shogun.tscn | CyberShogun (CharacterBody2D) | Sprite2D, CollisionShape2D, JumpSound | CyberShogun.cs | none serialized |
| scenes/gameplay/finish.tscn | Finish (Area2D) | Sprite2D, CollisionShape2D, FinishAudio | finish.gd | none serialized |
| scenes/gameplay/level1.tscn | Node (Node) | GameManager, SceneObjects, UI, AudioStreamPlayer, CanvasLayer | GameManager.cs | main_character.tscn x1, collectable.tscn x21, finish.tscn x1, pause_menu.tscn x1 |
| scenes/gameplay/level2.tscn | Node (Node) | GameManager, SceneObjects, UI, AudioStreamPlayer, CanvasLayer | GameManager.cs | main_character.tscn x1, collectable.tscn x27, finish.tscn x1, pause_menu.tscn x1 |
| scenes/gameplay/level3.tscn | Node (Node) | GameManager, SceneObjects, UI, AudioStreamPlayer, CanvasLayer | GameManager.cs | main_character.tscn x1, collectable.tscn x40, finish.tscn x1, pause_menu.tscn x1 |
| scenes/gameplay/level4.tscn | Node (Node) | GameManager, SceneObjects, UI, AudioStreamPlayer, CanvasLayer | GameManager.cs | main_character.tscn x1, collectable.tscn x20, finish.tscn x1, pause_menu.tscn x1 |
| scenes/gameplay/main_character.tscn | CharacterBody2D (CharacterBody2D) | Sprite2D, CollisionShape2D, JumpSound | MainCharacter.cs | none serialized |
| scenes/gameplay/pause_menu.tscn | PauseMenu (Control) | WorldEnvironment, ColorRect, ColorRect2, AnimationPlayer | pause_menu.gd | none serialized |
| scenes/gameplay/training_protocol.tscn | TrainingLevel (Node) | GameManager, SceneObjects, UI | GameManager.cs | main_character.tscn x1, pause_menu.tscn x1 |
| scenes/main_menu/3d_menu_experience.tscn | 3DMenuExperience (Control) | WorldEnvironment, SubViewportContainer, HomeOverlay, TitleContainer, ButtonContainer, ProfileContainer, StatusTicker, QuitBackground, QuitPopup, ConfirmPopup, SettingsPopup, KeybindingsPopup, KeyDetectModal, InstructionsPopup, NewGamePopup, ContinuePopup, FadeOverlay | CyberButton.cs, CyberDropdown.cs, ThreeDMenuExperience.cs, audio_options.gd | none serialized |
| scenes/main_menu/3d_story_intro.tscn | ThreeDStoryIntro (Node3D) | WorldEnvironment, Camera3D, DirectionalLight3D_Key, DirectionalLight3D_Fill, CityBackgroundQuad, CanvasLayer | ThreeDStoryIntro.cs | none serialized |
| scenes/main_menu/character_selection.tscn | CharacterSelection (Control) | WorldEnvironment, BackgroundTexture, DarkOverlay, SubViewportContainer, Header, MainLayout | CharacterSelection.cs, SegmentedStatBar.cs | none serialized |
| scenes/main_menu/falling_character.tscn | FallingCharacter (AnimatedSprite2D) | none | FallingCharacter.cs | none serialized |
| scenes/main_menu/game_home.tscn | GameHome (Control) | Background, DarkOverlay, CenterContainer, BottomMarginContainer, FadeOverlay | GameHome.cs | none serialized |
| scenes/main_menu/leaderboard.tscn | Leaderboard (Control) | WorldEnvironment, TextureRect, VBoxContainer | leaderboard.gd | none serialized |
| scenes/main_menu/level_selection.tscn | Node (Control) | WorldEnvironment, BackgroundTexture, DarkOverlay, Header, MainLayout | level_selection.gd | none serialized |
| scenes/main_menu/loading_screen.tscn | LoadingScreen (Control) | WorldEnvironment, BackgroundTexture, DarkOverlay, FadeOverlay, CenterContainer | CyberCircularSpinner.cs, LoadingScreen.cs | none serialized |
| scenes/main_menu/login_scene.tscn | LoginScreen (Control) | WorldEnvironment, BackgroundTexture, DarkOverlay, SplitGrid, WebsiteButton, LanguageSelector, NetworkErrorPopup | LoginScene.cs | none serialized |
| scenes/main_menu/main_menu.tscn | MainMenu (Control) | WorldEnvironment, TextureRect2, TitleContainer, ButtonContainer, ProfileContainer, StatusTicker, Control, QuitBackground, QuitPopup, SettingsPopup, KeybindingsPopup, KeyDetectModal, InstructionsPopup, NewGamePopup, ContinuePopup, ConfirmPopup, VBoxContainer | CyberButton.cs, CyberDropdown.cs, MainMenu.cs, audio_options.gd | none serialized |
| scenes/main_menu/story_intro.tscn | StoryIntro (Control) | WorldEnvironment, BackgroundTexture, DarkOverlay, TitleBanner, HBoxContainer, ContinueButton, SkipButton | StoryIntro.cs | none serialized |

## Final GLB aggregate

```json
{
  "glb_files": 50,
  "glb_bytes": 2038447188,
  "meshes": 50,
  "skinned_glbs": 24,
  "skins": 24,
  "animated_glbs": 24,
  "animation_clips": 24,
  "materials": 50,
  "pbr_declarations": 50,
  "base_color_texture_slots": 50,
  "metallic_roughness_texture_slots": 26,
  "normal_texture_slots": 25,
  "emissive_texture_slots": 50,
  "nonzero_emissive_factors": 50,
  "embedded_images": 127,
  "external_image_uris": 0
}
```

All GLB images are embedded. Texture sidecars require import/runtime dependency review before deletion.
