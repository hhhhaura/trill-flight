using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Diagnostics; // For starting Python scripts
using System.Runtime.InteropServices; // For DllImport


public class Airplane : MonoBehaviour {

    [Header("Level Information")]
    LevelData.Level.Event[] noteEventList;
    private int curMode = 0;
    private int curEvent = 0;
    private float totalBeat = 0;
    private float baseTime = 0f;
    

    [Header("Airplane Settings")]
    public static float targetPitch = 100f;
    public float pitchSensitivity = 3f; // How sensitive the airplane is to pitch changes
    public float gravity = 9.8f;          // Gravitational acceleration (m/s^2)
    private float verticalVelocity = 0f;
    private float lerpSpeed = 50f;
    private Rigidbody rb;

    [Header("Propeller Settings")]
    public Transform propeller;       // Reference to the propeller Transform
    public float maxSpinSpeed = 10000f; // Maximum spin speed for the propeller
    public static bool trillState = false;  // Lip trill intensity (0 to 1)

    [Header("Audio Processing")]
    private Process pythonProcess; // Process to handle the Python script
    private UDPReceiver trillReceiver; // Keeps the trillReceiver logic
    private float[] audioData;

    [Header("Audio Settings")]
    public uint sampleRate = 44100;  // Audio sample rate
    public uint bufferSize = 2048;   // Buffer size for pitch detection
    private AudioClip microphoneClip;
    private bool isMicrophoneActive = false;


    public enum FlightMode { Manual, Auto };
    public FlightMode currentMode = FlightMode.Manual;

    [Header("Auto Airplane")]
    private List<Vector3> flightPath; // 保存飛行路徑
    private int currentTargetIndex = 0; // 當前目標點索引
    private float delayBeats = 10f;
    public float lerpInterval = 0.3f;
    public bool isCrashed = false;


    [Header("Slime")]
    public GameObject slime;
    public float jump_out_distance = 3;
    public float SlimeDuration;
    void Awake() {
        baseTime = GlobalSettings.curBeat();
    }

    void Start() {
        rb = GetComponent<Rigidbody>();
        trillReceiver = new UDPReceiver(5007, ReceiveTrillData);
        AubioWrapper.Initialize(bufferSize, bufferSize / 2, sampleRate);
        StartMicrophone();
    }

    void StartMicrophone() {
        if (Microphone.devices.Length > 0) {
            string micName = Microphone.devices[0];
            microphoneClip = Microphone.Start(micName, true, 1, (int)sampleRate);
            isMicrophoneActive = true;
            UnityEngine.Debug.Log($"Microphone started: {micName}");
        }
        else UnityEngine.Debug.LogError("No microphone detected!");
    }

