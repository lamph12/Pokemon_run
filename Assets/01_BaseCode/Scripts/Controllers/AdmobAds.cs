﻿//using com.adjust.sdk;

using System;
using System.Collections;
using Firebase.Analytics;
using UnityEngine;
using UnityEngine.Events;
using static MaxSdkBase;

public class AdmobAds : MonoBehaviour
{
    public bool offBanner;

    public float countdownAds;


    private bool _isInited;

#if UNITY_ANDROID
    private const string MaxSdkKey =
        "M4GLwqezVT2WDo75OWFGOV873pVg6-3S3Kpz8Rxe_-9CnHI9oXPB2TI5LpnRnqvr8hpH8kw7i4KTMcc891KCad";

    private const string InterstitialAdUnitId = "e4f879e9f4c7404d";
    private const string RewardedAdUnitId = "3e8c00cb5b9acf9c";
    private const string BanerAdUnitId = "cf8262f4e14e8a96";
#elif UNITY_IOS
    private const string MaxSdkKey =
 "M4GLwqezVT2WDo75OWFGOV873pVg6-3S3Kpz8Rxe_-9CnHI9oXPB2TI5LpnRnqvr8hpH8kw7i4KTMcc891KCad";
    private const string InterstitialAdUnitId = "c8d31e48f08ed31e";
    private const string RewardedAdUnitId = "02932bb866cbb369";
    private const string BanerAdUnitId = "ff665c0a75cadcc4";
#endif
    public void Init()
    {
        countdownAds = 10000;

        #region Applovin Ads

        CheckResetCaping();
        MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
        {
            Debug.Log("MAX SDK Initialized");
            InitInterstitial();
            InitRewardVideo();
            InitializeBannerAds();

            // MaxSdk.ShowMediationDebugger();
        };
        MaxSdk.SetVerboseLogging(true);
        MaxSdk.SetSdkKey(MaxSdkKey);
        MaxSdk.InitializeSdk();

        #endregion

        _isInited = true;
    }

    #region Interstitial

    public UnityAction actionInterstitialClose;

    public int amountInterClick
    {
        get => PlayerPrefs.GetInt("Amount_Inter_Click", 0);
        set => PlayerPrefs.SetInt("Amount_Inter_Click", value);
    }

    public int amountLoadFailInter
    {
        get => PlayerPrefs.GetInt("Amount_Load_Fail_Inter", 0);
        set => PlayerPrefs.SetInt("Amount_Load_Fail_Inter", value);
    }

    public DateTime timeLoadFailInter
    {
        get
        {
            var timeLoad = DateTime.Now.AddSeconds(0);
            if (PlayerPrefs.HasKey("Time_Load_Fail_Inter"))
            {
                var binaryDateTime = long.Parse(PlayerPrefs.GetString("Time_Load_Fail_Inter"));
                timeLoad = DateTime.FromBinary(binaryDateTime);
            }

            return timeLoad;
        }
        set => PlayerPrefs.SetString("Time_Load_Fail_Inter", DateTime.Now.ToBinary().ToString());
    }

    private bool _isLoading;
    private int errorCodeLoadFail_Inter;

    public bool IsLoadedInterstitial()
    {
        return MaxSdk.IsInterstitialReady(InterstitialAdUnitId);
    }

    private void InitInterstitial()
    {
        MaxSdkCallbacks.OnInterstitialLoadedEvent += OnInterstitialLoadedEvent;
        MaxSdkCallbacks.OnInterstitialLoadFailedEvent += OnInterstitialFailedEvent;
        MaxSdkCallbacks.OnInterstitialAdFailedToDisplayEvent += InterstitialFailedToDisplayEvent;
        MaxSdkCallbacks.OnInterstitialHiddenEvent += OnInterstitialHiddenEvent;
        MaxSdkCallbacks.OnInterstitialClickedEvent += MaxSdkCallbacks_OnInterstitialClickedEvent;
        MaxSdkCallbacks.OnInterstitialDisplayedEvent += MaxSdkCallbacks_OnInterstitialDisplayedEvent;

        RequestInterstitial();

        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        // MaxSdkCallbacks.
    }

    public void ShowInterstitial(bool isShowImmediatly = false, string actionWatchLog = "other",
        UnityAction actionIniterClose = null, string level = null)
    {
        if (GameController.Instance.useProfile.IsRemoveAds)
        {
            if (actionIniterClose != null)
                actionIniterClose();
            return;
        }


        if (isShowImmediatly)
        {
            GameController.Instance.AnalyticsController.LoadInterEligible();
            ShowInterstitialHandle(isShowImmediatly, actionWatchLog, actionIniterClose, level);
        }
        else
        {
            if (UseProfile.CurrentLevel >
                RemoteConfigController.GetFloatConfig(FirebaseConfig.LEVEL_START_SHOW_INITSTIALL, 3))
            {
                GameController.Instance.AnalyticsController.LoadInterEligible();

                if (countdownAds > RemoteConfigController.GetFloatConfig(FirebaseConfig.DELAY_SHOW_INITSTIALL, 90))
                {
                    ShowInterstitialHandle(isShowImmediatly, actionWatchLog, actionIniterClose, level);
                }
                else
                {
                    if (actionIniterClose != null)
                        actionIniterClose();
                }
            }
            else
            {
                if (actionIniterClose != null)
                    actionIniterClose();
            }
        }
    }

