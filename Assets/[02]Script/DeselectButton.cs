using UnityEngine;
using UnityEngine.EventSystems;

public class DeselectButton : MonoBehaviour
{

    GameObject myEventSystem;

    public void Start() {
        //try to find the EventSystem in the scene and assign it to myEventSystem
        if (myEventSystem == null)
        {
            myEventSystem = FindFirstObjectByType<EventSystem>().gameObject;
        }
    }

    public void Deselect()
    {
        myEventSystem.GetComponent<UnityEngine.EventSystems.EventSystem>().SetSelectedGameObject(null);
    }
        
}

