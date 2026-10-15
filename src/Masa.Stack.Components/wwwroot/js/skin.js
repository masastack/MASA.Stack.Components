const storageKey = 'masa-skin:skin-pref';
const primaryStorageKey = 'masa-skin:primary-color';
const apps = new WeakMap();
let nextScope = 0;

export function getSavedSkin() {
    try { return localStorage.getItem(storageKey); } catch { return null; }
}

export function getSavedPrimaryColor() {
    try { return localStorage.getItem(primaryStorageKey); } catch { return null; }
}

function findApp(anchor) {
    return anchor?.closest?.('.m-application') ?? document.querySelector('.m-application.masa-stack-components');
}

function mix(color, target, amount) {
    return '#' + color.match(/\w\w/g).map((part, i) => Math.round(parseInt(part, 16) * (1 - amount) + target[i] * amount)
        .toString(16).padStart(2, '0')).join('');
}

function primaryCss(scope, id, color, foreground, dark) {
    const rgb = color.match(/\w\w/g).map(part => parseInt(part, 16)).join(', ');
    const onRgb = foreground.match(/\w\w/g).map(part => parseInt(part, 16)).join(', ');
    const hover = mix(color, dark ? [255, 255, 255] : [0, 0, 0], .12);
    const active = mix(color, dark ? [255, 255, 255] : [0, 0, 0], .24);
    // Nested themed controls can redeclare tokens, so bridge them as well as the app and its attached overlays.
    const tokens = `${scope}, ${scope} [class*="theme--"]`;
    const primary = ':is(.primary, .Primary)';
    const text = ':is(.primary--text, .Primary--text)';
    const filled = `.m-btn${primary}:not(.m-btn--text):not(.m-btn--outlined):not(.m-btn--disabled)`;
    const interactive = `${scope} :is(.m-list-item--active, .m-tab--active):not(.error--text):not(.success--text):not(.warning--text)`;
    return `
${tokens} {
  --m-theme-primary: ${rgb} !important; --m-theme-on-primary: ${onRgb} !important;
  --m-text-primary: ${rgb} !important; --m-text-on-primary: ${onRgb} !important;
  --${id}-link-color: ${color} !important;
  --antd-snapshot-color-primary: ${color} !important;
  --antd-snapshot-color-primary-hover: ${hover} !important;
  --antd-snapshot-color-primary-active: ${active} !important;
  --antd-snapshot-color-primary-bg: rgba(${rgb}, .12) !important;
  --p2e-primary: ${color} !important; --p2e-primary-dark: ${active} !important;
  --p2e-primary-rgb: ${rgb} !important; --p2e-primary-500: ${color} !important;
  --p2e-on-primary: ${foreground} !important;
  --p2e-focus-ring: ${color} !important; --p2e-focus: ${color} !important;
  --p2e-focus-color: ${color} !important;
}
${scope} ${primary}:not(.m-btn--disabled):not(.m-btn--text):not(.m-btn--outlined) { background-color: ${color} !important; border-color: ${color} !important; color: ${foreground} !important; }
${scope} ${text} { color: ${color} !important; caret-color: ${color} !important; }
${scope} .m-btn:is(.primary, .Primary, .primary--text, .Primary--text):is(.m-btn--outlined, .m-btn--text):not(.m-btn--disabled) { background-color: transparent !important; color: ${color} !important; }
${scope} ${filled} { background-color: ${color} !important; color: ${foreground} !important; border-color: ${color} !important; }
${scope} ${filled}:hover { background-color: ${hover} !important; border-color: ${hover} !important; }
${scope} ${filled}:active { background-color: ${active} !important; border-color: ${active} !important; }
${scope} :is(.m-btn:not(.m-btn--text):not(.m-btn--outlined):not(.m-btn--disabled), .m-list-item, .m-tab)${primary} .white--text { color: ${foreground} !important; }
${scope} .m-btn${text}:is(.m-btn--outlined, .m-btn--text):hover { background-color: rgba(${rgb}, .10) !important; }
${scope} .m-btn${text}:is(.m-btn--outlined, .m-btn--text):active { background-color: rgba(${rgb}, .18) !important; }
${interactive} { color: ${color} !important; background-color: rgba(${rgb}, .12) !important; }
${interactive}:hover { background-color: rgba(${rgb}, .18) !important; }
${interactive}:active { background-color: rgba(${rgb}, .24) !important; }
${interactive}${primary} { color: ${foreground} !important; background-color: ${color} !important; }
${interactive}${primary}:hover { background-color: ${hover} !important; }
${interactive}${primary}:active { background-color: ${active} !important; }
${scope} :is(.m-input--is-focused, .m-input--is-dirty)${text}:not(.error--text) { color: ${color} !important; }
${scope} :is(.m-btn, .m-list-item, input):focus-visible { outline-color: ${color}; }
`;
}

export async function applySkin(id, href, registeredHrefs, dark, persist, primary = null, onPrimary = '#FFFFFF', anchor = null) {
    const app = findApp(anchor);
    if (!app) return;
    const root = app.getRootNode();
    let state = apps.get(app);
    if (!state) {
        state = { scope: `masa-${++nextScope}`, style: null };
        apps.set(app, state);
        app.dataset.masaAppearance = state.scope;
    }
    const links = [...root.querySelectorAll('link[rel="stylesheet"]')];
    const skinHrefs = registeredHrefs.filter(path => path);
    const matches = (link, path) => new URL(link.href).pathname.endsWith('/' + path.replace(/^\//, ''));

    let skinReady = Promise.resolve();
    if (href) {
        const assetBase = import.meta.url.split('_content/Masa.Stack.Components/')[0];
        if (!links.some(link => matches(link, href))) {
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = new URL(href, assetBase).href;
            skinReady = new Promise(resolve => {
                const complete = () => {
                    link.removeEventListener('load', complete);
                    link.removeEventListener('error', complete);
                    resolve();
                };
                link.addEventListener('load', complete);
                link.addEventListener('error', complete);
            });
            if (root === document) document.head.appendChild(link);
            else root.appendChild(link);
            links.push(link);
        }
    }

    for (const link of links) {
        if (skinHrefs.some(path => matches(link, path))) link.disabled = !href || !matches(link, href);
    }
    await skinReady;
    app.dataset.skin = id;
    app.dataset.theme = dark ? 'dark' : 'light';
    if (primary) {
        state.style ??= document.createElement('style');
        state.style.dataset.masaPrimary = state.scope;
        // Extra specificity beats the skins' fixed primary rules without changing semantic colors.
        const scope = `.masa-stack-components[data-masa-appearance="${state.scope}"][data-masa-appearance="${state.scope}"]`;
        state.style.textContent = primaryCss(scope, id, primary, onPrimary, dark);
        (root === document ? document.head : root).appendChild(state.style);
    } else {
        state.style?.remove();
        state.style = null;
    }
    if (persist) {
        try {
            localStorage.setItem(storageKey, id);
            if (primary) localStorage.setItem(primaryStorageKey, primary);
            else localStorage.removeItem(primaryStorageKey);
        } catch { /* Storage can be disabled. */ }
    }
}

export function releaseApp(anchor) {
    const app = findApp(anchor);
    if (!app) return;
    apps.get(app)?.style?.remove();
    apps.delete(app);
    delete app.dataset.masaAppearance;
}
