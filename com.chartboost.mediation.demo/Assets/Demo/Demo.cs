using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using Chartboost;
using Chartboost.AdFormats.Banner;
using Chartboost.AdFormats.Banner.Unity;
using Chartboost.AdFormats.Fullscreen;
using Chartboost.Banner;
using Chartboost.Requests;
using Chartboost.Utilities;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Demo : MonoBehaviour
{
    private const string DefaultPlacementFullscreen = "CBRewarded";
    private const string DefaultPlacementBanner = "ACAdaptiveBanner";
    private const string DefaultUserIdentifier = "123456";
    private const string DefaultFullscreenAdCustomData = "{\"testkey\":\"testvalue\"}";

    // advertisement type selection
    public GameObject fullScreenPanel;
    public GameObject bannerPanel;

    // interstitial controls
    public InputField fullscreenPlacementInputField;
    private IChartboostMediationFullscreenAd _fullscreenAd;
    
    // banner controls
    public InputField bannerPlacementInputField;
    public Dropdown bannerSizeDropdown;
    public Dropdown bannerLocationDropdown;
    private ChartboostMediationUnityBannerAd _bannerAd;
    public Dropdown horizontalAlignmentDropdown;
    public Dropdown verticalAlignmentDropdown;
    public Dropdown resizeDropdown;
    private bool _bannerAdIsVisible = true;
    private bool _bannerAdIsDraggable = true;

    public ScrollRect outputTextScrollRect;
    public Text outputText;

    public GameObject objectToDestroyForTest;
    

    #region Lifecycle
    private void Awake()
    {
        Application.targetFrameRate = 60;
        ChartboostMediation.DidStart += DidStart;
        ChartboostMediation.DidReceivePartnerInitializationData += DidReceivePartnerInitializationData;
        ChartboostMediation.DidReceiveImpressionLevelRevenueData += DidReceiveImpressionLevelRevenueData;
        ChartboostMediation.UnexpectedSystemErrorDidOccur += UnexpectedSystemErrorDidOccur;
    }
    
    private void Start()
    {
        if (outputText != null)
            outputText.text = string.Empty;
        fullScreenPanel.SetActive(true);
        bannerPanel.SetActive(false);

        fullscreenPlacementInputField.SetTextWithoutNotify(DefaultPlacementFullscreen);
        bannerPlacementInputField.SetTextWithoutNotify(DefaultPlacementBanner);

        ChartboostMediation.StartWithOptions(ChartboostMediationSettings.AppId, ChartboostMediationSettings.AppSignature);
    }

    private void OnDestroy()
    {
        OnInvalidateFullscreenClick();

        if (_bannerAd == null) 
            return;
        
        _bannerAd.ResetAd();
        Destroy(_bannerAd);
        Log("destroyed an existing banner");
    }

    private void DidStart(string error)
    {
        Log($"DidStart: {error}");
        ChartboostMediation.SetUserIdentifier(DefaultUserIdentifier);

        Log($"User Identifier Set: {ChartboostMediation.GetUserIdentifier()}");

        if (error != null) return;
        ChartboostMediation.SetSubjectToGDPR(false);
        ChartboostMediation.SetSubjectToCoppa(false);
        ChartboostMediation.SetUserHasGivenConsent(true);
        ChartboostMediation.SetTestMode(true);
    }

    private void DidReceiveImpressionLevelRevenueData(string placement, Hashtable impressionData)
    {
        var json =  JsonTools.Serialize(impressionData);
        Log($"DidReceiveImpressionLevelRevenueData {placement}: {JsonPrettify(json)}");
    }

    private void DidReceivePartnerInitializationData(string partnerInitializationData)
    {
        Log($"DidReceivePartnerInitializationData: ${JsonPrettify(partnerInitializationData)}");
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

    #region Fullscreen
    public async void OnLoadFullscreenClick()
    {
        var keywords = new Dictionary<string, string>()
        {
            { "i12_keyword1", "i12_value1" },
            { "i12_keyword2", "i12_value2" }
        };

        var loadRequest = new ChartboostMediationFullscreenAdLoadRequest(fullscreenPlacementInputField.text, keywords);

        loadRequest.DidClick += fullscreenAd => Log($"DidClick Name: {fullscreenAd.Request.PlacementName}");

        loadRequest.DidClose += (fullscreenAd, error) => Log(!error.HasValue
            ? $"DidClose Name: {fullscreenAd.Request.PlacementName}"
            : $"DidClose Name: {fullscreenAd.Request.PlacementName}, Code: {error?.Code}, Message: {error?.Message}");

        loadRequest.DidReward += fullscreenAd => Log($"DidReward Name: {fullscreenAd.Request.PlacementName}");

        loadRequest.DidRecordImpression += fullscreenAd => Log($"DidImpressionRecorded Name: {fullscreenAd.Request.PlacementName}");

        loadRequest.DidExpire += fullscreenAd => Log($"DidExpire Name: {fullscreenAd.Request.PlacementName}");

        var loadResult = await ChartboostMediation.LoadFullscreenAd(loadRequest);
        
        // Failed to Load
        if (loadResult.Error.HasValue)
        {
            Log($"Fullscreen Failed to Load: {loadResult.Error?.Code}, message: {loadResult.Error?.Message}");
            return;
        }

        // Loaded but AD is null?
        _fullscreenAd = loadResult.Ad;
        if (_fullscreenAd == null)
        {
            Log("Fullscreen Ad is null but no error was found???");
            return;
        }

        // DidLoad
        _fullscreenAd.CustomData = DefaultFullscreenAdCustomData;
        var customData = _fullscreenAd.CustomData;
        var adLoadId = _fullscreenAd.LoadId;
        var bidInfo = _fullscreenAd.WinningBidInfo;
        var placementName = _fullscreenAd?.Request?.PlacementName;
        var loadId = loadResult.LoadId;
        var metrics = loadResult.Metrics;
        Log($"Fullscreen: {placementName} Loaded with: \nAdRequestId {adLoadId} \nRequestID {loadId} \nBidInfo: {JsonConvert.SerializeObject(bidInfo, Formatting.Indented)} \n Metrics:{JsonConvert.SerializeObject(metrics, Formatting.Indented)} \n Custom Data: {customData}");
    }

    public void OnInvalidateFullscreenClick()
    {
        if (_fullscreenAd == null)
        {
            Log("fullscreen ad does not exist");
            return;
        }

        _fullscreenAd.Invalidate();
        Log("fullscreen ad has been invalidated");
    }

    public async void OnShowFullscreenClick()
    {
        if (_fullscreenAd == null)
            return;

        var adShowResult = await _fullscreenAd.Show();
        if (adShowResult.Error.HasValue)
        {
            Log($"Fullscreen Failed to Show with Value: {adShowResult.Error?.Code}, {adShowResult.Error?.Message}");
            return;
        }

        var metrics = adShowResult.Metrics;
        Log($"Fullscreen Ad Did Show: {JsonConvert.SerializeObject(metrics, Formatting.Indented)}");
    }
    #endregion
    
    #region Banners


    public async void OnCreateBannerClick()
    {
        if(_bannerAd != null)
            Destroy(_bannerAd.gameObject);

        var widthNative = ChartboostMediationConverters.PixelsToNative(bannerPanel.GetComponent<RectTransform>().LayoutParams().width);
        
        var size = bannerSizeDropdown.value switch
        {
            11 => ChartboostMediationBannerAdSize.Adaptive9X16(widthNative),
            10 => ChartboostMediationBannerAdSize.Adaptive1X4(widthNative),
            9 => ChartboostMediationBannerAdSize.Adaptive1X3(widthNative),
            8 => ChartboostMediationBannerAdSize.Adaptive1X2(widthNative),
            7 => ChartboostMediationBannerAdSize.Adaptive10X1(widthNative),
            6 => ChartboostMediationBannerAdSize.Adaptive8X1(widthNative),
            5 => ChartboostMediationBannerAdSize.Adaptive6X1(widthNative),
            4 => ChartboostMediationBannerAdSize.Adaptive4X1(widthNative),
            3 => ChartboostMediationBannerAdSize.Adaptive2X1(widthNative),
            2 => ChartboostMediationBannerAdSize.Leaderboard,
            1 => ChartboostMediationBannerAdSize.MediumRect,
            _ => ChartboostMediationBannerAdSize.Standard
        };
        
        var screenPos = bannerLocationDropdown.value switch
        {
            0 => ChartboostMediationBannerAdScreenLocation.TopLeft,
            1 => ChartboostMediationBannerAdScreenLocation.TopCenter,
            2 => ChartboostMediationBannerAdScreenLocation.TopRight,
            3 => ChartboostMediationBannerAdScreenLocation.Center,
            4 => ChartboostMediationBannerAdScreenLocation.BottomLeft,
            5 => ChartboostMediationBannerAdScreenLocation.BottomCenter,
            6 => ChartboostMediationBannerAdScreenLocation.BottomRight,
            _ => ChartboostMediationBannerAdScreenLocation.TopCenter
        };
        
        
        
        Log($"Creating new Unity banner ad");
        _bannerAd = ChartboostMediation.GetUnityBannerAd(bannerPlacementInputField.text, true, size, screenPos);
        _bannerAd.WillAppear += WillAppearBanner;
        _bannerAd.DidClick +=DidClickBanner;
        _bannerAd.DidRecordImpression += DidRecordImpressionBanner;
        _bannerAd.DidDrag += DidDragBanner;

        var keywords = _bannerAd.Keywords ??= new Dictionary<string, string>();
        keywords.Add("bnr_keyword1", "bnr_value1"); 
        keywords.Add("bnr_keyword2", "bnr_value2");
        _bannerAd.Keywords = keywords;

        _bannerAd.VerticalAlignment = (ChartboostMediationBannerVerticalAlignment)verticalAlignmentDropdown.value;
        _bannerAd.HorizontalAlignment = (ChartboostMediationBannerHorizontalAlignment)horizontalAlignmentDropdown.value;
        
        var resizeAxis = resizeDropdown.value switch
        {
            0 => ChartboostMediationBannerResizeAxis.Horizontal,
            1 => ChartboostMediationBannerResizeAxis.Vertical,
            _ => ChartboostMediationBannerResizeAxis.Both
        };

        var shouldResize = resizeDropdown.value != 3;   // 3 => "No Resize"
        _bannerAd.ResizeAxis = resizeAxis;
        _bannerAd.ResizeToFit = shouldResize;
        
        // TODO: Debug mode (Remove)
        var image =_bannerAd.gameObject.AddComponent<Image>();
        image.color = new Color(0.25f,1,0.25f, .25f);
        
        var result = await _bannerAd.Load();
        Log(result.Error == null ? "Successfully loaded banner" : result.Error?.Message);

        
    }

    public void OnHorizontalAlignmentChange()
    {
        _bannerAd.HorizontalAlignment = (ChartboostMediationBannerHorizontalAlignment) horizontalAlignmentDropdown.value;
    }

    public void OnVerticalAlignmentChange()
    {
        _bannerAd.VerticalAlignment = (ChartboostMediationBannerVerticalAlignment)verticalAlignmentDropdown.value;
    }

    public void OnToggleBannerVisibilityClick()
    {
        if (_bannerAd != null)
        {
            _bannerAdIsVisible = !_bannerAdIsVisible;
            _bannerAd.gameObject.SetActive(_bannerAdIsVisible);
        }
        Log("Banner Visibility Toggled");
    }
    
    public void OnToggleBannerDraggabilityClick()
    {
        if (_bannerAd != null)
        {
            _bannerAdIsDraggable = !_bannerAdIsDraggable;
            _bannerAd.Draggable = _bannerAdIsDraggable;
        }
        Log("Banner Draggability Toggled");
    }
    
    public void OnResetBannerClick()
    {
        if (_bannerAd == null)
        {
            Log("banner ad does not exist");
            return;
        }
        _bannerAd.ResetAd();
        Log("banner ad has been reset");
    }

    public void OnRemoveBannerClick()
    {
        if (_bannerAd == null)
        {
            Log("banner ad does not exist");
            return;
        }

        Destroy(_bannerAd.gameObject);
        Log("banner ad has been destroyed");

    }

    private void WillAppearBanner()
    {
        Log($"WillAppearBanner {_bannerAd}");
    }
    
    private void DidRecordImpressionBanner()
    {
        Log($"DidRecordImpressionBanner {_bannerAd}");
    }

    private void DidClickBanner()
    {
        Log($"DidClickBanner {_bannerAd}");
    }

    private void DidDragBanner(float x, float y)
    {
        
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

    private void UnexpectedSystemErrorDidOccur(string error)
    {
        Log($"<color='red'>{error}</color>");
        Debug.LogErrorFormat(error);
    }
    
    public static string JsonPrettify(string json)
    {
        
        using var stringReader = new StringReader(json);
        using var stringWriter = new StringWriter();
        var jsonReader = new JsonTextReader(stringReader);
        var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
        jsonWriter.WriteToken(jsonReader);
        return stringWriter.ToString();
    }
    #endregion
}
