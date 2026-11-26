import numpy as np
from PIL import Image
import scipy.ndimage as ndimage
import json

im = Image.open('Arrows_Level.png').convert('RGB')
arr = np.array(im)
h, w, _ = arr.shape

r, g, b = arr[:,:,0].astype(float), arr[:,:,1].astype(float), arr[:,:,2].astype(float)
is_bg = (r > 200) & (g > 200) & (b > 200) & (np.abs(r - g) < 25) & (np.abs(g - b) < 25)
is_border = (r < 50) & (g < 50) & (b < 50)

# Mask inside the board
board_mask = np.zeros((h, w), dtype=bool)
board_mask[20:h-20, 20:w-20] = True
is_line = (~is_bg) & board_mask

# Helper to evaluate Catmull-Rom identical to Unity
def catmull_rom_eval(p0, p1, p2, p3, t):
    t2 = t * t
    t3 = t2 * t
    return 0.5 * (
        (2.0 * p1) +
        (-p0 + p2) * t +
        (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * t2 +
        (-p0 + 3.0 * p1 - 3.0 * p2 + p3) * t3
    )

def evaluate_path(control_points, t):
    pts = np.array(control_points)
    n = len(pts)
    segment_count = n - 1
    t = np.clip(t, 0.0, 1.0)
    seg_float = t * segment_count
    seg = min(int(seg_float), segment_count - 1)
    local_t = seg_float - seg
    
    p1 = pts[seg]
    p2 = pts[seg + 1]
    p0 = 2 * pts[0] - pts[1] if seg == 0 else pts[seg - 1]
    p3 = 2 * pts[-1] - pts[-2] if seg + 2 >= n else pts[seg + 2]
    
    return catmull_rom_eval(p0, p1, p2, p3, local_t)

print("Helper ready")

