from PIL import Image, ImageFile, ImageCms, features

import io
import os
import sys
import shutil

ImageFile.LOAD_TRUNCATED_IMAGES = True

INPUT_PATH = os.getenv("IMAGE_INPUT_PATH")

RAW_FOLDER = "raw"
CLEAN_FOLDER = "clean"
OUTPUT_FOLDER = "converted"
CORRUPTED_FOLDER = "corrupted"


class ConvertToJpeg:
    def file_iter(self, path, ext):
        """ Iterate throught files with given extension in given directory. """
        for root, dirs, files in os.walk(path):
            for filename in files:
                if filename.endswith(ext):
                    yield os.path.join(root, filename)

    def convert_to_srgb(self, img):
        """Convert PIL image to sRGB color space (if possible)"""
        icc = img.info.get("icc_profile", "")
        if icc:
            io_handle = io.BytesIO(icc)  # virtual file
            src_profile = ImageCms.ImageCmsProfile(io_handle)
            dst_profile = ImageCms.createProfile("sRGB")
            img = ImageCms.profileToProfile(img, src_profile, dst_profile)
        return img

    def execute(self, args):
        if features.check_module("webp") == False:
            print(
                "Error: The WebP module for Pillow seems to be missing, please make sure to install it."
            )
            return

        if INPUT_PATH is None:
            print(f"Invalid IMAGE_INPUT_PATH value '{INPUT_PATH}' received. Aborting.")
            return

        RAW_PATH = os.path.join(os.getcwd(), INPUT_PATH, CLEAN_FOLDER)
        OUTPUT_PATH = os.path.join(os.getcwd(), INPUT_PATH, OUTPUT_FOLDER)

        try:
            # Create a folder to store the images
            if not os.path.exists(OUTPUT_PATH):
                os.mkdir(OUTPUT_PATH)

            # Create a folder to store the corrupted images
            if not os.path.exists(CORRUPTED_FOLDER):
                os.mkdir(CORRUPTED_FOLDER)

            # Process every file
            for filepath_src in self.file_iter(RAW_PATH, ".jpg"):
                filepath_dst = os.path.splitext(filepath_src)[0] + ".jpg"
                filepath_dst = filepath_dst.replace(CLEAN_FOLDER, OUTPUT_FOLDER)
                # print(filepath_dst)

                print(f"Processing: {filepath_src}")

                try:
                    # Load image
                    image_obj = Image.open(filepath_src)

                    # Convert to sRGB Color Profile
                    img_conv = self.convert_to_srgb(image_obj)

                    if image_obj.info.get("icc_profile", "") != img_conv.info.get(
                        "icc_profile", ""
                    ):
                        # ICC profile was changed -> save converted file
                        img_conv.save(
                            filepath_dst,
                            format="JPEG",
                            quality=100,
                            icc_profile=img_conv.info.get("icc_profile", ""),
                        )
                    else:
                        # No need to change color profile, just save as JPEG
                        img_conv.save(filepath_dst, format="JPEG", quality=100)
                except Exception as e:
                    print(f"Error: could not open image: {filepath_src}.")
                    # print(e)

                    # Move file into 'corrupted' folder
                    shutil.move(
                        filepath_src, filepath_src.replace(RAW_FOLDER, CORRUPTED_FOLDER)
                    )

                    # Ignore errors for invalid images
                    continue

        except Exception as e:
            print("Error: could not crop image.")
            print(e)


if __name__ == "__main__":
    from sys import argv

    try:
        app = ConvertToJpeg()
        app.execute(argv)
    except KeyboardInterrupt:
        pass

    sys.exit()

