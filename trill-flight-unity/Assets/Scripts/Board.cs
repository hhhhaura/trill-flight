using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.UI;

[System.Serializable]
public class NoteEvent
{
    public int note;       // 音高
    public float time;     // 開始時間
    public float duration; // 持續時間
}

[System.Serializable]
public class NoteEventList
{
    public List<NoteEvent> notes;
}

public class Board : MonoBehaviour
{
    public GameObject boardPrefab;  // 板子的預製件
    public GameObject boxPrefab;    // 箱子的預製件
    public GameObject checkpointPrefab; // Checkpoint 的預製件
    public Transform spawnParent;  // 板子生成的父物件
    public string jsonFilePath;    // MIDI 文件路徑

    [Header("Height Configuration")]
    public float groundLevel = 0f;  // 地面高度
    public float heightOffset = 0f; // 高度偏移量
    public float ceilingLevel = 50f; // 天花板高度
    public int H_range_Midi = 80;   // 最高音對應的 MIDI 碼
    public int L_range_Midi = 53;   // 最低音對應的 MIDI 碼

    [Header("Horizontal Range")]
    public float left = 0f;  // 最左邊的位置
    public float right = 50f;  // 最右邊的位置

    [Header("Board Configuration")]
    public float baseLength = 1f;  // 最短音符對應的板子長度
    public float spacing = 3f;   // 板子之間的最小間距
    public float boxOffset = 2f; // 箱子距離板子最左邊的距離

    [Header("Flight Configuration")]
    public GameObject airplane; // 飛機物件
    public float flightSpeed = 3f; // 飛行速度

    private List<Vector3> flightPath; // 保存飛行路徑
    private int currentTargetIndex = 0; // 當前目標點索引

    public Transform propeller;       // Reference to the propeller Transform
    public float maxSpinSpeed = 10000f; // Maximum spin speed for the propeller
    //private bool trillState = false;  // Lip trill intensity (0 to 1)
    public bool isCrashed = false;

    [Header("UI Elements")]
    public Text gameOverText;
    public Text gameWonText;

    void Start()
    {
        flightPath = new List<Vector3>();
        LoadAndGenerateBoards(jsonFilePath);

        if (flightPath.Count > 0)
        {
            airplane.transform.position = new Vector3(-20, flightPath[0].y, 0);
        }
    }

    void Update()
    {
        if (isCrashed)
        {
            // 墜機後停止移動
            return;
        }
        if (flightPath.Count == 0 || airplane == null) return;

        // 飛機移動到下一個目標點
        Vector3 targetPosition = flightPath[currentTargetIndex];
        //targetPosition.y = airplane.transform.position.y;

        airplane.transform.position = Vector3.MoveTowards(
            airplane.transform.position,
            targetPosition,
            flightSpeed * Time.deltaTime
        );
        airplane.transform.position = new Vector3(airplane.transform.position.x, targetPosition.y, airplane.transform.position.z);

        // 當飛機到達目標點時，切換到下一個點
        if (Vector3.Distance(airplane.transform.position, targetPosition) < 0.1f)
        {
            currentTargetIndex++;
            if (currentTargetIndex >= flightPath.Count)
            {
                // 飛行結束，可選：重置到起點或停止
                //currentTargetIndex = 0;
            }
            Debug.Log("currentTargetIndex = " + currentTargetIndex);
        }
        propeller.Rotate(Vector3.forward, maxSpinSpeed * Time.deltaTime);

    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("trigger: " + other.tag);
        if (other.tag == "Box")
        {
            Debug.Log("crash");
            Crash();
        }
        else if (other.tag == "Checkpoint")
        {
            Debug.Log("Checkpoint reached!");
            GameWon();
        }

    }

    string GetNoteName(int midiNote)
    {
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int octave = (midiNote / 12) - 1;
        int noteIndex = midiNote % 12;
        return $"{noteNames[noteIndex]}{octave}";
    }

