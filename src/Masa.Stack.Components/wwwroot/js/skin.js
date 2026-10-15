const storageKey = 'masa-skin:skin-pref';

export function getSavedSkin() {
    try { return localStorage.getItem(storageKey); } catch { return null; }
}

function collectRoots(root, registeredHrefs, matches, roots, links) {
    const rootLinks = [...root.querySelectorAll('link[rel="stylesheet"]')];
    links.push(...rootLinks);
    if (rootLinks.some(link => registeredHrefs.some(path => matches(link, path)))) roots.push(root);
    for (const element of root.querySelectorAll('*')) {
        if (element.shadowRoot) collectRoots(element.shadowRoot, registeredHrefs, matches, roots, links);
    }
}

export function applySkin(id, href, registeredHrefs, dark, persist) {
    const links = [];
    const roots = [];
    const skinHrefs = registeredHrefs.filter(path => path);
    const matches = (link, path) => new URL(link.href).pathname.endsWith('/' + path.replace(/^\//, ''));
    collectRoots(document, skinHrefs, matches, roots, links);
    if (roots.length === 0) roots.push(document);

    if (href) {
        const assetBase = import.meta.url.split('_content/Masa.Stack.Components/')[0];
        for (const root of roots) {
            const rootLinks = links.filter(link => link.getRootNode() === root);
            if (rootLinks.some(link => matches(link, href))) continue;

            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = new URL(href, assetBase).href;
            if (root === document) document.head.appendChild(link);
            else root.appendChild(link);
            links.push(link);
        }
    }

    for (const link of links) {
        if (skinHrefs.some(path => matches(link, path))) link.disabled = !href || !matches(link, href);
    }
    document.documentElement.setAttribute('data-skin', id);
    document.documentElement.setAttribute('data-theme', dark ? 'dark' : 'light');
    if (persist) {
        try { localStorage.setItem(storageKey, id); } catch { /* Storage can be disabled. */ }
    }
}
