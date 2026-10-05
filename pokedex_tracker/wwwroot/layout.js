let sourceOpener;

export function getSidebarCollapsed() {
    try {
        const preference = localStorage.getItem('pokedex-sidebar-collapsed');
        if (preference !== null) return preference === 'true';
    } catch { /* Navigation still works when browser storage is unavailable. */ }
    return matchMedia('(max-width: 1179px)').matches;
}

export function saveSidebarCollapsed(collapsed) {
    try { localStorage.setItem('pokedex-sidebar-collapsed', String(collapsed)); } catch { }
}

export function rememberSourceOpener() {
    sourceOpener = document.activeElement;
}

export function restoreSourceFocus() {
    const target = sourceOpener?.isConnected && sourceOpener.checkVisibility({visibilityProperty: true})
        ? sourceOpener : document.getElementById('main-content');
    target?.focus({preventScroll: true});
    sourceOpener = null;
}
