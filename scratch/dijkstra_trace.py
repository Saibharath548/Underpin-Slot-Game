import numpy as np
from PIL import Image
import heapq

im = Image.open('Arrows_Level.png').convert('RGB')
arr = np.array(im)
h, w, _ = arr.shape

r, g, b = arr[:,:,0].astype(float), arr[:,:,1].astype(float), arr[:,:,2].astype(float)
is_bg = (r > 200) & (g > 200) & (b > 200) & (np.abs(r - g) < 25) & (np.abs(g - b) < 25)

def dijkstra_path(start, goal, color_mask, allow_crossings=True):
    # start, goal are (x, y)
    sx, sy = int(round(start[0])), int(round(start[1]))
    gx, gy = int(round(goal[0])), int(round(goal[1]))
    
    # Cost array
    cost = np.full((h, w), 1e6, dtype=float)
    # matching color
    cost[color_mask] = 1.0
    if allow_crossings:
        # non-bg, non-matching color (other lines)
        other_lines = (~is_bg) & (~color_mask)
        cost[other_lines] = 3.5
    
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
                c = cost[ny, nx]
                if c >= 1e5:
                    continue
                step_dist = np.hypot(dx, dy) * c
                nd = d + step_dist
                if nd < dist[ny, nx]:
                    dist[ny, nx] = nd
                    prev[(nx, ny)] = (x, y)
                    heapq.heappush(pq, (nd, nx, ny))
                    
    # Reconstruct path
    path = []
    curr = (gx, gy)
    if curr not in prev and curr != (sx, sy):
        print(f"Failed to find path from {start} to {goal}")
        return []
    while curr in prev:
        path.append(curr)
        curr = prev[curr]
    path.append((sx, sy))
    path.reverse()
    return path

print("Dijkstra tracer ready")

