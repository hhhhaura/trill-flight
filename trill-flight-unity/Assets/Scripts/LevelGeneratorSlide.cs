using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

public class LevelGeneratorSlide : MonoBehaviour
{
    [System.Serializable]
    public class PathPoint {
        public float x;
        public float z;
    }

    [System.Serializable]
    public class LevelEvent {
        public int eventType;
        public float eventStart;
        public float eventLength; // In 4th notes
        public float startKey; // In semitones from A4
        public float endKey; // In semitones from A4
    }

    [System.Serializable]
    public class LevelData {
        public List<PathPoint> path;
        public float tempo;
        public List<LevelEvent> level;
    }

    public string jsonFilePath = "Levels/slide1.json"; // Path to your JSON file
    public LevelData levelData;

    public GameObject hoopPrefab;
    public GameObject coinPrefab;

    // List of smoothed path points
    private List<Vector3> path = new List<Vector3>();

    // Other scripts
    private SceneManagement sceneManagerScript;

    // HyperParameters 
    private float coinInterval = 0.4f;


    // for position calculation
    float totalTime = 0;
    float totalDis = 0, curDis = 0, prevTime = 0;
    int curPoint = 0;
    void Awake() {
        sceneManagerScript = GetComponent<SceneManagement>();
        LoadLevelData();
        SetPath();
        GenerateLevelObjects();
    }
    void Start() {
    }

    void LoadLevelData() { // Load and parse JSON file
        if (File.Exists(jsonFilePath)) {
            string jsonData = File.ReadAllText(jsonFilePath);
            levelData = JsonConvert.DeserializeObject<LevelData>(jsonData);
        }
        else Debug.LogError("JSON file not found at path: " + jsonFilePath);
        foreach (var levelEvent in levelData.level) totalTime += levelEvent.eventLength;
    }

    void SetPath() { // averaging neighboring points
        path.Clear();
        for (int i = 0; i < levelData.path.Count; i++) {
            path.Add(new Vector3(levelData.path[i].x, 0, levelData.path[i].z));
            if (i>0) totalDis += Vector3.Distance(path[i], path[i - 1]);
        }
    }

    Vector3 getPosition(float curTime) { // make sure that the ratio can only be bigger not smaller
        //assert(curTime >= prevTime);
        float dis2Next = Vector3.Distance(path[curPoint], path[curPoint + 1]);
        float tmpDis = totalDis * (curTime / totalTime);
        while (tmpDis > curDis + dis2Next) {
            curDis += dis2Next;
            curPoint += 1;
            dis2Next = Vector3.Distance(path[curPoint], path[curPoint + 1]);
        }
        Vector3 pos = Vector3.Lerp(path[curPoint], path[curPoint + 1], (tmpDis - curDis)/dis2Next);
        return pos;
    }

    Vector3 getForward(float curTime) { 
        float dis2Next = Vector3.Distance(path[curPoint], path[curPoint + 1]);
        float tmpDis = totalDis * (curTime / totalTime);
        while (tmpDis > curDis + dis2Next) {
            curDis += dis2Next;
            curPoint += 1;
            dis2Next = Vector3.Distance(path[curPoint], path[curPoint + 1]);
        }
        return (path[curPoint + 1] - path[curPoint]).normalized;
    }

    void GenerateLevelObjects() { // Generate level objects based on events

        float curTime = 0;
        foreach (var levelEvent in levelData.level) {
            if (levelEvent.eventType == 1) CreateHoop(GlobalSettings.key2height(levelEvent.startKey), curTime); 
            else if (levelEvent.eventType == 2) CreateCoins(
                GlobalSettings.key2height(levelEvent.startKey), 
                GlobalSettings.key2height(levelEvent.endKey), 
                curTime, levelEvent.eventLength
            );
            else if (levelEvent.eventType == 3) GlobalSettings.changeTime = curTime;
            else ;
            curTime += levelEvent.eventLength;
        }
    }

    void CreateHoop(float height, float time) { // Create a hoop at the specified key point
        Vector3 position = getPosition(time);
        position.y = height;
        Quaternion lookRotation = Quaternion.LookRotation(getForward(time));
        lookRotation *= Quaternion.Euler(90f, 0f, 0f);
        Instantiate(hoopPrefab, position, lookRotation);
    }

    void CreateCoins(float startHeight, float endHeight, float startTime, float length) {
        int cnt = (int)Math.Floor(length / coinInterval);
        for (int i = 0; i < cnt; i ++) {
            Vector3 pos = getPosition(startTime + i * coinInterval);
            pos.y = startHeight + (endHeight - startHeight) * ((float) i / cnt);
            Quaternion lookRotation = Quaternion.LookRotation(getForward(startTime + i * coinInterval));
            lookRotation *= Quaternion.Euler(0f, 0f, 90f);
            Instantiate(coinPrefab, pos, lookRotation);
        }
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
