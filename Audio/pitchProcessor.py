import numpy as np
import sounddevice as sd
import socket

# UDP Configuration
UDP_IP = "127.0.0.1"  # Replace with Unity's IP if needed
UDP_PORT = 5005       # Match Unity's port
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Audio Configuration
SAMPLE_RATE = 44100  # Standard audio sampling rate
A4_FREQUENCY = 440.0  # Reference frequency for A4

def process_audio(indata, frames, time, status):
    if status:
        print(f"Status: {status}")

    # Extract pitch from the audio signal
    audio_data = np.frombuffer(indata, dtype=np.float32)
    fft = np.fft.rfft(audio_data)
    freqs = np.fft.rfftfreq(len(audio_data), 1 / SAMPLE_RATE)
    pitch_idx = np.argmax(np.abs(fft))  # Find the dominant frequency
    pitch = freqs[pitch_idx]

    # Convert pitch to semitones relative to A4
    if pitch > 0:  # Avoid invalid log for zero or negative frequencies
        semitones = 12 * np.log2(pitch / A4_FREQUENCY)
        print(f"Real time pitch: {pitch:.2f} Hz, Semitones from A4: {semitones:.2f}")
    else:
        semitones = -np.inf  # Indicate no valid pitch detected
        print("No valid pitch detected.")

    # Send pitch or semitone to Unity
    message = f"{semitones:.2f}" if pitch > 0 else "0"
    sock.sendto(message.encode(), (UDP_IP, UDP_PORT))

# Start the audio stream
try:
    print(f"Sending pitch data to {UDP_IP}:{UDP_PORT}. Press Ctrl+C to stop.")
    with sd.InputStream(channels=1, samplerate=SAMPLE_RATE, callback=process_audio):
        while True:
            pass
except KeyboardInterrupt:
    print("\nStopped by user.")
except Exception as e:
    print(f"Error: {e}")