    private void ShowInterstitialHandle(bool isShowImmediatly = false, string actionWatchLog = "other",
        UnityAction actionIniterClose = null, string level = null)
    {
        if (IsLoadedInterstitial())
        {
            actionInterstitialClose = actionIniterClose;
            MaxSdk.ShowInterstitial(InterstitialAdUnitId, actionWatchLog);

            if (!isShowImmediatly)
                countdownAds = 0;

            GameController.Instance.AnalyticsController.LogInterShow();
        }
        else
        {
            if (actionIniterClose != null)
                actionIniterClose();
            RequestInterstitial();
        }
    }

    private void RequestInterstitial()
    {
        if (_isLoading) return;

        MaxSdk.LoadInterstitial(InterstitialAdUnitId);
        GameController.Instance.AnalyticsController.LogInterLoad();
        _isLoading = true;
    }

    #endregion

    #region Video Reward

    private UnityAction _actionClose;
    private UnityAction _actionRewardVideo;
    private UnityAction _actionNotLoadedVideo;
    private ActionWatchVideo actionWatchVideo;

    public int amountVideoRewardClick
    {
        get => PlayerPrefs.GetInt("Amount_VideoReward_Click", 0);
        set => PlayerPrefs.SetInt("Amount_VideoReward_Click", value);
    }

    private int numRequestedInScene_Video;

    private bool isVideoDone;

    private void InitRewardVideo()
    {
        InitializeRewardedAds();
    }

    public bool IsLoadedVideoReward()
    {
        var result = MaxSdk.IsRewardedAdReady(RewardedAdUnitId);
        if (!result) RequestInterstitial();
        return result;
    }

    public bool IsLoadedAds()
    {
        var result = IsLoadedVideoReward();
        return !result ? IsLoadedInterstitial() : result;
    }

