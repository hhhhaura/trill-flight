import numpy as np
import sounddevice as sd
import socket
import aubio
from collections import deque

# UDP Configuration
UDP_IP = "127.0.0.1"
UDP_PORT = 5005
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Audio Configuration
SAMPLE_RATE = 44100
BUFFER_SIZE = 512  # Input buffer size
HOP_SIZE = BUFFER_SIZE  # Hop size for aubio
PITCH_RANGE = (98.0, 1568.0)  # G2 to G6
A4_FREQUENCY = 440.0
AMPLITUDE_THRESHOLD = 0.001  # Minimum RMS amplitude
MAX_HISTORY = 10  # Number of recent pitch values to store
MAX_DEVIATION = 6  # Maximum allowed deviation in semitones

# Initialize aubio pitch detection
pitch_detector = aubio.pitch("default", buf_size=BUFFER_SIZE,  hop_size=HOP_SIZE, samplerate=SAMPLE_RATE)
pitch_detector.set_unit("Hz")
pitch_detector.set_silence(-40)

# Initialize pitch history
pitch_history = deque(maxlen=MAX_HISTORY)

def process_audio(indata, frames, time, status):
    global pitch_history

    if status:
        print(f"Status: {status}")

    # Convert audio to mono if stereo
    audio_data = np.mean(indata, axis=1)

    # Ensure the buffer size matches the expected size for aubio
    if len(audio_data) != BUFFER_SIZE:
        print(f"Skipping frame with unexpected buffer size: {len(audio_data)}")
        return

    # Calculate RMS amplitude
    rms_amplitude = np.sqrt(np.mean(audio_data**2))
    if rms_amplitude < AMPLITUDE_THRESHOLD:
        print(f"Amplitude too low (RMS: {rms_amplitude:.5f}), skipping analysis.")
        return

    # Detect pitch
    pitch = pitch_detector(audio_data.astype(np.float32))[0]
    if pitch < PITCH_RANGE[0] or pitch > PITCH_RANGE[1]:
        print(f"Pitch {pitch:.2f} Hz out of range ({PITCH_RANGE[0]}-{PITCH_RANGE[1]} Hz), ignoring.")
        return

    # Convert pitch to semitones
    semitones = 12 * np.log2(pitch / A4_FREQUENCY)

    # Check for outliers based on history
    if len(pitch_history) > 0:
        avg_semitones = np.mean(pitch_history)
        if abs(semitones - avg_semitones) > MAX_DEVIATION:
            print(f"Pitch {semitones:.2f} semitones deviates too much from history ({avg_semitones:.2f}), ignoring.")
            return

    # Update pitch history
    pitch_history.append(semitones)
    average = sum(pitch_history) / len(pitch_history)
    print(average)

    # Send pitch to Unity
    message = f"{semitones:.2f}"
    message = f"{average:.2f}"
    sock.sendto(message.encode(), (UDP_IP, UDP_PORT))
    print(f"Pitch: {pitch:.2f} Hz, Semitones: {semitones:.2f}, RMS: {rms_amplitude:.5f}")
    print(f"Pitch history: {list(pitch_history)}")

# Start the audio stream
try:
    print(f"Sending pitch data to {UDP_IP}:{UDP_PORT}. Press Ctrl+C to stop.")
    with sd.InputStream(callback=process_audio, channels=1, samplerate=SAMPLE_RATE, blocksize=BUFFER_SIZE):
        while True:
            pass
except KeyboardInterrupt:
    print("\nStopped by user.")
except Exception as e:
    print(f"Error: {e}")
