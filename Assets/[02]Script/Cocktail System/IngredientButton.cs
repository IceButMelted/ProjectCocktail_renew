using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


public class IngredientButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    enum MixerOrAlcohol
    {
        None,
        Mixer,
        Alcohol,
    }

    [SerializeField]
    private Material m_Material;
    [SerializeField]
    private Texture2D T_Default;
    [SerializeField]
    private Texture2D T_Hover;
    [SerializeField]
    private Texture2D T_Clicked;

    private CocktailMaker cocktailMaker;

    private bool CanClick = false;
    [SerializeField]
    private MixerOrAlcohol TypeIngredient;
    [SerializeField]
    private E_Cocktail.Mixer mixer;
    [SerializeField]
    private E_Cocktail.Alcohol alcohol;

    private void Awake()
    {
        m_Material = GetComponent<MeshRenderer>().material;
        ;  

        cocktailMaker = FindFirstObjectByType<CocktailMaker>();
        m_Material.SetFloat("_EmssionStrength", 0);
        m_Material.SetTexture("_CurrentTexture", T_Default);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        CanClick = true;
        m_Material.SetFloat("_EmssionStrength", 0.25f);
        m_Material.SetTexture("_CurrentTexture", T_Hover);
        Debug.Log("You can Click");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CanClick &= !CanClick;
        m_Material.SetFloat("_EmssionStrength", 0);
        m_Material.SetTexture("_CurrentTexture", T_Default);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //was press mouse down
        if (CanClick)
        {
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    if (TypeIngredient == MixerOrAlcohol.Mixer)
                        cocktailMaker.OnAddMixer?.Invoke(mixer, 1);
                    else if (TypeIngredient == MixerOrAlcohol.Alcohol)
                        cocktailMaker.OnAddAlcohol?.Invoke(alcohol, 1);
                    cocktailMaker.OnAddIngredient?.Invoke();
                }
            }
        }
        m_Material.SetFloat("_EmssionStrength", 0.125f);
        m_Material.SetTexture("_CurrentTexture", T_Clicked);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        m_Material.SetFloat("_EmssionStrength", 0);
        m_Material.SetTexture("_CurrentTexture", T_Default);
    }
}
