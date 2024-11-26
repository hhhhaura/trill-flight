using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Diagnostics;

public class Slime : MonoBehaviour
{
    [Header("Slime Settings")]
    public float speed = 3f;          // Forward speed of the airplane
    public float pitchSensitivity = 3f; // How sensitive the airplane is to pitch changes
    public int L_range_Midi = 53;
    public int H_range_Midi = 80;
    private float targetPitch = 100f;
    private float prePitch = 100f;
    public float gravity = 9.8f;          // Gravitational acceleration (m/s^2)
    public float groundLevel = 0f;        // Ground height
    public float ceilingLevel = 100f;
    private float verticalVelocity = 0f;
    private float lerpSpeed = 50f;
    private float heightOffset = 0f;

    [Header("Rigidbody Settings")]
    private Rigidbody rb;

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


    [Header("board height")]
    public GameObject boardPrefab; // 木板的預製件
    public float boardSpacing = 1f; // 木板之間的間距
    public int startNote = 53; // 開始的 MIDI 音高
    public int endNote = 80; // 結束的 MIDI 音高

    public float baseHeight = 0f; // 最低木板的高度
    public float heightPerSemitone = 0.5f; // 每半音的高度

    private GameObject activeBoard; // 當前活動音板

    [Header("update freq")]

    private Queue<float> pitchHistory = new Queue<float>(); // 保存最近的 pitch 值
    public int updatesPerSecond = 30; // 每秒更新次數
    private float updateInterval; // 每次更新的時間間隔
    private float elapsedTime = 0f; // 累計時間


    private Animator animator; // 角色的 Animator
    private float previousHeight; // 保存上一幀的高度

    void Start()
    {
        trillReceiver = new UDPReceiver(5007, ReceiveTrillData);
        AubioWrapper.Initialize(bufferSize, bufferSize / 2, sampleRate);
        StartMicrophone();
        //GenerateBoards();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        updateInterval = 1f / updatesPerSecond;
    }


    void Update()
    {
        DetectPitch();
        elapsedTime += Time.deltaTime; // 累加時間
        UnityEngine.Debug.Log("targetPitch" + (targetPitch -69));
        // 如果時間達到更新間隔，則更新高度
        if (targetPitch > 0)
        {
            pitchHistory.Enqueue(targetPitch);
            if (pitchHistory.Count > updatesPerSecond) // 限制隊列長度
            {
                pitchHistory.Dequeue();
            }
        }
        if (elapsedTime >= updateInterval)
        {
            UnityEngine.Debug.Log("pitchfistory = " + pitchHistory.Count);
            elapsedTime -= updateInterval; // 重置累計時間

            // 將當前 pitch 添加到歷史記錄中
            

            // 計算平均 pitch
            float averagePitch = CalculateAveragePitch();
            //float averagePitch = targetPitch;
            int targetMidi = Mathf.RoundToInt(averagePitch);

            // 計算高度
            float height = groundLevel + heightOffset +
                           (ceilingLevel - groundLevel - heightOffset) *
                           (targetMidi - L_range_Midi) / (H_range_Midi - L_range_Midi) + 4;

            UnityEngine.Debug.Log("height1 " + height);
            // 確保高度在範圍內
            height = Mathf.Clamp(height, groundLevel, ceilingLevel);
            UnityEngine.Debug.Log("height2 " + height);
            if (targetMidi > 0)
            {
                // 啟動 Walk 動畫
                animator.SetBool("walk", true);
                Vector3 transposition = Vector3.right * speed * Time.deltaTime + rb.position;
                transposition.y = height;
                UnityEngine.Debug.Log("trans_y = " + transposition.y);
                rb.MovePosition(transposition);
                previousHeight = height;
            }
            else
            {
                // 停止 Walk 動畫
                animator.SetBool("walk", false);
            }

            if (Mathf.Abs(height - previousHeight) > 0.1f)
            {
                // 啟動 Jump 動畫
                animator.SetTrigger("jump");
                animator.SetBool("jump", false);
                //UnityEngine.Debug.Log("jump");
            }

            // 更新物體位置
            //Vector3 transposition = Vector3.right * speed * Time.deltaTime + rb.position;
            //transposition.y = height;
            //UnityEngine.Debug.Log("trans_y = " + transposition.y);
            //rb.MovePosition(transposition);
            //previousHeight = height;
        }

    }

    float CalculateAveragePitch()
    {
        if (pitchHistory.Count == 0)
        {
            return prePitch; // 如果沒有值，返回上一個 pitch
        }

        float sum = 0;
        foreach (float pitch in pitchHistory)
        {
            sum += pitch;
        }

        return sum / pitchHistory.Count; // 返回平均值
    }

    // 生成所有音板
    private void GenerateBoards()
    {
        for (int i = startNote; i <= endNote; i++)
        {
            float height = baseHeight + (i - startNote) * heightPerSemitone;
            Vector3 position = new Vector3(transform.position.x, height, transform.position.z + (i - startNote) * boardSpacing);
            Instantiate(boardPrefab, position, Quaternion.identity);
        }
    }


    // 可選：高亮當前音板
    private void HighlightActiveBoard(GameObject board)
    {
        Renderer renderer = board.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red; // 將當前活動音板設置為紅色
        }
    }




    void StartMicrophone()
    {
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
        //targetPitch = Mathf.Clamp(pitch + heightOffset, 0, 150); // Restrict pitch range
        targetPitch = pitch + 69;
    }

    void ReceiveTrillData(string message)
    {
        try
        {
            trillState = message.Trim() == "1";
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Trill UDP Receive Error: {ex.Message}");
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
