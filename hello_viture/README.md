# Viture SDK Hello World (Linux x86_64)

This is a minimal "Hello World" example for the Viture XR Glasses SDK on Linux.

## Prerequisites

-   Linux x86_64 system.
-   `g++` compiler.
-   Viture SDK (located in `../Viture/Linux_x86_64/release`).
-   `libusb-1.0` (provided with SDK).
-   `libudev` (system library).

## Structure

-   `main.cpp`: The source code.
-   `CMakeLists.txt`: CMake configuration (optional, if you have cmake).

## Building manually with g++

The SDK libraries have dependencies on OpenCV and other libs provided in the SDK folder. You need to link them all.

Run the following command from the parent directory of `hello_viture` (or adjust paths):

```bash
g++ -o hello_viture/hello_viture hello_viture/main.cpp \
    -I Viture/Linux_x86_64/release/include \
    -L Viture/Linux_x86_64/release/x86_64 \
    -lglasses \
    -lcarina_vio \
    -lopencv_core \
    -lopencv_imgproc \
    -lopencv_highgui \
    -lopencv_imgcodecs \
    -lopencv_features2d \
    -lopencv_calib3d \
    -lopencv_video \
    -lopencv_videoio \
    -lopencv_flann \
    -lusb-1.0 \
    -ludev \
    -Wl,-rpath,'$ORIGIN/../Viture/Linux_x86_64/release/x86_64'
```

**Note:** You might need to adjust the path to `libudev` if your system doesn't have it in the standard search path.

## Running

1.  Make sure your Viture glasses are connected via USB.
2.  Ensure you have appropriate permissions (udev rules) to access the USB device.
3.  Run the executable:

```bash
./hello_viture/hello_viture
```

If no Product ID is provided as an argument, it will scan for valid Viture Product IDs and attempt to initialize the first one found.

You can also provide a specific Product ID (in hex or decimal):

```bash
./hello_viture/hello_viture 0x1011
```

## Troubleshooting

-   **"No HID devices found"**: Ensure the glasses are connected and you have permissions (check `dmesg` or `lsusb`).
-   **"error while loading shared libraries"**: Ensure the `rpath` is set correctly or `LD_LIBRARY_PATH` includes the SDK library directory.
