﻿using System;
using System.Collections;
using System.Text;
using Helium;
using UnityEngine;
using UnityEngine.UI;

public class Demo : MonoBehaviour
{
#if UNITY_ANDROID
    private const string AppID = "5a4e797538a5f00cf60738d6";
    private const string AppSIG = "d29d75ce6213c746ba986f464e2b4a510be40399";
    private const string DefaultPlacementInterstitial = "CBInterstitial";
    private const string DefaultPlacementRewarded = "CBRewarded";
#else
    private const string AppID = "59c04299d989d60fc5d2c782";
    private const string AppSIG = "6deb8e06616569af9306393f2ce1c9f8eefb405c";
    private const string DefaultPlacementInterstitial = "Startup";
    private const string DefaultPlacementRewarded = "Startup-Rewarded";
#endif
    private const string DefaultPlacementBanner = "AllNetworkBanner";
    private const string DefaultUserIdentifier = "123456";
    private const string DefaultRewardedAdCustomData = "{\"testkey\":\"testvalue\"}";

    // advertisement type selection
    public GameObject fullScreenPanel;
    public Button fullScreenTypeButton;
    public GameObject bannerPanel;
    public Button bannerTypeButton;

    // interstitial controls
    public InputField i12PlacementInputField;
    public Button i12CacheButton;
    public Button i12ClearButton;
    public Button i12ShowButton;
    private HeliumInterstitialAd _interstitialAd;

    // rewarded controls
    public InputField rewardedPlacementInputField;
    public Button rewardedCacheButton;
    public Button rewardedClearButton;
    public Button rewardedShowButton;
    private HeliumRewardedAd _rewardedAd;

    // banner controls
    public InputField bannerPlacementInputField;
    public Button bannerCreateButton;
    public Button bannerRemoveButton;
    public Button bannerDisplayButton;
    public Button bannerClearButton;
    public Dropdown bannerSizeDropdown;
    public Dropdown bannerPlacementDropdown;
    private HeliumBannerAd _bannerAd;
    private bool _bannerAdIsVisible;

    public ScrollRect outputTextScrollRect;
    public Text outputText;

    public GameObject objectToDestroyForTest;

    #region Lifecycle

    private void Awake()
    {        
        HeliumSDK.DidStart += DidStartHelium;
        HeliumSDK.DidReceiveImpressionLevelRevenueData += DidReceiveImpressionLevelRevenueData;
        HeliumSDK.UnexpectedSystemErrorDidOccur += UnexpectedSystemErrorDidOccur;
        SetupInterstitialDelegates();
        SetupRewardedDelegates();
        SetupBannerDelegates();
    }

    private void Start()
    {
        if (outputText != null)
            outputText.text = string.Empty;
        fullScreenPanel.SetActive(true);
        bannerPanel.SetActive(false);

        i12PlacementInputField.SetTextWithoutNotify(DefaultPlacementInterstitial);
        rewardedPlacementInputField.SetTextWithoutNotify(DefaultPlacementRewarded);
        bannerPlacementInputField.SetTextWithoutNotify(DefaultPlacementBanner);

        HeliumSDK.StartWithAppIdAndAppSignature(AppID, AppSIG);
    }

    private void OnDestroy()
    {
        if (_interstitialAd != null)
        {
            _interstitialAd.ClearLoaded();
            _interstitialAd.Destroy();
            Log("destroyed an existing interstitial");
        }
        if (_rewardedAd != null)
        {
            _rewardedAd.ClearLoaded();
            _rewardedAd.Destroy();
            Log("destroyed an existing rewarded");
        }
        if (_bannerAd != null)
        {
            _bannerAd.ClearLoaded();
            _bannerAd.Destroy();
            Log("destroyed an existing banner");
        }
    }

    private void DidStartHelium(HeliumError error)
    {
        Log($"DidStart: {error}");
        HeliumSDK.SetUserIdentifier(DefaultUserIdentifier);

        if (error != null) return;
        HeliumSDK.SetSubjectToGDPR(false);
        HeliumSDK.SetSubjectToCoppa(false);
        HeliumSDK.SetUserHasGivenConsent(true);
    }

