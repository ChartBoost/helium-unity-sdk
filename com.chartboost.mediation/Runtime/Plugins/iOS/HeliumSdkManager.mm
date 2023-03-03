/*
* HeliumSdkManager.mm
* Helium SDK
*/

#import <objc/runtime.h>
#import "HeliumSdkManager.h"
#import <ChartboostMediationSDK/ChartboostMediationSDK-Swift.h>
#import <ChartboostMediationSDK/HeliumInitResultsEvent.h>

struct Implementation {
    SEL selector;
    IMP imp;
};

// interstitial ad objects
NSMutableDictionary * storedAds = nil;

// lifecycle callbacks
static HeliumEvent _didStartCallback;
static HeliumILRDEvent _didReceiveILRDCallback;
static HeliumPartnerInitializationDataEvent _didReceivePartnerInitializationDataCallback;

// interstitial callbacks
static HeliumPlacementLoadEvent _interstitialDidLoadCallback;
static HeliumPlacementEvent _interstitialDidClickCallback;
static HeliumPlacementEvent _interstitialDidCloseCallback;
static HeliumPlacementEvent _interstitialDidShowCallback;
static HeliumPlacementEvent _interstitialDidRecordImpressionCallback;

// rewarded callbacks
static HeliumPlacementLoadEvent _rewardedDidLoadCallback;
static HeliumPlacementEvent _rewardedDidClickCallback;
static HeliumPlacementEvent _rewardedDidCloseCallback;
static HeliumPlacementEvent _rewardedDidShowCallback;
static HeliumPlacementEvent _rewardedDidRecordImpressionCallback;
static HeliumPlacementEvent _rewardedDidReceiveRewardCallback;

// banner callbacks
static HeliumPlacementLoadEvent _bannerDidLoadCallback;
static HeliumPlacementEvent _bannerDidRecordImpressionCallback;
static HeliumPlacementEvent _bannerDidClickCallback;

void UnityPause(int pause);

const char* serializeDictionary(NSDictionary *data)
{
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:data options:0 error:NULL];
    NSString *json = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
    NSLog(@"event: %@", json);
    return json.UTF8String;
}

const char* formatError(ChartboostMediationError *error)
{
    if (error == nil)
        return "";
    
    NSInteger code = error.code;
    
    NSString* localizedDescription = @"";
    if (localizedDescription != nil)
        localizedDescription = error.localizedDescription;
    
    NSString* localizedFailureReason = @"";
    if (localizedFailureReason != nil)
        localizedFailureReason = error.localizedFailureReason;
    
    NSString* localizedRecoverySuggestion = @"";
    if (localizedRecoverySuggestion != nil)
        localizedRecoverySuggestion = error.localizedRecoverySuggestion;
    
    NSString* formattedError = @"";
    
    formattedError = [formattedError stringByAppendingFormat:@"(CM_%ld) Message: %@ Cause: %@ Resolution: %@", code, localizedDescription, localizedFailureReason, localizedRecoverySuggestion];
    
    return formattedError.UTF8String;
}

const void serializeHeliumEvent(ChartboostMediationError *error, HeliumEvent event)
{
    if (event == nil)
        return;
    
    event(formatError(error));
}

const void serializePlacementWithError(NSString *placementName, ChartboostMediationError *error, HeliumPlacementEvent placementEvent)
{
    if (placementEvent == nil)
        return;
    
    placementEvent(placementName.UTF8String, formatError(error));
}

const void serializePlacementLoadWithError(NSString *placementName, NSString *requestIdentifier, NSDictionary *winningBidInfo, ChartboostMediationError *error, HeliumPlacementLoadEvent placementLoadEvent)
{
    if (placementLoadEvent == nil)
        return;
    
    NSString* partnerId = [winningBidInfo objectForKey:@"partner-id"];
    NSString* auctionId = [winningBidInfo objectForKey:@"auction-id"];
    NSNumber* price = [winningBidInfo objectForKey:@"price"];

    if (partnerId == nil)
        partnerId = @"";

    if (auctionId == nil)
        auctionId = @"";

    if (price == nil)
        price = 0;
    
    placementLoadEvent(placementName.UTF8String, requestIdentifier.UTF8String, auctionId.UTF8String, partnerId.UTF8String, [price doubleValue], formatError(error));
}

