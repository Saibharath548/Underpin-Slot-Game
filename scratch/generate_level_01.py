import json

with open('scratch/reconstructed_lines.json', 'r') as f:
    lines = json.load(f)

yaml_lines = []
yaml_lines.append("%YAML 1.1")
yaml_lines.append("%TAG !u! tag:unity3d.com,2011:")
yaml_lines.append("--- !u!114 &11400000")
yaml_lines.append("MonoBehaviour:")
yaml_lines.append("  m_ObjectHideFlags: 0")
yaml_lines.append("  m_CorrespondingSourceObject: {fileID: 0}")
yaml_lines.append("  m_PrefabInstance: {fileID: 0}")
yaml_lines.append("  m_PrefabAsset: {fileID: 0}")
yaml_lines.append("  m_GameObject: {fileID: 0}")
yaml_lines.append("  m_Enabled: 1")
yaml_lines.append("  m_EditorHideFlags: 0")
yaml_lines.append("  m_Script: {fileID: 11500000, guid: f6da35ae39186e0428574f5889764a9b, type: 3}")
yaml_lines.append("  m_Name: Level_01")
yaml_lines.append("  m_EditorClassIdentifier: ")
yaml_lines.append("  levelName: Level 01 (Assignment)")
yaml_lines.append("  startingLives: 3")
yaml_lines.append("  lines:")

for line in lines:
    yaml_lines.append(f"  - lineId: {line['lineId']}")
    c = line['color']
    yaml_lines.append(f"    lineColor: {{r: {c['r']}, g: {c['g']}, b: {c['b']}, a: {c['a']}}}")
    yaml_lines.append(f"    lineWidth: {line['lineWidth']}")
    yaml_lines.append("    controlPoints:")
    for pt in line['controlPoints']:
        yaml_lines.append(f"    - {{x: {pt['x']}, y: {pt['y']}}}")
    yaml_lines.append(f"    arrowheadScale: {line['arrowheadScale']}")
    yaml_lines.append(f"    movementSpeed: {line['movementSpeed']}")
    yaml_lines.append(f"    collisionRadius: {line['collisionRadius']}")

yaml_content = "\n".join(yaml_lines) + "\n"

with open("Assets/Data/Levels/Level_01.asset", "w") as f:
    f.write(yaml_content)

meta_content = """fileFormatVersion: 2
guid: a1b2c3d4e5f60718293a4b5c6d7e8f90
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
with open("Assets/Data/Levels/Level_01.asset.meta", "w") as f:
    f.write(meta_content)

print(f"Successfully generated Assets/Data/Levels/Level_01.asset with {len(lines)} lines.")