    private void DidReceiveImpressionLevelRevenueData(string placement, Hashtable impressionData)
    {
        var json =  HeliumJSON.Serialize(impressionData);
        Log($"DidReceiveImpressionLevelRevenueData {placement}: {json}");
    }

    public void OnSelectFullScreenClicked()
    {
        fullScreenPanel.SetActive(true);
        bannerPanel.SetActive(false);
    }

    public void OnSelectBannersClicked()
    {
        fullScreenPanel.SetActive(false);
        bannerPanel.SetActive(true);
    }

    #endregion

    #region Interstitials

    private void SetupInterstitialDelegates()
    {
        HeliumSDK.DidLoadInterstitial += DidLoadInterstitial;
        HeliumSDK.DidShowInterstitial += DidShowInterstitial;
        HeliumSDK.DidCloseInterstitial += DidCloseInterstitial;
        HeliumSDK.DidClickInterstitial += DidClickInterstitial;
        HeliumSDK.DidWinBidInterstitial += DidWinBidInterstitial;
    }

    public void OnCacheInterstitialClick()
    {
        _interstitialAd = HeliumSDK.GetInterstitialAd(i12PlacementInputField.text);

        if (_interstitialAd == null)
        {
            Log("Interstitial Ad not found");
            return;
        }

        // example keywords usage
        _interstitialAd.SetKeyword("i12_keyword1", "i12_value1"); // accepted set
        _interstitialAd.SetKeyword("i12_keyword2", "i12_value2"); // accepted set
        _interstitialAd.SetKeyword(GenerateRandomString(65), "i12_value2"); // rejected set
        _interstitialAd.SetKeyword("i12_keyword3", GenerateRandomString(257)); // rejected set
        _interstitialAd.SetKeyword("i12_keyword4", "i12_value4"); // accepted set
        var keyword4 = this._interstitialAd.RemoveKeyword("i12_keyword4"); // removal of existing
        _interstitialAd.RemoveKeyword("i12_keyword4"); // removal of non-existing
        _interstitialAd.SetKeyword("i12_keyword5", keyword4); // accepted set using prior value
        _interstitialAd.SetKeyword("i12_keyword6", "i12_value6"); // accepted set
        _interstitialAd.SetKeyword("i12_keyword6", "i12_value6_replaced"); // accepted replace

        _interstitialAd.Load();
    }

    public void OnClearInterstitialClick()
    {
        _interstitialAd.ClearLoaded();
    }

    public void OnShowInterstitialClick()
    {
        if (_interstitialAd.ReadyToShow())
            _interstitialAd.Show();
    }

    private void DidLoadInterstitial(string placementName, HeliumError error)
    {
        Log($"DidLoadInterstitial {placementName}: {error}");
    }

    private  void DidShowInterstitial(string placementName, HeliumError error)
    {
        Log($"DidShowInterstitial {placementName}: {error}");
    }

    private void DidCloseInterstitial(string placementName, HeliumError error)
    {
        Log($"DidCloseInterstitial {placementName}: {error}");
    }

    private void DidWinBidInterstitial(string placementName, HeliumBidInfo info)
    {
        Log($"DidWinBidInterstitial {placementName}: ${info.Price:F4}, Auction Id: {info.AuctionId}, Partner Id: {info.PartnerId}");
    }

    private void DidClickInterstitial(string placementName, HeliumError error)
    {
        Log($"DidClickInterstitial {placementName}: {error}");
    }

    #endregion

    #region Rewarded

    private void SetupRewardedDelegates()
    {
        HeliumSDK.DidLoadRewarded += DidLoadRewarded;
        HeliumSDK.DidShowRewarded += DidShowRewarded;
        HeliumSDK.DidCloseRewarded += DidCloseRewarded;
        HeliumSDK.DidReceiveReward += DidReceiveReward;
        HeliumSDK.DidWinBidRewarded += DidWinBidRewarded;
        HeliumSDK.DidClickRewarded += DidClickRewarded;
    }

