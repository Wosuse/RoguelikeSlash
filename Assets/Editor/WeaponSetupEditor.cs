using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponSetup))]
public sealed class WeaponSetupEditor : Editor
{
    Editor definitionEditor;

    public override void OnInspectorGUI()
    {
        var definition = ((WeaponSetup)target).definition;
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("武器配置资产", definition, typeof(WeaponDefinition), false);
        if (!definition) return;

        // Use the asset's own Inspector so every value has one source of truth.
        CreateCachedEditor(definition, null, ref definitionEditor);
        definitionEditor.OnInspectorGUI();
    }

    void OnDisable()
    {
        if (definitionEditor) DestroyImmediate(definitionEditor);
    }
}

[CustomEditor(typeof(WeaponDefinition))]
public sealed class WeaponDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(WeaponDefinition.kind)));
        var kind = (WeaponKind)serializedObject.FindProperty(nameof(WeaponDefinition.kind)).intValue;
        var property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            switch (property.name)
            {
                case "m_Script":
                case nameof(WeaponDefinition.kind):
                    continue;
                case nameof(WeaponDefinition.spinRadius):
                case nameof(WeaponDefinition.spinTick):
                    if (kind != WeaponKind.Dart) continue;
                    break;
                case nameof(WeaponDefinition.bleedTick):
                    if (kind != WeaponKind.Knife) continue;
                    break;
                case nameof(WeaponDefinition.deceleration):
                case nameof(WeaponDefinition.upgradedRange):
                case nameof(WeaponDefinition.childRange):
                case nameof(WeaponDefinition.searchRadius):
                case nameof(WeaponDefinition.obstacleMask):
                    if (kind != WeaponKind.Bow) continue;
                    break;
            }
            EditorGUILayout.PropertyField(property, true);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
