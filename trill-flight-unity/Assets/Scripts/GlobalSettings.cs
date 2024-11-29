using UnityEngine;

public static class GlobalSettings {
    public static float heightOffset = 5f;
    public static string basePitch = "A4";
    public static float pitchSensitivity = 3;
    public static float timeRatio = 0.3f;

    public static int mode = 0;
    public static float changeTime;

    public static float key2height(float key) {
        return (key + heightOffset) * pitchSensitivity;
    }
    public static float curBeat() { //Time to 拍子 
        return Time.time / timeRatio;
    }
}

