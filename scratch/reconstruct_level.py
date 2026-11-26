import numpy as np
from PIL import Image, ImageDraw
import scipy.ndimage as ndimage
import json

im = Image.open('Arrows_Level.png').convert('RGB')
arr = np.array(im)
h, w, _ = arr.shape

r, g, b = arr[:,:,0].astype(float), arr[:,:,1].astype(float), arr[:,:,2].astype(float)
is_bg = (r > 200) & (g > 200) & (b > 200) & (np.abs(r - g) < 25) & (np.abs(g - b) < 25)

# Color masks
masks = {
    'Pink': (r > 180) & (b > 180) & (g < 160),
    'Red': (r > 180) & (g < 80) & (b < 80),
    'Blue': (b > 170) & (r < 110),
    'Green': (g > 150) & (r < 120) & (b < 120),
    'Orange': (r > 150) & (g > 80) & (g < 170) & (b < 110),
    'Purple': (b > 130) & (r > 90) & (g < 80)
}

# In each color mask, let's find the connected components and arrowheads
for cname, m in masks.items():
    lbl, num = ndimage.label(m)
    print(f'Color {cname}: {num} raw connected components')

