using UnityEngine;

public class ActiveGameObject : MonoBehaviour
{
    public bool initState;

    private void Awake()
    {
        gameObject.SetActive(initState);
    }

    public void SetActiveState(bool state)
    {
        gameObject.SetActive(state);
    }

    //flipflop state
    public void SetActiveState() {
        initState = !initState;
        gameObject.SetActive(initState);
    }
}
