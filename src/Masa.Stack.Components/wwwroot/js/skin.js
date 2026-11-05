// Browser side of SkinManager. The only thing the page ever learns about a skin is the appearance channel
//   <html data-skin="<id>" data-theme="light|dark" data-skin-profile="identity|themed">
// plus one lazily loaded stylesheet per skin. Colours and geometry live in the skin catalog (Masa.Stack.Skins);
// MASA Blazor itself receives the roles from SkinManager.UpdateTheme. Nothing here recolours components.
const skinKey = 'masa-skin:skin-pref';
const primaryKey = 'masa-skin:primary-color';
// "1" / "0": the user's (or host's) opt-in to the optional component layer of a skin; absent = the host default.
const componentsKey = 'masa-skin:components-pref';
// Read by skin-boot.js before the first paint of the next page load.
const bootKey = 'masa-skin:boot';

const assetBase = import.meta.url.split('_content/Masa.Stack.Components/')[0];

export function getSavedSkin() {
    try { return localStorage.getItem(skinKey); } catch { return null; }
}

export function getSavedPrimaryColor() {
    try { return localStorage.getItem(primaryKey); } catch { return null; }
}

export function getSavedComponentLayer() {
    try { return localStorage.getItem(componentsKey); } catch { return null; }
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
        if (existing.disabled) existing.disabled = false;
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

/**
 * Step 1 of a switch: get the stylesheets of the target skin into the document and wait until they have loaded.
 * It has no visible effect (the skin css is scoped to its own html[data-skin], the base css to the themed profile),
 * so a slow network only delays the switch instead of painting a half-switched page.
 */
export async function prepareSkin(tokensHref, baseHref, componentsHref = '') {
    const pending = [];
    if (baseHref) pending.push(ensureStylesheet('base', baseHref));
    if (tokensHref) pending.push(ensureStylesheet('tokens', tokensHref));
    // Appended after the tokens, so the optional component layer never loses a source-order tie to them.
    if (componentsHref) pending.push(ensureStylesheet('components', componentsHref));
    await Promise.all(pending);
}

/**
 * Step 2: flip the page. SkinManager calls this in the same task in which it hands the roles to MASA, and nothing in
 * here awaits, so the new role colours and the new appearance channel reach the screen in the same frame.
 */
export function applySkin(id, profile, tokensHref, baseHref, dark, persist, primary = null, onPrimary = '#FFFFFF',
    componentsHref = '', componentLayer = false) {
    for (const link of document.querySelectorAll('link[data-masa-skin-css="tokens"]')) {
        const off = !tokensHref || !hrefMatches(link, tokensHref);
        if (link.disabled !== off) link.disabled = off;
    }
    // The optional component layer: its rules are scoped to html[data-skin="<id>"][data-skin-components], so with the
    // attribute absent (or the sheet disabled) the page is exactly what the catalog's tokens and base css describe.
    for (const link of document.querySelectorAll('link[data-masa-skin-css="components"]')) {
        const off = !componentsHref || !hrefMatches(link, componentsHref);
        if (link.disabled !== off) link.disabled = off;
    }

    const root = document.documentElement;
    root.dataset.skin = id;
    root.dataset.theme = dark ? 'dark' : 'light';
    root.dataset.skinProfile = profile;
    if (componentsHref) root.setAttribute('data-skin-components', '');
    else root.removeAttribute('data-skin-components');
    primaryStyle(primary, onPrimary);

    try {
        localStorage.setItem(bootKey, JSON.stringify({
            id, profile, dark, tokens: tokensHref || '', base: baseHref || '', primary: primary || '', onPrimary,
            components: componentsHref || '',
        }));
        if (persist) {
            localStorage.setItem(skinKey, id);
            localStorage.setItem(componentsKey, componentLayer ? '1' : '0');
            if (primary) localStorage.setItem(primaryKey, primary);
            else localStorage.removeItem(primaryKey);
        }
    } catch { /* Storage can be disabled. */ }
}
