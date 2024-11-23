using System;
using System.Diagnostics; // For starting Python scripts
using System.Runtime.InteropServices; // For DllImport
using UnityEngine;

public class AirplaneControlWithPropeller : MonoBehaviour
{
    [Header("Airplane Settings")]
    public float speed = 0f;          // Forward speed of the airplane
    public float pitchSensitivity = 3f; // How sensitive the airplane is to pitch changes
    private float targetPitch = 100f;
    public float gravity = 9.8f;          // Gravitational acceleration (m/s^2)
    public float groundLevel = 0f;        // Ground height
    private float verticalVelocity = 0f;
    private float lerpSpeed = 50f;
    private float heightOffset = 40f;

    [Header("Propeller Settings")]
    public Transform propeller;       // Reference to the propeller Transform
    public float maxSpinSpeed = 10000f; // Maximum spin speed for the propeller
    private bool trillState = false;  // Lip trill intensity (0 to 1)

    [Header("Audio Processing")]
    private Process pythonProcess; // Process to handle the Python script
    private UDPReceiver trillReceiver; // Keeps the trillReceiver logic
    private float[] audioData;

    [Header("Audio Settings")]
    public uint sampleRate = 44100;  // Audio sample rate
    public uint bufferSize = 2048;   // Buffer size for pitch detection
    private AudioClip microphoneClip;
    private bool isMicrophoneActive = false;

    void Start() {
        // Start the Python script for trillReceiver
        //StartPythonTrillReceiver();

        // Initialize the trillReceiver UDP listener
        trillReceiver = new UDPReceiver(5007, ReceiveTrillData);
        AubioWrapper.Initialize(bufferSize, bufferSize/2, sampleRate);
        StartMicrophone();
    }

    void StartMicrophone() {
        if (Microphone.devices.Length > 0)
        {
            string micName = Microphone.devices[0];
            microphoneClip = Microphone.Start(micName, true, 1, (int)sampleRate);
            isMicrophoneActive = true;
            UnityEngine.Debug.Log($"Microphone started: {micName}");
        }
        else
        {
            UnityEngine.Debug.LogError("No microphone detected!");
        }
    }

    void Update() {
        // Use aubio_get_pitch to detect pitch and update targetPitch
        DetectPitch();

        // Handle airplane movement
        if (trillState)
        {
            float verticalInput = Mathf.Lerp(
                transform.position.y,
                targetPitch * pitchSensitivity,
                Time.deltaTime * lerpSpeed // Increase interpolation speed
            );
            UnityEngine.Debug.Log($"Target pitch: {targetPitch - heightOffset}, position: {transform.position.y}, vertical: {verticalInput}");

            transform.position = new Vector3(
                transform.position.x,
                verticalInput,
                transform.position.z + speed * Time.deltaTime
            );

            // Reset vertical velocity if trill is active
            verticalVelocity = 0f;
        }
        else
        {
            // Simulate freefall when not trilling
            verticalVelocity -= gravity * Time.deltaTime;
            float newY = transform.position.y + verticalVelocity * Time.deltaTime;

            // Prevent the airplane from going below the ground
            if (newY < groundLevel)
            {
                newY = groundLevel;
                verticalVelocity = 0f; // Reset velocity when hitting the ground
            }

            transform.position = new Vector3(
                transform.position.x,
                newY,
                transform.position.z + speed * Time.deltaTime
            );

            UnityEngine.Debug.Log($"Freefall - Position: {transform.position.y}, Velocity: {verticalVelocity}");
        }

        // Handle propeller spinning
        float spinSpeed = (trillState ? 1.0f : 0.0f) * maxSpinSpeed;
        propeller.Rotate(Vector3.forward, spinSpeed * Time.deltaTime);

        UnityEngine.Debug.Log($"Lip Trill State: {trillState}");
    }

    void DetectPitch()
    {
        if (!isMicrophoneActive || microphoneClip == null)
            return;

        // Ensure microphone has started
        int micPosition = (int)Microphone.GetPosition(null) - (int)bufferSize;
        if (micPosition < 0)
            return;

        // Retrieve audio data from the microphone
        audioData = new float[bufferSize];
        microphoneClip.GetData(audioData, micPosition);

        float pitch = AubioWrapper.GetPitch(audioData);
        UnityEngine.Debug.Log($"Detected pitch: {pitch}");
        targetPitch = Mathf.Clamp(pitch + heightOffset, 0, 150); // Restrict pitch range
    }

    void ReceiveTrillData(string message)
    {
        try {
            trillState = message.Trim() == "1";
        }
        catch (Exception ex) {
            UnityEngine.Debug.LogError($"Trill UDP Receive Error: {ex.Message}");
        }
    }

    void StartPythonTrillReceiver() {
        try {
            pythonProcess = new Process();
            pythonProcess.StartInfo.FileName = "python3";
            pythonProcess.StartInfo.Arguments = "Audio/trillReceiver.py"; // Ensure the script is in the working directory
            pythonProcess.StartInfo.CreateNoWindow = true;
            pythonProcess.StartInfo.UseShellExecute = false;
            pythonProcess.StartInfo.RedirectStandardOutput = true;
            pythonProcess.StartInfo.RedirectStandardError = true;
            pythonProcess.Start();

            UnityEngine.Debug.Log("Started trillReceiver Python script.");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to start Python script: {ex.Message}");
        }
    }

    void OnApplicationQuit()
    {
        // Kill the Python script process
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            pythonProcess.Kill();
            UnityEngine.Debug.Log("Stopped trillReceiver Python script.");
        }

        trillReceiver.Stop();
        AubioWrapper.CleanUp();
    }
}