    public bool ShowVideoReward(UnityAction actionReward, UnityAction actionNotLoadedVideo, UnityAction actionClose,
        ActionWatchVideo actionType, string level)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            actionNotLoadedVideo?.Invoke();
            GameController.Instance.AnalyticsController.LogWatchVideo(actionType, true, false, level);
            return false;
        }

        actionWatchVideo = actionType;
        GameController.Instance.AnalyticsController.LogRequestVideoReward(actionType.ToString());
        GameController.Instance.AnalyticsController.LogVideoRewardEligible();
        if (IsLoadedVideoReward())
        {
            countdownAds = 0;
            _actionNotLoadedVideo = actionNotLoadedVideo;
            _actionClose = actionClose;
            _actionRewardVideo = actionReward;

            MaxSdk.ShowRewardedAd(RewardedAdUnitId, actionType.ToString());
            GameController.Instance.AnalyticsController.LogWatchVideo(actionType, true, true, level);
            GameController.Instance.AnalyticsController.LogVideoRewardShow(actionWatchVideo.ToString());
        }
        else
        {
            if (IsLoadedInterstitial())
            {
                _actionNotLoadedVideo = actionNotLoadedVideo;
                _actionClose = actionClose;
                _actionRewardVideo = actionReward;

                ShowInterstitial(true, actionType.ToString(), () => { }, level);
                GameController.Instance.AnalyticsController.LogWatchVideo(actionType, true, true, level);
                Debug.Log("ShowInterstitial !!!");
                countdownAds = 0;
                return true;
            }

            //ConfirmBox.Setup().AddMessageYes(Localization.Get("s_noti"), Localization.Get("s_TryAgain"), () => { });
            Debug.Log("No ads !!!");
            actionNotLoadedVideo?.Invoke();
            GameController.Instance.AnalyticsController.LogWatchVideo(actionType, false, true, level);
            return false;
        }

        return true;
    }

    #endregion

    #region Applovin Rewards Ads

    private void InitializeRewardedAds()
    {
        // Attach callbacks
        MaxSdkCallbacks.OnRewardedAdLoadedEvent += OnRewardedAdLoadedEvent;
        MaxSdkCallbacks.OnRewardedAdLoadFailedEvent += OnRewardedAdFailedEvent;
        MaxSdkCallbacks.OnRewardedAdFailedToDisplayEvent += OnRewardedAdFailedToDisplayEvent;
        MaxSdkCallbacks.OnRewardedAdDisplayedEvent += OnRewardedAdDisplayedEvent;
        MaxSdkCallbacks.OnRewardedAdClickedEvent += OnRewardedAdClickedEvent;
        MaxSdkCallbacks.OnRewardedAdHiddenEvent += OnRewardedAdDismissedEvent;
        MaxSdkCallbacks.OnRewardedAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        // Load the first RewardedAd
        LoadRewardedAd();
    }

    private void LoadRewardedAd()
    {
        MaxSdk.LoadRewardedAd(RewardedAdUnitId);
    }

    private void OnRewardedAdLoadedEvent(string adUnitId)
    {
        GameController.Instance.AnalyticsController.LogVideoRewardReady();
    }

    private void OnRewardedAdFailedEvent(string adUnitId, int errorCode)
    {
        Debug.Log("Rewarded ad failed to load with error code: " + errorCode);
        Invoke("LoadRewardedAd", 15);
        GameController.Instance.AnalyticsController.LogVideoRewardLoadFail(actionWatchVideo.ToString(),
            errorCode.ToString());
    }

    private void OnRewardedAdFailedToDisplayEvent(string adUnitId, int errorCode)
    {
        Debug.Log("Rewarded ad failed to display with error code: " + errorCode);
        isVideoDone = false;

        //if (IsLoadedInterstitial())
        //{
        //    ShowInterstitial(isShowImmediatly: true);
        //}
        //else
        //{
        //    //ConfirmBox.Setup().AddMessageYes(Localization.Get("s_noti"), Localization.Get("s_TryAgain"), () => { });
        //}
        LoadRewardedAd();
    }

    private void OnRewardedAdDisplayedEvent(string adUnitId)
    {
        Debug.Log("Rewarded ad displayed " + isVideoDone);
        isVideoDone = false;
    }

    private void OnRewardedAdClickedEvent(string adUnitId)
    {
        amountVideoRewardClick++;
        Debug.Log("Rewarded ad clicked");
        isVideoDone = true;
        GameController.Instance.AnalyticsController.LogClickToVideoReward(actionWatchVideo.ToString());
    }

    private void OnRewardedAdDismissedEvent(string adUnitId)
    {
        // Rewarded ad is hidden. Pre-load the next ad
        Debug.Log("Rewarded ad dismissed");
        _actionClose?.Invoke();
        _actionClose = null;
        LoadRewardedAd();
    }

    private void OnRewardedAdReceivedRewardEvent(string adUnitId, Reward reward)
    {
        // Rewarded ad was displayed and user should receive the reward
        Debug.Log("Rewarded ad received reward");
        isVideoDone = true;
        _actionRewardVideo?.Invoke();
        _actionRewardVideo = null;
        countdownAds = 0;
        GameController.Instance.AnalyticsController.LogVideoRewardShowDone(actionWatchVideo.ToString());
    }

    #endregion

    #region Applovin Interstitial

    private void OnInterstitialLoadedEvent(string adUnitId)
    {
        _isLoading = true;
        GameController.Instance.AnalyticsController.LogInterReady();
    }

    private void OnInterstitialFailedEvent(string adUnitId, int errorCode)
    {
        _isLoading = false;
        actionInterstitialClose?.Invoke();
        actionInterstitialClose = null;
        Invoke("RequestInterstitial", 3);

        errorCodeLoadFail_Inter = errorCode;
        GameController.Instance.AnalyticsController.LogInterLoadFail(errorCodeLoadFail_Inter.ToString());
    }

    private void InterstitialFailedToDisplayEvent(string adUnitId, int errorCode)
    {
        _isLoading = false;
        actionInterstitialClose?.Invoke();
        actionInterstitialClose = null;
        RequestInterstitial();
    }

    private void OnInterstitialHiddenEvent(string adUnitId)
    {
        _isLoading = false;
        Debug.Log("InterstitialAdClosedEvent");
        Time.timeScale = 1;

        _actionRewardVideo?.Invoke();
        _actionRewardVideo = null;

        _actionClose?.Invoke();
        _actionClose = null;

        actionInterstitialClose?.Invoke();
        actionInterstitialClose = null;

        RequestInterstitial();
    }

    private void MaxSdkCallbacks_OnInterstitialDisplayedEvent(string adUnitId)
    {
        Debug.Log("InterstitialAdOpenedEvent");
        _isLoading = false;
        Time.timeScale = 0;
    }

    private void MaxSdkCallbacks_OnInterstitialClickedEvent(string adUnitId)
    {
        amountInterClick++;
        GameController.Instance.AnalyticsController.LogInterClick();
        _isLoading = false;
    }

    #endregion

    #region Applovin Baner

    public int amountBanerClick
    {
        get => PlayerPrefs.GetInt("Amount_Baner_Click", 0);
        set => PlayerPrefs.SetInt("Amount_Baner_Click", value);
    }

    public int amountLoadFailBaner
    {
        get => PlayerPrefs.GetInt("Amount_Load_Fail_Baner", 0);
        set => PlayerPrefs.SetInt("Amount_Load_Fail_Baner", value);
    }

    public DateTime timeLoadFailBaner
    {
        get
        {
            var timeLoad = DateTime.Now.AddSeconds(0);
            if (PlayerPrefs.HasKey("Time_Load_Fail_Baner"))
            {
                var binaryDateTime = long.Parse(PlayerPrefs.GetString("Time_Load_Fail_Baner"));
                timeLoad = DateTime.FromBinary(binaryDateTime);
            }

            return timeLoad;
        }
        set => PlayerPrefs.SetString("Time_Load_Fail_Baner", DateTime.Now.ToBinary().ToString());
    }

    private IEnumerator reloadBannerCoru;

    public void InitializeBannerAds()
    {
        MaxSdkCallbacks.OnBannerAdLoadedEvent += OnBannerAdLoadedEvent;
        MaxSdkCallbacks.OnBannerAdLoadFailedEvent += OnBannerAdLoadFailedEvent;
        MaxSdkCallbacks.OnBannerAdClickedEvent += OnBannerAdClickedEvent;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;

        MaxSdk.CreateBanner(BanerAdUnitId, BannerPosition.BottomCenter);
        MaxSdk.SetBannerBackgroundColor(BanerAdUnitId, Color.clear);

        if (MaxSdkUtils.IsTablet())
            MaxSdk.SetBannerWidth(BanerAdUnitId, 520);
        else
            MaxSdk.SetBannerWidth(BanerAdUnitId, 320);

        GameController.Instance.admobAds.ShowBanner();
    }


    private void OnBannerAdClickedEvent(string obj)
    {
        //inter click
        Debug.Log("Click Baner !!!");
        amountBanerClick++;
    }

    private void OnBannerAdLoadFailedEvent(string arg1, int arg2)
    {
        if (reloadBannerCoru != null)
        {
            StopCoroutine(reloadBannerCoru);
            reloadBannerCoru = null;
        }

        reloadBannerCoru = Helper.StartAction(() => { ShowBanner(); }, 0.3f);
        StartCoroutine(reloadBannerCoru);
    }

    private void OnBannerAdLoadedEvent(string obj)
    {
        Debug.Log("Request success");
        if (reloadBannerCoru != null)
        {
            StopCoroutine(reloadBannerCoru);
            reloadBannerCoru = null;
        }
    }

    public void DestroyBanner()
    {
        if (reloadBannerCoru != null)
        {
            StopCoroutine(reloadBannerCoru);
            reloadBannerCoru = null;
        }

        MaxSdk.HideBanner(BanerAdUnitId);
    }

    public void ShowBanner()
    {
        //if (GameController.Instance.useProfile.IsRemoveAds)
        //    return;


        //if (DataManager.RemoveAds != 0)
        //    return;

        MaxSdk.ShowBanner(BanerAdUnitId);
    }

    #endregion

    #region Limit Click

    public DateTime ToDayAds
    {
        get
        {
            if (!PlayerPrefs.HasKey("TODAY_ADS"))
                PlayerPrefs.SetString("TODAY_ADS", DateTime.Now.AddDays(-1).ToString());
            return DateTime.Parse(PlayerPrefs.GetString("TODAY_ADS"));
        }
        set => PlayerPrefs.SetString("TODAY_ADS", value.ToString());
    }

    public void CheckResetCaping()
    {
        // bool isPassday = TimeManager.IsPassTheDay(ToDayAds, DateTime.Now);
        // if (isPassday)
        {
            amountLoadFailInter = 0;
            amountLoadFailBaner = 0;
            amountInterClick = 0;
            amountBanerClick = 0;
            amountVideoRewardClick = 0;
            ToDayAds = DateTime.Now;
        }
    }

    #endregion

    private void OnAdRevenuePaidEvent(string adUnitId, AdInfo impressionData)
    {
        var revenue = impressionData.Revenue;
        var impressionParameters = new[]
        {
            new Parameter("ad_platform", "AppLovin"),
            new Parameter("ad_source", impressionData.NetworkName),
            new Parameter("ad_unit_name", impressionData.AdUnitIdentifier),
            new Parameter("ad_format", impressionData.AdFormat),
            new Parameter("value", revenue),
            new Parameter("currency", "USD") // All AppLovin revenue is sent in USD
        };
        FirebaseAnalytics.LogEvent("ad_impression", impressionParameters);
    }

    private void OnLevelWasLoaded(int level)
    {
        _actionRewardVideo = null;
        _actionClose = null;
        actionInterstitialClose = null;
    }

    private void Update()
    {
        countdownAds += Time.unscaledDeltaTime;
    }
}