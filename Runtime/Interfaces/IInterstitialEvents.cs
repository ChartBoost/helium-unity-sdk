// ReSharper disable InconsistentNaming
using UnityEngine.Scripting;

namespace Helium.Interfaces
{
    /// <summary>
    /// All Interstitial placement callbacks.
    /// </summary>
    public interface IInterstitialEvents
    {
        /// <summary>
        ///   Called after an interstitial has been loaded from the Helium API
        ///   servers and cached locally.
        /// </summary>
        [Preserve]
        public event HeliumPlacementEvent DidLoadInterstitial;

        /// <summary>
        ///   Called after an interstitial has been displayed on screen.
        /// </summary>
        [Preserve]
        public event HeliumPlacementEvent DidShowInterstitial;

        /// <summary>
        ///  Called after an interstitial has been closed.
        ///  Implement to be notified of when an interstitial has been closed for a given placement.
        /// </summary>
        [Preserve]
        public event HeliumPlacementEvent DidCloseInterstitial;

        /// <summary>
        ///  Called after an interstitial has been clicked.
        ///  Implement to be notified of when an interstitial has been clicked for a given placement.
        /// </summary>
        [Preserve]
        public event HeliumPlacementEvent DidClickInterstitial;
        
        /// <summary>
        ///   Called with bid information after an interstitial has been loaded from the Helium API
        ///   servers and cached locally.
        /// </summary>
        [Preserve]
        public event HeliumBidEvent DidWinBidInterstitial;
    }
}