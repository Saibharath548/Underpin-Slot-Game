import numpy as np
from PIL import Image, ImageDraw
import heapq
import json
import scipy.ndimage as ndimage

im = Image.open('Arrows_Level.png').convert('RGB')
arr = np.array(im)
h, w, _ = arr.shape

r, g, b = arr[:,:,0].astype(float), arr[:,:,1].astype(float), arr[:,:,2].astype(float)
is_bg = (r > 200) & (g > 200) & (b > 200) & (np.abs(r - g) < 25) & (np.abs(g - b) < 25)
is_line_any = (~is_bg)

def make_dijkstra_cost(color_mask):
    # Distance to the target color
    dist_to_color = ndimage.distance_transform_edt(~color_mask)
    # Cost is low on target color, moderate on other lines, very high on background
    cost = 1.0 + dist_to_color * 1.5
    cost[is_bg] += 50.0 + dist_to_color[is_bg] * 10.0
    return cost

def dijkstra_between(start, goal, cost_map):
    sx, sy = int(round(start[0])), int(round(start[1]))
    gx, gy = int(round(goal[0])), int(round(goal[1]))
    
    dist = np.full((h, w), 1e9, dtype=float)
    dist[sy, sx] = 0.0
    prev = {}
    pq = [(0.0, sx, sy)]
    moves = [(-1, 0), (1, 0), (0, -1), (0, 1), (-1, -1), (-1, 1), (1, -1), (1, 1)]
    
    while pq:
        d, x, y = heapq.heappop(pq)
        if x == gx and y == gy:
            break
        if d > dist[y, x]:
            continue
        for dx, dy in moves:
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h:
                c = cost_map[ny, nx]
                step_dist = np.hypot(dx, dy) * c
                nd = d + step_dist
                if nd < dist[ny, nx]:
                    dist[ny, nx] = nd
                    prev[(nx, ny)] = (x, y)
                    heapq.heappush(pq, (nd, nx, ny))
                    
    path = []
    curr = (gx, gy)
    if curr not in prev and curr != (sx, sy):
        return []
    while curr in prev:
        path.append(curr)
        curr = prev[curr]
    path.append((sx, sy))
    path.reverse()
    return path

