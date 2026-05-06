window.topekaTheme = {
    get() {
        try { return localStorage.getItem("topeka-theme"); } catch { return null; }
    },
    set(theme) {
        window.topekaTheme._settingSelf = true;
        document.documentElement.setAttribute("data-theme", theme);
        window.topekaTheme._settingSelf = false;
        try { localStorage.setItem("topeka-theme", theme); } catch { }
    },
    apply() {
        const t = window.topekaTheme.get();
        if (t && document.documentElement.getAttribute("data-theme") !== t) {
            window.topekaTheme.set(t);
        }
    },
    _settingSelf: false
};

window.topekaNav = {
    toggle() {
        const app = document.querySelector(".app");
        if (!app) return false;
        const collapsed = app.classList.toggle("nav-collapsed");
        try { localStorage.setItem("topeka-nav-collapsed", collapsed ? "1" : "0"); } catch { }
        return collapsed;
    },
    apply() {
        try {
            const collapsed = localStorage.getItem("topeka-nav-collapsed") === "1";
            const app = document.querySelector(".app");
            if (app) app.classList.toggle("nav-collapsed", collapsed);
        } catch { }
    }
};

// Blazor enhanced navigation diffs the <html> element's attributes back to
// the server-rendered value on every nav. Watch for that and re-apply our
// stored theme so it survives page transitions.
(function () {
    const target = document.documentElement;
    const obs = new MutationObserver(muts => {
        if (window.topekaTheme._settingSelf) return;
        for (const m of muts) {
            if (m.type === "attributes" && m.attributeName === "data-theme") {
                window.topekaTheme.apply();
                break;
            }
        }
    });
    obs.observe(target, { attributes: true, attributeFilter: ["data-theme"] });
    window.topekaTheme.apply();
})();
