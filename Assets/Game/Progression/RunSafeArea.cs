using UnityEngine;
[ExecuteAlways]
public sealed class RunSafeArea:MonoBehaviour
{
    void LateUpdate()
    {
        if(Screen.width<=0||Screen.height<=0)return;
        var rect=(RectTransform)transform;var area=Screen.safeArea;
        rect.anchorMin=new Vector2(area.xMin/Screen.width,area.yMin/Screen.height);
        rect.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);
        rect.offsetMin=rect.offsetMax=Vector2.zero;
    }
}
