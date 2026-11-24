using UnityEngine;
public sealed partial class CardLab
{
 [Min(.1f)] public float minAttackRange=1,maxAttackRange=40;
 public void SetAttackRange(float value){if(arena&&arena.v06Attack)arena.v06Attack.range=Mathf.Clamp(value,Mathf.Max(.1f,minAttackRange),Mathf.Max(minAttackRange,maxAttackRange));}
 public bool showAttackRange;
 public Color attackRangeColor=new Color(1,.85f,.2f,.8f);
 [Min(.01f)]public float attackRangeLineWidth=.05f;
 public bool wheelZoom=true;
 [Min(.1f)]public float zoomStep=1.5f;
 [Min(.1f)]public float minViewSize=2,maxViewSize=60;
 LineRenderer attackRangeLine;Material attackRangeMaterial;
 void LateUpdate(){var camera=Camera.main;if(wheelZoom&&camera&&Application.isPlaying){Vector2 mouse=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y)/PanelScale;bool overPanel=showPanel&&PanelRect(PanelScale).Contains(mouse);if(!overPanel&&Input.mouseScrollDelta.y!=0)SetViewSize(camera.orthographicSize-Input.mouseScrollDelta.y*zoomStep);}RefreshRangeCircle();}
 public void SetViewSize(float size){var camera=Camera.main;if(camera&&camera.orthographic)camera.orthographicSize=Mathf.Clamp(size,Mathf.Max(.1f,minViewSize),Mathf.Max(minViewSize,maxViewSize));}
 [ContextMenu("测试/完整查看射程")]
 public void FitRangeInView(){if(!Application.isPlaying||!arena||!arena.v06Attack||!Camera.main)return;SetViewSize(arena.v06Attack.EffectiveRange*1.1f/Mathf.Min(1,Mathf.Max(.01f,Camera.main.aspect)));}
 public void RefreshRangeCircle(){if(!Application.isPlaying)return;if(!showAttackRange||!arena||!arena.v06Attack){if(attackRangeLine)attackRangeLine.enabled=false;return;}
 if(!attackRangeLine){var obj=new GameObject("调试普攻射程圈");obj.transform.SetParent(transform,false);attackRangeLine=obj.AddComponent<LineRenderer>();attackRangeMaterial=new Material(Shader.Find("Sprites/Default"));attackRangeLine.sharedMaterial=attackRangeMaterial;attackRangeLine.useWorldSpace=true;attackRangeLine.loop=true;attackRangeLine.sortingOrder=20;attackRangeLine.positionCount=128;}
 attackRangeLine.enabled=true;attackRangeLine.widthMultiplier=attackRangeLineWidth;attackRangeLine.startColor=attackRangeLine.endColor=attackRangeColor;float radius=arena.v06Attack.EffectiveRange;Vector2 center=arena.PlayerPosition;for(int i=0;i<128;i++){float angle=i*Mathf.PI*2/128;attackRangeLine.SetPosition(i,new Vector3(center.x+Mathf.Cos(angle)*radius,center.y+Mathf.Sin(angle)*radius,-.1f));}}
 void OnDisable(){if(attackRangeLine)attackRangeLine.enabled=false;}
 void OnDestroy(){if(attackRangeMaterial)Destroy(attackRangeMaterial);}
 void OnDrawGizmos(){if(Application.isPlaying||!showAttackRange||!arena||!arena.v06Attack)return;Gizmos.color=attackRangeColor;Vector2 center=arena.PlayerPosition;float radius=arena.v06Attack.EffectiveRange;for(int i=0;i<128;i++){float a=i*Mathf.PI*2/128,b=(i+1)*Mathf.PI*2/128;Gizmos.DrawLine(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius);}}
}
