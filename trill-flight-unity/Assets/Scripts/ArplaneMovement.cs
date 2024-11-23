using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class AirplaneControlWithPropeller : MonoBehaviour
{
    [Header("Airplane Settings")]
    public float speed = 0f;          // Forward speed of the airplane
    public float pitchSensitivity = 0.1f; // How sensitive the airplane is to pitch changes
    private float targetPitch = 100f;

    [Header("Propeller Settings")]
    public Transform propeller;       // Reference to the propeller Transform
    public float maxSpinSpeed = 10000f; // Maximum spin speed for the propeller
    private bool trillState = false; // Lip trill intensity (0 to 1)

    private UDPReceiver pitchReceiver;
    private UDPReceiver trillReceiver;

    void Start()
    {
        // Initialize UDP listeners
        pitchReceiver = new UDPReceiver(5005, ReceivePitchData);
        trillReceiver = new UDPReceiver(5007, ReceiveTrillData);
    }

    void Update()
    {
        // Handle airplane movement
        float verticalInput = Mathf.Lerp(transform.position.y, targetPitch * pitchSensitivity, Time.deltaTime);
        Debug.Log($"Target pitch: {targetPitch}, position: {transform.position.y}, vertical: {verticalInput}");
        transform.position = new Vector3(transform.position.x, verticalInput, transform.position.z + speed * Time.deltaTime);

        // Handle propeller spinning
        float spinSpeed = (trillState? 1.0f : 0.0f) * maxSpinSpeed;
        propeller.Rotate(Vector3.forward, spinSpeed * Time.deltaTime);

        Debug.Log($"Lip Trill State: {trillState}");
    }

    void ReceivePitchData(string message)
    {
        try
        {
            if (float.TryParse(message, out float receivedPitch))
            {
                targetPitch = Mathf.Clamp(receivedPitch + 50, 0, 150); // Restrict pitch range
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Pitch UDP Receive Error: {ex.Message}");
        }
    }

    void ReceiveTrillData(string message)
    {
        try
        {
            trillState = message.Trim() == "1";
        }
        catch (Exception ex)
        {
            Debug.LogError($"Trill UDP Receive Error: {ex.Message}");
        }
    }

    void OnApplicationQuit()
    {
        pitchReceiver.Stop();
        trillReceiver.Stop();
    }
}
