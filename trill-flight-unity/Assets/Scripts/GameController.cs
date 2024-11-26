using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject startUI;        // Start 按鈕的 UI

    [Header("Game Elements")]
    public GameObject airplane;       // 飛機物件
    public GameObject slime;          // Slime 角色
    public GameObject boardGenerator; // 板子生成器

    [Header("Game State")]
    private bool gameStarted = false; // 遊戲是否已開始

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