    public void OnCacheRewardedClick()
    {
        _rewardedAd = HeliumSDK.GetRewardedAd(rewardedPlacementInputField.text);
        
        if (_rewardedAd == null)
        {
            Log("Rewarded Ad not found");
            return;
        }

        // example keywords usage
        _rewardedAd.SetKeyword("rwd_keyword1", "rwd_value1"); // accepted set
        _rewardedAd.SetKeyword("rwd_keyword2", "rwd_value2"); // accepted set
        _rewardedAd.SetKeyword(GenerateRandomString(65), "rwd_value2"); // rejected set
        _rewardedAd.SetKeyword("rwd_keyword3", GenerateRandomString(257)); // rejected set
        _rewardedAd.SetKeyword("rwd_keyword4", "rwd_value4"); // accepted set
        var keyword4 = this._rewardedAd.RemoveKeyword("rwd_keyword4"); // removal of existing
        _rewardedAd.RemoveKeyword("rwd_keyword4"); // removal of non-existing
        _rewardedAd.SetKeyword("rwd_keyword5", keyword4); // accepted set using prior value
        _rewardedAd.SetKeyword("rwd_keyword6", "rwd_value6"); // accepted set
        _rewardedAd.SetKeyword("rwd_keyword6", "rwd_value6_replaced"); // accepted replace

        // example custom data usage
        var bytesToEncode = Encoding.UTF8.GetBytes(DefaultRewardedAdCustomData);
        var encodedText = Convert.ToBase64String(bytesToEncode);
        _rewardedAd.SetCustomData(encodedText);

        _rewardedAd.Load();
    }

    public void OnClearRewardedClick()
    {
        _rewardedAd.ClearLoaded();
    }

    public void OnShowRewardedClick()
    {
        if (_rewardedAd.ReadyToShow())
            _rewardedAd.Show();
    }

    private void DidLoadRewarded(string placementName, HeliumError error)
    {
        Log($"DidLoadRewarded {placementName}: {error}");
    }

    private void DidShowRewarded(string placementName, HeliumError error)
    {
        Log($"DidShowRewarded {placementName}: {error}");
    }

    private void DidCloseRewarded(string placementName, HeliumError error)
    {
        Log($"DidCloseRewarded {placementName}: {error}");
    }

    private void DidReceiveReward(string placementName, int reward)
    {
        Log($"DidReceiveReward {placementName}: {reward}");
    }

    private void DidWinBidRewarded(string placementName, HeliumBidInfo info)
    {
        Log($"DidWinBidRewarded {placementName}: {placementName}: ${info.Price:F4}, Auction Id: {info.AuctionId}, Partner Id: {info.PartnerId}");
    }

    private void DidClickRewarded(string placementName, HeliumError error)
    {
        Log($"DidClickRewarded {placementName}: {error}");
    }

    #endregion

    #region Banners

    private void SetupBannerDelegates()
    {
        HeliumSDK.DidLoadBanner += DidLoadBanner;
        HeliumSDK.DidShowBanner += DidShowBanner;
        HeliumSDK.DidWinBidBanner += DidWinBidBanner;
        HeliumSDK.DidClickBanner += DidClickBanner;
    }

    public void OnCreateBannerClick()
    {
        var size = bannerSizeDropdown.value switch
        {
            2 => HeliumBannerAdSize.Leaderboard,
            1 => HeliumBannerAdSize.MediumRect,
            _ => HeliumBannerAdSize.Standard
        };
        if (_bannerAd == null)
        {
            Log("Creating banner on placement: " + bannerPlacementInputField.text + " with size: " + size);
            _bannerAd = HeliumSDK.GetBannerAd(bannerPlacementInputField.text, size);
            
            if (_bannerAd == null)
            {
                Log("Banner not found");
                return;
            }

            // example keywords usage
            _bannerAd.SetKeyword("bnr_keyword1", "bnr_value1"); // accepted set
            _bannerAd.SetKeyword("bnr_keyword2", "bnr_value2"); // accepted set
            _bannerAd.SetKeyword(GenerateRandomString(65), "bnr_value2"); // rejected set
            _bannerAd.SetKeyword("bnr_keyword3", GenerateRandomString(257)); // rejected set
            _bannerAd.SetKeyword("bnr_keyword4", "bnr_value4"); // accepted set
            var keyword4 = this._bannerAd.RemoveKeyword("bnr_keyword4"); // removal of existing
            _bannerAd.RemoveKeyword("bnr_keyword4"); // removal of non-existing
            _bannerAd.SetKeyword("bnr_keyword5", keyword4); // accepted set using prior value
            _bannerAd.SetKeyword("bnr_keyword6", "bnr_value6"); // accepted set
            _bannerAd.SetKeyword("bnr_keyword6", "bnr_value6_replaced"); // accepted replace
        }
        _bannerAd.Load();
    }

