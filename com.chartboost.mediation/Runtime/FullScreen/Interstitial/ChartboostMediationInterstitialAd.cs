using System;

namespace Chartboost.FullScreen.Interstitial
{
    /// <summary>
    /// Chartboost Mediation interstitial ad object.
    /// </summary>
    [Obsolete("ChartboostMediationInterstitialAd has been deprecated, use the new fullscreen API instead.")]
    public class ChartboostMediationInterstitialAd : ChartboostMediationFullScreenBase
    {
        private readonly ChartboostMediationFullScreenBase _platformInterstitial;

        public ChartboostMediationInterstitialAd(string placementName) : base(placementName)
        {
            #if UNITY_EDITOR
            _platformInterstitial = new ChartboostMediationInterstitialUnsupported(placementName);
            #elif UNITY_ANDROID
            _platformInterstitial = new ChartboostMediationInterstitialAndroid(placementName);
            #elif UNITY_IOS
            _platformInterstitial = new ChartboostMediationInterstitialIOS(placementName);
            #else
            _platformInterstitial = new ChartboostMediationInterstitialUnsupported(placementName);
            #endif
        }

        /// <inheritdoc cref="ChartboostMediationFullScreenBase.SetKeyword"/>>
        public override bool SetKeyword(string keyword, string value) 
            => _platformInterstitial.IsValid && _platformInterstitial.SetKeyword(keyword, value);

        /// <inheritdoc cref="ChartboostMediationFullScreenBase.RemoveKeyword"/>>
        public override string RemoveKeyword(string keyword)
            => _platformInterstitial.IsValid ? _platformInterstitial.RemoveKeyword(keyword) : null; 

        /// <inheritdoc cref="ChartboostMediationFullScreenBase.Destroy"/>>
        public override void Destroy()
        {
            if (!_platformInterstitial.IsValid)
                return;
            _platformInterstitial.Destroy();
            base.Destroy();
        }

        /// <inheritdoc cref="ChartboostMediationFullScreenBase.Load"/>>
        public override void Load()
        {
            if (_platformInterstitial.IsValid)
                _platformInterstitial.Load();
        }

        /// <inheritdoc cref="ChartboostMediationFullScreenBase.Show"/>>
        public override void Show()
        {
            if (_platformInterstitial.IsValid)
                _platformInterstitial.Show();
        }

        /// <inheritdoc cref="ChartboostMediationFullScreenBase.ReadyToShow"/>>
        public override bool ReadyToShow() 
            => IsValid && _platformInterstitial.ReadyToShow();

        /// <inheritdoc cref="ChartboostMediationFullScreenBase.ClearLoaded"/>>
        public override void ClearLoaded()
        {
            if (_platformInterstitial.IsValid)
                _platformInterstitial.ClearLoaded();
        }

        ~ChartboostMediationInterstitialAd() => Destroy();
    }
}
