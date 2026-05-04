using UnityEngine;
using Yarn.Unity;

public class Test_NPCGetIn : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [YarnCommand("MoveIn")]
    public void MoveIn() {
        Debug.Log($"{name} Move In");
    }

    [YarnCommand("MoveOut")]
    public void MoveOut() {
        Debug.Log($"{name} Move Out");
    }
}
