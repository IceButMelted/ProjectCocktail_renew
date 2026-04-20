using UnityEngine;

public class ToggleActive : MonoBehaviour
{
    public void ToggleAtiveGameObject() {
        gameObject.SetActive(!gameObject.active);
    }
}
