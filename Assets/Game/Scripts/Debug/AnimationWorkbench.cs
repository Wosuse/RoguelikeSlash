using UnityEngine;
public sealed class AnimationWorkbench:MonoBehaviour
{
 public enum PreviewState{待机,移动,攻击,短按闪避,冲刺奔跑,短按斩,进入蓄力,保持蓄力,旋转斩,月牙剑气,圆环剑气,向前闪避}
 public PreviewState state;
 [InspectorName("技能动画配置")] public SkillAnimationSet skillAnimations;
 public Sprite[] idleFrames,moveFrames,attackFrames;
 public Material material;public Color tint=Color.white;
 [Min(.1f),Tooltip("仅旧待机/移动/攻击序列；新增技能动作读取技能动画配置的每帧秒数。")] public float fps=12;
 [Min(.1f)] public float size=1.7f;
 public AudioClip previewSound;
 public bool play=true;
 SpriteRenderer visual;AudioSource audioSource;float age;
 void Awake(){var obj=new GameObject("动画预览");obj.transform.SetParent(transform,false);visual=obj.AddComponent<SpriteRenderer>();audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;}
 void Update(){if(play)age+=Time.deltaTime;var frames=state==PreviewState.待机?idleFrames:state==PreviewState.移动?moveFrames:attackFrames;if(frames!=null&&frames.Length>0)visual.sprite=frames[Mathf.FloorToInt(age*fps)%frames.Length];if(skillAnimations&&(int)state>=3){SkillAnimationClip clip=null;switch(state){case PreviewState.短按闪避:clip=skillAnimations.dodge;break;case PreviewState.向前闪避:clip=skillAnimations.forwardDodge;break;case PreviewState.冲刺奔跑:clip=skillAnimations.run;break;case PreviewState.短按斩:clip=skillAnimations.slash;break;case PreviewState.进入蓄力:clip=skillAnimations.chargeEnter;break;case PreviewState.保持蓄力:clip=skillAnimations.chargeHold;break;case PreviewState.旋转斩:clip=skillAnimations.spin;break;case PreviewState.月牙剑气:clip=skillAnimations.crescent;break;case PreviewState.圆环剑气:clip=skillAnimations.ring;break;}if(clip!=null&&clip.Valid)visual.sprite=clip.Sample(age,true);}
 visual.color=tint;if(material)visual.sharedMaterial=material;visual.transform.localScale=Vector3.one*size;}
 [ContextMenu("预览/从第一帧播放")]public void Replay(){age=0;play=true;}
 [ContextMenu("预览/播放音效")]public void PlaySound(){if(Application.isPlaying&&previewSound)audioSource.PlayOneShot(previewSound);}
}