    void Update() {
        UnityEngine.Debug.Log("Timescale: " + Time.timeScale);
        if (Time.timeScale <= 1e-6) return;
        // Use aubio_get_pitch to detect pitch and update targetPitch
        float currentBeat = GlobalSettings.curBeat();
        DetectPitch();
        if (curMode != (GlobalSettings.level & 1)) {
            curMode = GlobalSettings.level & 1;
            baseTime = GlobalSettings.curBeat();
            curEvent = 0;
            totalBeat = 0;
            noteEventList = GlobalSettings.levelData.levels[GlobalSettings.level].level;
        }

        // Update currentMode
        if (curMode == 1) currentMode = FlightMode.Auto;
        else currentMode = FlightMode.Manual;

            
        // Handle airplane movement
        if (currentMode == FlightMode.Manual) {
            if (trillState) {
                rb.useGravity = false;
                float height = GlobalSettings.key2height(targetPitch);
                float verticalInput = Mathf.Lerp(
                    transform.position.y,
                    height,
                    Time.deltaTime * lerpSpeed // Increase interpolation speed
                );
                //UnityEngine.Debug.Log($"Target pitch: {targetPitch - GlobalSettings.heightOffset}, position: {transform.position.y}, vertical: {verticalInput}");

                transform.position = GlobalSettings.slideControl.getPosition(currentBeat - baseTime);
                transform.position = new Vector3(transform.position.x, height, transform.position.z);
            } else {
                rb.isKinematic = false;
                rb.useGravity = true;
                Vector3 targetPosition = GlobalSettings.slideControl.getPosition(currentBeat - baseTime);
                rb.position = new Vector3(targetPosition.x, rb.position.y, targetPosition.z);
                rb.rotation = Quaternion.LookRotation(GlobalSettings.slideControl.getForward(currentBeat - baseTime));
            }
            // Handle forward look


            // Handle propeller spinning
            float spinSpeed = (trillState ? 1.0f : 0.0f) * maxSpinSpeed;
            propeller.Rotate(Vector3.forward, spinSpeed * Time.deltaTime);
        }
        else if (currentMode == FlightMode.Auto) {
            if (isCrashed) return;
            float curTime = currentBeat - baseTime - delayBeats;
            propeller.Rotate(Vector3.forward, maxSpinSpeed * Time.deltaTime);
            if (GlobalSettings.stepControl == null) {
                UnityEngine.Debug.Log("no step control");
            }

            transform.position = GlobalSettings.stepControl.getPosition(curTime);
            // Check validity
            while (curTime >= totalBeat + noteEventList[curEvent].eventLength) {
                totalBeat += noteEventList[curEvent].eventLength;
                curEvent += 1;
            }
            
            float height = 0;
            UnityEngine.Debug.Log($"The key is {noteEventList[curEvent].startKey}");
            /*if (curEvent + 1 < noteEventList.Length
                && noteEventList[curEvent].eventLength - (currentBeat - totalBeat) <= lerpInterval) {
                float verticalInput = Mathf.Lerp(
                    transform.position.y,
                    GlobalSettings.key2height(noteEventList[curEvent + 1].startKey),
                    Time.deltaTime * lerpSpeed
                );

            } else */
            height = GlobalSettings.key2height(noteEventList[curEvent].startKey);
            transform.position = new Vector3(transform.position.x, height, transform.position.z);
            // Set forward looking direction
            transform.rotation = Quaternion.LookRotation(GlobalSettings.stepControl.getForward(curTime));

            /* 
            // 飛機移動到下一個目標點
            Vector3 targetPosition = flightPath[currentTargetIndex];
            //targetPosition.y = airplane.transform.position.y;

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                speed * Time.deltaTime
            );
            transform.position = new Vector3(transform.position.x, targetPosition.y, transform.position.z);

            // 當飛機到達目標點時，切換到下一個點
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                currentTargetIndex++;
                if (currentTargetIndex >= flightPath.Count)
                {
                    currentMode = FlightMode.Manual;
                }
                UnityEngine.Debug.Log("currentTargetIndex = " + currentTargetIndex);
            }
            */

        }

        UnityEngine.Debug.Log($"Lip Trill State: {trillState}");
    }

    private void OnTriggerEnter(Collider other) {
        UnityEngine.Debug.Log("trigger: " + other.tag);
        if (other.tag == "Box") {
            UnityEngine.Debug.Log("crash");
            Crash();
        }
        else if (other.tag == "Floor") {
            // Crash and end game
            GlobalSettings.sceneManager.EndGame();
        }

    }
    public float getPitch() {
        return targetPitch;
    }
    public bool getTrillState() {
        return trillState;
    }
    public float getBaseTime() {
        return baseTime;
    }
    public int getCurMode() {
        return curMode;
    }


    void DetectPitch() {
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
        //UnityEngine.Debug.Log($"Detected pitch: {pitch}");
        targetPitch = Mathf.Clamp(pitch + GlobalSettings.heightOffset, 0, 150); // Restrict pitch range

    }

