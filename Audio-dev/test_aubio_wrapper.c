#include <stdio.h>
#include <stdlib.h>
#include <dlfcn.h>

// Function pointers
void (*InitPitchDetector)(unsigned int, unsigned int, unsigned int);
float (*DetectPitch)(float*, uint32_t);
void (*CleanupPitchDetector)();

void load_library(const char* lib_path) {
    void* handle = dlopen(lib_path, RTLD_NOW);
    if (!handle) {
        fprintf(stderr, "Error loading library: %s\n", dlerror());
        exit(EXIT_FAILURE);
    }

    InitPitchDetector = dlsym(handle, "InitPitchDetector");
    if (!InitPitchDetector) {
        fprintf(stderr, "Failed to resolve InitPitchDetector: %s\n", dlerror());
        dlclose(handle);
        exit(EXIT_FAILURE);
    }

    DetectPitch = dlsym(handle, "DetectPitch");
    if (!DetectPitch) {
        fprintf(stderr, "Failed to resolve DetectPitch: %s\n", dlerror());
        dlclose(handle);
        exit(EXIT_FAILURE);
    }

    CleanupPitchDetector = dlsym(handle, "CleanupPitchDetector");
    if (!CleanupPitchDetector) {
        fprintf(stderr, "Failed to resolve CleanupPitchDetector: %s\n", dlerror());
        dlclose(handle);
        exit(EXIT_FAILURE);
    }
}

int main() {
    const char* lib_path = "./libaubio_wrapper.dylib";
    load_library(lib_path);

    unsigned int buffer_size = 1024;
    unsigned int hop_size = 512;
    unsigned int sample_rate = 44100;

    InitPitchDetector(buffer_size, hop_size, sample_rate);
    printf("Pitch detector initialized.\n");

    float test_audio[1024] = {0.0f};
    float pitch = DetectPitch(test_audio, 1024);
    printf("Detected pitch: %.2f Hz\n", pitch);

    CleanupPitchDetector();
    printf("Pitch detector cleaned up.\n");

    return 0;
}