    public void OnDisplayBannerClick()
    {
        if (_bannerAd.ReadyToShow())
        {
            var screenPos = bannerPlacementDropdown.value switch
            {
                0 => HeliumBannerAdScreenLocation.TopLeft,
                1 => HeliumBannerAdScreenLocation.TopCenter,
                2 => HeliumBannerAdScreenLocation.TopRight,
                3 => HeliumBannerAdScreenLocation.Center,
                4 => HeliumBannerAdScreenLocation.BottomLeft,
                5 => HeliumBannerAdScreenLocation.BottomCenter,
                6 => HeliumBannerAdScreenLocation.BottomRight,
                _ => HeliumBannerAdScreenLocation.TopCenter
            };
            _bannerAd.Show(screenPos);
            _bannerAdIsVisible = true;
        }
        else
        {
            Log("Banner is not ready to load.");
        }
    }

    public void OnRemoveBannerClick()
    {
        _bannerAd?.Remove();
        _bannerAd = null;
        _bannerAdIsVisible = false;
        Log("Banner Removed");
    }

    public void OnClearBannerClick()
    {
        _bannerAd?.ClearLoaded();
        _bannerAdIsVisible = false;
        Log("Banner Cleared");
    }

    public void OnToggleBannerVisibilityClick()
    {
        if (_bannerAd != null)
        {
            _bannerAdIsVisible = !_bannerAdIsVisible;
            _bannerAd.SetVisibility(_bannerAdIsVisible);
        }
        Log("Banner Visibility Toggled");
    }

    private void DidLoadBanner(string placementName, HeliumError error)
    {
        Log($"DidLoadBanner{placementName}: {error}");
    }

    private void DidShowBanner(string placementName, HeliumError error)
    {
        if (error == null)
            _bannerAdIsVisible = true;
        Log($"DidShowBanner {placementName}: {error}");
    }

    private void DidWinBidBanner(string placementName, HeliumBidInfo info)
    {
        Log($"DidWinBidBanner {placementName}: {placementName}: ${info.Price:F4}, Auction Id: {info.AuctionId}, Partner Id: {info.PartnerId}");
    }

    private void DidClickBanner(string placementName, HeliumError error)
    {
        Log($"DidClickBanner {placementName}: {error}");
    }

    #endregion

    #region Utility

    private void Log(string text)
    {
        Debug.Log(text);
        if (outputText != null)
            outputText.text += $"{text}\n";
    }

    public void OnTestDestroyClick()
    {
        if (objectToDestroyForTest != null)
            Destroy(objectToDestroyForTest);
    }

    public void OnClearLogClick()
    {
        outputText.text = null;
        outputTextScrollRect.normalizedPosition = new Vector2(0, 1);
    }

    /// <summary>
    /// Generates a random string to help demonstrate the designed constraints
    /// of setting keywords with a word that is too long or a value that is too long.
    /// </summary>
    /// <param name="length">The length of the string to generate.</param>
    /// <returns>A random string of the specified length.</returns>
    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var stringChars = new char[length];
        var random = new System.Random();
        for (var i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }
        return new string(stringChars);
    }

    private static void UnexpectedSystemErrorDidOccur(HeliumError error)
    {
        Debug.LogErrorFormat(error.ErrorDescription);
    }

    #endregion
}