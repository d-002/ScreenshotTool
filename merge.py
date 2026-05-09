import os
import json
import numpy as np
from PIL import Image

with open('room.json') as f:
    metadata = json.load(f)

room_size = np.array(metadata['roomSize'])
camera_size = np.array(metadata['cameraSize'])
viewport = np.array(metadata['viewPort'])

projection = viewport / camera_size

canvas_size = tuple((room_size * projection).astype(int))
merged = Image.new('RGBA', canvas_size)

OUT_FILE = 'out.png'
w, h = room_size * projection
print(f'Final image size: {int(w)}x{int(h)}')

files = [file for file in os.listdir()
         if file != OUT_FILE and file.endswith('.png')]

for i, file in enumerate(files):
    chunks = file[:-4].split(',')
    dx = int(chunks[1].split('=')[1])
    dy = int(chunks[2].split('=')[1])

    with Image.open(file) as img:
        img = img.convert('RGBA')

    pos = (np.array([dx, dy]) - camera_size / 2) * projection
    pos_tuple = tuple(pos.astype(int))

    # img as 3rd argument for alpha channel as a mask
    merged.paste(img, pos_tuple, img)

    print(f'{(i+1) / len(files) * 100:.2f}% complete.', end='\r')
print()

print('Saving to merged file...')
merged.save(OUT_FILE)
