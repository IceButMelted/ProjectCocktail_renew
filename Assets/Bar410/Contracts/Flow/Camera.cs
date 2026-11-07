using static E_Cocktail;

namespace Bar410.Contracts
{
    public enum CameraShot { Bar, Shaker, Garnish }

    /// <summary>impl: Host CameraDirector.</summary>
    public interface ICameraDirector
    {
        string ActiveCameraId { get; }
        void Show(CameraShot shot);
        void Cut(CameraShot shot);
        void SwitchTo(string cameraId);
        void CutTo(string cameraId);
        void SetLookFree(bool free);
        void ResetLook();
        void AimCustomerInset(CharacterName who);
    }
}
