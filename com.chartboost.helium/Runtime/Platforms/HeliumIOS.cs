#if UNITY_IPHONE
using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace Helium.Platforms
{
    public sealed class HeliumIOS : HeliumExternal
    {
        #region Objective-C Extern Members
        // callback definitions for objective-c layer
        private delegate void ExternHeliumEvent(int errorCode, string errorDescription);

        private delegate void ExternHeliumILRDEvent(string impressionDataJson);

        private delegate void ExternHeliumPlacementEvent(string placementName, int errorCode, string errorDescription);

        private delegate void ExternHeliumWinBidEvent(string placementName, string auctionId, string partnerId, double price);

        private delegate void ExternHeliumRewardEvent(string placementName, int reward);

        [DllImport("__Internal")]
        private static extern void _setLifeCycleCallbacks(ExternHeliumEvent DidStartCallback,
            ExternHeliumILRDEvent DidReceiveILRDCallback);

        [DllImport("__Internal")]
        private static extern void _setInterstitialCallbacks(ExternHeliumPlacementEvent DidLoadCallback,
            ExternHeliumPlacementEvent DidShowCallback, ExternHeliumPlacementEvent DidClickCallback,
            ExternHeliumPlacementEvent DidCloseCallback, ExternHeliumWinBidEvent DidWinBidCallback);

        [DllImport("__Internal")]
        private static extern void _setRewardedCallbacks(ExternHeliumPlacementEvent DidLoadCallback,
            ExternHeliumPlacementEvent DidShowCallback, ExternHeliumPlacementEvent DidClickCallback,
            ExternHeliumPlacementEvent DidCloseCallback, ExternHeliumWinBidEvent DidWinBidCallback,
            ExternHeliumRewardEvent DidReceiveReward);

        [DllImport("__Internal")]
        private static extern void _setBannerCallbacks(ExternHeliumPlacementEvent DidLoadCallback,
            ExternHeliumPlacementEvent DidShowCallback, ExternHeliumPlacementEvent DidClickCallback,
            ExternHeliumWinBidEvent DidWinBidCallback);

        [DllImport("__Internal")]
        private static extern void _heliumSdkInit(string appId, string appSignature, string unityVersion);

        [DllImport("__Internal")]
        private static extern IntPtr _heliumSdkGetInterstitialAd(string placementName);

        [DllImport("__Internal")]
        private static extern IntPtr _heliumSdkGetRewardedAd(string placementName);

        [DllImport("__Internal")]
        private static extern IntPtr _heliumSdkGetBannerAd(string placementName, int size);

        [DllImport("__Internal")]
        private static extern void _heliumSdkSetSubjectToCoppa(bool isSubject);

        [DllImport("__Internal")]
        private static extern void _heliumSdkSetSubjectToGDPR(bool isSubject);

        [DllImport("__Internal")]
        private static extern void _heliumSdkSetUserHasGivenConsent(bool hasGivenConsent);

        [DllImport("__Internal")]
        private static extern void _heliumSetCCPAConsent(bool hasGivenConsent);

        [DllImport("__Internal")]
        private static extern void _heliumSetUserIdentifier(string userIdentifier);

        [DllImport("__Internal")]
        private static extern string _heliumGetUserIdentifier();
        #endregion

        #region Helium
        private static HeliumIOS _instance;

        public HeliumIOS()
        {
            _instance = this;
            LOGTag = "Helium(iOS)";
            _setLifeCycleCallbacks(ExternDidStart, ExternDidReceiveILRD);
            _setInterstitialCallbacks(ExternDidLoadInterstitial, ExternDidShowInterstitial, ExternDidClickInterstitial,
                ExternDidCloseInterstitial, ExternDidWinBidInterstitial);
            _setRewardedCallbacks(ExternDidLoadRewarded, ExternDidShowRewarded, ExternDidClickRewarded,
                ExternDidCloseRewarded, ExternDidWinBidRewarded, ExternDidReceiveReward);
            _setBannerCallbacks(ExternDidLoadBanner, ExternDidShowBanner, ExternDidClickBanner, ExternDidWinBidBanner);
        }

        public override void Init()
        {
            base.Init();
            var appID = HeliumSettings.GetIOSAppId();
            var appSignature = HeliumSettings.GetIOSAppSignature();
            InitWithAppIdAndSignature(appID, appSignature);
        }

        public override void InitWithAppIdAndSignature(string appId, string appSignature)
        {
            base.InitWithAppIdAndSignature(appId, appSignature);
            _heliumSdkInit(appId, appSignature, Application.unityVersion);
            IsInitialized = true;
        }

        public override void SetSubjectToCoppa(bool isSubject)
        {
            base.SetSubjectToCoppa(isSubject);
            _heliumSdkSetSubjectToCoppa(isSubject);
        }

        public override void SetSubjectToGDPR(bool isSubject)
        {
            base.SetSubjectToGDPR(isSubject);
            _heliumSdkSetSubjectToGDPR(isSubject);
        }

        public override void SetUserHasGivenConsent(bool hasGivenConsent)
        {
            base.SetUserHasGivenConsent(hasGivenConsent);
            _heliumSdkSetUserHasGivenConsent(hasGivenConsent);
        }

        public override void SetCCPAConsent(bool hasGivenConsent)
        {
            base.SetCCPAConsent(hasGivenConsent);
            _heliumSetCCPAConsent(hasGivenConsent);
        }

        public override void SetUserIdentifier(string userIdentifier)
        {
            base.SetUserIdentifier(userIdentifier);
            _heliumSetUserIdentifier(userIdentifier);
        }

        public override string GetUserIdentifier()
        {
            base.GetUserIdentifier();
            return _heliumGetUserIdentifier();
        }

        public override HeliumInterstitialAd GetInterstitialAd(string placementName)
        {
            if (!CanFetchAd(placementName))
                return null;

            base.GetInterstitialAd(placementName);

            var adId = _heliumSdkGetInterstitialAd(placementName);
            return adId == IntPtr.Zero ? null : new HeliumInterstitialAd(adId);
        }

        public override HeliumRewardedAd GetRewardedAd(string placementName)
        {
            if (!CanFetchAd(placementName))
                return null;

            base.GetRewardedAd(placementName);

            var adId = _heliumSdkGetRewardedAd(placementName);
            if (adId == IntPtr.Zero)
                return null;

            return adId == IntPtr.Zero ? null : new HeliumRewardedAd(adId);
        }

        public override HeliumBannerAd GetBannerAd(string placementName, HeliumBannerAdSize size)
        {
            if (!CanFetchAd(placementName))
                return null;

            base.GetBannerAd(placementName, size);

            var adId = _heliumSdkGetBannerAd(placementName, (int)size);
            if (adId == IntPtr.Zero)
                return null;

            return adId == IntPtr.Zero ? null : new HeliumBannerAd(adId);
        }
        #endregion

        #region LifeCycle Callbacks
        [MonoPInvokeCallback(typeof(ExternHeliumEvent))]
        private static void ExternDidStart(int errorCode, string errorDescription)
        {
            HeliumEventProcessor.ProcessHeliumEvent(errorCode, errorDescription, _instance.DidStart);
        }

        [MonoPInvokeCallback(typeof(ExternHeliumILRDEvent))]
        private static void ExternDidReceiveILRD(string impressionDataJson)
        {
            HeliumEventProcessor.ProcessEventWithILRD(impressionDataJson,
                _instance.DidReceiveImpressionLevelRevenueData);
        }
        public override event HeliumEvent DidStart;
        public override event HeliumILRDEvent DidReceiveImpressionLevelRevenueData;
        #endregion

        #region Interstitial Callbacks
        [MonoPInvokeCallback(typeof(ExternHeliumPlacementEvent))]
        private static void ExternDidLoadInterstitial(string placementName, int errorCode, string errorDescription)
        {
            HeliumEventProcessor.ProcessHeliumPlacementEvent(placementName, errorCode, errorDescription,
                _instance.DidLoadInterstitial);
        }

        [MonoPInvokeCallback(typeof(ExternHeliumPlacementEvent))]
        private static void ExternDidShowInterstitial(string placementName, int errorCode, string errorDescription)
        {
            HeliumEventProcessor.ProcessHeliumPlacementEvent(placementName, errorCode, errorDescription,
                _instance.DidShowInterstitial);
        }

        [MonoPInvokeCallback(typeof(ExternHeliumPlacementEvent))]
        private static void ExternDidClickInterstitial(string placementName, int errorCode, string errorDescription)
        {
            HeliumEventProcessor.ProcessHeliumPlacementEvent(placementName, errorCode, errorDescription,
                _instance.DidClickInterstitial);
        }

        [MonoPInvokeCallback(typeof(ExternHeliumPlacementEvent))]
        private static void ExternDidCloseInterstitial(string placementName, int errorCode, string errorDescription)
        {
            HeliumEventProcessor.ProcessHeliumPlacementEvent(placementName, errorCode, errorDescription,
                _instance.DidCloseInterstitial);
        }

        [MonoPInvokeCallback(typeof(ExternHeliumWinBidEvent))]
        private static void ExternDidWinBidInterstitial(string placementName, string auctionId, string partnerId,
            double price)
        {
            HeliumEventProcessor.ProcessHeliumBidEvent(placementName, auctionId, partnerId, price,
                _instance.DidWinBidInterstitial);
        }

        public override event HeliumPlacementEvent DidLoadInterstitial;
        public override event HeliumPlacementEvent DidShowInterstitial;
        public override event HeliumPlacementEvent DidClickInterstitial;
        public override event HeliumPlacementEvent DidCloseInterstitial;
        public override event HeliumBidEvent DidWinBidInterstitial;
        #endregion

        #region Rewarded Callbacks
        [MonoPInvokeCallback(typeof(ExternHeliumPlacementEvent))]
        private static void ExternDidLoadRewarded(string placementName, int errorCode, string errorDescription)
        {
            HeliumEventProcessor.ProcessHeliumPlacementEvent(placementName, errorCode, errorDescription,
                _instance.DidLoadRewarded);
        }

        [MonoPInvokeCallback(typeof(ExternHeliumPlacementEvent))]
        private static void ExternDidShowRewarded(string placementName, int errorCode, string errorDescription)
        {
            HeliumEventProcessor.ProcessHeliumPlacementEvent(placementName, errorCode, errorDescription,
                _instance.DidShowRewarded);
        }

        [MonoPInvokeCallback(typeof(ExternHeliumPlacementEvent))]
        private static void ExternDidClickRewarded(string placementName, int errorCode, string errorDescription)
        {
            HeliumEventProcessor.ProcessHeliumPlacementEvent(placementName, errorCode, errorDescription,
                _instance.DidClickRewarded);
        }

        [MonoPInvokeCallback(typeof(ExternHeliumPlacementEvent))]
        private static void ExternDidCloseRewarded(string placementName, int errorCode, string errorDescription)
        {
            HeliumEventProcessor.ProcessHeliumPlacementEvent(placementName, errorCode, errorDescription,
                _instance.DidCloseRewarded);
        }

        [MonoPInvokeCallback(typeof(ExternHeliumWinBidEvent))]
        private static void ExternDidWinBidRewarded(string placementName, string auctionId,
            string partnerId, double price)
        {
            HeliumEventProcessor.ProcessHeliumBidEvent(placementName,  auctionId, partnerId, price,
                _instance.DidWinBidRewarded);
        }

        [MonoPInvokeCallback(typeof(ExternHeliumRewardEvent))]
        private static void ExternDidReceiveReward(string placementName, int reward)
        {
            HeliumEventProcessor.ProcessHeliumRewardEvent(placementName, reward, _instance.DidReceiveReward);
        }

        public override event HeliumPlacementEvent DidLoadRewarded;
        public override event HeliumPlacementEvent DidShowRewarded;
        public override event HeliumPlacementEvent DidCloseRewarded;
        public override event HeliumPlacementEvent DidClickRewarded;
        public override event HeliumBidEvent DidWinBidRewarded;
        public override event HeliumRewardEvent DidReceiveReward;
        #endregion

        #region Banner Callbacks
        [MonoPInvokeCallback(typeof(ExternHeliumPlacementEvent))]
        private static void ExternDidLoadBanner(string placementName, int errorCode, string errorDescription)
        {
            HeliumEventProcessor.ProcessHeliumPlacementEvent(placementName, errorCode, errorDescription,
                _instance.DidLoadBanner);
        }

        [MonoPInvokeCallback(typeof(ExternHeliumPlacementEvent))]
        private static void ExternDidShowBanner(string placementName, int errorCode, string errorDescription)
        {
            HeliumEventProcessor.ProcessHeliumPlacementEvent(placementName, errorCode, errorDescription,
                _instance.DidShowBanner);
        }

        [MonoPInvokeCallback(typeof(ExternHeliumPlacementEvent))]
        private static void ExternDidClickBanner(string placementName, int errorCode, string errorDescription)
        {
            HeliumEventProcessor.ProcessHeliumPlacementEvent(placementName, errorCode, errorDescription,
                _instance.DidClickBanner);
        }

        [MonoPInvokeCallback(typeof(ExternHeliumWinBidEvent))]
        private static void ExternDidWinBidBanner(string placementName, string auctionId,
            string partnerId, double price)
        {
            HeliumEventProcessor.ProcessHeliumBidEvent(placementName, auctionId, partnerId, price,
                _instance.DidWinBidBanner);
        }

        public override event HeliumPlacementEvent DidLoadBanner;
        public override event HeliumPlacementEvent DidShowBanner;
        public override event HeliumPlacementEvent DidClickBanner;
        public override event HeliumBidEvent DidWinBidBanner;
        #endregion
    }
}
#endif