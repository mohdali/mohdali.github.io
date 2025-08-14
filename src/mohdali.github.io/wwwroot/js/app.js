'use strict'

const ThemeOption = {
    System: "System",
    Dark: "Dark",
    Light: "Light"
}

window.getTheme = () => localStorage.getItem('theme');

window.storeTheme = theme => localStorage.setItem('theme', theme);

window.getThemePreference = () => {
    const theme = getTheme()

    if (theme && theme != ThemeOption.System)
        return theme;

    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'Dark' : 'Light'
}

window.isSystemDarkMode = () => {
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
}

window.setTheme = () => {
    const theme = getThemePreference();

    if (theme == ThemeOption.Dark) {
        document.body.classList.add('mud-theme-dark');
        document.body.classList.remove('mud-theme-light');
    }
    else {
        document.body.classList.add('mud-theme-light');
        document.body.classList.remove('mud-theme-dark');
    }
}

setTheme();

window.addEventListener("DOMContentLoaded", () => {
    setTheme();
});

window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
    setTheme();
})