static void heliumSubscribeToILRDNotifications()
{
    static id ilrdObserverId = nil;

    if (ilrdObserverId != nil)
        [[NSNotificationCenter defaultCenter] removeObserver:ilrdObserverId];

    ilrdObserverId = [[NSNotificationCenter defaultCenter] addObserverForName:kHeliumDidReceiveILRDNotification object:nil queue:nil usingBlock:^(NSNotification* _Nonnull note) {
        HeliumImpressionData *ilrd = note.object;
        NSString *placement = ilrd.placement;
        NSDictionary *json = ilrd.jsonData;
        NSDictionary *data = [NSDictionary dictionaryWithObjectsAndKeys:
                              placement, @"placementName",
                              json ? json : [NSNull null], @"ilrd",
                              nil];
        const char* jsonToUnity = serializeDictionary(data);

        if (_didReceiveILRDCallback != nil)
            _didReceiveILRDCallback(jsonToUnity);
    }];
}

static void heliumSubscribeToPartnerInitializationNotifications()
{
    static id partnerInitializationObserver = nil;
    
    if (partnerInitializationObserver != nil)
        [[NSNotificationCenter defaultCenter] removeObserver:partnerInitializationObserver];
    
    partnerInitializationObserver = [[NSNotificationCenter defaultCenter] addObserverForName:kHeliumDidReceiveInitResultsNotification object:nil queue:nil usingBlock:^(NSNotification * _Nonnull notification) {
        // Extract the results payload.
        NSDictionary *results = (NSDictionary *)notification.object;
        const char* jsonToUnity = serializeDictionary(results);
        if (_didReceivePartnerInitializationDataCallback != nil)
            _didReceivePartnerInitializationDataCallback(jsonToUnity);
    }];
}

@interface HeliumSdkManager() <HeliumSdkDelegate, CHBHeliumInterstitialAdDelegate, CHBHeliumRewardedAdDelegate, CHBHeliumBannerAdDelegate>

@end

@implementation HeliumSdkManager

-(Implementation)getImplementationFromClassNamed:(NSString*)className selectorName:(NSString*)selectorName
{
    Class cls = NSClassFromString(className);
    SEL selector = NSSelectorFromString(selectorName);
    Method method = class_getClassMethod(cls, selector);
    IMP imp = method_getImplementation(method);
    struct Implementation implementation;
    implementation.selector = selector;
    implementation.imp = imp;
    return implementation;
}

///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark NSObject

+ (HeliumSdkManager*)sharedManager
{
    static HeliumSdkManager *sharedSingleton;

    if (!sharedSingleton)
        sharedSingleton = [[HeliumSdkManager alloc] init];

    return sharedSingleton;
}

///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark Public

- (void)setLifeCycleCallbacks:(HeliumEvent)didStartCallback didReceiveILRDCallback:(HeliumILRDEvent)didReceiveILRDCallback didReceivePartnerInitializationData:(HeliumPartnerInitializationDataEvent)didReceivePartnerInitializationDataCallback
{
    _didStartCallback = didStartCallback;
    _didReceiveILRDCallback = didReceiveILRDCallback;
    _didReceivePartnerInitializationDataCallback = didReceivePartnerInitializationDataCallback;
}

- (void)setInterstitialCallbacks:(HeliumPlacementLoadEvent)didLoadCallback didShowCallback:(HeliumPlacementEvent)didShowCallback  didCloseCallback:(HeliumPlacementEvent)didCloseCallback didClickCallback:(HeliumPlacementEvent)didClickCallback didRecordImpression:(HeliumPlacementEvent)didRecordImpression
{
    _interstitialDidLoadCallback = didLoadCallback;
    _interstitialDidShowCallback = didShowCallback;
    _interstitialDidClickCallback = didClickCallback;
    _interstitialDidCloseCallback = didCloseCallback;
    _interstitialDidRecordImpressionCallback = didRecordImpression;
}

- (void)setRewardedCallbacks:(HeliumPlacementLoadEvent)didLoadCallback didShowCallback:(HeliumPlacementEvent)didShowCallback didCloseCallback:(HeliumPlacementEvent)didCloseCallback didClickCallback:(HeliumPlacementEvent)didClickCallback didRecordImpression:(HeliumPlacementEvent)didRecordImpression didReceiveRewardCallback:(HeliumPlacementEvent)didReceiveRewardCallback
{
    _rewardedDidLoadCallback = didLoadCallback;
    _rewardedDidShowCallback = didShowCallback;
    _rewardedDidClickCallback = didClickCallback;
    _rewardedDidCloseCallback = didCloseCallback;
    _rewardedDidRecordImpressionCallback = didRecordImpression;
    _rewardedDidReceiveRewardCallback = didReceiveRewardCallback;
}

