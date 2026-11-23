namespace Masa.Stack.Components.Infrastructure.Identity;

/// <summary>
/// 本库与浏览器端 OIDC 客户端打交道时共用的约定值：库内各处只引用这里，不再各写一份字面量。
/// <para>
/// 有两处「重复」在编译期共用不了，由 <c>OidcClientConstsTests</c> 钉住：
/// JS 函数名要与 wwwroot/js/components.js 里的定义一致；登录路径要与认证页（Authentication.razor）的路由、框架默认值一致。
/// </para>
/// </summary>
internal static class OidcClientConsts
{
    /// <summary>清掉本标签页 sessionStorage 里的 OIDC 用户（components.js）。</summary>
    public const string CLEAR_TOKEN_CACHE_FUNCTION = "MasaStackComponents.clearOidcTokenCache";

    /// <summary>本标签页有没有本人的 OIDC 用户（components.js；只读，不像取令牌那样会触发静默登录）。</summary>
    public const string HAS_USER_FUNCTION = "MasaStackComponents.hasOidcUser";

    /// <summary>
    /// 登录页路径。前导 "/" 不能省：<see cref="MicroFrontendNavigationManager"/> 只给以 "/" 开头的路径补微前端前缀。
    /// </summary>
    public const string LOGIN_PATH = "/authentication/login";
}
