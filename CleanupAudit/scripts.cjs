const fs=require('fs'),path=require('path');
function walk(d){return fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(d+'/'+e.name):[d+'/'+e.name]);}
const files=walk('Assets'),assets=files.filter(p=>!p.endsWith('.meta')),scripts=assets.filter(p=>p.endsWith('.cs')),guid=new Map(),deps=new Map(),source=new Map(),types=new Map();
for(const p of files.filter(p=>p.endsWith('.meta'))){const m=fs.readFileSync(p,'utf8').match(/^guid: ([a-f0-9]{32})/m);if(m)guid.set(m[1],p.slice(0,-5));}
for(const p of assets.filter(p=>/\.(unity|prefab|asset|mat|controller|overridecontroller|anim|playable|terrainlayer|meta)$/i.test(p))){const b=fs.readFileSync(p);if(b.includes(0))continue;deps.set(p,[...b.toString().matchAll(/guid:\s*([a-f0-9]{32})/g)].map(m=>guid.get(m[1])).filter(Boolean));}
for(const p of scripts){const s=fs.readFileSync(p,'utf8');source.set(p,s);for(const m of s.matchAll(/\b(?:class|enum|interface|struct)\s+(\w+)/g)){if(!types.has(m[1]))types.set(m[1],[]);types.get(m[1]).push(p);}}
const callers=new Map(scripts.map(p=>[p,[]]));
for(const [p,s] of source){const edges=[];for(const [t,ps] of types)if(new RegExp('\\b'+t+'\\b').test(s))for(const q of ps)if(p!==q){edges.push(q);callers.get(q).push(p);}deps.set(p,edges);}
function closure(roots){const seen=new Set(),q=[...roots];while(q.length){const p=q.pop();if(seen.has(p))continue;seen.add(p);q.push(...deps.get(p)||[]);}return seen;}
const settings=walk('ProjectSettings').filter(p=>p.endsWith('.asset')&&!p.endsWith('/EditorBuildSettings.asset')).flatMap(p=>[...fs.readFileSync(p,'utf8').matchAll(/guid:\s*([a-f0-9]{32})/g)].map(m=>guid.get(m[1])).filter(Boolean));
const mainRoots=['Assets/Scenes/Prototyping/MainGame.unity',...settings,...assets.filter(p=>p.includes('/Resources/')), ...scripts.filter(p=>!p.includes('/Editor/')&&source.get(p).includes('[RuntimeInitializeOnLoadMethod'))];
const main=closure(mainRoots),all=closure([...mainRoots,...assets.filter(p=>p.endsWith('.unity'))]);
const results=scripts.map(p=>({path:p,bytes:fs.statSync(p).size,status:p.includes('/Editor/')?'EDITOR TOOL':main.has(p)?'KEEP: main game or runtime code dependency':all.has(p)?'OTHER SCENES ONLY':'CANDIDATE: no game/scene/code reachability found',serializedReferences:[...deps].filter(([q,ds])=>!q.endsWith('.cs')&&ds.includes(p)).map(([q])=>q),codeCallers:callers.get(p),autoInitialize:/RuntimeInitializeOnLoadMethod/.test(source.get(p))}));
fs.writeFileSync('CleanupAudit/scripts.json',JSON.stringify(results,null,2));console.log(JSON.stringify(results,null,2));

