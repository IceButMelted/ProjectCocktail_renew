using System;
using Bar410.Contracts;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Bar410.Host
{
    /// <summary>
    /// World half of the garnish station: the one glass on the plate, its decoration and the pour.
    /// GarnishManager (UI) keeps every rule (glass first, one pour per glass, no ice removal after the pour)
    /// and calls only this. Host-only: no asmdef consumer needs it.
    /// </summary>
    public interface IGarnishTable
    {
        PlacedGlassInstance Glass { get; }
        /// <summary>What has been applied to the current glass (recorded on every Apply*).</summary>
        GarnishDecoration Decoration { get; }
        /// <summary>Destroys the previous glass first. False if the option has no usable prefab.</summary>
        bool ChooseGlass(SO_GlassOption option);
        /// <summary>Cancels a running pour, then destroys the glass.</summary>
        void Clear();
        void ApplyIce(bool on);
        void ApplyItem(int slot, SO_GarnishItemOption item);
        void ApplyRim(SO_GarnishRimOption rim);
        void Pour(S_Drink drink);
        /// <summary>The current glass's pour finished filling.</summary>
        event Action FillComplete;
        /// <summary>A garnish slot on the current glass was clicked (GarnishSlotButton).</summary>
        event Action<int> SlotClicked;
    }

    /// <summary>Plain implementation of <see cref="IGarnishTable"/> (extracted from GarnishManager).</summary>
    public sealed class GarnishTableCore : IGarnishTable
    {
        readonly Transform _spawnPoint;
        readonly PourVisual _pour;
        GarnishDecoration _decoration;
        PlacedGlassInstance _pouring;

        public GarnishTableCore(Transform spawnPoint, PourVisual pour) { _spawnPoint = spawnPoint; _pour = pour; }

        public PlacedGlassInstance Glass { get; private set; }
        public GarnishDecoration Decoration => _decoration;
        public event Action FillComplete;
        public event Action<int> SlotClicked;

        public bool ChooseGlass(SO_GlassOption option)
        {
            if (_spawnPoint == null) { Debug.LogWarning("[GarnishTable] No glass spawn point."); return false; }
            if (option == null || option.PlacedPrefab == null)
            {
                Debug.LogWarning("[GarnishTable] ChooseGlass needs an SO_GlassOption with a PlacedPrefab.");
                return false;
            }

            Clear();
            var instance = Object.Instantiate(option.PlacedPrefab, _spawnPoint.position, _spawnPoint.rotation);
            var glass = instance.GetComponent<PlacedGlassInstance>();
            if (glass == null)
            {
                Debug.LogWarning($"[GarnishTable] '{option.PlacedPrefab.name}' has no PlacedGlassInstance component.");
                Object.Destroy(instance);
                return false;
            }

            glass.Initialize(option);
            glass.GarnishSlotClicked += OnSlotClicked;
            Glass = glass;
            _decoration = new GarnishDecoration { glass = option };
            return true;
        }

        public void Clear()
        {
            if (_pouring != null && _pour != null) _pour.Cancel();   // the pour must let go of the glass before it dies
            StopListeningToPour();

            if (Glass != null)
            {
                Glass.GarnishSlotClicked -= OnSlotClicked;
                Object.Destroy(Glass.gameObject);
            }
            Glass = null;
            _decoration = default;
        }

        public void ApplyIce(bool on)
        {
            if (Glass == null) return;
            Glass.ApplyIce(on);
            _decoration.ice = on;
        }

        public void ApplyItem(int slot, SO_GarnishItemOption item)
        {
            if (Glass == null) return;
            Glass.ApplyGarnishItem(slot, item);
            if (slot == 0) _decoration.item0 = item;
            else if (slot == 1) _decoration.item1 = item;
        }

        public void ApplyRim(SO_GarnishRimOption rim)
        {
            if (Glass == null) return;
            Glass.ApplyGarnishRim(rim);
            _decoration.rim = rim;
        }

        public void Pour(S_Drink drink)
        {
            if (Glass == null) return;
            StopListeningToPour();   // re-pouring into the same glass must not double-subscribe
            _pouring = Glass;
            _pouring.OnFillComplete += OnFilled;
            _pouring.Pour(_pour, drink);
        }

        void OnFilled()
        {
            StopListeningToPour();
            FillComplete?.Invoke();
        }

        void StopListeningToPour()
        {
            if (_pouring == null) return;
            _pouring.OnFillComplete -= OnFilled;
            _pouring = null;
        }

        void OnSlotClicked(int slot) => SlotClicked?.Invoke(slot);
    }
}
