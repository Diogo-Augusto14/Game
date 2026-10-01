"""Tira os sons (DS*) do freedoom2.wad pra pasta fd/sfx. No Ubuntu: apt install freedoom."""
import struct, sys, wave, os
os.chdir(os.path.dirname(os.path.abspath(__file__)))
w = open('/usr/share/games/doom/freedoom2.wad','rb').read()
ident, n, ofs = struct.unpack('<4sii', w[:12])
os.makedirs('fd/sfx', exist_ok=True)
for i in range(n):
    pos, size, name = struct.unpack('<ii8s', w[ofs+16*i: ofs+16*i+16])
    name = name.rstrip(b'\0').decode('ascii', 'replace')
    data = w[pos:pos+size]
    if name.startswith('DS') and data[:2] == b'\x03\x00':
        fmt, rate, num = struct.unpack('<HHI', data[:8])
        pcm = data[8+16: 8+num-16]
        with wave.open(f'fd/sfx/{name}.wav','wb') as o:
            o.setnchannels(1); o.setsampwidth(1); o.setframerate(rate); o.writeframes(pcm)
        print(f'{name:10s} {rate:6d} {len(pcm)/rate:5.2f}s')
