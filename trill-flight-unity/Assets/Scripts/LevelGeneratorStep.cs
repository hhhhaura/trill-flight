using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

public class LevelGeneratorStep : MonoBehaviour {


    private LevelData.Level data;
    public GameObject parent;
    private List<Vector3> path = new List<Vector3>();

    private SceneManagement sceneManagerScript;

    [Header("Board Configuration")]
    public GameObject boardPrefab;  // 板子的預製件
    public GameObject boxPrefab;    // 箱子的預製件
    public GameObject checkpointPrefab; // Checkpoint 的預製件
    public float baseLength = 1f;  // 最短音符對應的板子長度
    public float spacing = 3f;   // 板子之間的最小間距
    public float boxOffset = 2f; // 箱子距離板子最左邊的距離


    // for position calculation
    float totalTime = 0;
    float totalDis = 0, curDis = 0, prevTime = 0;
    int curPoint = 0;
    void Awake() {
        GlobalSettings.stepControl = this;
        LoadLevelData();
        SetPath();
        GenerateLevelObjects();
        Time.timeScale = 1f;
    }
    void LoadLevelData() { // Load and parse JSON file
        data = GlobalSettings.levelData.levels[GlobalSettings.level];
        foreach (var levelEvent in data.level) totalTime += levelEvent.eventLength;
        GlobalSettings.changeTime = GlobalSettings.curBeat() + totalTime;
    }

    void SetPath() { // should have only two points
        path.Clear();
        for (int i = 0; i < data.path.Length; i++) {
            path.Add(new Vector3(data.path[i].x, 0, data.path[i].z));
            if (i>0) totalDis += Vector3.Distance(path[i], path[i - 1]);
        }
    }

    public Vector3 getPosition(float curTime) { // make sure that the ratio can only be bigger not smaller
        curTime = Mathf.Min(totalTime, Mathf.Max(0f, curTime));

        //assert(curTime >= prevTime);
        float dis2Next = Vector3.Distance(path[curPoint], path[curPoint + 1]);
        float tmpDis = totalDis * (curTime / totalTime);
        while (curPoint + 1 < path.Count && tmpDis > curDis + dis2Next) {
            curDis += dis2Next;
            curPoint += 1;
            dis2Next = Vector3.Distance(path[curPoint], path[curPoint + 1]);
        }
        Vector3 pos = Vector3.Lerp(path[curPoint], path[curPoint + 1], (tmpDis - curDis)/dis2Next);
        return pos;
    }

    public Vector3 getForward(float curTime) { 
        curTime = Mathf.Min(totalTime, Mathf.Max(0f, curTime));
        float dis2Next = Vector3.Distance(path[curPoint], path[curPoint + 1]);
        float tmpDis = totalDis * (curTime / totalTime);
        while (curPoint + 1 < path.Count && tmpDis > curDis + dis2Next) {
            curDis += dis2Next;
            curPoint += 1;
            dis2Next = Vector3.Distance(path[curPoint], path[curPoint + 1]);
        }
        return (path[curPoint + 1] - path[curPoint]).normalized;
    }

    void GenerateLevelObjects() { // Generate level objects based on events

        /*
        float levelWidth = totalDis;
        float horizontalStep = Mathf.Max(0.5f, levelWidth / (noteEventList.notes.Count - 1));
        float currentX = left;
        */

        float curTime = 0;
        foreach (var levelEvent in data.level) {
            SpawnBoard(curTime, levelEvent.eventLength, levelEvent.startKey);
            curTime += levelEvent.eventLength;
        }
    }
    GameObject SpawnBoard(float curTime, float span, int note) {
        float length = span / totalTime * totalDis;
        Quaternion angle = Quaternion.LookRotation(getForward(curTime));
        Quaternion spin = Quaternion.AngleAxis(90f, Vector3.up); 
        angle *= spin;
        Vector3 position = getPosition(curTime);
        position.y = GlobalSettings.key2height(note);
        GameObject board = Instantiate(boardPrefab, position + getForward(curTime) * length/3, angle, parent.transform);

        Vector3 newScale = new Vector3(
            length,
            board.transform.localScale.y,
            board.transform.localScale.z
        );

        // 應用縮放
        board.transform.localScale = newScale;

        // 生成箱子
        GameObject box = Instantiate(boxPrefab, position, angle, parent.transform);
        BoxController boxController = box.AddComponent<BoxController>();
        boxController.board = board;        // 關聯箱子的板子
        boxController.destroyHeight = - 5f; // 設置最低銷毀高度
        boxController.boardMargin = 1f;    // 設置板子邊界範圍

        //GenerateNoteName(board, position, note);
        return board;
    }

    void SpawnBox(GameObject board, Vector3 position, int note) {
        if (boxPrefab == null) {
            Debug.LogWarning("Box prefab is not assigned!");
            return;
        }

        // 生成箱子
        position.y = GlobalSettings.key2height(note);
    }


    // Visualize the path in the editor using Gizmos
    void OnDrawGizmos() {
        if (path != null && path.Count > 1) {
            Gizmos.color = Color.green;
            for (int i = 0; i < path.Count - 1; i++) {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }
        }
    }

    // Optionally, visualize the path when the object is selected in the editor
    void OnDrawGizmosSelected() {
        OnDrawGizmos();
    }
}