- (void)setBannerCallbacks:(HeliumPlacementLoadEvent)didLoadCallback didRecordImpression:(HeliumPlacementEvent)didRecordImpression didClickCallback:(HeliumPlacementEvent)didClickCallback
{
    _bannerDidLoadCallback = didLoadCallback;
    _bannerDidRecordImpressionCallback = didRecordImpression;
    _bannerDidClickCallback = didClickCallback;
}

- (void)startHeliumWithAppId:(NSString*)appId andAppSignature:(NSString*)appSignature unityVersion:(NSString *)unityVersion initializationOptions:(const char**)initializationOptions initializationOptionsSize:(int)initializationOptionsSize
{
    heliumSubscribeToILRDNotifications();
    heliumSubscribeToPartnerInitializationNotifications();
    HeliumInitializationOptions* heliumInitializationOptions = nil;
    
    if (initializationOptionsSize > 0) {
        NSMutableArray *initializationPartners = [NSMutableArray new];
        for (int x = 0; x < initializationOptionsSize; x++)
        {
            if(strlen(initializationOptions[x]) > 0)
                [initializationPartners addObject:[NSString stringWithUTF8String:initializationOptions[x]]];
        }
        heliumInitializationOptions = [[HeliumInitializationOptions alloc] initWithSkippedPartnerIdentifiers:initializationPartners];
    }
    
    [[Helium sharedHelium] startWithAppId:appId andAppSignature:appSignature options:heliumInitializationOptions delegate:self];
}

- (void)setSubjectToCoppa:(BOOL)isSubject
{
    [[Helium sharedHelium] setSubjectToCoppa: isSubject];
}

- (void)setSubjectToGDPR:(BOOL)isSubject
{
    [[Helium sharedHelium] setSubjectToGDPR: isSubject];
}

- (void)setUserHasGivenConsent:(BOOL)hasGivenConsent
{
    [[Helium sharedHelium] setUserHasGivenConsent: hasGivenConsent];
}

- (void)setCCPAConsent:(BOOL)hasGivenConsent
{
    [[Helium sharedHelium] setCCPAConsent: hasGivenConsent];
}

- (void)setUserIdentifier:(NSString*)userIdentifier
{
    [Helium sharedHelium].userIdentifier = userIdentifier;
}

- (NSString*)getUserIdentifier
{
    return [Helium sharedHelium].userIdentifier;
}

-(void)setTestMode:(BOOL)isTestModeEnabled
{
    Implementation implementation = [self getImplementationFromClassNamed:@"CHBHTestModeHelper" selectorName:@"setTestModeIsEnabledForced:"];
    typedef void (*Signature)(id, SEL, BOOL);
    Signature function = (Signature)implementation.imp;
    function(self, implementation.selector, isTestModeEnabled);
}

- (id<HeliumInterstitialAd>)getInterstitialAd:(NSString*)placementName
{
    id<HeliumInterstitialAd> ad = [[Helium sharedHelium] interstitialAdProviderWithDelegate: self andPlacementName: placementName];
    if (ad == NULL)
        return NULL;

    // Else return the address of the ad as an int, which can be used as a unique id
    // Also store the object in a dictionary so that it can later be deleted
    if (storedAds == nil)
        storedAds = [[NSMutableDictionary alloc] init];
    [storedAds setObject:ad forKey:[NSNumber numberWithLong:(long)ad]];
    return ad;
}

- (id<HeliumRewardedAd>)getRewardedAd:(NSString*)placementName
{
    id<HeliumRewardedAd> ad = [[Helium sharedHelium] rewardedAdProviderWithDelegate: self andPlacementName: placementName];
    if (ad == NULL)
        return NULL;

    // Else return the address of the ad as an int, which can be used as a unique id
    // Also store the object in a dictionary so that it can later be deleted
    if (storedAds == nil)
        storedAds = [[NSMutableDictionary alloc] init];
    [storedAds setObject:ad forKey:[NSNumber numberWithLong:(long)ad]];
    return ad;
}

- (HeliumBannerView*)getBannerAd:(NSString*)placementName andSize:(CHBHBannerSize)size
{
    HeliumBannerView* ad = [[Helium sharedHelium] bannerProviderWithDelegate:self andPlacementName:placementName andSize:size];
    if (ad == NULL)
        return NULL;

    // Else return the address of the ad as an int, which can be used as a unique id
    // Also store the object in a dictionary so that it can later be deleted
    if (storedAds == nil)
        storedAds = [[NSMutableDictionary alloc] init];
    [storedAds setObject:ad forKey:[NSNumber numberWithLong:(long)ad]];
    return ad;
}

