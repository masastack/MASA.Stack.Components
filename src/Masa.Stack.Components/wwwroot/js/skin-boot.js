// First-paint helper for hosts that use the Masa.Stack.Skins catalog. Include it as a plain (not async, not
// deferred, not module) script in <head>:
//   <script src="_content/Masa.Stack.Components/js/skin-boot.js"></script>
// It restores the appearance channel and the stylesheet of the skin that was active on the previous visit,
// so the page does not paint in the default look before Blazor has started. SkinManager takes over afterwards.
(function () {
    try {
        var boot = JSON.parse(localStorage.getItem('masa-skin:boot') || 'null');
        if (!boot || typeof boot.id !== 'string') return;

        var script = document.currentScript;
        var marker = '_content/Masa.Stack.Components/';
        var base = script && script.src && script.src.indexOf(marker) >= 0
            ? script.src.split(marker)[0]
            : document.baseURI;

        var root = document.documentElement;
        root.dataset.skin = boot.id;
        root.dataset.theme = boot.dark ? 'dark' : 'light';
        root.dataset.skinProfile = boot.profile === 'identity' ? 'identity' : 'themed';

        [['base', boot.base], ['tokens', boot.tokens]].forEach(function (entry) {
            if (!entry[1]) return;
            var link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = new URL(entry[1], base).href;
            link.dataset.masaSkinCss = entry[0];
            document.head.appendChild(link);
        });

        // Same rule as primaryStyle() in skin.js, which replaces this element once Blazor has started.
        if (boot.primary && boot.onPrimary) {
            var rgb = function (value) {
                return value.replace('#', '').match(/\w\w/g).map(function (part) { return parseInt(part, 16); }).join(', ');
            };
            var scope = 'html[data-skin][data-theme]';
            var style = document.createElement('style');
            style.dataset.masaSkinPrimary = '';
            style.textContent = scope + ', ' + scope + ' .m-application, ' + scope + ' [class*="theme--"] {\n'
                + '  --m-theme-primary: ' + rgb(boot.primary) + '; --m-theme-primary-text: ' + rgb(boot.primary)
                + '; --m-theme-on-primary: ' + rgb(boot.onPrimary) + ';\n}';
            document.head.appendChild(style);
        }
    } catch (e) { /* Storage can be disabled; the default look is fine. */ }
})();
