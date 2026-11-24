using UnityEngine;using UnityEngine.EventSystems;
public sealed class V06SkillInput:MonoBehaviour,IPointerDownHandler,IPointerUpHandler{
 public V06Skills owner;public int index;
 public void OnPointerDown(PointerEventData e)=>owner.Down(index,e.pointerId);
 public void OnPointerUp(PointerEventData e)=>owner.Up(index,e.pointerId);
}