- (void)freeInterstitialAd:(NSNumber*)adId
{
    [storedAds removeObjectForKey:adId];
}

- (void)freeRewardedAd:(NSNumber*)adId
{
    [storedAds removeObjectForKey:adId];
}

- (void)freeBannerAd:(NSNumber*)adId
{
    [storedAds removeObjectForKey:adId];
}

///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark HeliumSdkDelegate

- (void)heliumDidStartWithError:(ChartboostMediationError *)error;
{
    serializeHeliumEvent(error, _didStartCallback);
}

///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark CHBHeliumInterstitialAdDelegate

- (void)heliumInterstitialAdWithPlacementName:(NSString*)placementName requestIdentifier:(NSString *) requestIdentifier winningBidInfo:(NSDictionary<NSString *, id> *)winningBidInfo didLoadWithError:(ChartboostMediationError *)error
{
    serializePlacementLoadWithError(placementName, requestIdentifier, winningBidInfo, error, _interstitialDidLoadCallback);
}

- (void)heliumInterstitialAdWithPlacementName:(NSString*)placementName didShowWithError:(ChartboostMediationError *)error
{
    serializePlacementWithError(placementName, error, _interstitialDidShowCallback);
    if (!error) {
        UnityPause(true);
    }
}

- (void)heliumInterstitialAdWithPlacementName:(NSString*)placementName didCloseWithError:(ChartboostMediationError *)error
{
    UnityPause(false);
    serializePlacementWithError(placementName, error, _interstitialDidCloseCallback);
}

- (void)heliumInterstitialAdWithPlacementName:(NSString *)placementName didClickWithError:(ChartboostMediationError *)error
{
    serializePlacementWithError(placementName, error, _interstitialDidClickCallback);
}

- (void)heliumInterstitialAdDidRecordImpressionWithPlacementName: (NSString*)placementName
{
    serializePlacementWithError(placementName, nil, _interstitialDidRecordImpressionCallback);
}
///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark CHBHeliumRewardedVideoAdDelegate

- (void)heliumRewardedAdWithPlacementName:(NSString*)placementName requestIdentifier:(NSString *) requestIdentifier winningBidInfo:(NSDictionary<NSString *, id> *)winningBidInfo didLoadWithError:(ChartboostMediationError *)error
{
    serializePlacementLoadWithError(placementName, requestIdentifier, winningBidInfo, error, _rewardedDidLoadCallback);
}

- (void)heliumRewardedAdWithPlacementName:(NSString*)placementName didShowWithError:(ChartboostMediationError *)error
{
    serializePlacementWithError(placementName, error, _rewardedDidShowCallback);
    if (!error) {
        UnityPause(true);
    }
}

- (void)heliumRewardedAdWithPlacementName:(NSString*)placementName didCloseWithError:(ChartboostMediationError *)error
{
    UnityPause(false);
    serializePlacementWithError(placementName, error, _rewardedDidCloseCallback);
}

- (void)heliumRewardedAdWithPlacementName:(NSString*)placementName didClickWithError:(ChartboostMediationError *)error
{
    serializePlacementWithError(placementName, error, _rewardedDidClickCallback);
}

- (void)heliumRewardedAdDidRecordImpressionWithPlacementName: (NSString*)placementName
{
    serializePlacementWithError(placementName, nil, _rewardedDidRecordImpressionCallback);
}

- (void)heliumRewardedAdDidGetRewardWithPlacementName:(NSString *)placementName
{
    serializePlacementWithError(placementName, nil, _rewardedDidReceiveRewardCallback);
}

///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark CHBHeliumBannerAdDelegate

- (void)heliumBannerAdWithPlacementName:(NSString*)placementName requestIdentifier:(NSString *) requestIdentifier winningBidInfo:(NSDictionary<NSString *, id> *)winningBidInfo didLoadWithError:(ChartboostMediationError *)error
{
    serializePlacementLoadWithError(placementName, requestIdentifier, winningBidInfo, error, _bannerDidLoadCallback);
}

- (void)heliumBannerAdWithPlacementName:(NSString *)placementName didClickWithError:(ChartboostMediationError *)error
{
    serializePlacementWithError(placementName, error, _bannerDidClickCallback);
}

- (void)heliumBannerAdDidRecordImpressionWithPlacementName: (NSString*)placementName
{
    serializePlacementWithError(placementName, nil, _bannerDidRecordImpressionCallback);
}
@end
