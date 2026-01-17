using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ScaleOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 initScale;

    [SerializeField]
    private float newScale = 1.1f;

    [SerializeField]
    private float scaleDuration = 0.2f; // Optional: smooth scaling

    private Vector3 targetScale;
    private bool isScaling = false;

    private void Awake()
    {
        initScale = transform.localScale;
        targetScale = initScale;

        // Check if object has a collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"ScaleOnHover on '{gameObject.name}' requires a Collider component! Adding BoxCollider...");
            gameObject.AddComponent<BoxCollider>();
        }
    }

    private void Update()
    {
        // Optional: Smooth scaling
        if (isScaling)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime / scaleDuration);

            if (Vector3.Distance(transform.localScale, targetScale) < 0.001f)
            {
                transform.localScale = targetScale;
                isScaling = false;
            }
        }
    }

    //private void OnMouseEnter()
    //{
    //    ChangeScale(true);
    //    Debug.Log("Mouse entered: " + gameObject.name);
    //}

    //private void OnMouseExit()
    //{
    //    ChangeScale(false);
    //    Debug.Log("Mouse exited: " + gameObject.name);
    //}

    //private void OnMouseDown()
    //{
    //    Debug.Log("Mouse down on: " + gameObject.name);
    //}

    private void ChangeScale(bool status)
    {
        if (status)
        {
            targetScale = initScale * newScale;
        }
        else
        {
            targetScale = initScale;
        }

        // For instant scaling, uncomment this line and comment out the smooth scaling in Update()
        // transform.localScale = targetScale;

        // For smooth scaling
        isScaling = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ChangeScale(true);
        Debug.Log("Mouse entered: " + gameObject.name);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ChangeScale(false);
        Debug.Log("Mouse entered: " + gameObject.name);
    }
}