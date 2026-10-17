from pathlib import Path
from PIL import Image
import json,os
r=Path(os.environ.get('ERAFLOW_QA_WORK',str(Path(__file__).resolve().parent)))/'compat-android'
frames={}
for p in r.glob('*.png'):
    im=Image.open(p).convert('RGB');pixels=list(im.getdata())
    frames[p.stem]={'green':sum(g>200 and red<30 and 80<b<160 for red,g,b in pixels),'black':sum(red==g==b==0 for red,g,b in pixels),'pixels':len(pixels)}
checks=[]
def check(name,passed):checks.append({'name':name,'pass':bool(passed)})
check('Fixed sprite rendered',frames['visible']['green']>8000)
check('Image preference hides fixed sprite',frames['images-off']['green']==0)
check('Image preference restores fixed sprite',frames['images-on']['green']==frames['visible']['green'])
check('Group fade hides sprite',frames['group-hidden']['green']==0)
check('Group fade and shake restore sprite',frames['group-restored']['green']==frames['visible']['green'])
check('Full-screen fade reaches black',frames['screen-hidden']['black']==frames['screen-hidden']['pixels'])
check('CLEARALL removes fixed sprite',frames['cleared-all']['green']==0)
check('Sprite reappears for clear-line test',frames['before-clearline']['green']==frames['visible']['green'])
check('CLEARLINE removes fixed sprite',frames['after-clearline']['green']==0)
data={'frames':frames,'checks':checks,'pass':all(c['pass'] for c in checks)}
(r/'pixels.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
print(json.dumps(data,indent=2));assert data['pass']
