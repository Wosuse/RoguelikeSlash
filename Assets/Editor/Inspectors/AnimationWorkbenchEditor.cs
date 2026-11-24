using UnityEditor;using UnityEngine;
[CustomEditor(typeof(AnimationWorkbench))]public sealed class AnimationWorkbenchEditor:Editor{
 public override void OnInspectorGUI(){DrawDefaultInspector();var bench=(AnimationWorkbench)target;GUI.enabled=Application.isPlaying;if(GUILayout.Button("从第一帧播放"))bench.Replay();if(GUILayout.Button("播放音效"))bench.PlaySound();GUI.enabled=true;}}
