using UnityEngine;
using UnityEngine.EventSystems;

public class MouseOverAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Animator _anim;

    private void Start()
    {
        _anim = GetComponent<Animator>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _anim.Play("EnterAnimation");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _anim.Play("ExitAnimation");
    }
}
