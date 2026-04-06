document.addEventListener("DOMContentLoaded", () => {
    const appContainer = document.getElementById("appContainer");
    const sidebarToggle = document.getElementById("sidebarToggle");
    const navGroups = document.querySelectorAll("[data-nav-group]");

    const initializeNavGroups = () => {
        if (!navGroups.length) {
            return;
        }

        const accordionStorageKey = "cdc-portal-nav-group-open-v3";
        const groups = Array.from(navGroups)
            .map((group) => {
                const toggle = group.querySelector("[data-nav-group-toggle]");
                const panel = group.querySelector("[data-nav-group-panel]");

                if (!toggle || !panel) {
                    return null;
                }

                return {
                    group,
                    toggle,
                    panel,
                    groupKey: group.dataset.groupKey ?? "",
                    defaultOpen: group.dataset.groupDefaultOpen !== "false",
                    hasActiveLink: !!group.querySelector(".nav-link.is-active, .nav-link[aria-current='page']")
                };
            })
            .filter(Boolean);

        if (!groups.length) {
            return;
        }

        const applyGroupState = (targetGroupKey) => {
            groups.forEach((item) => {
                const expanded = item.groupKey === targetGroupKey;
                item.toggle.setAttribute("aria-expanded", expanded.toString());
                item.panel.hidden = !expanded;
            });
        };

        let expandedGroupKey = "";

        const activeGroup = groups.find((item) => item.hasActiveLink);
        if (activeGroup) {
            expandedGroupKey = activeGroup.groupKey;
        } else {
            try {
                const storedValue = localStorage.getItem(accordionStorageKey);
                if (storedValue && groups.some((item) => item.groupKey === storedValue)) {
                    expandedGroupKey = storedValue;
                }
            } catch {
            }

            if (!expandedGroupKey) {
                const defaultGroup = groups.find((item) => item.defaultOpen);
                if (defaultGroup) {
                    expandedGroupKey = defaultGroup.groupKey;
                }
            }
        }

        applyGroupState(expandedGroupKey);

        groups.forEach((item) => {
            item.toggle.addEventListener("click", () => {
                const isExpanded = item.toggle.getAttribute("aria-expanded") === "true";
                const nextExpandedGroupKey = isExpanded ? "" : item.groupKey;
                applyGroupState(nextExpandedGroupKey);

                try {
                    if (nextExpandedGroupKey) {
                        localStorage.setItem(accordionStorageKey, nextExpandedGroupKey);
                    } else {
                        localStorage.removeItem(accordionStorageKey);
                    }
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
