using UnityEditor;

[CustomEditor(typeof(ArenaSkillPreview))]
public sealed class ArenaSkillPreviewEditor : Editor
{
    public override bool RequiresConstantRepaint() => EditorApplication.isPlaying;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.LabelField("当前状态", ((ArenaSkillPreview)target).Status);
    }
}