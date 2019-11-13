from PIL import Image, ImageFile

import sys
import os
import shutil

ImageFile.LOAD_TRUNCATED_IMAGES = True

INPUT_PATH = os.getenv("IMAGE_INPUT_PATH")

OUTPUT_FOLDER = "clean"
CORRUPTED_FOLDER = "corrupted"
RAW_FOLDER = "raw"


class UnmarkShutterStock:
    def file_iter(self, path, ext):
        """ Iterate throught files with given extension in given directory. """
        for root, dirs, files in os.walk(path):
            for filename in files:
                if filename.endswith(ext):
                    yield os.path.join(root, filename)

    def execute(self, args):
        if INPUT_PATH is None:
            print(f"Invalid IMAGE_INPUT_PATH value '{INPUT_PATH}' received. Aborting.")
            return

        OUTPUT_PATH = os.path.join(os.getcwd(), INPUT_PATH, OUTPUT_FOLDER)
        CORRUPTED_PATH = os.path.join(os.getcwd(), INPUT_PATH, CORRUPTED_FOLDER)
        RAW_PATH = os.path.join(os.getcwd(), INPUT_PATH, RAW_FOLDER)

        try:
            # Create a folder to store the images into
            if not os.path.exists(OUTPUT_PATH):
                os.mkdir(OUTPUT_PATH)

            # Segregate corrupted images into a specific folder
            if not os.path.exists(CORRUPTED_PATH):
                os.mkdir(CORRUPTED_PATH)

            # Process every file
            for filepath_src in self.file_iter(RAW_PATH, ".jpg"):
                filepath_dst = os.path.splitext(filepath_src)[0] + ".jpg"
                filepath_dst = filepath_dst.replace(RAW_FOLDER, OUTPUT_FOLDER)
                # print(filepath_dst)

                print(f"Processing: {filepath_src}")

                try:
                    # Load image
                    image_obj = Image.open(filepath_src)
                except Exception as e:
                    print(f"Error: could not open image: {filepath_src}.")
                    # print(e)

                    # Move file into 'corrupted' folder
                    shutil.move(filepath_src, filepath_src.replace(RAW_FOLDER, CORRUPTED_FOLDER))

                    # Ignore errors for invalid images
                    continue

                # Get image dimensions
                img_width, img_height = image_obj.size

                # Remove pixels from bottom of the image
                cropped_image = image_obj.crop((0, 0, img_width, img_height - 20))
                cropped_image.save(filepath_dst)

        except Exception as e:
            print("Error: could not crop image.")
            print(e)


if __name__ == "__main__":
    from sys import argv

    try:
        app = UnmarkShutterStock()
        app.execute(argv)
    except KeyboardInterrupt:
        pass

    sys.exit()

