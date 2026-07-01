function _applyTheme(theme) {
    document.documentElement.setAttribute('data-bs-theme', theme === 'light' ? 'light' : 'dark');
}

function _saveTheme(theme) {
    localStorage.setItem('pch-theme', theme);
    document.cookie = 'pch-theme=' + theme + '; path=/; max-age=31536000; SameSite=Lax';
}

window.pch = window.pch || {};
Object.assign(window.pch, {
    getTheme:         () => localStorage.getItem('pch-theme') || 'dark',
    applyStoredTheme: () => { _applyTheme(localStorage.getItem('pch-theme') || 'dark'); },
    setTheme:         (theme) => { _saveTheme(theme); _applyTheme(theme); },
    toggleTheme:      () => {
        const next = (localStorage.getItem('pch-theme') || 'dark') === 'dark' ? 'light' : 'dark';
        _saveTheme(next);
        _applyTheme(next);
        return next;
    },
});
