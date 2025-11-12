#!/usr/bin/env python3
"""
Generate icons and banner for TV application
"""
from PIL import Image, ImageDraw, ImageFont
import os

# Define colors
PRIMARY_COLOR = (41, 128, 185)  # Blue
SECONDARY_COLOR = (52, 152, 219)  # Lighter blue
ACCENT_COLOR = (255, 255, 255)  # White
BACKGROUND_COLOR = (44, 62, 80)  # Dark blue-gray

def create_rounded_rectangle_mask(size, radius):
    """Create a rounded rectangle mask"""
    mask = Image.new('L', size, 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle([(0, 0), size], radius=radius, fill=255)
    return mask

def create_icon(size, rounded=False):
    """Create a TV icon"""
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Create gradient background circle
    for i in range(size):
        for j in range(size):
            # Calculate distance from center
            dx = i - size/2
            dy = j - size/2
            distance = (dx*dx + dy*dy) ** 0.5

            if distance < size/2:
                # Gradient from PRIMARY to SECONDARY
                ratio = distance / (size/2)
                r = int(PRIMARY_COLOR[0] + (SECONDARY_COLOR[0] - PRIMARY_COLOR[0]) * ratio)
                g = int(PRIMARY_COLOR[1] + (SECONDARY_COLOR[1] - PRIMARY_COLOR[1]) * ratio)
                b = int(PRIMARY_COLOR[2] + (SECONDARY_COLOR[2] - PRIMARY_COLOR[2]) * ratio)
                img.putpixel((i, j), (r, g, b, 255))

    # Draw TV screen
    padding = size // 5
    screen_width = size - 2 * padding
    screen_height = int(screen_width * 0.6)
    screen_x = padding
    screen_y = padding

    # Draw TV frame
    draw.rounded_rectangle(
        [(screen_x, screen_y), (screen_x + screen_width, screen_y + screen_height)],
        radius=size//20,
        fill=ACCENT_COLOR,
        outline=BACKGROUND_COLOR,
        width=max(1, size//50)
    )

    # Draw TV screen (inner)
    inner_padding = size // 25
    draw.rounded_rectangle(
        [(screen_x + inner_padding, screen_y + inner_padding),
         (screen_x + screen_width - inner_padding, screen_y + screen_height - inner_padding)],
        radius=size//25,
        fill=BACKGROUND_COLOR
    )

    # Draw stand
    stand_width = screen_width // 3
    stand_height = size // 10
    stand_x = screen_x + (screen_width - stand_width) // 2
    stand_y = screen_y + screen_height + size // 30

    draw.rectangle(
        [(stand_x, stand_y), (stand_x + stand_width, stand_y + stand_height)],
        fill=ACCENT_COLOR,
        outline=BACKGROUND_COLOR,
        width=max(1, size//50)
    )

    # Apply rounded mask if requested
    if rounded:
        mask = create_rounded_rectangle_mask((size, size), size//8)
        output = Image.new('RGBA', (size, size), (0, 0, 0, 0))
        output.paste(img, (0, 0), mask)
        return output

    return img

def create_banner(width, height):
    """Create a TV banner"""
    img = Image.new('RGBA', (width, height), BACKGROUND_COLOR)
    draw = ImageDraw.Draw(img)

    # Draw gradient overlay
    for y in range(height):
        ratio = y / height
        alpha = int(30 * (1 - ratio))
        for x in range(width):
            r, g, b, _ = img.getpixel((x, y))
            new_r = min(255, r + alpha)
            new_g = min(255, g + alpha)
            new_b = min(255, b + alpha)
            img.putpixel((x, y), (new_r, new_g, new_b, 255))

    # Draw TV icon on the left
    icon_size = int(height * 0.6)
    icon_x = int(width * 0.05)
    icon_y = (height - icon_size) // 2

    # Create small TV icon for banner
    tv_icon = create_icon(icon_size, rounded=False)

    # Paste icon with transparency
    img.paste(tv_icon, (icon_x, icon_y), tv_icon)

    # Draw text
    try:
        # Try to use a default font
        font_size = height // 4
        font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", font_size)
    except:
        # Fallback to default
        font = ImageFont.load_default()

    text = "TV App"
    text_x = icon_x + icon_size + int(width * 0.05)
    text_y = height // 2

    # Draw text with shadow
    shadow_offset = max(2, height // 80)
    draw.text((text_x + shadow_offset, text_y + shadow_offset), text,
              font=font, fill=(0, 0, 0, 128), anchor="lm")
    draw.text((text_x, text_y), text, font=font, fill=ACCENT_COLOR, anchor="lm")

    # Draw decorative elements
    line_y = int(height * 0.75)
    line_start_x = text_x
    line_end_x = int(width * 0.9)
    draw.line([(line_start_x, line_y), (line_end_x, line_y)],
              fill=PRIMARY_COLOR, width=max(2, height//60))

    return img

def main():
    print("Generating TV app icons and banner...")

    # Define Android icon sizes
    android_sizes = {
        'mdpi': 48,
        'hdpi': 72,
        'xhdpi': 96,
        'xxhdpi': 144,
        'xxxhdpi': 192
    }

    # Create Android icons
    for density, size in android_sizes.items():
        output_dir = f'android/app/src/main/res/mipmap-{density}'
        os.makedirs(output_dir, exist_ok=True)

        # Create square icon
        icon = create_icon(size, rounded=False)
        icon.save(f'{output_dir}/ic_launcher.png', 'PNG', optimize=True)
        print(f"Created {output_dir}/ic_launcher.png ({size}x{size})")

        # Create round icon
        round_icon = create_icon(size, rounded=True)
        round_icon.save(f'{output_dir}/ic_launcher_round.png', 'PNG', optimize=True)
        print(f"Created {output_dir}/ic_launcher_round.png ({size}x{size})")

    # Create Android TV banner (320x180 recommended, but 400x240 is also common)
    banner = create_banner(400, 240)
    drawable_dir = 'android/app/src/main/res/drawable'
    os.makedirs(drawable_dir, exist_ok=True)
    banner.save(f'{drawable_dir}/tv_banner.png', 'PNG', optimize=True)
    print(f"Created {drawable_dir}/tv_banner.png (400x240)")

    # Create iOS icons
    ios_sizes = [20, 29, 40, 60, 76, 83.5, 1024]
    ios_dir = 'ios/tv/Images.xcassets/AppIcon.appiconset'

    if os.path.exists(ios_dir):
        for size in ios_sizes:
            # iOS needs exact pixel sizes
            pixel_sizes = [int(size), int(size * 2), int(size * 3)]
            for pixel_size in pixel_sizes[:2]:  # Most common are @1x and @2x
                if pixel_size <= 1024:
                    icon = create_icon(pixel_size, rounded=False)
                    scale = pixel_size // size
                    icon.save(f'{ios_dir}/icon_{size}x{size}@{scale}x.png', 'PNG', optimize=True)
                    print(f"Created {ios_dir}/icon_{size}x{size}@{scale}x.png ({pixel_size}x{pixel_size})")

    print("\nAll icons and banner generated successfully!")
    print("\nAndroid assets:")
    print("  - Icons: android/app/src/main/res/mipmap-*/ic_launcher*.png")
    print("  - Banner: android/app/src/main/res/drawable/tv_banner.png")
    if os.path.exists(ios_dir):
        print("\niOS assets:")
        print(f"  - Icons: {ios_dir}/icon_*.png")

if __name__ == '__main__':
    main()
