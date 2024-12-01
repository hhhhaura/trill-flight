using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Diagnostics;

public class Slime : MonoBehaviour{
    public static bool autoMode = false;
    private Airplane airplane;

    [Header("Slime Settings")]
    public float pitchSensitivity = 3f; // How sensitive the airplane is to pitch changes
    private bool trillState = false;  // Lip trill intensity (0 to 1)
    private float targetPitch = 100f;
    private float prePitch = 100f;
    public float gravity = 9.8f;          // Gravitational acceleration (m/s^2)

    private float verticalVelocity = 0f;
    private float lerpSpeed = 50f;
    private float heightOffset = 0f;

    [Header("Rigidbody Settings")]
    private Rigidbody rb;


    [Header("Audio Processing")]
    private Process pythonProcess; // Process to handle the Python script
    private UDPReceiver trillReceiver; // Keeps the trillReceiver logic
    private float[] audioData;

    [Header("update freq")]

    private Queue<float> pitchHistory = new Queue<float>(); // 保存最近的 pitch 值
    public int updatesPerSecond = 30; // 每秒更新次數
    private float updateInterval; // 每次更新的時間間隔
    private float elapsedTime = 0f; // 累計時間
    private Animator animator; // 角色的 Animator
    private float previousHeight; // 保存上一幀的高度

    void Start() {
        airplane = GetComponentInParent<Airplane>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        previousHeight = 0;
    }

    void Update() {
        autoMode = airplane.getCurMode() == 1;
        UnityEngine.Debug.Log("slime automode " + autoMode);
        if (!autoMode) { 
            transform.parent = airplane.transform;
            transform.localPosition = new Vector3(0f, 1.97f, 0.2f);
            animator.SetBool("walk", false);    
            rb.isKinematic = true;
            return;
        }

        transform.parent = null;
        //rb.isKinematic = false;
        targetPitch = airplane.getPitch();
        trillState = airplane.getTrillState();

        int targetMidi = Mathf.RoundToInt(targetPitch);
        prePitch = targetPitch;
        float height = GlobalSettings.key2height(targetMidi);
        UnityEngine.Debug.Log($"HERE! {targetMidi}");

        //if (targetMidi > 0) {
        // 啟動 Walk 動畫
        animator.SetBool("walk", true);
        Vector3 gpos = GlobalSettings.stepControl.getPosition(GlobalSettings.curBeat() - airplane.getBaseTime());
        Vector3 gfor = GlobalSettings.stepControl.getForward(GlobalSettings.curBeat() - airplane.getBaseTime());
        gpos.y = height;
        /* TODO handle up down rotation */
        transform.position = gpos;
        rb.MoveRotation(Quaternion.LookRotation(gfor));
        previousHeight = height;
        //} else animator.SetBool("walk", false);

        if (Mathf.Abs(height - previousHeight) > 0.3f) {
            // 啟動 Jump 動畫
            animator.SetTrigger("jump");
            animator.SetBool("jump", false);
        }
        previousHeight = height;
    }

    // 可選：高亮當前音板
    private void HighlightActiveBoard(GameObject board) {
        Renderer renderer = board.GetComponent<Renderer>();
        if (renderer != null) {
            renderer.material.color = Color.red; // 將當前活動音板設置為紅色
        }
    }
}