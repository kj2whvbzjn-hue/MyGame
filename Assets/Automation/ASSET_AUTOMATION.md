# Asset Automation

`asset-map.json` maps short logical names used by AI to concrete Unity asset paths.

Supported commands:

- `spawnAsset`: instantiate a Prefab/GameObject asset, or create a GameObject from a Sprite.
- `setSpriteAsset`: assign a Sprite asset to an existing target GameObject.
- `setAudioAsset`: assign an AudioClip to an existing target GameObject's AudioSource.

You can either use a logical `asset` key from `asset-map.json` or specify `assetPath` directly.

Example:

```json
{
  "type": "spawnAsset",
  "asset": "goblin",
  "name": "Goblin_01",
  "position": [4, 0, 0]
}
```

Example asset map entry:

```json
{
  "key": "goblin",
  "path": "Assets/Art/Prefabs/Goblin.prefab",
  "kind": "prefab"
}
```

The example entries included in `asset-map.json` are placeholders. Replace them with real assets after importing assets into the Unity repository.