# Catmull-Rom evaluator
def catmull_rom_eval(p0, p1, p2, p3, t):
    t2 = t * t
    t3 = t2 * t
    return 0.5 * (
        (2.0 * p1) +
        (-p0 + p2) * t +
        (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * t2 +
        (-p0 + 3.0 * p1 - 3.0 * p2 + p3) * t3
    )

def evaluate_catmull_rom_path(pts, t):
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

curves_specs = [
    # 1. Pink Inner Loop (oval)
    {
        'id': 'Pink_Inner',
        'color': '#F659E1',
        'rgb_255': (246, 89, 225),
        'rgb_unity': {'r': 0.965, 'g': 0.349, 'b': 0.882, 'a': 1.0},
        'mask': (r > 180) & (b > 180) & (g < 160),
        'guide': [(117, 170), (133, 155), (150, 170)],
        'num_ctrl': 5
    },
    # 2. Pink Mid Loop (egg/oval)
    {
        'id': 'Pink_Mid',
        'color': '#F659E1',
        'rgb_255': (246, 89, 225),
        'rgb_unity': {'r': 0.965, 'g': 0.349, 'b': 0.882, 'a': 1.0},
        'mask': (r > 180) & (b > 180) & (g < 160),
        'guide': [(100, 150), (128, 128), (155, 145)],
        'num_ctrl': 5
    },
    # 3. Pink Outer Heart/Loop
    {
        'id': 'Pink_Outer',
        'color': '#F659E1',
        'rgb_255': (246, 89, 225),
        'rgb_unity': {'r': 0.965, 'g': 0.349, 'b': 0.882, 'a': 1.0},
        'mask': (r > 180) & (b > 180) & (g < 160),
        'guide': [(116, 122), (65, 75), (120, 48), (155, 75), (200, 48), (240, 75), (141, 118)],
        'num_ctrl': 9
    },
    # 4. Blue Upper/Left Circular Loop
    {
        'id': 'Blue_Upper',
        'color': '#1C82F5',
        'rgb_255': (28, 130, 245),
        'rgb_unity': {'r': 0.110, 'g': 0.510, 'b': 0.961, 'a': 1.0},
        'mask': (b > 170) & (r < 110),
        'guide': [(138, 203), (130, 260), (180, 330), (246, 322)],
        'num_ctrl': 6
    },
    # 5. Blue Lower Loop
    {
        'id': 'Blue_Lower',
        'color': '#1C82F5',
        'rgb_255': (28, 130, 245),
        'rgb_unity': {'r': 0.110, 'g': 0.510, 'b': 0.961, 'a': 1.0},
        'mask': (b > 170) & (r < 110),
        'guide': [(336, 341), (350, 385), (290, 415), (234, 410)],
        'num_ctrl': 5
    },
    # 6. Red Upper Small Curve
    {
        'id': 'Red_Upper',
        'color': '#FB0D0D',
        'rgb_255': (251, 13, 13),
        'rgb_unity': {'r': 0.984, 'g': 0.051, 'b': 0.051, 'a': 1.0},
        'mask': (r > 180) & (g < 80) & (b < 80),
        'guide': [(138, 257), (165, 250), (194, 230)],
        'num_ctrl': 4
    },
    # 7. Red Mid-Left C-Curve
    {
        'id': 'Red_Mid',
        'color': '#FB0D0D',
        'rgb_255': (251, 13, 13),
        'rgb_unity': {'r': 0.984, 'g': 0.051, 'b': 0.051, 'a': 1.0},
        'mask': (r > 180) & (g < 80) & (b < 80),
        'guide': [(113, 359), (105, 340), (135, 325), (163, 323)],
        'num_ctrl': 5
    },
    # 8. Red Bottom-Left Curve
    {
        'id': 'Red_Lower',
        'color': '#FB0D0D',
        'rgb_255': (251, 13, 13),
        'rgb_unity': {'r': 0.984, 'g': 0.051, 'b': 0.051, 'a': 1.0},
        'mask': (r > 180) & (g < 80) & (b < 80),
        'guide': [(221, 488), (240, 515), (261, 506)],
        'num_ctrl': 4
    },
    # 9. Green Upper Teardrop
    {
        'id': 'Green_Upper',
        'color': '#30DB27',
        'rgb_255': (48, 219, 39),
        'rgb_unity': {'r': 0.188, 'g': 0.859, 'b': 0.153, 'a': 1.0},
        'mask': (g > 150) & (r < 120) & (b < 120),
        'guide': [(162, 238), (175, 220), (185, 235), (168, 260)],
        'num_ctrl': 5
    },
    # 10. Green Mid Leaf
    {
        'id': 'Green_Mid',
        'color': '#30DB27',
        'rgb_255': (48, 219, 39),
        'rgb_unity': {'r': 0.188, 'g': 0.859, 'b': 0.153, 'a': 1.0},
        'mask': (g > 150) & (r < 120) & (b < 120),
        'guide': [(133, 325), (150, 332), (143, 345)],
        'num_ctrl': 4
    },
    # 11. Green Right Sweep Loop
    {
        'id': 'Green_Right',
        'color': '#30DB27',
        'rgb_255': (48, 219, 39),
        'rgb_unity': {'r': 0.188, 'g': 0.859, 'b': 0.153, 'a': 1.0},
        'mask': (g > 150) & (r < 120) & (b < 120),
        'guide': [(437, 336), (440, 380), (410, 425), (356, 430)],
        'num_ctrl': 6
    },
    # 12. Orange Right Upper Loop
    {
        'id': 'Orange_Right',
        'color': '#D77B4A',
        'rgb_255': (215, 123, 74),
        'rgb_unity': {'r': 0.843, 'g': 0.482, 'b': 0.290, 'a': 1.0},
        'mask': (r > 150) & (g > 80) & (g < 170) & (b < 110),
        'guide': [(334, 299), (350, 255), (410, 250), (442, 313)],
        'num_ctrl': 5
    },
    # 13. Orange Lower Arc
    {
        'id': 'Orange_Lower',
        'color': '#D77B4A',
        'rgb_255': (215, 123, 74),
        'rgb_unity': {'r': 0.843, 'g': 0.482, 'b': 0.290, 'a': 1.0},
        'mask': (r > 150) & (g > 80) & (g < 170) & (b < 110),
        'guide': [(245, 421), (275, 435), (280, 465), (271, 486)],
        'num_ctrl': 5
    },
    # 14. Purple Lower Hairpin
    {
        'id': 'Purple_Lower',
        'color': '#870EF0',
        'rgb_255': (135, 14, 240),
        'rgb_unity': {'r': 0.529, 'g': 0.055, 'b': 0.941, 'a': 1.0},
        'mask': (b > 130) & (r > 90) & (g < 80),
        'guide': [(342, 437), (345, 470), (300, 485), (244, 481)],
        'num_ctrl': 5
    }
]

scale = 0.013
cx_img, cy_img = 249.0, 283.0

reconstructed_lines = []
preview_img = Image.new('RGB', (w, h), (240, 240, 240))
draw = ImageDraw.Draw(preview_img)

for spec in curves_specs:
    cost_map = make_dijkstra_cost(spec['mask'])
    full_path = []
    guide = spec['guide']
    for i in range(len(guide) - 1):
        seg = dijkstra_between(guide[i], guide[i+1], cost_map)
        if not seg:
            print(f"Failed seg {guide[i]} to {guide[i+1]}")
            seg = [guide[i], guide[i+1]]
        if i > 0:
            seg = seg[1:]
        full_path.extend(seg)
        
    raw_arr = np.array(full_path)
    diffs = np.diff(raw_arr, axis=0)
    dists = np.hypot(diffs[:, 0], diffs[:, 1])
    cum_dist = np.insert(np.cumsum(dists), 0, 0.0)
    total_len = cum_dist[-1]
    
    k = spec['num_ctrl']
    sampled_pts = []
    for i in range(k):
        target_d = total_len * (i / (k - 1))
        idx = np.searchsorted(cum_dist, target_d)
        if idx == 0:
            sampled_pts.append(raw_arr[0].astype(float))
        elif idx >= len(raw_arr):
            sampled_pts.append(raw_arr[-1].astype(float))
        else:
            t = (target_d - cum_dist[idx-1]) / (cum_dist[idx] - cum_dist[idx-1] + 1e-9)
            pt = raw_arr[idx-1] + t * (raw_arr[idx] - raw_arr[idx-1])
            sampled_pts.append(pt)
            
    sampled_pts = np.array(sampled_pts)
    
    # Evaluate Catmull-Rom
    spline_points = []
    for s in range(101):
        pt = evaluate_catmull_rom_path(sampled_pts, s / 100.0)
        spline_points.append((pt[0], pt[1]))
        
    for s in range(len(spline_points) - 1):
        draw.line([spline_points[s], spline_points[s+1]], fill=spec['rgb_255'], width=4)
        
    p_start = spline_points[0]
    p_end = spline_points[-1]
    r_ah = 6
    draw.ellipse([p_start[0]-r_ah, p_start[1]-r_ah, p_start[0]+r_ah, p_start[1]+r_ah], fill=spec['rgb_255'], outline=(0, 0, 0))
    draw.ellipse([p_end[0]-r_ah, p_end[1]-r_ah, p_end[0]+r_ah, p_end[1]+r_ah], fill=spec['rgb_255'], outline=(0, 0, 0))
    
    unity_points = []
    for pt in sampled_pts:
        ux = (pt[0] - cx_img) * scale
        uy = -(pt[1] - cy_img) * scale
        unity_points.append({'x': round(float(ux), 4), 'y': round(float(uy), 4)})
        
    reconstructed_lines.append({
        'lineId': spec['id'],
        'color': spec['rgb_unity'],
        'lineWidth': 0.08,
        'arrowheadScale': 0.4,
        'movementSpeed': 3.0,
        'collisionRadius': 0.25,
        'controlPoints': unity_points
    })
    print(f"Successfully processed {spec['id']}: length={total_len:.1f}px, {k} control points.")

preview_img.save('scratch/reconstructed_preview.png')

# Side by side comparison
comp_w = w * 2 + 10
comp_img = Image.new('RGB', (comp_w, h), (30, 30, 30))
comp_img.paste(im, (0, 0))
comp_img.paste(preview_img, (w + 10, 0))
comp_img.save('scratch/side_by_side_comparison.png')
comp_img.save('C:/Users/Asus/.gemini/antigravity/brain/8353069f-f360-4953-8c04-e1aa288d9dcd/reconstruction_comparison.png')

with open('scratch/reconstructed_lines.json', 'w') as f:
    json.dump(reconstructed_lines, f, indent=2)

print("\nALL 14 LINES SUCCESSFULLY RECONSTRUCTED!")
