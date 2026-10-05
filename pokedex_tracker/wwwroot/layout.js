let sourceOpener;

export function getLastTracker() {
    try { return localStorage.getItem('pokedex-last-tracker'); } catch { return null; }
}

export function saveLastTracker(id) {
    try {
        if (id === null) localStorage.removeItem('pokedex-last-tracker');
        else localStorage.setItem('pokedex-last-tracker', id);
    } catch { /* Tracker URLs still work when preferences cannot be saved. */ }
}

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
