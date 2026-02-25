#include <iostream>
#include <vector>
#include <string>
#include <thread>
#include <chrono>
#include <iomanip>

#include "viture_glasses_provider.h"
#include "viture_device.h"
#include "viture_protocol_public.h"

// Helper to print hex
struct Hex {
    int value;
    Hex(int v) : value(v) {}
};
std::ostream& operator<<(std::ostream& os, const Hex& h) {
    return os << "0x" << std::hex << std::setw(4) << std::setfill('0') << h.value << std::dec;
}

int main(int argc, char** argv) {
    std::cout << "Viture SDK Hello World" << std::endl;

    int productId = 0;

    if (argc > 1) {
        productId = std::stoi(argv[1], nullptr, 0); // Handles hex (0x...) or dec
        std::cout << "Using provided Product ID: " << Hex(productId) << std::endl;
    } else {
        std::cout << "No Product ID provided. Scanning for valid Viture Product IDs..." << std::endl;
        // Scan a reasonable range or all uint16
        // Viture PIDs are likely in a specific range, but let's scan all to be safe and thorough.
        // This check is fast as it's just a lookup in the SDK, not a USB probe.
        for (int i = 0; i <= 0xFFFF; ++i) {
            if (xr_device_provider_is_product_id_valid(i)) {
                std::cout << "Found valid Product ID: " << Hex(i) << std::endl;
                char marketName[256] = {0};
                int length = 256;
                if (xr_device_provider_get_market_name(i, marketName, &length) == 0) {
                     std::cout << "  Market Name: " << marketName << std::endl;
                }
                if (productId == 0) productId = i; // Pick the first one
            }
        }
    }

    if (productId == 0) {
        std::cerr << "No valid Product ID found or provided." << std::endl;
        return 1;
    }

    std::cout << "Attempting to initialize device with Product ID: " << Hex(productId) << std::endl;

    XRDeviceProviderHandle handle = xr_device_provider_create(productId);
    if (!handle) {
        std::cerr << "Failed to create device provider handle." << std::endl;
        return 1;
    }

    // Set log level to Info
    xr_device_provider_set_log_level(2);

    int ret = xr_device_provider_initialize(handle, nullptr, nullptr);
    if (ret != 0) {
        std::cerr << "Failed to initialize device provider. Error code: " << ret << std::endl;
        std::cerr << "Ensure the glasses are connected and you have permissions (udev rules)." << std::endl;
        xr_device_provider_destroy(handle);
        return 1;
    }

    std::cout << "Device initialized successfully!" << std::endl;

    int deviceType = xr_device_provider_get_device_type(handle);
    std::cout << "Device Type: " << deviceType << std::endl;
    
    // Get version
    char version[256] = {0};
    int verLength = 256;
    ret = xr_device_provider_get_glasses_version(handle, version, &verLength);
    if (ret == 0) {
        std::cout << "Firmware Version: " << version << std::endl;
    } else {
        std::cerr << "Failed to get firmware version. Error: " << ret << std::endl;
    }

    // Basic interaction: Get brightness
    int brightness = xr_device_provider_get_brightness_level(handle);
    if (brightness >= 0) {
        std::cout << "Current Brightness: " << brightness << std::endl;
    } else {
        std::cerr << "Failed to get brightness. Error: " << brightness << std::endl;
    }

    std::cout << "Shutting down..." << std::endl;
    xr_device_provider_shutdown(handle);
    xr_device_provider_destroy(handle);

    std::cout << "Done." << std::endl;

    return 0;
}
