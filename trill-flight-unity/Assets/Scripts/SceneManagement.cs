using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagement : MonoBehaviour {
    string[] scenes = {"TrillSlidePro", "TrillStepPro"};

    GameObject FindFromScene(string sceneName, string objectName) {
        Scene targetScene = SceneManager.GetSceneByName(sceneName);
        if (targetScene.isLoaded) {
            GameObject[] rootObjects = targetScene.GetRootGameObjects();

            foreach (GameObject rootObject in rootObjects) {
                Debug.Log(rootObject.name);
                if (rootObject.name == objectName) {
                    Debug.Log("Found object: " + rootObject.name);
                    return rootObject;
                }
            }
            return null;
        }
        else {
            Debug.Log("The target scene is not loaded.");
            return null;
        }
    }
    void Awake() {
        if (GlobalSettings.isInit == false) GlobalSettings.Initialize();
        GameObject targetObject = GameObject.Find("SceneManager");
        GlobalSettings.sceneManager = targetObject.GetComponent<SceneManagement>();
        GlobalSettings.isInit = true;
    }

    // Load a scene additively (internal access)
    internal void LoadSceneAdditive(string sceneName) {
       // SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        Debug.Log("Loading scene additively: " + sceneName);
    }

/*
    internal void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode) {
        // set the appropriate scenes in the current level;
        int mode = (GlobalSettings.level & 1);
        if (mode == 0) {
            GameObject targetObject = GameObject.Find("SlideSceneManager");
            GlobalSettings.slideControl = targetObject.GetComponent<LevelGeneratorSlide>();
        } else {
            GameObject targetObject = GameObject.Find("StepSceneManager");
            GlobalSettings.stepControl = targetObject.GetComponent<LevelGeneratorStep>();
        }
    }
    */
    
    // Unload a scene by name (internal access)
    internal void UnloadSceneByName(string sceneName) {
     //   SceneManager.sceneLoaded -= OnSceneLoaded;
        if (SceneManager.GetSceneByName(sceneName).isLoaded) {
            SceneManager.UnloadSceneAsync(sceneName);
        } else { 
            Debug.LogWarning("Scene is not loaded, so cannot be unloaded. Scene name: " + sceneName); 
        }
    }
    public void SwitchMode() {
        int mode = (GlobalSettings.level & 1);
        Time.timeScale = 0f;
        UnloadSceneByName(scenes[mode]);
        mode = 1 - mode;
        Debug.Log("Mode switch: " + mode);
        LoadSceneAdditive(scenes[mode]);
        return;
    }
    public void EndGame() {

    }
    void Update() {
        //Debug.Log("Difference: " + (GlobalSettings.changeTime - GlobalSettings.curBeat()));
        if (GlobalSettings.changeTime - GlobalSettings.curBeat() <= 1e-6) {
            GlobalSettings.changeTime = 10000000000;
            SwitchMode();
            GlobalSettings.level += 1;
        }
    }
}
