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

window.setTheme = () => {
    const theme = getThemePreference();

    if (theme == ThemeOption.Dark) {
        document.body.classList.add(
            'dark-theme'
        );
    }
    else {
        document.body.classList.remove(
            'dark-theme'
        );
    }
}

document.addEventListener("DOMContentLoaded", () => {
    setTheme();
});