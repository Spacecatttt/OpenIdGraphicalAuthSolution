window.themeInterop = {
    initialize: () => {
        const savedTheme = localStorage.getItem("theme") || "light";
        document.documentElement.className = savedTheme + "-theme";
        document.body.setAttribute("data-bs-theme", savedTheme);
        return savedTheme;
    },
    isLight: () => {
        return (localStorage.getItem("theme") || "light") === "light";
    },
    toggle: () => {
        const currentTheme = localStorage.getItem("theme") || "light";
        const newTheme = currentTheme === "light" ? "dark" : "light";
        localStorage.setItem("theme", newTheme);
        document.documentElement.className = newTheme + "-theme";
        document.body.setAttribute("data-bs-theme", newTheme);
    }
};
