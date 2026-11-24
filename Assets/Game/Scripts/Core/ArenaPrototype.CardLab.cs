using UnityEngine;
// Test hooks are inert in GameScene. Only CardLab enables them.
public sealed partial class ArenaPrototype
{
 [System.NonSerialized] public bool labMode,labFreezeEnemies,labInvincible,labAutoSpawn;
 [System.NonSerialized] public float labSpawnInterval=1;
 public void LabClearEnemies(){for(int i=enemies.Count-1;i>=0;i--)Return(enemies,i,enemyPool);for(int i=bullets.Count-1;i>=0;i--)Return(bullets,i,bulletPool);}
 public void LabSpawn(Vector2 offset,float health){SpawnEnemy();var enemy=enemies[enemies.Count-1];enemy.Position=PlayerPosition+offset;enemy.Object.transform.position=enemy.Position;enemy.Health=health;enemy.LabDummy=true;}
 public void LabReadDummyPositions(System.Collections.Generic.List<Vector2> positions){positions.Clear();foreach(var e in enemies)if(e.LabDummy)positions.Add(e.Position);}
 public void LabClearDummies(){for(int i=enemies.Count-1;i>=0;i--)if(enemies[i].LabDummy)Return(enemies,i,enemyPool);}
 public void LabRemoveDummy(Vector2 position,float radius){int index=-1;float nearest=radius;for(int i=0;i<enemies.Count;i++){if(!enemies[i].LabDummy)continue;float d=Vector2.Distance(position,enemies[i].Position);if(d<nearest){nearest=d;index=i;}}if(index>=0)Return(enemies,index,enemyPool);}
 public void LabDamagePlayer(float amount){if(runStats)runStats.Receive(amount);}
}
