using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


public class IngredientButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    enum MixerOrAlcohol
    {
        None,
        Mixer,
        Alcohol,
    }

    private CocktailMaker cocktailMaker;

    private bool CanClick = false;
    [SerializeField]
    private MixerOrAlcohol m_MixerOrAlcohol;
    [SerializeField]
    private E_Cocktail.Mixer mixer;
    [SerializeField]
    private E_Cocktail.Alcohol alcohol;

    private void Awake()
    {
        cocktailMaker = FindFirstObjectByType<CocktailMaker>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        CanClick = true;
        Debug.Log("You can Click");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CanClick &= !CanClick;
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
                    if (m_MixerOrAlcohol == MixerOrAlcohol.Mixer)
                        cocktailMaker.OnAddMixer?.Invoke(mixer, 1);
                    else if (m_MixerOrAlcohol == MixerOrAlcohol.Alcohol)
                        cocktailMaker.OnAddAlcohol?.Invoke(alcohol, 1);
                }
            }
        }
    }
}
