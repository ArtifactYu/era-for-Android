from pathlib import Path
import json,zipfile,struct,hashlib,os,sys
r=Path(os.environ.get('ERAFLOW_QA_WORK',str(Path(__file__).resolve().parent)))
apk=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else r.parent/'outputs/era-for-Android-1.1.0.apk'
raw=apk.read_bytes();checks=[]
with zipfile.ZipFile(apk) as z:
    assert z.testzip() is None
    for f in z.infolist():
        if not(f.filename.startswith('lib/') and f.filename.endswith('.so')):continue
        b=z.read(f);assert b[:6]==b'\x7fELF\x02\x01'
        offset=struct.unpack_from('<Q',b,32)[0]
        size,count=struct.unpack_from('<HH',b,54)
        loads=[]
        for i in range(count):
            p=offset+i*size
            if struct.unpack_from('<I',b,p)[0]!=1:continue
            file_offset,address=struct.unpack_from('<QQ',b,p+8)
            align=struct.unpack_from('<Q',b,p+48)[0]
            loads.append(align>=16384 and (address-file_offset)%16384==0)
        name_len,extra_len=struct.unpack_from('<HH',raw,f.header_offset+26)
        data_offset=f.header_offset+30+name_len+extra_len
        checks.append({'file':f.filename,'elf16k':bool(loads) and all(loads),'compressed':f.compress_type!=0,'zip16k':None if f.compress_type!=0 else data_offset%16384==0})
assert len(checks)==24 and all(x['elf16k'] and (x['compressed'] or x['zip16k']) for x in checks)
data={'apk':apk.name,'sha256':hashlib.sha256(raw).hexdigest(),'bytes':len(raw),'libraries':checks,'pass':True}
(r/'apk-1.1-verification.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
print(json.dumps({'pass':True,'libraries':len(checks),'sha256':data['sha256'],'bytes':len(raw)}))
