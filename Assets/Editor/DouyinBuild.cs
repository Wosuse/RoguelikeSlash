using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class DouyinBuild
{
    public static async void Build()
    {
        try
        {
            var settings = TTSDK.Tool.StarkBuilderSettings.Instance;
            settings.appId = "tt3c554ae8a2291d8907";
            var serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("_orientation").intValue = 1; // SDK: Portrait=0, Landscape=1.
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            settings.OutputDir = System.IO.Path.GetFullPath("Builds/DouyinM3UI");
            settings.framework = TTSDK.Tool.Framework.Wasm;
            settings.isOldBuildFormat = false;
            settings.dataLoadType = TTSDK.Tool.DataLoadType.Package;
            settings.buildOptions = BuildOptions.Development;
            settings.needCompress = true;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            var path = await TTSDK.Tool.API.BuildManager.Build(TTSDK.Tool.Framework.Wasm, false);
            if (string.IsNullOrEmpty(path)) throw new Exception("SDK returned empty output path");
            Debug.Log("DOUYIN_BUILD_OK " + path);
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    public static void Inspect()
    {
        var settings = TTSDK.Tool.StarkBuilderSettings.Instance;
        foreach (var f in settings.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            Debug.Log("SDK_FIELD " + f.Name + " " + f.FieldType + (f.FieldType.IsEnum ? " " + string.Join(",", Enum.GetNames(f.FieldType)) : ""));
    }
}




