using System.Collections.Generic;
using UnityEngine;using UnityEngine.UI;
[ExecuteAlways]
public sealed class SwordChargeHud:MonoBehaviour
{
 public V06Skills skills;
 public Image dotTemplate;
 [Header("沿圆边排列")]
 public float startAngle=145;
 public float stepAngle=14;
 public float radiusScale=.5f;
 public Color readyColor=new Color(1,.83f,.15f,1),emptyColor=new Color(.32f,.27f,.10f,.85f);
 [InspectorName("剑心必暴就绪颜色")] public Color criticalReadyColor=new Color(1,.12f,.16f,1);
 [Header("编辑模式预览")]
 public bool previewMoon;
 public bool previewRipple;
 [InspectorName("预览剑心必暴就绪")] public bool previewCriticalReady;
 [Range(1,10)]public int previewCapacity=3;
 [Range(0,10)]public int previewReady=1;
 [Range(0,1)]public float previewRecovery=.5f;
 readonly List<Image> dots=new List<Image>();
 public float RecoveryProgress=>Application.isPlaying&&skills?skills.SwordNextRecovery():previewMoon||previewRipple?previewReady<previewCapacity?previewRecovery:-1:-1;
 public int VisibleDotCount {get;private set;}
 public int LitDotCount {get;private set;}
 void LateUpdate(){Refresh();}
 public void Refresh(){if(!dotTemplate)return;dotTemplate.gameObject.SetActive(false);int used=0,lit=0;float angle=startAngle;var rect=(RectTransform)transform;float radius=Mathf.Min(rect.rect.width,rect.rect.height)*radiusScale;int capacity=Application.isPlaying&&skills?skills.SwordPoolCapacity:(previewMoon||previewRipple)?previewCapacity:0;int ready=Application.isPlaying&&skills?skills.SwordPoolReady:Mathf.Min(previewReady,capacity);for(int i=0;i<capacity;i++){if(used==dots.Count){var image=Instantiate(dotTemplate,dotTemplate.transform.parent,false);image.name="剑气充能点"+used;image.gameObject.hideFlags=HideFlags.DontSaveInEditor|HideFlags.DontSaveInBuild;dots.Add(image);}var dot=dots[used++];dot.gameObject.SetActive(true);dot.sprite=dotTemplate.sprite;dot.rectTransform.sizeDelta=dotTemplate.rectTransform.sizeDelta;bool critical=Application.isPlaying?skills&&skills.arena&&skills.arena.v06Attack&&skills.arena.v06Attack.build&&skills.arena.v06Attack.build.HeartReady:previewCriticalReady;dot.color=i<ready?(critical?criticalReadyColor:readyColor):emptyColor;dot.raycastTarget=false;dot.rectTransform.anchoredPosition=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad))*radius;angle-=stepAngle;if(i<ready)lit++;}

 for(int i=used;i<dots.Count;i++)if(dots[i])dots[i].gameObject.SetActive(false);VisibleDotCount=used;LitDotCount=lit;}
 void OnDisable(){foreach(var dot in dots)if(dot)dot.gameObject.SetActive(false);}
 void OnDestroy(){foreach(var dot in dots)if(dot){if(Application.isPlaying)Destroy(dot.gameObject);else DestroyImmediate(dot.gameObject);}dots.Clear();}
}
