using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagement : MonoBehaviour {
    private int mode = 0;
    string[] scenes = {"TrillSlidePro", "TrillPitchScene"};

    // Load a scene additively (internal access)
    internal void LoadSceneAdditive(string sceneName) {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        Debug.Log("Loading scene additively: " + sceneName);
    }

    // Unload a scene by name (internal access)
    internal void UnloadSceneByName(string sceneName) {
        SceneManager.UnloadSceneAsync(sceneName);
        Debug.Log("Unloading scene: " + sceneName);
    }
    
    public void SwitchMode(int mode) {
        UnloadSceneByName(scenes[mode]);
        mode = 1 - mode;
        LoadSceneAdditive(scenes[mode]);
        return;
    }
    void Update() {
        if (GlobalSettings.changeTime - GlobalSettings.curBeat() <= 1e-6) {
            SwitchMode(mode);
        }
    }
}
