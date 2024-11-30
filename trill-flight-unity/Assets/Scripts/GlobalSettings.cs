using UnityEngine;

public static class GlobalSettings {
    public static float heightOffset = 5f;
    public static string basePitch = "A4";
    public static float pitchSensitivity = 3;
    public static float timeRatio = 0.3f;

    [Header("Height Configuration")]
    public static float groundLevel = 0f;
    public static float ceilingLevel = 40f;

    public static int H_range_Midi = 80;   // 最高音對應的 MIDI 碼
    public static int L_range_Midi = 53;   // 最低音對應的 MIDI 碼

    public static int mode = 0;
    public static float changeTime;

    public static float key2height(float key) {
        return groundLevel + heightOffset +
               (ceilingLevel - groundLevel - heightOffset) *
               (key - L_range_Midi) / (H_range_Midi - L_range_Midi);
    }
    public static float curBeat() { //Time to 拍子 
        return Time.time / timeRatio;
    }
}

