#!/usr/bin/env python3
"""
Generate static PNG test fixtures for texture test suite.

This script creates solid color PNG files that are used by SharedTextureTestFixtures
and FitTextureSizeWindowTests.

Usage:
    python scripts/generate-test-textures.py           # Generate base textures only
    python scripts/generate-test-textures.py --extended  # Generate base + extended textures

Output:
    Tests/Editor/TestAssets/Textures/
        Base textures:
            solid_300x100_magenta.png
            solid_128x128_white.png
            solid_256x256_cyan.png
            solid_64x64_red.png
            solid_384x10_blue.png
            solid_512x512_gray.png

        Extended textures (with --extended flag):
            solid_1x1_black.png
            solid_2x2_black.png
            solid_32x32_black.png
            solid_1024x1024_black.png
            solid_2048x2048_black.png
            solid_4096x4096_black.png
            solid_257x64_gray.png
            solid_255x255_gray.png
            solid_513x400_gray.png
            solid_511x511_gray.png
            solid_1x512_yellow.png
            solid_512x1_yellow.png
            solid_100x200_green.png
            solid_400x240_blue.png
            solid_450x254_blue.png
"""

import argparse
import os
import random
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    print("PIL/Pillow not found. Installing...")
    import subprocess
    subprocess.check_call(["pip", "install", "Pillow"])
    from PIL import Image


def generate_guid() -> str:
    """Generate a 32-character lowercase hex GUID for Unity meta files."""
    return ''.join(random.choices('0123456789abcdef', k=32))


def create_solid_texture(
    width: int,
    height: int,
    color: tuple,
    output_path: str
) -> None:
    """Create a solid color texture."""
    image = Image.new("RGBA", (width, height), color)
    image.save(output_path, "PNG")
    print(f"Created: {output_path}")


def generate_texture_meta_file(output_path: str) -> None:
    """Generate a Unity meta file for a texture (Default type, not sprite)."""
    guid = generate_guid()

    meta_content = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 1
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 3
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 3
    buildTarget: WebGL
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""

    with open(output_path, 'w') as f:
        f.write(meta_content)
    print(f"Created: {output_path}")


def generate_folder_meta_file(output_path: str) -> None:
    """Generate a Unity meta file for a folder."""
    guid = generate_guid()

    meta_content = f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

    with open(output_path, 'w') as f:
        f.write(meta_content)
    print(f"Created: {output_path}")


def main():
    parser = argparse.ArgumentParser(
        description='Generate static PNG test fixtures for texture test suite.'
    )
    parser.add_argument(
        '--extended',
        action='store_true',
        help='Generate extended texture set (15 additional textures)'
    )
    args = parser.parse_args()

    script_dir = Path(__file__).parent.parent
    output_dir = script_dir / "Tests" / "Editor" / "TestAssets" / "Textures"

    os.makedirs(output_dir, exist_ok=True)
    print(f"Output directory: {output_dir}")

    # Define base textures: (name, width, height, color_rgba)
    textures = [
        ("solid_300x100_magenta", 300, 100, (255, 0, 255, 255)),   # Magenta
        ("solid_128x128_white", 128, 128, (255, 255, 255, 255)),   # White
        ("solid_256x256_cyan", 256, 256, (0, 255, 255, 255)),      # Cyan
        ("solid_64x64_red", 64, 64, (255, 0, 0, 255)),             # Red
        ("solid_384x10_blue", 384, 10, (0, 0, 255, 255)),          # Blue
        ("solid_512x512_gray", 512, 512, (128, 128, 128, 255)),    # Gray
    ]

    # Extended textures for comprehensive dimension coverage
    extended_textures = [
        ("solid_1x1_black", 1, 1, (0, 0, 0, 255)),                 # Minimum edge case
        ("solid_2x2_black", 2, 2, (0, 0, 0, 255)),                 # Very small POT
        ("solid_32x32_black", 32, 32, (0, 0, 0, 255)),             # Min clamp default
        ("solid_1024x1024_black", 1024, 1024, (0, 0, 0, 255)),     # Large POT
        ("solid_2048x2048_black", 2048, 2048, (0, 0, 0, 255)),     # Very large POT
        ("solid_4096x4096_black", 4096, 4096, (0, 0, 0, 255)),     # Maximum POT
        ("solid_257x64_gray", 257, 64, (128, 128, 128, 255)),      # Just over 256 boundary
        ("solid_255x255_gray", 255, 255, (128, 128, 128, 255)),    # Just under 256
        ("solid_513x400_gray", 513, 400, (128, 128, 128, 255)),    # Just over 512
        ("solid_511x511_gray", 511, 511, (128, 128, 128, 255)),    # Just under 512
        ("solid_1x512_yellow", 1, 512, (255, 255, 0, 255)),        # Tall strip
        ("solid_512x1_yellow", 512, 1, (255, 255, 0, 255)),        # Wide strip
        ("solid_100x200_green", 100, 200, (0, 255, 0, 255)),       # Portrait NPOT
        ("solid_400x240_blue", 400, 240, (0, 0, 255, 255)),        # Bug case dimension
        ("solid_450x254_blue", 450, 254, (0, 0, 255, 255)),        # Reported bug case
    ]

    if args.extended:
        textures.extend(extended_textures)
        print("Including extended texture set...")

    # Generate folder meta file if it doesn't exist
    folder_meta_path = str(output_dir) + ".meta"
    if not os.path.exists(folder_meta_path):
        generate_folder_meta_file(folder_meta_path)

    for name, width, height, color in textures:
        png_path = str(output_dir / f"{name}.png")
        meta_path = str(output_dir / f"{name}.png.meta")

        create_solid_texture(width, height, color, png_path)
        generate_texture_meta_file(meta_path)

    print("\nAll texture files created successfully!")
    print(f"\nFiles in {output_dir}:")
    for f in sorted(output_dir.glob("*.png")):
        print(f"  - {f.name}")


if __name__ == "__main__":
    main()
