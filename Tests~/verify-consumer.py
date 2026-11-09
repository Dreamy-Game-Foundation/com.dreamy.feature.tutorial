#!/usr/bin/env python3
"""Compile and test Tutorial in an isolated Unity project using a host's resolved dependencies."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--unity', type=Path, required=True)
parser.add_argument('--host', type=Path, required=True, help='Unity host with resolved package cache')
parser.add_argument('--output', type=Path, required=True, help='New verification directory')
parser.add_argument('--mode', choices=['EditMode', 'PlayMode', 'both'], default='both')
args = parser.parse_args()
host = args.host.resolve()
output = args.output.resolve()
package = Path(__file__).resolve().parents[1]
consumer = output / 'consumer'
# Never overwrite a preexisting project or someone else's verification output.
if output.exists():
    parser.error('--output must be a new directory')
(consumer / 'Assets').mkdir(parents=True)
(consumer / 'Packages').mkdir()
(consumer / 'ProjectSettings').mkdir()
host_manifest = json.loads((host / 'Packages/manifest.json').read_text())
packages = {k: v for k, v in host_manifest['dependencies'].items() if k.startswith('com.unity.modules.')}
for name in ['com.dreamy.core', 'com.dreamy.dataconfig', 'com.dreamy.datasave', 'com.dreamy.ui', 'com.dreamy.assets']:
    local = host / 'LocalPackages' / name
    if not (local / 'package.json').exists():
        parser.error(f'Missing local dependency {local}')
    packages[name] = 'file:' + str(local)
packages['com.dreamy.feature.tutorial'] = 'file:' + str(package)
lock = json.loads((host / 'Packages/packages-lock.json').read_text())['dependencies']
for name in ['com.cysharp.unitask', 'com.demigiant.dotween']:
    candidates = list((host / 'Library/PackageCache').glob(name + '@*'))
    if len(candidates) != 1:
        parser.error(f'Expected one resolved cache for {name}, found {len(candidates)}')
    packages[name] = 'file:' + str(candidates[0])
for name in ['com.unity.test-framework', 'com.unity.ugui', 'com.unity.nuget.newtonsoft-json', 'com.unity.addressables']:
    packages[name] = lock[name]['version']
manifest = {'dependencies': packages, 'testables': ['com.dreamy.feature.tutorial']}
(consumer / 'Packages/manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
shutil.copy2(host / 'ProjectSettings/ProjectVersion.txt', consumer / 'ProjectSettings/ProjectVersion.txt')
shutil.copytree(package / 'Samples~/Tutorial Feature', consumer / 'Assets/Tutorial Feature')

# Fingerprint production sources, tests, manifest and asmdefs; release receipts/docs are excluded.
inputs = sorted(p for p in package.rglob('*') if p.is_file() and '.git' not in p.parts and
                (p.suffix in {'.cs', '.asmdef'} or p == package / 'package.json'))
fingerprint = hashlib.sha256()
for path in inputs:
    fingerprint.update(str(path.relative_to(package)).encode() + b'\0' + path.read_bytes() + b'\0')
receipt = {'sourceFingerprint': fingerprint.hexdigest(), 'consumer': str(consumer), 'runs': []}
for mode in (['EditMode', 'PlayMode'] if args.mode == 'both' else [args.mode]):
    result = output / (mode.lower() + '.xml')
    log = output / (mode.lower() + '.log')
    assemblies = 'Dreamy.Tutorial.Editor.Tests' if mode == 'EditMode' else 'Dreamy.Tutorial.PlayMode.Tests;Dreamy.Tutorial.Sample.Tests'
    command = [str(args.unity.resolve()), '-batchmode', '-nographics', '-projectPath', str(consumer),
               '-runTests', '-testPlatform', mode, '-assemblyNames', assemblies, '-testResults', str(result), '-logFile', str(log)]
    status = subprocess.run(command).returncode
    summary = ET.parse(result).getroot().attrib if result.exists() else {}
    receipt['runs'].append({'mode': mode, 'command': command, 'exitCode': status, 'summary': summary})
    (output / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    if status != 0 or summary.get('result') != 'Passed' or int(summary.get('total', 0)) == 0:
        raise SystemExit(f'{mode} failed; inspect {log}')
    print(f"PASS {mode}: {summary['passed']}/{summary['total']}", flush=True)
