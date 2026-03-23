document.addEventListener("DOMContentLoaded", () => {
    const appContainer = document.getElementById("appContainer");
    const sidebarToggle = document.getElementById("sidebarToggle");
    const navGroups = document.querySelectorAll("[data-nav-group]");

    const initializeNavGroups = () => {
        if (!navGroups.length) {
            return;
        }

        navGroups.forEach((group) => {
            const toggle = group.querySelector("[data-nav-group-toggle]");
            const panel = group.querySelector("[data-nav-group-panel]");

            if (!toggle || !panel) {
                return;
            }

            const groupKey = group.dataset.groupKey;
            const storageKey = groupKey ? `cdc-portal-nav-group-v2-${groupKey}` : null;
            const defaultOpen = group.dataset.groupDefaultOpen !== "false";

            const applyGroupState = (expanded) => {
                toggle.setAttribute("aria-expanded", expanded.toString());
                panel.hidden = !expanded;
            };

            let expanded = defaultOpen;

            if (storageKey) {
                try {
                    const storedValue = localStorage.getItem(storageKey);
                    if (storedValue !== null) {
                        expanded = storedValue === "true";
                    }
                } catch {
                }
            }

            applyGroupState(expanded);

            toggle.addEventListener("click", () => {
                const nextExpanded = toggle.getAttribute("aria-expanded") !== "true";
                applyGroupState(nextExpanded);

                if (!storageKey) {
                    return;
                }

                try {
                    localStorage.setItem(storageKey, nextExpanded.toString());
                } catch {
                }
            });
        });
    };

    initializeNavGroups();

    if (!appContainer || !sidebarToggle || appContainer.dataset.sidebarCollapsible !== "true") {
        return;
    }

    const storageKey = "cdc-portal-sidebar-collapsed-v2";

    const applySidebarState = (collapsed) => {
        appContainer.classList.toggle("sidebar-collapsed", collapsed);
        sidebarToggle.setAttribute("aria-pressed", collapsed.toString());

        const actionLabel = collapsed ? "Expandir menu lateral" : "Contraer menu lateral";
        sidebarToggle.setAttribute("aria-label", actionLabel);
        sidebarToggle.setAttribute("title", actionLabel);
    };

    try {
        applySidebarState(localStorage.getItem(storageKey) === "true");
    } catch {
        applySidebarState(false);
    }

    sidebarToggle.addEventListener("click", () => {
        const collapsed = !appContainer.classList.contains("sidebar-collapsed");
        applySidebarState(collapsed);

        try {
            localStorage.setItem(storageKey, collapsed.toString());
        } catch {
        }
    });
});
