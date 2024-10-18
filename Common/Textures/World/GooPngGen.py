import png

width = 255
height = 255

alpha_steps = 20

name = "GG_Goo"

for i in range(1,alpha_steps+1):
    img = []
    for y in range(height):
        row = ()
        for x in range(width):
            row = row + (255, 255, 255, int((i/20)*255))
        img.append(row)
    with open(f'{name}_{i}.png', 'wb') as f:
        w = png.Writer(width, height, greyscale=False, alpha=True, bitdepth=8, compression=9)
        w.write(f, img)
