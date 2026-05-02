'use strict'

const ThemeOption = {
    System: "System",
    Dark: "Dark",
    Light: "Light"
}

const themeStorageKey = 'theme';

const normalizeThemeOption = theme => {
    return Object.values(ThemeOption).includes(theme) ? theme : ThemeOption.System;
};

window.getTheme = () => {
    try {
        return localStorage.getItem(themeStorageKey);
    }
    catch {
        return null;
    }
};

window.storeTheme = theme => {
    try {
        localStorage.setItem(themeStorageKey, theme);
    }
    catch {
        // Ignore storage failures and keep the theme in memory for this page load.
    }
};

window.isSystemDarkMode = () => {
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
}

window.getThemePreference = () => {
    const theme = normalizeThemeOption(getTheme());

    if (theme !== ThemeOption.System) {
        return theme;
    }

    return isSystemDarkMode() ? ThemeOption.Dark : ThemeOption.Light;
}

window.setTheme = () => {
    const selectedTheme = normalizeThemeOption(getTheme());
    const resolvedOption = getThemePreference();
    const isDark = resolvedOption === ThemeOption.Dark;
    const resolvedTheme = isDark ? 'dark' : 'light';
    const root = document.documentElement;

    root.dataset.theme = resolvedTheme;
    root.dataset.themeOption = selectedTheme.toLowerCase();
    root.style.colorScheme = resolvedTheme;

    if (document.body) {
        document.body.classList.toggle('mud-theme-dark', isDark);
        document.body.classList.toggle('mud-theme-light', !isDark);
    }

    return resolvedOption;
}

setTheme();

window.addEventListener("DOMContentLoaded", () => {
    setTheme();
});

const colorSchemeQuery = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)');
if (colorSchemeQuery) {
    colorSchemeQuery.addEventListener('change', () => {
        if (!getTheme() || getTheme() === ThemeOption.System) {
            setTheme();
        }
    });
}
