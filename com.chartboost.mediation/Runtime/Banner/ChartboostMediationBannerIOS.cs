#if UNITY_IOS
using System;
using System.Runtime.InteropServices;

namespace Chartboost.Banner
{
    /// <summary>
    /// Chartboost Mediation banner object for iOS.
    /// </summary>
    public class ChartboostMediationBannerIOS : ChartboostMediationBannerBase
    {
        private readonly IntPtr _uniqueId;

        public ChartboostMediationBannerIOS(string placement, ChartboostMediationBannerAdSize size) : base(placement, size)
        {
            LogTag = "ChartboostMediation Banner (iOS)";
            _uniqueId = _heliumSdkGetBannerAd(placement, (int)size);
        }

        /// <inheritdoc cref="ChartboostMediationBannerBase.SetKeyword"/>>
        public override bool SetKeyword(string keyword, string value)
        {
            base.SetKeyword(keyword, value);
            return _heliumSdkBannerSetKeyword(_uniqueId, keyword, value);
        }

        /// <inheritdoc cref="ChartboostMediationBannerBase.RemoveKeyword"/>>
        public override string RemoveKeyword(string keyword)
        {
            base.RemoveKeyword(keyword);
            return _heliumSdkBannerRemoveKeyword(_uniqueId, keyword);
        }

        /// <inheritdoc cref="ChartboostMediationBannerBase.Load"/>>
        public override void Load(ChartboostMediationBannerAdScreenLocation location)
        {
            base.Load(location);
            _heliumSdkBannerAdLoad(_uniqueId, (int)location);
        }

        /// <inheritdoc cref="ChartboostMediationBannerBase.SetVisibility"/>>
        public override void SetVisibility(bool isVisible)
        {
            base.SetVisibility(isVisible);
            _heliumSdkBannerSetVisibility(_uniqueId, isVisible);
        }

        /// <inheritdoc cref="ChartboostMediationBannerBase.ClearLoaded"/>>
        public override void ClearLoaded()
        {
            base.ClearLoaded();
            _heliumSdkBannerClearLoaded(_uniqueId);
        }

        /// <inheritdoc cref="ChartboostMediationBannerBase.Remove"/>>
        public override void Remove()
        {
            base.Remove();
            _heliumSdkBannerRemove(_uniqueId);
        }

        ~ChartboostMediationBannerIOS()
            => _heliumSdkFreeBannerAdObject(_uniqueId);

        #region External Methods
        [DllImport("__Internal")]
        private static extern IntPtr _heliumSdkGetBannerAd(string placementName, int size);
        [DllImport("__Internal")]
        private static extern bool _heliumSdkBannerSetKeyword(IntPtr uniqueId, string keyword, string value);
        [DllImport("__Internal")]
        private static extern string _heliumSdkBannerRemoveKeyword(IntPtr uniqueID, string keyword);
        [DllImport("__Internal")]
        private static extern void _heliumSdkBannerAdLoad(IntPtr uniqueID, int screenLocation);
        [DllImport("__Internal")]
        private static extern void _heliumSdkBannerClearLoaded(IntPtr uniqueID);
        [DllImport("__Internal")]
        private static extern bool _heliumSdkBannerRemove(IntPtr uniqueID);
        [DllImport("__Internal")]
        private static extern bool _heliumSdkBannerSetVisibility(IntPtr uniqueID, bool isVisible);
        [DllImport("__Internal")]
        private static extern void _heliumSdkFreeBannerAdObject(IntPtr uniqueID);
        #endregion
    }
}
#endif