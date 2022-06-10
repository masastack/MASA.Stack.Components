﻿using Microsoft.AspNetCore.Http;

namespace Masa.Stack.Components.Configs;

public class GlobalConfig
{
    private const string DarkCookieKey = "GlobalConfig_IsDark";
    private const string PageModeKey = "GlobalConfig_PageMode";
    private const string MiniCookieKey = "GlobalConfig_NavigationMini";
    private const string FavoriteCookieKey = "GlobalConfig_Favorite";

    private readonly CookieStorage? _cookieStorage;
    private readonly I18nConfig? _i18NConfig;
    private bool _dark;
    private bool _mini;
    private string _favorite;

    public delegate void GlobalConfigChanged();
    public event GlobalConfigChanged? OnLanguageChanged;

    public GlobalConfig(CookieStorage cookieStorage, I18nConfig i18nConfig, IHttpContextAccessor httpContextAccessor)
    {
        _cookieStorage = cookieStorage;
        _i18NConfig = i18nConfig;

        if (httpContextAccessor.HttpContext is not null)
            Initialization(httpContextAccessor.HttpContext.Request.Cookies);
    }

    public string? Language
    {
        get => _i18NConfig?.Language;
        set
        {
            if (_i18NConfig is null)
            {
                return;
            }

            _i18NConfig.Language = value;
            OnLanguageChanged?.Invoke();
        }
    }

    public bool Dark
    {
        get => _dark;
        set
        {
            _dark = value;
            _cookieStorage?.SetItemAsync(DarkCookieKey, value);
        }
    }

    public bool Mini
    {
        get => _mini;
        set
        {
            _mini = value;
            _cookieStorage?.SetItemAsync(MiniCookieKey, value);
        }
    }

    public string Favorite
    {
        get => _favorite;
        set
        {
            _favorite = value;
            _cookieStorage?.SetItemAsync(FavoriteCookieKey, value);
        }
    }

    private void Initialization(IRequestCookieCollection cookies)
    {
        _dark = Convert.ToBoolean(cookies[DarkCookieKey]);
        _mini = !cookies.ContainsKey(MiniCookieKey) || Convert.ToBoolean(cookies[MiniCookieKey]);
        _favorite = cookies[FavoriteCookieKey];
    }
}