#include <aubio/aubio.h>
#include <stdlib.h>
#include <stdio.h>
#include <math.h>

// Global variables for pitch detection
aubio_pitch_t *pitch_detector = NULL;
fvec_t *pitch_vector = NULL;
const float amplitude_threshold = 0.001;
void InitPitchDetector(uint_t buffer_size, uint_t hop_size, uint_t sample_rate) {
    pitch_detector = new_aubio_pitch("default", buffer_size, hop_size, sample_rate);
    aubio_pitch_set_unit(pitch_detector, "Hz");  // Set output unit to Hz
    aubio_pitch_set_silence(pitch_detector, -40.0f);  // Set silence threshold
    pitch_vector = new_fvec(1);  // Allocate pitch vector (1 sample for pitch)
}

float DetectPitch(float *audio_buffer, uint_t buffer_size) {
    fvec_t *input_vector = new_fvec(buffer_size);

    // Copy audio data into the input vector
    for (uint_t i = 0; i < buffer_size; i++) {
        input_vector->data[i] = audio_buffer[i];
    }

    // Perform pitch detection
    aubio_pitch_do(pitch_detector, input_vector, pitch_vector);

    // Extract the pitch value
    float pitch = fvec_get_sample(pitch_vector, 0);

    // Calculate semitones from A4
    float semitones = 0.0;
    if (pitch > 0.0) { // Avoid log errors for zero or negative pitch
        semitones = 12.0 * log2(pitch / 440.0);
    } else {
        semitones = -40.0;
        fprintf(stderr, "Pitch too low or not detected.\n");
    }

    // Log the detected pitch and its corresponding semitones
    fprintf(stderr, "Detected pitch: %.2f Hz, Semitones from A4: %.2f\n", pitch, semitones);

    // Clean up the input vector
    del_fvec(input_vector);

    return semitones;
}


void CleanupPitchDetector() {
    del_aubio_pitch(pitch_detector);
    del_fvec(pitch_vector);
    aubio_cleanup();  // Cleans up all allocated Aubio resources
}
