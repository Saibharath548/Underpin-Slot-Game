import numpy as np
from PIL import Image
import scipy.ndimage as ndimage
import json

im = Image.open('Arrows_Level.png').convert('RGB')
arr = np.array(im)
h, w, _ = arr.shape

r, g, b = arr[:,:,0].astype(float), arr[:,:,1].astype(float), arr[:,:,2].astype(float)
is_bg = (r > 200) & (g > 200) & (b > 200) & (np.abs(r - g) < 25) & (np.abs(g - b) < 25)

# Distinct color definitions
colors = {
    'Pink_Outer':  {'range': (r > 180) & (b > 180) & (g < 160), 'hex': '#F659E1'},
    'Pink_Mid':    {'range': (r > 180) & (b > 180) & (g < 160), 'hex': '#F659E1'},
    'Pink_Inner':  {'range': (r > 180) & (b > 180) & (g < 160), 'hex': '#F659E1'},
    'Blue_Upper':  {'range': (b > 170) & (r < 110), 'hex': '#1C82F5'},
    'Blue_Lower':  {'range': (b > 170) & (r < 110), 'hex': '#1C82F5'},
    'Red_Upper':   {'range': (r > 180) & (g < 80) & (b < 80), 'hex': '#FB0D0D'},
    'Red_Mid':     {'range': (r > 180) & (g < 80) & (b < 80), 'hex': '#FB0D0D'},
    'Red_Lower':   {'range': (r > 180) & (g < 80) & (b < 80), 'hex': '#FB0D0D'},
    'Green_Upper': {'range': (g > 150) & (r < 120) & (b < 120), 'hex': '#30DB27'},
    'Green_Mid':   {'range': (g > 150) & (r < 120) & (b < 120), 'hex': '#30DB27'},
    'Green_Right': {'range': (g > 150) & (r < 120) & (b < 120), 'hex': '#30DB27'},
    'Orange_Right':{'range': (r > 150) & (g > 80) & (g < 170) & (b < 110), 'hex': '#D77B4A'},
    'Orange_Lower':{'range': (r > 150) & (g > 80) & (g < 170) & (b < 110), 'hex': '#D77B4A'},
    'Purple_Lower':{'range': (b > 130) & (r > 90) & (g < 80), 'hex': '#870EF0'}
}

print('Analyzing 14 curves...')

