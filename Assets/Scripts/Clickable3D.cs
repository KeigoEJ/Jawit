using UnityEngine;
using UnityEngine.Events;

public class Clickable3D : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string hoverTrigger = "Hover";
    [SerializeField] private string leaveTrigger = "Leave";
    [SerializeField] private string clickTrigger = "Click";

    public UnityEvent onClick;
    public UnityEvent onHoverEnter;
    public UnityEvent onHoverExit;

    void Awake() => animator = animator ?? GetComponent<Animator>();

    void OnMouseEnter()
    {
        // Cancel the leave trigger so it doesn't queue up!
        animator.ResetTrigger(leaveTrigger); 
        animator.SetTrigger(hoverTrigger);
        onHoverEnter?.Invoke();
    }

    void OnMouseExit()
    {
        // Cancel the hover trigger if we leave super fast!
        animator.ResetTrigger(hoverTrigger);
        animator.SetTrigger(leaveTrigger);
        onHoverExit?.Invoke();
    }

    void OnMouseDown()
    {
        animator.SetTrigger(clickTrigger);
        onClick?.Invoke();
    }
}