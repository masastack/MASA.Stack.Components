using System.Reflection;
using System.Text.RegularExpressions;
using Masa.Stack.Components.Infrastructure.Identity;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Xunit;

namespace Masa.Stack.Components.Tests.Identity;

/// <summary>
/// <see cref="OidcClientConsts"/> 里有些值在别处还得「再写一份」，编译器看不见这层对应关系：
/// JS 函数名要在 wwwroot/js/components.js 里定义，登录路径要等于认证页（Authentication.razor）的路由。
/// 这里把对应关系钉死——改了任何一边而没改另一边，测试就红，不会等到浏览器里才发现「找不到函数」或「跳到不存在的页面」。
/// </summary>
public class OidcClientConstsTests
{
    [Theory]
    [InlineData(OidcClientConsts.CLEAR_TOKEN_CACHE_FUNCTION)]
    [InlineData(OidcClientConsts.HAS_USER_FUNCTION)]
    public void JsFunction_IsDefinedInComponentsJs(string function)
    {
        var script = File.ReadAllText(FindRepoFile("src/Masa.Stack.Components/wwwroot/js/components.js"));

        // components.js 是经典脚本，函数顶层挂在 window 上：window.MasaStackComponents.xxx = ...
        // 锚定行首，免得被注释掉的旧定义蒙混过关。
        Assert.True(
            Regex.IsMatch(script, $@"(?m)^window\.{Regex.Escape(function)}\s*=(?!=)"),
            $"components.js 里没有定义 window.{function}");
    }

    [Fact]
    public void LoginPath_IsTheFrameworkDefaultWithALeadingSlash()
    {
        // AddOidcAuthentication 没有改 AuthenticationPaths，框架认的就是它的默认登录路径（不带前导 "/"）
        Assert.Equal(OidcClientConsts.LOGIN_PATH, "/" + RemoteAuthenticationDefaults.LoginPath);
    }

    [Fact]
    public void LoginPath_IsTheRouteOfTheAuthenticationPage()
    {
        // 认证页的路由是 @page "/authentication/{action}"；action 取 login 就是登录页
        var template = typeof(Authentication).GetCustomAttributes<RouteAttribute>().Single().Template;

        Assert.Equal(OidcClientConsts.LOGIN_PATH, template.Replace("{action}", RemoteAuthenticationActions.LogIn));
    }

    /// <summary>从测试输出目录往上找仓库里的文件。找不到就让测试失败：悄悄跳过会让「钉住」形同虚设。</summary>
    private static string FindRepoFile(string relativePath)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"从 {AppContext.BaseDirectory} 往上找不到 {relativePath}；这个测试需要在完整的仓库目录里运行。");
    }
}
