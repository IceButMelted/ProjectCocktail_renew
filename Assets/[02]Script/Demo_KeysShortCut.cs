using UnityEngine;
using UnityEngine.Events;

public class Demo_KeysShortCut : MonoBehaviour
{
   public UnityEvent OnPressReset;
    
    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)){
            OnPressReset?.Invoke();
        }
    }
}
