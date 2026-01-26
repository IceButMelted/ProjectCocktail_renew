using UnityEngine;
using UnityEditor;
using UnityEditor.Actions;
using UnityEngine.Events;
using System;
using static E_Cocktail;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using NUnit.Framework.Internal;

public class BTN_2_5D : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField]
    private Material m_Material;
    [SerializeField]
    private Texture2D T_Default;
    [SerializeField]
    private Texture2D T_Hover;
    [SerializeField]
    private Texture2D T_Clicked;

    [Space(10)]
    public UnityEvent OnClicked;


    private void Awake()
    {
        m_Material = GetComponent<MeshRenderer>().material;;
        m_Material.SetFloat("_EmssionStrength", 0);
        m_Material.SetTexture("_CurrentTexture", T_Default);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        m_Material.SetFloat("_EmssionStrength", 0.25f);
        m_Material.SetTexture("_CurrentTexture", T_Hover);
        Debug.Log("You can Click");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        m_Material.SetFloat("_EmssionStrength", 0);
        m_Material.SetTexture("_CurrentTexture", T_Default);
    }

    public void OnPointerDown(PointerEventData eventData)
    {

        this.OnClicked?.Invoke();
        m_Material.SetFloat("_EmssionStrength", 0.125f);
        m_Material.SetTexture("_CurrentTexture", T_Clicked);
    }

    public void TestFire() {
        Debug.Log($"{this.name} is Fired");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        m_Material.SetFloat("_EmssionStrength", 0);
        m_Material.SetTexture("_CurrentTexture", T_Default);
    }
}
