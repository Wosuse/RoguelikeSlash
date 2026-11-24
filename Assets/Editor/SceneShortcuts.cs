using UnityEditor;
using UnityEditor.SceneManagement;

public static class SceneShortcuts
{
    [MenuItem("Tools/Arena/打开正式游戏")]
    public static void OpenGame() => Open("GameScene");

    [MenuItem("Tools/Arena/打开卡牌实验室")]
    public static void OpenLab() => Open("CardLabScene");

    static void Open(string name)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene("Assets/Game/Scenes/" + name + ".unity");
    }
}
