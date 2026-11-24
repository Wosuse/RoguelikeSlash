using UnityEngine;
// Scene owns geometry, artwork and text. This presenter changes state only.
[ExecuteAlways,DefaultExecutionOrder(900)]
public sealed class SceneSkillHud:MonoBehaviour
{
 public enum Preview { 就绪,冷却,生效中,能量,禁用 }
 public V06Skills skills;
 [Tooltip("顺序：闪、斩、无双。布局由各自RectTransform控制。")]
 public ArenaSkillButton[] buttons=new ArenaSkillButton[3];
 public Color[] accents={new Color(1,.69f,.36f),new Color(.37f,.73f,1),new Color(1,.82f,.25f)};
 public Texture2D[] icons=new Texture2D[3];
 public Color progressColor=new Color(1,.8f,.2f);
 [Header("无双能量状态")]
 public CanvasGroup ultimateStateGroup;
 public Color ultimateFillColor=new Color(1,.8f,.2f,.65f);
 [HideInInspector] public bool ultimateWaterFillConfigured;
 [Range(0,1)] public float ultimateDimAlpha=.4f;
 [Range(0,1)] public float ultimateReadyAlpha=1;
 public Preview preview;
 [Range(0,1)]public float previewProgress=.65f;
 void LateUpdate(){Refresh();}
 public void Refresh(){for(int i=0;i<3;i++){if(buttons==null||i>=buttons.Length||!buttons[i])continue;var button=buttons[i];float remaining=0,total=1,duration=-1,energy=0;
 if(Application.isPlaying&&skills){remaining=skills.ButtonCooldown(i);total=skills.ButtonCooldownTotal(i);duration=skills.ButtonDuration(i);energy=skills.ButtonEnergy;}else{if(preview==Preview.冷却&&i<2){remaining=8*(1-previewProgress);total=8;}if(preview==Preview.生效中)duration=previewProgress;energy=preview==Preview.能量?previewProgress:0;}
 if(i==1){var chargeHud=button.GetComponent<SwordChargeHud>();duration=chargeHud&&chargeHud.enabled?chargeHud.RecoveryProgress:-1;}
 button.Show(accents!=null&&i<accents.Length?accents[i]:Color.white,icons!=null&&i<icons.Length?icons[i]:null,remaining,total,duration);button.progress.color=progressColor;button.duration.color=progressColor;if(i<2)button.progress.enabled=false;
 if(i==2&&remaining<=0){bool active=duration>=0;bool ready=energy>=.99999f;button.progress.enabled=!ready&&!active;button.progress.fillAmount=energy;button.progress.color=ultimateFillColor;if(ultimateStateGroup)ultimateStateGroup.alpha=ready||active?ultimateReadyAlpha:ultimateDimAlpha;}
 button.ShowDisabled(i<2&&(Application.isPlaying&&skills?skills.ButtonDisabled(i):preview==Preview.禁用));
 }}
}
