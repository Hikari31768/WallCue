#!/usr/bin/env python3
"""Run production geometry and visual state logic without a game or VR runtime."""
import argparse,pathlib,re,subprocess,json
root=pathlib.Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--dotnet',default='dotnet');p.add_argument('--sdk-root',type=pathlib.Path);p.add_argument('--game',type=pathlib.Path,help='Optional game directory for actual DLL integration checks');a=p.parse_args()
if a.sdk_root:sdk=a.sdk_root.resolve()
else:
    version,base=re.findall(r'^([^ ]+) \[(.+)\]$',subprocess.check_output([a.dotnet,'--list-sdks'],text=True),re.M)[-1]
    sdk=pathlib.Path(base)/version
home=sdk.parent.parent
packs=home/'packs'/'Microsoft.NETCore.App.Ref'
def key(path):return tuple(int(n) for n in re.findall(r'\d+',path.name)[:3])
pack=sorted(packs.iterdir(),key=key)[-1];ref=next((pack/'ref').iterdir())
out=root/'test-output';out.mkdir(exist_ok=True)
for name,files in [('Geometry',['src/WarningSettings.cs','src/PluginConfig.cs','src/WarningLogic.cs','tests/WarningLogicTests.cs']),('Visual',['src/WarningSettings.cs','src/WarningLogic.cs','src/WallVisual.cs','tests/VisualStubs.cs','tests/WallVisualTests.cs']),('Feedback',['src/CollisionFeedback.cs','tests/CollisionFeedbackTests.cs']),('Controller',['src/WarningSettings.cs','src/PluginConfig.cs','src/WarningLogic.cs','src/CollisionFeedback.cs','src/CueController.cs','tests/ControllerStubs.cs','tests/ControllerTests.cs'])]:
    # Controller's metadata double must match the optional assembly name lookup.
    assembly=out/(('SongCore' if name=='Controller' else name)+'.dll')
    args=[a.dotnet,str(sdk/'Roslyn'/'bincore'/'csc.dll'),'-noconfig','-nostdlib+','-target:exe','-out:'+str(assembly)]
    args+=['-r:'+str(r) for r in ref.glob('*.dll')]+[str(root/f) for f in files]
    subprocess.run(args,check=True)
    assembly.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':ref.name,'framework':{'name':'Microsoft.NETCore.App','version':pack.name}}}))
    subprocess.run([a.dotnet,str(assembly)],check=True)

if a.game:
    managed=a.game.resolve()/'Beat Saber_Data'/'Managed'
    plugins=a.game.resolve()/'Plugins'
    # Keep real plugin integration separate from the Controller suite's SongCore
    # double, which otherwise shadows the actual SongCore DLL in host probing.
    integration_out=out/'Integration';integration_out.mkdir(exist_ok=True)
    assembly=integration_out/'Integration.dll'
    args=[a.dotnet,str(sdk/'Roslyn'/'bincore'/'csc.dll'),'-noconfig','-nostdlib+','-target:exe','-out:'+str(assembly)]
    args+=['-r:'+str(r) for r in ref.glob('*.dll')]
    args+=['-r:'+str(r) for r in [plugins/'Counters+.dll',managed/'Newtonsoft.Json.dll',managed/'UnityEngine.CoreModule.dll',managed/'Zenject.dll',a.game.resolve()/'Libs'/'Hive.Versioning.dll']]
    args+=[str(root/'tests/IntegrationTests.cs')]
    if subprocess.run(args).returncode:raise SystemExit('Integration test compilation failed.')
    assembly.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':ref.name,'framework':{'name':'Microsoft.NETCore.App','version':pack.name}}}))
    if subprocess.run([a.dotnet,str(assembly),str(managed),str(plugins),str(root/'out')]).returncode:raise SystemExit('Integration checks failed.')
