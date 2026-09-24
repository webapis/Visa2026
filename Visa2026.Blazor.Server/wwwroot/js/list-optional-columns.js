window.visaListOptionalColumns = (function () {
    var observer = null;
    var timer = 0;
    var dotNet = null;
    var navClickAttached = false;
    var navPinnedOpen = false;
    var suppressResizeUntil = 0;

    function visibleGrid() {
        var nodes = document.querySelectorAll(".dxbl-grid");
        var best = null;
        var bestWidth = 0;
        for (var i = 0; i < nodes.length; i++) {
            var rect = nodes[i].getBoundingClientRect();
            if (rect.width < 200 || rect.height < 40)
                continue;
            if (rect.width > bestWidth) {
                best = nodes[i];
                bestWidth = rect.width;
            }
        }
        return best;
    }

    function scrollersOf(gridNode) {
        return gridNode.querySelectorAll(".dxbl-scroll-viewer-content");
    }

    function shell() {
        return document.getElementById("visa-app-shell");
    }

    function isNavToggle(target) {
        if (!target || !target.closest)
            return false;
        if (target.closest("#visa-preview-slot, .sidebar"))
            return false;
        var btn = target.closest("button, a[role='button'], .dxbl-btn");
        if (!btn)
            return false;
        if (btn.querySelector(".dx-icon-menu, .menu-icon-menu, [class*='menu-icon-menu'], .dx-icon-hamburger"))
            return true;
        var label = (btn.getAttribute("aria-label") || btn.getAttribute("title") || btn.textContent || "").toLowerCase();
        return label.indexOf("navigation") >= 0 || label.indexOf("nawig") >= 0
            || btn.classList.contains("collapse-toggle") || !!btn.closest(".collapse-toggle");
    }

    function onNavClick(event) {
        var el = shell();
        if (!el || !el.classList.contains("visa-app-shell--nav-collapsed-for-list"))
            return;
        if (!isNavToggle(event.target))
            return;
        event.preventDefault();
        event.stopPropagation();
        navPinnedOpen = true;
        el.classList.remove("visa-app-shell--nav-collapsed-for-list");
    }

    function contentOverflows(el) {
        return !!el && el.scrollWidth > el.clientWidth + 2;
    }

    function overflows() {
        var gridNode = visibleGrid();
        if (!gridNode)
            return false;

        var bars = gridNode.querySelectorAll(".dxbl-scroll-viewer-hor-scroll-bar");
        for (var b = 0; b < bars.length; b++) {
            if (bars[b].classList.contains("dxbl-active"))
                return true;
        }

        var scrollers = scrollersOf(gridNode);
        for (var i = 0; i < scrollers.length; i++) {
            if (contentOverflows(scrollers[i]))
                return true;
        }

        var spacers = gridNode.querySelectorAll("[dxbl-right-virtual-spacer-element]");
        for (var s = 0; s < spacers.length; s++) {
            if (spacers[s].offsetWidth > 1)
                return true;
        }

        var row = gridNode.querySelector(".dxbl-grid-header-row") || gridNode.querySelector("thead tr");
        if (row) {
            var sum = 0;
            var cells = row.children;
            for (var c = 0; c < cells.length; c++)
                sum += cells[c].offsetWidth;
            var host = scrollers.length ? scrollers[0] : gridNode;
            if (sum > host.clientWidth + 2)
                return true;
            var rowRect = row.getBoundingClientRect();
            if (rowRect.right > window.innerWidth + 1)
                return true;
        }

        return false;
    }

    function resetScroll() {
        var gridNode = visibleGrid();
        if (!gridNode)
            return;
        var scrollers = scrollersOf(gridNode);
        for (var i = 0; i < scrollers.length; i++)
            scrollers[i].scrollLeft = 0;
    }

    function setNavCollapsed(collapse) {
        var el = shell();
        if (!el)
            return;
        if (collapse && navPinnedOpen)
            return;
        if (!collapse)
            navPinnedOpen = false;
        suppressResizeUntil = Date.now() + 700;
        el.classList.toggle("visa-app-shell--nav-collapsed-for-list", !!collapse);
    }

    function observe(ref) {
        window.clearTimeout(timer);
        if (observer)
            observer.disconnect();
        observer = null;
        dotNet = ref;
        if (!navClickAttached) {
            document.addEventListener("click", onNavClick, true);
            navClickAttached = true;
        }
        var gridNode = visibleGrid();
        if (!gridNode || typeof ResizeObserver === "undefined")
            return;
        var scroller = gridNode.querySelector(".dxbl-scroll-viewer-content") || gridNode;
        observer = new ResizeObserver(function () {
            if (Date.now() < suppressResizeUntil)
                return;
            window.clearTimeout(timer);
            timer = window.setTimeout(function () {
                if (Date.now() < suppressResizeUntil)
                    return;
                if (dotNet)
                    dotNet.invokeMethodAsync("OnListWidthChanged");
            }, 200);
        });
        observer.observe(scroller);
    }

    function listNeedsNavClosed() {
        return /\/Application_ListView/i.test(location.pathname);
    }

    function syncListNav() {
        var el = shell();
        if (!el)
            return;
        if (!listNeedsNavClosed()) {
            navPinnedOpen = false;
            el.classList.remove("visa-app-shell--nav-collapsed-for-list");
            return;
        }
        if (!navPinnedOpen)
            el.classList.add("visa-app-shell--nav-collapsed-for-list");
    }

    function release() {
        window.clearTimeout(timer);
        if (observer)
            observer.disconnect();
        observer = null;
        dotNet = null;
    }

    if (!navClickAttached) {
        document.addEventListener("click", onNavClick, true);
        navClickAttached = true;
    }
    syncListNav();
    window.setInterval(syncListNav, 300);
    window.addEventListener("popstate", syncListNav);
    window.addEventListener("resize", syncListNav);

    return {
        overflows: overflows,
        resetScroll: resetScroll,
        setNavCollapsed: setNavCollapsed,
        observe: observe,
        release: release
    };
})();