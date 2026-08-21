// ============================================================
//  ShakerPanelController.cs — Which shaker panels the player is
//  allowed to open right now.
//
//  Split out of CocktailShaker, which was an interactable AND a
//  panel state machine. The HSM cares about this: each flow state
//  decides what the player may do, and CocktailFlowBridge (plan
//  §6.2) drives these flags on state entry rather than leaving them
//  to whichever button happened to fire last.
// ============================================================

using UnityEngine;

public class ShakerPanelController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject _methodUI;
    [SerializeField] private GameObject _serveUI;
    [SerializeField] private GameObject _addIceUI;

    [Header("Initial Permissions")]
    [SerializeField] private bool _canShowMethodUI = true;
    [SerializeField] private bool _canShowAddIceUI = false;
    [SerializeField] private bool _canShowServeUI = false;

    /// <summary>
    /// Seeds panel references when this component was created at runtime by the
    /// CocktailShaker compatibility shim. Assign them in the Inspector after migrating.
    /// </summary>
    public void Initialize(GameObject methodUI, GameObject addIceUI, GameObject serveUI)
    {
        if (_methodUI == null) _methodUI = methodUI;
        if (_addIceUI == null) _addIceUI = addIceUI;
        if (_serveUI == null) _serveUI = serveUI;
    }

    public bool CanShowMethodUI => _canShowMethodUI;
    public bool CanShowAddIceUI => _canShowAddIceUI;
    public bool CanShowServeUI => _canShowServeUI;

    public void SetCanShowMethodUI(bool allowed) => _canShowMethodUI = allowed;
    public void SetCanShowAddIceUI(bool allowed) => _canShowAddIceUI = allowed;
    public void SetCanShowServeUI(bool allowed) => _canShowServeUI = allowed;

    /// <summary>Show or hide the serve panel outright.</summary>
    public void SetActiveServe(bool active)
    {
        if (_serveUI != null) _serveUI.gameObject.SetActive(active);
    }

    /// <summary>Toggles whichever panels are currently permitted.</summary>
    public void ToggleUI()
    {
        if (_canShowMethodUI && _methodUI != null) _methodUI.gameObject.SetActive(!_methodUI.gameObject.activeSelf);
        if (_canShowAddIceUI && _addIceUI != null) _addIceUI.gameObject.SetActive(!_addIceUI.gameObject.activeSelf);
        if (_canShowServeUI && _serveUI != null) _serveUI.gameObject.SetActive(!_serveUI.gameObject.activeSelf);
    }

    
    
    /// <summary>Back to the start-of-drink permissions: method only.</summary>
    public void ResetPermissions()
    {
        _canShowMethodUI = true;
        _canShowAddIceUI = false;
        _canShowServeUI = false;
    }

    /// <summary>Locks every panel. Used when the flow leaves the drink-building steps.</summary>
    public void LockAll()
    {
        _canShowMethodUI = false;
        _canShowAddIceUI = false;
        _canShowServeUI = false;
    }
}
