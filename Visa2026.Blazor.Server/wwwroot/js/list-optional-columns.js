window.visaListOptionalColumns = (function () {
    var observer = null;
    var timer = 0;
    var dotNet = null;
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

    function setNavCollapsed() {
        var el = shell();
        if (!el)
            return;
        // Keep the XAF left navigation open on Application list views.
        el.classList.remove("visa-app-shell--nav-collapsed-for-list");
    }

    function observe(ref) {
        window.clearTimeout(timer);
        if (observer)
            observer.disconnect();
        observer = null;
        dotNet = ref;
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

    function release() {
        window.clearTimeout(timer);
        if (observer)
            observer.disconnect();
        observer = null;
        dotNet = null;
        var el = shell();
        if (el)
            el.classList.remove("visa-app-shell--nav-collapsed-for-list");
    }

    return {
        overflows: overflows,
        resetScroll: resetScroll,
        setNavCollapsed: setNavCollapsed,
        observe: observe,
        release: release
    };
})();