/*
 * Author: Rickman Roedavan
 * Created: 29 September 2024
 * Desc: This script provides functionality for handling touch joystick controls in the game.
 *       It enables intuitive on-screen joystick input for controlling character movement,
 *       especially for mobile and touch-based devices, ensuring smooth player interaction.
 *       
 * Note: This script is part of the CAROLINA Framework (Capstone Project Research for Open Learning & Interactive Multimedia)
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class AvatarJoystickController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{

    private RectTransform container;
    private RectTransform handle;

    [Header("Input Settings")]
    public bool ArrowKeysSimulationEnabled = false;

    [Header("Joystick Value")]
    public Vector2 point;
    public Vector2 normalizedPoint;

    [Header("Event Settings")]
    public UnityEvent OnJoystickDownEvents;
    public UnityEvent OnJoystickUpEvents;
    private float maxLength;
    private bool _isTouching = false;
    public bool IsTouching { get { return _isTouching; } }

    public UnityAction OnJoystickDownAction;
    public UnityAction OnJoystickUpAction;

    private PointerEventData pointerEventData;
    private Camera cam;



    private void OnEnable()
    {
        OnPointerUp(null);
    }

    private void Awake()
    {
        container = transform.GetComponent<RectTransform>();
        handle = container.GetChild(0).GetComponent<RectTransform>();
        maxLength = (container.sizeDelta.x / 2f) - (handle.sizeDelta.x / 2f) - 5f;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (OnJoystickDownAction != null)
            OnJoystickDownAction.Invoke();
        _isTouching = true;
        cam = e.pressEventCamera;
        OnDrag(e);
    }

    public void OnDrag(PointerEventData e)
    {
        pointerEventData = e;
    }

    void Update()
    {
        if (_isTouching && RectTransformUtility.ScreenPointToLocalPointInRectangle(container, pointerEventData.position, cam, out point))
        {
            point = Vector2.ClampMagnitude(point, maxLength);
            handle.anchoredPosition = point;

            float length = Mathf.InverseLerp(0f, maxLength, point.magnitude);
            normalizedPoint = Vector2.ClampMagnitude(point, length);
        }

        OnJoystickDownEvents?.Invoke();
        OnJoystickUpEvents?.Invoke();
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (OnJoystickUpAction != null)
            OnJoystickUpAction.Invoke();

        _isTouching = false;
        normalizedPoint = Vector3.zero;
        handle.anchoredPosition = Vector3.zero;
    }

    public float Horizontal()
    {
        if (ArrowKeysSimulationEnabled)
            return (normalizedPoint.x != 0) ? normalizedPoint.x : Input.GetAxis("Horizontal");

        return normalizedPoint.x;
    }

    public float Vertical()
    {
        if (ArrowKeysSimulationEnabled)
            return (normalizedPoint.y != 0) ? normalizedPoint.y : Input.GetAxis("Vertical");

        return normalizedPoint.y;
    }
}

///  <summary>
///  EasyJoystick
///  Developed by Hamza Herbou 
///  Modified by Rickman Roedavan
///  -------------------------------------------------------
///  Email    : hamza95herbou@gmail.com
///  Github   : https://github.com/herbou/
///  Youtube  : https://youtube.com/c/hamzaherbou
/// </summary>
