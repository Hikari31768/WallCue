#!/usr/bin/env python3
"""Build against your own patched Beat Saber 1.40.8 installation; no NuGet downloads."""
import argparse, pathlib, re, subprocess
ROOT = pathlib.Path(__file__).resolve().parent
p = argparse.ArgumentParser()
p.add_argument('--game', type=pathlib.Path, required=True, help='Beat Saber 1.40.8 game directory')
p.add_argument('--dotnet', default='dotnet', help='.NET SDK 8+ dotnet executable')
p.add_argument('--sdk-root', type=pathlib.Path, help='Optional exact SDK directory containing Roslyn/bincore/csc.dll')
a = p.parse_args()
if a.sdk_root:
    sdk = a.sdk_root.resolve()
else:
    text = subprocess.check_output([a.dotnet, '--list-sdks'], text=True)
    found = re.findall(r'^([^ ]+) \[(.+)\]$', text, flags=re.M)
    if not found: raise SystemExit('Install .NET SDK 8 or newer first.')
    version, base = found[-1]; sdk = pathlib.Path(base) / version
managed = a.game.resolve() / 'Beat Saber_Data' / 'Managed'
plugins = a.game.resolve() / 'Plugins'
refs = ['mscorlib','System','System.Core','netstandard','IPA.Loader','Main','DataModels','GameplayCore',
        'BeatmapCore','Core','HMLib','HMRendering','UnityEngine.CoreModule','UnityEngine','Zenject','Zenject-usage','HMUI',
        'Unity.TextMeshPro','UnityEngine.UI','UnityEngine.UIModule','UnityEngine.ImageConversionModule']
paths = [managed/(n+'.dll') for n in refs]+[plugins/(n+'.dll') for n in ['SiraUtil','BSML','Counters+']]
missing = [str(x) for x in paths if not x.exists()]
if missing: raise SystemExit('Missing game/dependency references:\n'+'\n'.join(missing))
out = ROOT/'out'; out.mkdir(exist_ok=True)
args = [a.dotnet,str(sdk/'Roslyn'/'bincore'/'csc.dll'),'-noconfig','-nostdlib+','-target:library',
        '-langversion:9','-optimize+','-deterministic+','-out:'+str(out/'WallCue.dll'),
        '-resource:'+str(ROOT/'manifest.json')+',WallCue.manifest.json']
args += ['-resource:'+str(p)+',WallCue.Resources.'+p.name for p in sorted((ROOT/'Resources').iterdir()) if p.is_file()]
args += ['-r:'+str(x) for x in paths]
args += [str(x) for x in sorted((ROOT/'src').glob('*.cs'))]
subprocess.run(args, check=True)
print('Built:',out/'WallCue.dll')
