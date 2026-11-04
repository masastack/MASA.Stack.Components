// Browser side of SkinManager. The only thing the page ever learns about a skin is the appearance channel
//   <html data-skin="<id>" data-theme="light|dark" data-skin-profile="identity|themed">
// plus one lazily loaded stylesheet per skin. Colours and geometry live in the skin catalog (Masa.Stack.Skins);
// MASA Blazor itself receives the roles from SkinManager.UpdateTheme. Nothing here recolours components.
const skinKey = 'masa-skin:skin-pref';
const primaryKey = 'masa-skin:primary-color';
// Read by skin-boot.js before the first paint of the next page load.
const bootKey = 'masa-skin:boot';

const assetBase = import.meta.url.split('_content/Masa.Stack.Components/')[0];

export function getSavedSkin() {
    try { return localStorage.getItem(skinKey); } catch { return null; }
}

export function getSavedPrimaryColor() {
    try { return localStorage.getItem(primaryKey); } catch { return null; }
}

/** Fetches skins.json and returns its text; SkinManager parses and validates it. */
export async function loadCatalog(path) {
    const response = await fetch(new URL(path, assetBase).href, { cache: 'no-cache' });
    if (!response.ok) throw new Error(`Cannot load the skin catalog (${response.status}): ${path}`);
    return await response.text();
}

function hrefMatches(link, href) {
    return new URL(link.href).pathname.endsWith('/' + href.replace(/^\//, ''));
}

/** Adds the stylesheet once and resolves when it has loaded (or failed; the page stays usable on defaults). */
function ensureStylesheet(kind, href) {
    const existing = [...document.querySelectorAll(`link[data-masa-skin-css="${kind}"]`)].find(l => hrefMatches(l, href));
    if (existing) {
        existing.disabled = false;
        return existing.sheet ? Promise.resolve() : new Promise(resolve => {
            existing.addEventListener('load', resolve, { once: true });
            existing.addEventListener('error', resolve, { once: true });
        });
    }
    return new Promise(resolve => {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = new URL(href, assetBase).href;
        link.dataset.masaSkinCss = kind;
        link.addEventListener('load', resolve, { once: true });
        link.addEventListener('error', resolve, { once: true });
        document.head.appendChild(link);
    });
}

/**
 * Custom primary colour. The catalog mirrors the role colours under html[data-skin][data-theme] .theme--*,
 * which outranks the runtime theme MASA writes, so a user-chosen primary needs the same selectors. It is
 * a plain override of three variables (no !important, no component selectors) and is re-appended after every
 * skin stylesheet so source order decides between equal selectors.
 */
function primaryStyle(primary, onPrimary) {
    let style = document.querySelector('style[data-masa-skin-primary]');
    if (!primary) { style?.remove(); return; }
    const rgb = value => value.replace('#', '').match(/\w\w/g).map(part => parseInt(part, 16)).join(', ');
    const scope = 'html[data-skin][data-theme]';
    style ??= document.createElement('style');
    style.dataset.masaSkinPrimary = '';
    style.textContent = `${scope}, ${scope} .m-application, ${scope} [class*="theme--"] {
  --m-theme-primary: ${rgb(primary)}; --m-theme-primary-text: ${rgb(primary)}; --m-theme-on-primary: ${rgb(onPrimary)};
}`;
    document.head.appendChild(style);
}

export async function applySkin(id, profile, tokensHref, baseHref, dark, persist, primary = null, onPrimary = '#FFFFFF') {
    const pending = [];
    if (baseHref) pending.push(ensureStylesheet('base', baseHref));
    if (tokensHref) pending.push(ensureStylesheet('tokens', tokensHref));
    for (const link of document.querySelectorAll('link[data-masa-skin-css="tokens"]')) {
        link.disabled = !tokensHref || !hrefMatches(link, tokensHref);
    }
    await Promise.all(pending);

    // Flip the channel only after the stylesheet is in, so the page changes from one skin to the next in one step.
    const root = document.documentElement;
    root.dataset.skin = id;
    root.dataset.theme = dark ? 'dark' : 'light';
    root.dataset.skinProfile = profile;
    primaryStyle(primary, onPrimary);

    try {
        localStorage.setItem(bootKey, JSON.stringify({
            id, profile, dark, tokens: tokensHref || '', base: baseHref || '', primary: primary || '', onPrimary,
        }));
        if (persist) {
            localStorage.setItem(skinKey, id);
            if (primary) localStorage.setItem(primaryKey, primary);
            else localStorage.removeItem(primaryKey);
        }
    } catch { /* Storage can be disabled. */ }
}
