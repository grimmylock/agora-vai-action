# Yan's generated boulder (refs/brute/boulder_yan.jpg, 2000x1091, checkerboard baked in, ~17 px blocks) -> game pixels: mask the rock (the checker is grey 139/203, the rock is grey too, so the mask is found from the
# connected, opened foreground), sample the block grid, clean the fringe, halve it by 2x2 majority so it is about 32 px wide, snap to 14 colours.  python3 boulder_recover.py -> mutants_anim/brute/boulder.png
import os, numpy as np
from PIL import Image
from scipy import ndimage as nd
HERE = os.path.dirname(os.path.abspath(__file__)); im = np.array(Image.open(os.path.join(HERE, "refs", "brute", "boulder_yan.jpg")).convert("RGB")).astype(int)
sat = im.max(2) - im.min(2); lum = im.mean(2); fg = ~((sat < 22) & (lum > 180)); fg = nd.binary_opening(fg, iterations=3); lab, n = nd.label(fg); fg = lab == 1 + int(np.argmax(nd.sum(fg, lab, range(1, n + 1)))); fg = nd.binary_fill_holes(fg)
ys, xs = np.where(fg); y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1; sub = im[y0:y1, x0:x1]; mm = fg[y0:y1, x0:x1]; TW = 30                                                                  # target width in game pixels (the generated rock is ~64 cells wide)
TH = int(round(TW * sub.shape[0] / sub.shape[1])); m = mm.astype(float); cols = sub.astype(float) * m[..., None]
def box(arr): return np.array(Image.fromarray(arr.astype(np.float32), mode="F").resize((TW, TH), Image.BOX)) if arr.ndim == 2 else np.stack([box(arr[..., c]) for c in range(arr.shape[2])], -1)
cov = box(m); rgb = np.where(cov[..., None] > 0.05, box(cols) / np.maximum(cov[..., None], 1e-3), 0)                      # area-weighted colour, background excluded
alpha = cov > 0.55; alpha = nd.binary_opening(alpha, structure=np.ones((2, 2))); lb, k = nd.label(alpha); alpha = lb == 1 + int(np.argmax(nd.sum(alpha, lb, range(1, k + 1)))); alpha = nd.binary_fill_holes(alpha)
edge = alpha & ~nd.binary_erosion(alpha, structure=np.array([[0, 1, 0], [1, 1, 1], [0, 1, 0]]))
out = np.zeros((TH, TW, 4), np.uint8); out[..., :3] = np.clip(rgb * 1.08, 0, 255).astype(np.uint8); out[..., 3] = alpha * 255; out[edge, :3] = (22, 20, 28)           # a 1 px dark outline all round
q = Image.fromarray(out[..., :3]).quantize(colors=14, method=Image.MEDIANCUT, dither=Image.NONE).convert("RGB"); out[..., :3] = np.array(q); out[edge, :3] = (22, 20, 28); out[~alpha, :3] = 0
ys, xs = np.where(out[..., 3] > 0); out = out[ys.min(): ys.max() + 1, xs.min(): xs.max() + 1]
Image.fromarray(out).save(os.path.join(HERE, "mutants_anim", "brute", "boulder.png")); print("boulder", out.shape[1], "x", out.shape[0])
