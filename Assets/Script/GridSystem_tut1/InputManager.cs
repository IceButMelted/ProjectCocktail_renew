using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.InputSystem;
using System;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    [SerializeField]
    private Camera SceneCamera;

    private Vector3 lastPosition;

    [SerializeField]
    private LayerMask placementLayerMask;

    public event Action OnClicked, OnExit;

    private void Update() {
        if (Mouse.current == null) {
            return;
        }
        if (Mouse.current.leftButton.wasPressedThisFrame) {
            OnClicked?.Invoke();
        }
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) {
            OnExit?.Invoke();
        }
    }

    public bool IsPointerOverUI()
        => EventSystem.current.IsPointerOverGameObject();
        //{
        //if (Mouse.current == null) {
        //    return false;
        //}
        //Vector3 mousePos = Mouse.current.position.ReadValue();
        //Ray ray = SceneCamera.ScreenPointToRay(mousePos);
        //RaycastHit hit;
        //if (Physics.Raycast(ray, out hit, 200f, LayerMask.GetMask("UI"))) {
        //    return true;
        //}
        //return false;
        //}
    

    public Vector3 GetSelectMapPosition() {

        if (Mouse.current == null) {
            return lastPosition;
        }

        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = SceneCamera.transform.position.y;
        Ray ray = SceneCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 200f, placementLayerMask)) { 
            lastPosition = hit.point;
        }
        return lastPosition;
    }
}
