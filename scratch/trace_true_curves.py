import numpy as np
from PIL import Image, ImageDraw
import scipy.ndimage as ndimage
import json

im = Image.open('Arrows_Level.png').convert('RGB')
arr = np.array(im)
h, w, _ = arr.shape
r, g, b = arr[:,:,0].astype(float), arr[:,:,1].astype(float), arr[:,:,2].astype(float)

is_bg = (r > 200) & (g > 200) & (b > 200) & (np.abs(r - g) < 25) & (np.abs(g - b) < 25)

# Catmull-Rom evaluator identical to Unity
def catmull_rom_eval(p0, p1, p2, p3, t):
    t2 = t * t
    t3 = t2 * t
    return 0.5 * (
        (2.0 * p1) +
        (-p0 + p2) * t +
        (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * t2 +
        (-p0 + 3.0 * p1 - 3.0 * p2 + p3) * t3
    )

def evaluate_path(pts, t):
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

# Accurate specifications for all 14 lines in pixel coordinates
# Each curve has finely measured control points tracing the visible curves in Arrows_Level.png
true_curves = [
    # ── PINK LINES (3 lines) ──
    # 1. Pink Outer Heart/Dome: from (116, 122) up around dome to (141, 118)
    {
        'id': 'Pink_Outer',
        'color_name': 'Pink',
        'rgb_255': (246, 89, 225),
        'rgb_unity': {'r': 0.965, 'g': 0.349, 'b': 0.882, 'a': 1.0},
        'pixel_points': [
            (116.0, 122.3),
            (95.0, 85.0),
            (105.0, 50.0),
            (135.0, 48.0),
            (152.0, 75.0),
            (165.0, 48.0),
            (195.0, 50.0),
            (210.0, 85.0),
            (180.0, 115.0),
            (141.1, 118.2)
        ]
    },
    # 2. Pink Left Loop: from (100, 150) curving left/down to (117, 170)
    {
        'id': 'Pink_Left',
        'color_name': 'Pink',
        'rgb_255': (246, 89, 225),
        'rgb_unity': {'r': 0.965, 'g': 0.349, 'b': 0.882, 'a': 1.0},
        'pixel_points': [
            (100.0, 150.3),
            (88.0, 148.0),
            (88.0, 165.0),
            (102.0, 175.0),
            (117.1, 169.8)
        ]
    },
    # 3. Pink Right Loop: from (155, 145) curving right/down to (150, 170)
    {
        'id': 'Pink_Right',
        'color_name': 'Pink',
        'rgb_255': (246, 89, 225),
        'rgb_unity': {'r': 0.965, 'g': 0.349, 'b': 0.882, 'a': 1.0},
        'pixel_points': [
            (155.3, 144.8),
            (170.0, 146.0),
            (172.0, 165.0),
            (162.0, 175.0),
            (150.0, 170.1)
        ]
    },

    # ── BLUE LINES (2 lines) ──
    # 4. Blue Left Sweep: from (138, 203) down left side around to (234, 410)
    {
        'id': 'Blue_LeftSweep',
        'color_name': 'Blue',
        'rgb_255': (28, 130, 245),
        'rgb_unity': {'r': 0.110, 'g': 0.510, 'b': 0.961, 'a': 1.0},
        'pixel_points': [
            (137.7, 202.8),
            (136.0, 240.0),
            (145.0, 280.0),
            (153.0, 315.0),
            (165.0, 360.0),
            (190.0, 400.0),
            (215.0, 415.0),
            (233.8, 410.1)
        ]
    },
    # 5. Blue Right Loop: from (246, 322) up and around to (336, 341)
    {
        'id': 'Blue_RightLoop',
        'color_name': 'Blue',
        'rgb_255': (28, 130, 245),
        'rgb_unity': {'r': 0.110, 'g': 0.510, 'b': 0.961, 'a': 1.0},
        'pixel_points': [
            (245.7, 322.2),
            (255.0, 260.0),
            (285.0, 215.0),
            (325.0, 220.0),
            (355.0, 260.0),
            (352.0, 310.0),
            (336.0, 340.6)
        ]
    },

    # ── RED LINES (3 lines) ──
    # 6. Red Upper Arc: from (138, 257) arching up-right to (194, 230)
    {
        'id': 'Red_Upper',
        'color_name': 'Red',
        'rgb_255': (251, 13, 13),
        'rgb_unity': {'r': 0.984, 'g': 0.051, 'b': 0.051, 'a': 1.0},
        'pixel_points': [
            (138.4, 256.7),
            (160.0, 265.0),
            (180.0, 255.0),
            (193.5, 229.8)
        ]
    },
    # 7. Red Mid Arc: from (113, 359) arching up-right to (163, 323)
    {
        'id': 'Red_Mid',
        'color_name': 'Red',
        'rgb_255': (251, 13, 13),
        'rgb_unity': {'r': 0.984, 'g': 0.051, 'b': 0.051, 'a': 1.0},
        'pixel_points': [
            (113.3, 358.9),
            (112.0, 335.0),
            (135.0, 320.0),
            (162.9, 323.2)
        ]
    },
    # 8. Red Bottom Arc: from (221, 488) curving down to (261, 506)
    {
        'id': 'Red_Bottom',
        'color_name': 'Red',
        'rgb_255': (251, 13, 13),
        'rgb_unity': {'r': 0.984, 'g': 0.051, 'b': 0.051, 'a': 1.0},
        'pixel_points': [
            (221.4, 487.9),
            (235.0, 515.0),
            (255.0, 515.0),
            (261.1, 506.2)
        ]
    },

    # ── GREEN LINES (3 lines) ──
    # 9. Green Upper Loop: from (162, 238) around loop to (169, 260)
    {
        'id': 'Green_Upper',
        'color_name': 'Green',
        'rgb_255': (48, 219, 39),
        'rgb_unity': {'r': 0.188, 'g': 0.859, 'b': 0.153, 'a': 1.0},
        'pixel_points': [
            (161.9, 238.2),
            (175.0, 205.0),
            (210.0, 195.0),
            (235.0, 230.0),
            (215.0, 265.0),
            (185.0, 268.0),
            (168.5, 259.5)
        ]
    },
    # 10. Green Mid Leaf: from (133, 325) looping left-down to (143, 345)
    {
        'id': 'Green_Leaf',
        'color_name': 'Green',
        'rgb_255': (48, 219, 39),
        'rgb_unity': {'r': 0.188, 'g': 0.859, 'b': 0.153, 'a': 1.0},
        'pixel_points': [
            (132.8, 325.0),
            (105.0, 335.0),
            (85.0, 365.0),
            (105.0, 395.0),
            (135.0, 375.0),
            (143.0, 345.3)
        ]
    },
    # 11. Green Right Sweep: from (437, 336) down and left to (356, 430)
    {
        'id': 'Green_RightSweep',
        'color_name': 'Green',
        'rgb_255': (48, 219, 39),
        'rgb_unity': {'r': 0.188, 'g': 0.859, 'b': 0.153, 'a': 1.0},
        'pixel_points': [
            (437.2, 335.8),
            (448.0, 375.0),
            (430.0, 420.0),
            (395.0, 438.0),
            (355.7, 430.0)
        ]
    },

    # ── ORANGE LINES (2 lines) ──
    # 12. Orange Right Loop: from (334, 299) arching up-right to (442, 313)
    {
        'id': 'Orange_RightLoop',
        'color_name': 'Orange',
        'rgb_255': (215, 123, 74),
        'rgb_unity': {'r': 0.843, 'g': 0.482, 'b': 0.290, 'a': 1.0},
        'pixel_points': [
            (333.6, 299.0),
            (345.0, 270.0),
            (385.0, 265.0),
            (425.0, 280.0),
            (442.0, 313.3)
        ]
    },
    # 13. Orange Center Arc: from (245, 421) down-right to (271, 486)
    {
        'id': 'Orange_CenterArc',
        'color_name': 'Orange',
        'rgb_255': (215, 123, 74),
        'rgb_unity': {'r': 0.843, 'g': 0.482, 'b': 0.290, 'a': 1.0},
        'pixel_points': [
            (245.0, 421.4),
            (265.0, 430.0),
            (278.0, 455.0),
            (271.0, 486.1)
        ]
    },

    # ── PURPLE LINE (1 line) ──
    # 14. Purple Hairpin: from (342, 437) looping down-left to (244, 481)
    {
        'id': 'Purple_Hairpin',
        'color_name': 'Purple',
        'rgb_255': (135, 14, 240),
        'rgb_unity': {'r': 0.529, 'g': 0.055, 'b': 0.941, 'a': 1.0},
        'pixel_points': [
            (342.4, 437.3),
            (350.0, 465.0),
            (320.0, 488.0),
            (280.0, 488.0),
            (244.2, 480.9)
        ]
    }
]

# Render preview overlay
preview = Image.new('RGB', (w, h), (255, 255, 255))
draw_prev = ImageDraw.Draw(preview)

scale = 0.013
cx_img, cy_img = 249.0, 283.0

output_lines = []

for curve in true_curves:
    pts = np.array(curve['pixel_points'])
    
    # Evaluate Catmull-Rom spline finely
    spline_pts = []
    for s in range(201):
        pt = evaluate_path(pts, s / 200.0)
        spline_pts.append((pt[0], pt[1]))
        
    for s in range(len(spline_pts) - 1):
        draw_prev.line([spline_pts[s], spline_pts[s+1]], fill=curve['rgb_255'], width=4)
        
    # Draw arrowheads
    p0 = spline_pts[0]
    p1 = spline_pts[-1]
    r_ah = 6
    draw_prev.ellipse([p0[0]-r_ah, p0[1]-r_ah, p0[0]+r_ah, p0[1]+r_ah], fill=curve['rgb_255'], outline=(0,0,0))
    draw_prev.ellipse([p1[0]-r_ah, p1[1]-r_ah, p1[0]+r_ah, p1[1]+r_ah], fill=curve['rgb_255'], outline=(0,0,0))
    
    # Unity control points
    unity_points = []
    for pt in pts:
        ux = (pt[0] - cx_img) * scale
        uy = -(pt[1] - cy_img) * scale
        unity_points.append({'x': round(float(ux), 4), 'y': round(float(uy), 4)})
        
    output_lines.append({
        'lineId': curve['id'],
        'color': curve['rgb_unity'],
        'lineWidth': 0.08,
        'arrowheadScale': 0.4,
        'movementSpeed': 3.0,
        'collisionRadius': 0.25,
        'controlPoints': unity_points
    })

# Save side by side comparison
comp_w = w * 2 + 10
comp_img = Image.new('RGB', (comp_w, h), (240, 240, 240))
comp_img.paste(im, (0, 0))
comp_img.paste(preview, (w + 10, 0))
comp_img.save('scratch/accurate_comparison.png')
comp_img.save('C:/Users/Asus/.gemini/antigravity/brain/8353069f-f360-4953-8c04-e1aa288d9dcd/accurate_comparison.png')

with open('scratch/accurate_lines.json', 'w') as f:
    json.dump(output_lines, f, indent=2)

print("Accurate reconstruction generated and saved!")
