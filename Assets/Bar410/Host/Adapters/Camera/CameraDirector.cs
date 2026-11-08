using System;
using Bar410.Contracts;
using Bar410.WorldViews;
using UnityEngine;
using static E_Cocktail;

namespace Bar410.Host
{
    [Serializable]
    public sealed class CameraState { public string activeCameraId; }

    /// <summary>
    /// ICameraDirector over the old CameraManager (G3-T5), plus the "camera" snapshot source.
    /// The customer inset camera belongs to a WorldViewSource (single owner of camera.enabled); the manager's own inset fields stay empty.
    /// </summary>
    public sealed class CameraDirector : ICameraDirector, ISnapshotSource
    {
        readonly CameraManager _manager;
        readonly WorldViewSource _inset;      // may be null
        readonly ICustomerStage _stage;       // may be null (no inset)
        readonly Vector3 _insetOffset;

        public CameraDirector(CameraManager manager, WorldViewSource customerInset, ICustomerStage stage, Vector3 insetOffset)
        { _manager = manager; _inset = customerInset; _stage = stage; _insetOffset = insetOffset; }

        public string ActiveCameraId => _manager.ActiveCameraId;

        public void Show(CameraShot shot)
        {
            switch (shot)
            {
                case CameraShot.Bar: _manager.ShowBar(); break;
                case CameraShot.Shaker: _manager.ShowShaker(); break;
                case CameraShot.Garnish: _manager.ShowGarnish(); break;
            }
        }

        public void Cut(CameraShot shot)
        {
            switch (shot)
            {
                case CameraShot.Bar: _manager.CutToBar(); break;
                case CameraShot.Shaker: _manager.CutToShaker(); break;
                case CameraShot.Garnish: _manager.CutToGarnish(); break;
            }
        }

        public void SwitchTo(string cameraId) => _manager.SwitchCamera(cameraId);   // unknown id: manager logs an error, no change
        public void CutTo(string cameraId) => _manager.CutToCamera(cameraId);
        public void SetLookFree(bool free) => _manager.SetLookFixed(!free);          // fixing snaps the view forward
        public void ResetLook() => _manager.ResetLook();

        public void AimCustomerInset(CharacterName who)
        {
            var handle = _inset != null ? _inset.Handle : null;
            if (handle == null) return;

            var anchor = who != CharacterName.None && _stage != null ? _stage.Anchor(who) : null;
            if (anchor == null) { handle.SetRendering(false); return; }

            handle.Camera.transform.position = anchor.position + _insetOffset;
            handle.SetRendering(true);
        }

        // ── ISnapshotSource "camera" ───────────────────────

        public string Key => "camera";
        public Type StateType => typeof(CameraState);
        public object Capture() => new CameraState { activeCameraId = ActiveCameraId };

        public void Restore(object state)
        {
            // Switch/Cut are absolute, so a re-run is harmless. Missing state = back to the bar shot.
            if (state is CameraState { activeCameraId: { Length: > 0 } id }) CutTo(id);
            else Cut(CameraShot.Bar);
        }
    }
}
