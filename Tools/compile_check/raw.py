# png <-> RAW1 (bottom-up RGBA) for the C# terrain render test
import sys, struct
from PIL import Image
def to_raw(png, raw):
    im = Image.open(png).convert("RGBA").transpose(Image.FLIP_TOP_BOTTOM)
    open(raw, "wb").write(b"RAW1" + struct.pack("<ii", *im.size) + im.tobytes())
def from_raw(raw, png, scale=1):
    b = open(raw, "rb").read(); w, h = struct.unpack("<ii", b[4:12])
    im = Image.frombytes("RGBA", (w, h), b[12:]).transpose(Image.FLIP_TOP_BOTTOM)
    if scale != 1: im = im.resize((w * scale, h * scale), Image.NEAREST)
    im.save(png)
if __name__ == "__main__":
    {"to": lambda: to_raw(sys.argv[2], sys.argv[3]), "from": lambda: from_raw(sys.argv[2], sys.argv[3], int(sys.argv[4]) if len(sys.argv) > 4 else 1)}[sys.argv[1]]()
