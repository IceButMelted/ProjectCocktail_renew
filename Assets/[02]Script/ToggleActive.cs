using UnityEngine;

public class ToggleActive : MonoBehaviour
{
    public void ToggleActiveGameObject() {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