    void ReceiveTrillData(string message) {
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
        } catch (Exception ex) {
            UnityEngine.Debug.LogError($"Failed to start Python script: {ex.Message}");
        }
    }

    void OnApplicationQuit() {
        // Kill the Python script process
        if (pythonProcess != null && !pythonProcess.HasExited) {
            pythonProcess.Kill();
            UnityEngine.Debug.Log("Stopped trillReceiver Python script.");
        }

        trillReceiver.Stop();
        AubioWrapper.CleanUp();
    }

    private void Crash() {
        return;
        isCrashed = true;
        /*
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
        }
        */
        // 停止飛機移動
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) {
            //rb.isKinematic = false; // 啟用物理效果
            rb.useGravity = true;
            rb.AddForce(Vector3.down * 10f, ForceMode.Impulse); // 模擬墜落
        }
    }
/*
    private IEnumerator SlimeJumpOut() {
        if (slime == null) yield break;

        // 播放跳躍動畫
        Animator animator = slime.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("jump");
        }

        // 設定初始位置、目標位置與初始縮放
        Vector3 startPosition = slime.transform.position;
        Vector3 endPosition = startPosition + slime.transform.forward * jump_out_distance; // 向前跳距離 n
        Vector3 startScale = slime.transform.localScale;
        Vector3 targetScale = Vector3.one * 3; // 放大到 scaleTarget

        float elapsedTime = 0f;

        while (elapsedTime < SlimeDuration)
        {
            // 計算插值
            float t = elapsedTime / SlimeDuration;

            float airplaneHeight = transform.position.y;

            // 計算跳躍的額外高度（拋物線效果）
            float jumpHeight = Mathf.Sin(t * Mathf.PI) * 2f;

            // 更新位置
            slime.transform.position = Vector3.Lerp(startPosition, endPosition, t) + new Vector3(0, airplaneHeight - startPosition.y + jumpHeight, 0);


            // 更新縮放
            slime.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            elapsedTime += Time.deltaTime;
            yield return null; // 等待下一幀
        }


        // 確保最後設置到目標值
        slime.transform.position = endPosition;
        slime.transform.localScale = targetScale;

        // 保存世界位置與旋轉
        Vector3 worldPosition = slime.transform.position;
        Quaternion worldRotation = slime.transform.rotation;

        // 分離父物件
        slime.transform.SetParent(null);

        // 還原世界位置與旋轉
        slime.transform.position = worldPosition;
        slime.transform.rotation = worldRotation;
        Slime.autoMode = true;

        UnityEngine.Debug.Log("Pilot detached and control enabled.");
    }

    private IEnumerator SlimeReturnToPlane()
    {
        if (slime == null) yield break;

        // 播放回飛機的動畫
        Animator animator = slime.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("return");
        }

        // 設定當前位置與飛機目標位置
        Vector3 startPosition = slime.transform.position;
        Vector3 targetPosition = transform.position + new Vector3(0, 1, 0); // 飛機上駕駛員的座位位置
        Vector3 startScale = slime.transform.localScale;
        Vector3 targetScale = Vector3.one * 2; // 恢復到正常大小

        float elapsedTime = 0f;
        Slime.autoMode = false;
        while (elapsedTime < SlimeDuration)
        {
            // 計算插值
            float t = elapsedTime / SlimeDuration;

            float airplaneHeight = transform.position.y;

            // 計算返回過程中的平滑過渡
            slime.transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            // 更新縮放
            slime.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            elapsedTime += Time.deltaTime;
            yield return null; // 等待下一幀
        }

        // 確保最後設置到目標值
        slime.transform.position = targetPosition;
        slime.transform.localScale = targetScale;

        // 設置為飛機的子物件
        slime.transform.SetParent(transform);

        // 恢復局部位置和旋轉
        slime.transform.localPosition = new Vector3(0, 1, 0); // 座位的局部位置
        slime.transform.localRotation = Quaternion.identity;


        UnityEngine.Debug.Log("Pilot reattached to the airplane.");
    }
*/

}