    void LoadAndGenerateBoards(string filePath)
    {
        // 讀取 JSON 文件
        string jsonContent = File.ReadAllText(filePath);
        NoteEventList noteEventList = JsonUtility.FromJson<NoteEventList>("{\"notes\":" + jsonContent + "}");

        // 生成板子
        float levelWidth = right - left;
        float horizontalStep = Mathf.Max(0.5f, levelWidth / (noteEventList.notes.Count - 1));
        float currentX = left;

        GameObject lastBoard = null;

        foreach (var note in noteEventList.notes)
        {
            float height = CalculateHeight(note.note);
            float length = CalculateLength(note.duration);
            
            Vector3 boardPosition = new Vector3(currentX, height, 0);
            // 計算板子的右端點作為飛行目標
            Vector3 targetPosition = new Vector3(currentX + length / 2, height, 0);

            // 添加到飛行路徑
            flightPath.Add(targetPosition);
            lastBoard = SpawnBoard(new Vector3(currentX, height, 0), length, note.note);
            currentX += horizontalStep;
        }
        //if (lastBoard != null && checkpointPrefab != null)
        //{
        //    Vector3 checkpointPosition = new Vector3(
        //        lastBoard.transform.position.x + lastBoard.transform.localScale.x / 2,
        //        lastBoard.transform.position.y + 1f, // 高度略高於板子
        //        lastBoard.transform.position.z
        //    );
        //    GameObject checkpoint = Instantiate(checkpointPrefab, checkpointPosition, Quaternion.identity, spawnParent);
        //    checkpoint.tag = "Checkpoint"; // 設置標籤
        //}
    }

    float CalculateHeight(int midiNote)
    {
        return groundLevel + heightOffset +
               (ceilingLevel - groundLevel - heightOffset) *
               (midiNote - L_range_Midi) / (H_range_Midi - L_range_Midi);
    }

    float CalculateLength(float duration)
    {
        return duration * 10f; // 將持續時間轉換為板子長度
    }

    GameObject SpawnBoard(Vector3 position, float length, int note)
    {
        GameObject board = Instantiate(boardPrefab, position, Quaternion.identity, spawnParent);
        Vector3 newScale = new Vector3(
            length,
            board.transform.localScale.y,
            board.transform.localScale.z
        );
        float lengthChange = length - board.transform.localScale.x; // 長度的變化量
        Vector3 positionOffset = new Vector3(lengthChange / 2, 0, 0); // 偏移位置，沿 x 軸前移

        // 應用縮放
        board.transform.localScale = newScale;

        // 調整位置
        board.transform.localPosition += positionOffset;

        // 生成箱子
        SpawnBox(board, length);

        GenerateNoteName(board, position, note);
        return board;
    }

    void SpawnBox(GameObject board, float length)
    {
        if (boxPrefab == null)
        {
            Debug.LogWarning("Box prefab is not assigned!");
            return;
        }

        // 計算箱子的位置
        Vector3 boxPosition = new Vector3(
            board.transform.position.x - length / 2 + boxOffset, // 最左邊加上偏移量
            board.transform.position.y + 0.5f,                   // 稍微抬高以便可見
            board.transform.position.z
        );

        // 生成箱子
        GameObject box = Instantiate(boxPrefab, boxPosition, Quaternion.identity, spawnParent);

        // 確保箱子為板子的子物件
        //box.transform.SetParent(board.transform);

        // 添加 BoxController 並設置屬性
        BoxController boxController = box.AddComponent<BoxController>();
        boxController.board = board;        // 關聯箱子的板子
        boxController.destroyHeight = groundLevel - 5f; // 設置最低銷毀高度
        boxController.boardMargin = 1f;    // 設置板子邊界範圍
    }

    void GenerateNoteName(GameObject board, Vector3 position, int note)
    {
        // 計算音階名稱
        string noteName = GetNoteName(Mathf.RoundToInt(note));

        // 創建一個 TextMesh 物件
        GameObject textObject = new GameObject("NoteName");
        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = noteName;
        textMesh.fontSize = 12;
        textMesh.color = Color.black;
        textMesh.alignment = TextAlignment.Center;

        // 設置文字位置（位於板子上方）
        textObject.transform.position = new Vector3(position.x, position.y + 2f, position.z);
        textObject.transform.SetParent(board.transform); // 將文字作為板子的子物件
    }


    private void Crash()
    {
        isCrashed = true;

        // 觸發墜機動畫（如果有）
        //Animator animator = GetComponent<Animator>();
        //if (animator != null)
        //{
        //    animator.SetTrigger("Crash");
        //}
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
        }
        // 停止飛機移動
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false; // 啟用物理效果
            rb.AddForce(Vector3.down * 10f, ForceMode.Impulse); // 模擬墜落
        }

        // 其他墜機處理邏輯...
    }
    void GameWon()
    {
        if (gameWonText != null) gameWonText.gameObject.SetActive(true);
    }
}

