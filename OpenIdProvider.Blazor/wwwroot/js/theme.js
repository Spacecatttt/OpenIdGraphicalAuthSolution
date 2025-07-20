let dotNetHelper;

function setDotNetHelper(helper) {
    dotNetHelper = helper;
}

function applyTheme(theme) {
    const currentClassName = document.documentElement.className;
    const newClassName = theme + "-theme";
    if (currentClassName !== newClassName) {
        document.documentElement.className = newClassName;
    }

    const currentBsTheme = document.body.getAttribute("data-bs-theme");
    if (currentBsTheme !== theme) {
        document.body.setAttribute("data-bs-theme", theme);
    }
}

function toggleTheme() {
    const currentTheme = localStorage.getItem("theme") || "light";
    const newTheme = currentTheme === "light" ? "dark" : "light";
    localStorage.setItem("theme", newTheme);
    applyTheme(newTheme);

    if (dotNetHelper) {
        const isLight = newTheme === 'light';
        dotNetHelper.invokeMethodAsync('UpdateThemeState', isLight);
    }
}

(function () {
    const savedTheme = localStorage.getItem("theme") || "light";
    applyTheme(savedTheme);

    const observer = new MutationObserver((mutationsList) => {
        for (const mutation of mutationsList) {
            if (mutation.type === 'attributes') {
                const expectedTheme = localStorage.getItem("theme") || "light";
                applyTheme(expectedTheme);
            }
        }
    });

    observer.observe(document.documentElement, { attributes: true });
    observer.observe(document.body, { attributes: true });
})();


window.themeInterop = {
    toggle: toggleTheme,
    setDotNetHelper: setDotNetHelper
};