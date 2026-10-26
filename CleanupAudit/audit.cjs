const fs = require('fs'), path = require('path');
const root = process.cwd();
function walk(dir) { if (!fs.existsSync(dir)) return []; return fs.readdirSync(dir,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(dir+'/'+e.name):[dir+'/'+e.name]); }
const files=walk('Assets'), assets=files.filter(p=>!p.endsWith('.meta'));
const sizes=new Map(assets.map(p=>[p,fs.statSync(p).size]));
const guidToPath=new Map(), refs=new Map(), incoming=new Map(), binary=[];
for(const p of files.filter(p=>p.endsWith('.meta'))) {const m=fs.readFileSync(p,'utf8').match(/^guid: ([a-f0-9]{32})/m);if(m)guidToPath.set(m[1],p.slice(0,-5));}
const rx=/\bguid:\s*([a-f0-9]{32})|\bGUID:([a-f0-9]{32})/gi;
function readRefs(p){const b=fs.readFileSync(p);if(b.includes(0)){return {b,ids:[]};}return {ids:[...b.toString('utf8').matchAll(rx)].map(m=>(m[1]||m[2]).toLowerCase())};}
const serial=new Set(['.asset','.unity','.prefab','.mat','.controller','.overridecontroller','.anim','.playable','.terrainlayer','.lighting','.rendertexture','.mask','.spriteatlas','.spriteatlasv2','.shadergraph','.shadersubgraph','.vfx','.preset','.physicsmaterial','.physicmaterial']);
for(const p of assets){let ids=[]; if(serial.has(path.extname(p).toLowerCase())){const r=readRefs(p);ids=r.ids;if(r.b)binary.push({p,b:r.b});} if(fs.existsSync(p+'.meta'))ids.push(...readRefs(p+'.meta').ids);refs.set(p,new Set(ids.map(g=>guidToPath.get(g)).filter(Boolean)));}
// Serialized Unity binary files store GUIDs as four little-endian uint32 words.
const rawGuids=new Map();
for(const [g,p] of guidToPath){const bytes=Buffer.from(g,'hex');rawGuids.set(bytes.toString('hex'),p);const swapped=Buffer.from(bytes);swapped.swap32();rawGuids.set(swapped.toString('hex'),p);}
for(const {p,b} of binary){for(let i=0;i<=b.length-16;i++){const dep=rawGuids.get(b.subarray(i,i+16).toString('hex'));if(dep)refs.get(p).add(dep);}}
const project=walk('ProjectSettings').filter(p=>p.endsWith('.asset'));
for(const [p,deps] of refs)deps.delete(p);
let config=[];for(const p of project){config.push(...readRefs(p).ids.map(g=>guidToPath.get(g)).filter(Boolean));}
const build=fs.readFileSync('ProjectSettings/EditorBuildSettings.asset','utf8');
const enabled=[...build.matchAll(/enabled: 1\s+path: (.+)/g)].map(m=>m[1].trim());
const scenes=assets.filter(p=>p.endsWith('.unity'));
const protectedRoots=assets.filter(p=> /\/(Resources|StreamingAssets|Editor|Editor Default Resources|Gizmos|Plugins)\//i.test(p)||/\.(cs|dll|asmdef|asmref|rsp|shader|cginc|hlsl|compute|shadervariants)$/i.test(p));
// Keep assets named by literal paths in source as well as serialized references.
const literals=[];for(const p of assets.filter(p=>p.endsWith('.cs'))){const s=fs.readFileSync(p,'utf8');for(const m of s.matchAll(/Assets\/[^"\r\n]+/g)){if(sizes.has(m[0]))literals.push(m[0]);}}
function closure(roots){const seen=new Set(), q=[...roots];while(q.length){const p=q.pop();if(seen.has(p))continue;seen.add(p);for(const d of refs.get(p)||[])q.push(d);if(!sizes.has(p)&&fs.existsSync(p)&&fs.statSync(p).isDirectory())for(const a of assets)if(a.startsWith(p+'/'))q.push(a);}return seen;}
const main=closure([...enabled,...config,...protectedRoots,...literals]);
const all=closure([...scenes,...config,...protectedRoots,...literals]);
for(const [p,deps] of refs)for(const d of deps){if(!incoming.has(d))incoming.set(d,[]);incoming.get(d).push(p);}
const rows=assets.map(p=>({path:p,bytes:sizes.get(p),status:main.has(p)?'KEEP: game, settings, code or protected folder':all.has(p)?'REVIEW: used by other scenes only':'CANDIDATE: no reference from any scene or protected roots',references:(incoming.get(p)||[]).length}));
const candidates=rows.filter(r=>r.status.startsWith('CANDIDATE')).sort((a,b)=>b.bytes-a.bytes);
const csv=rs=>'path,source_bytes,source_MiB,status,incoming_asset_references\n'+rs.map(r=>[r.path,r.bytes,(r.bytes/1048576).toFixed(3),r.status,r.references].map(v=>'"'+String(v).replaceAll('"','""')+'"').join(',')).join('\n');
fs.writeFileSync('CleanupAudit/asset-inventory.csv',csv(rows));fs.writeFileSync('CleanupAudit/unused-candidates.csv',csv(candidates));fs.writeFileSync('CleanupAudit/other-scenes-only.csv',csv(rows.filter(r=>r.status.startsWith('REVIEW'))));
const groups={};for(const r of rows){const g=r.path.split('/').slice(0,2).join('/');groups[g]??={total:0,candidate:0,other:0,keep:0,count:0,candidates:0};const v=groups[g];v.total+=r.bytes;v.count++;if(r.status.startsWith('CANDIDATE')){v.candidate+=r.bytes;v.candidates++;}else if(r.status.startsWith('REVIEW'))v.other+=r.bytes;else v.keep+=r.bytes;}
const dirs={};for(const e of fs.readdirSync('.',{withFileTypes:true}).filter(e=>e.isDirectory())){let size=0,n=0;for(const p of walk(e.name)){size+=fs.statSync(p).size;n++;}dirs[e.name]={bytes:size,files:n};}
const result={enabled,scenes,binary:binary.map(({p,b})=>({path:p,bytes:b.length})),counts:{all:rows.length,candidates:candidates.length,keep:rows.filter(r=>r.status.startsWith('KEEP')).length},candidateBytes:candidates.reduce((n,r)=>n+r.bytes,0),groups,dirs,largestCandidates:candidates.slice(0,35),largestKept:rows.filter(r=>r.status.startsWith('KEEP')).sort((a,b)=>b.bytes-a.bytes).slice(0,20)};
fs.writeFileSync('CleanupAudit/summary.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
