using UnityEngine;
public sealed partial class CardLab
{
 string configurationSaveMessage;
#if UNITY_EDITOR
 public static event System.Action<CardLab> SaveConfigurationRequested;
#endif
 [ContextMenu("测试/保存当前配置")]
 public void SaveCurrentConfiguration(){
#if UNITY_EDITOR
 if(SaveConfigurationRequested!=null)SaveConfigurationRequested(this);
#endif
 }
 public void SetConfigurationSaveMessage(string message){configurationSaveMessage=message;}
}
