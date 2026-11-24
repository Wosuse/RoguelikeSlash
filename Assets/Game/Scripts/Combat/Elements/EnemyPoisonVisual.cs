using UnityEngine;
// Shared by normal-attack poison and slash poison; pooling always restores original colours.
public sealed class EnemyPoisonVisual:MonoBehaviour
{
 V06Attack attack;int id;Material mesh;SpriteRenderer sprite;Color meshBase,spriteBase;float phase;bool tinted;
 public void Initialize(V06Attack owner,int spawnId,Material material,SpriteRenderer renderer){Restore();attack=owner;id=spawnId;mesh=material;sprite=renderer;meshBase=mesh?mesh.color:Color.white;spriteBase=sprite?sprite.color:Color.white;phase=0;}
 void LateUpdate(){Refresh();}
 public void Refresh(){bool poisoned=attack&&(attack.HasPoison(id)||attack.elements&&attack.elements.HasPoison(id));if(!poisoned){Restore();return;}var settings=attack.elements;float hz=settings?settings.poisonBlinkHz:2;float max=settings?settings.poisonTintStrength:.65f;if(!attack.clock||!attack.clock.Paused)phase+=Time.deltaTime*attack.arena.WorldRate;float blend=max*(.3f+.7f*(.5f+.5f*Mathf.Sin(phase*hz*Mathf.PI*2)));Color color=settings?settings.poisonTint:new Color(.82f,.65f,1,1);if(mesh)mesh.color=Color.Lerp(meshBase,color,blend);if(sprite)sprite.color=Color.Lerp(spriteBase,color,blend);tinted=true;}
 void Restore(){if(!tinted)return;if(mesh)mesh.color=meshBase;if(sprite)sprite.color=spriteBase;tinted=false;}
 void OnDisable(){Restore();}
}
