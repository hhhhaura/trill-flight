using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public static class GlobalSettings {
    
    // Some important scripts
    public static LevelGeneratorSlide slideControl = null;
    public static LevelGeneratorStep stepControl = null;
    public static SceneManagement sceneManager = null;
    public static float heightOffset = 12f;
    public static int basePitch = 69;
    public static float pitchSensitivity = 3;
    public static float timeRatio = 0.5f;
    public static string jsonFilePath = "Levels/game.json";
    public static LevelData levelData = null;
    public static bool isInit = false;

    public static float groundLevel = 0f;
    public static float ceilingLevel = 500f;


    public static int mode = 0;
    public static float changeTime = 100000000;

    public static int level = 0;

    public static float key2height(float key) {
        return Mathf.Clamp((key + heightOffset - basePitch) * pitchSensitivity, groundLevel, ceilingLevel);
    }
    public static float curBeat() { //Time to 拍子 
        return Time.time / timeRatio;
    }
    public static void Initialize() {
        if (File.Exists(jsonFilePath)) {
            string json = File.ReadAllText(jsonFilePath);
            levelData = JsonConvert.DeserializeObject<LevelData>(json);  // Deserialize to your specific class
            Debug.Log("Level Data Loaded Successfully");
        }
    }
}

