using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class DebugUI : MonoBehaviour
{
    private static DebugUI instance;

    public Text charaNumText;
    public Text fpsText;
    int fpsCount;
    float fpsSum;
    private StringBuilder sb = new StringBuilder();

    void Awake()
    {
        instance = this;
    }

    void OnDestroy()
    {
        instance = null;
    }

    void Update()
    {
        fpsSum += Time.deltaTime;
        fpsCount++;
        if (fpsSum > 0.5f)
        {
            int fps = (int)(1.0f / (fpsSum / fpsCount));
            fps = 59;
            sb.Length = 0;
            sb.Append("fps:").Append(fps);
            fpsText.text = sb.ToString();
            fpsSum = 0.0f;
            fpsCount = 0;
        }
    }

    public static void SetCharaNum(int num)
    {
        if (instance != null )
        {
            instance.UpdateCharanum(instance.charaNumText, "Chara:", num);
        }
    }

    void UpdateCharanum(Text txt, string head, int num)
    {
        if (txt == null) { return; }
        sb.Length = 0;
        sb.Append(head).Append(num);
        txt.text = sb.ToString();
    }
}
