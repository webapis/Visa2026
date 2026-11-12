window.visaCaseWorkspaceNav = window.visaCaseWorkspaceNav || (function () {
    var holds = 0;
    var rightWidthStorageKey = "visa.caseRight.widthPx.v2";
    var rightDefaultWidth = 560;
    var rightMinWidth = 360;
    var rightMinMain = 320;

    function shell() {
        return document.getElementById("visa-app-shell");
    }

    function caseVisible() {
        var nodes = document.querySelectorAll(".officer-case-workspace");
        for (var i = 0; i < nodes.length; i++) {
            if (nodes[i].getClientRects().length > 0)
                return true;
        }
        return false;
    }

    function apply() {
        var el = shell();
        if (!el)
            return;
        // Keep the XAF left navigation open while a case is on screen.
        el.classList.remove("visa-app-shell--nav-collapsed-for-case");
    }

    function rightLayout() {
        return document.querySelector(".officer-case-workspace .cw-layout--right-case");
    }

    function clampRightWidth(layout, width) {
        var max = rightDefaultWidth * 2;
        if (layout && layout.clientWidth > 0)
            max = Math.max(rightMinWidth, layout.clientWidth - rightMinMain);
        return Math.max(rightMinWidth, Math.min(max, Math.round(width)));
    }

    function readSavedRightWidth() {
        try {
            var raw = sessionStorage.getItem(rightWidthStorageKey);
            if (!raw)
                return 0;
            var saved = JSON.parse(raw);
            if (saved && saved.width > 0)
                return saved.width;
        } catch (e) { }
        return 0;
    }

    function persistRightWidth(width) {
        try {
            sessionStorage.setItem(rightWidthStorageKey, JSON.stringify({ width: width }));
        } catch (e) { }
    }

    function setRightWidth(layout, width, persist) {
        if (!layout)
            return 0;
        var next = clampRightWidth(layout, width);
        layout.style.setProperty("--cw-case-right-width", next + "px");
        layout.classList.add("cw-layout--right-sized");
        if (persist)
            persistRightWidth(next);
        return next;
    }

    function restoreRightWidth(layout) {
        if (!layout)
            return;
        var saved = readSavedRightWidth();
        setRightWidth(layout, saved > 0 ? saved : rightDefaultWidth, false);
    }

    function bindRightPanelResize() {
        var layout = rightLayout();
        if (!layout)
            return;
        restoreRightWidth(layout);

        var handle = layout.querySelector(".cw-case-right__resize");
        if (!handle || handle.getAttribute("data-cw-resize-bound") === "1")
            return;
        handle.setAttribute("data-cw-resize-bound", "1");

        var dragging = false;
        var activePointerId = null;
        var resizeRafId = 0;
        var pendingClientX = 0;

        var detachDocDrag = function () {
            document.removeEventListener("pointermove", onDocPointerMove);
            document.removeEventListener("pointerup", onDocPointerUp);
            document.removeEventListener("pointercancel", onDocPointerUp);
        };

        var flushResizeFrame = function () {
            resizeRafId = 0;
            if (!dragging)
                return;
            var rect = layout.getBoundingClientRect();
            var width = rect.right - pendingClientX;
            setRightWidth(layout, width, false);
        };

        var scheduleResize = function (clientX) {
            pendingClientX = clientX;
            if (resizeRafId)
                return;
            resizeRafId = window.requestAnimationFrame(flushResizeFrame);
        };

        var stopDrag = function () {
            if (!dragging)
                return;
            dragging = false;
            activePointerId = null;
            detachDocDrag();
            if (resizeRafId) {
                window.cancelAnimationFrame(resizeRafId);
                resizeRafId = 0;
                flushResizeFrame();
            }
            layout.classList.remove("cw-layout--right-resizing");
            document.body.style.cursor = "";
            document.body.style.userSelect = "";
            var current = layout.style.getPropertyValue("--cw-case-right-width");
            var px = parseInt(current, 10);
            if (px > 0)
                setRightWidth(layout, px, true);
        };

        var onDocPointerMove = function (event) {
            if (!dragging || event.pointerId !== activePointerId)
                return;
            event.preventDefault();
            scheduleResize(event.clientX);
        };

        var onDocPointerUp = function (event) {
            if (!dragging || (event.pointerId != null && event.pointerId !== activePointerId))
                return;
            stopDrag();
        };

        handle.addEventListener("pointerdown", function (event) {
            if (event.button !== 0)
                return;
            event.preventDefault();
            event.stopPropagation();
            dragging = true;
            activePointerId = event.pointerId != null ? event.pointerId : -1;
            layout.classList.add("cw-layout--right-resizing");
            document.body.style.cursor = "col-resize";
            document.body.style.userSelect = "none";
            document.addEventListener("pointermove", onDocPointerMove, { passive: false });
            document.addEventListener("pointerup", onDocPointerUp);
            document.addEventListener("pointercancel", onDocPointerUp);
            scheduleResize(event.clientX);
        });

        handle.addEventListener("dblclick", function (event) {
            event.preventDefault();
            try {
                sessionStorage.removeItem(rightWidthStorageKey);
            } catch (e) { }
            setRightWidth(layout, rightDefaultWidth, false);
        });
    }

    window.addEventListener("resize", apply);
    document.addEventListener("click", function () {
        if (holds > 0)
            requestAnimationFrame(apply);
    }, true);

    if (window.MutationObserver) {
        var observer = new MutationObserver(function () {
            if (holds > 0)
                apply();
        });
        observer.observe(document.documentElement, { childList: true, subtree: true });
    }

    return {
        hold: function () {
            holds++;
            apply();
            requestAnimationFrame(apply);
        },
        release: function () {
            holds = Math.max(0, holds - 1);
            apply();
        },
        bindRightPanelResize: bindRightPanelResize,
        readActiveSummaryField: function () {
            var el = document.activeElement;
            if (!el || typeof el.closest !== "function")
                return null;
            var host = el.closest("[data-summary-key]");
            if (!host)
                return null;
            return {
                key: host.getAttribute("data-summary-key") || "",
                kind: host.getAttribute("data-summary-kind") || "",
                value: "value" in el ? (el.value || "") : ""
            };
        },
        scrollJustLinkedPerson: function () {
            var row = document.querySelector(".officer-case-workspace .os-person-row.is-just-linked");
            if (!row)
                return;
            var scroller = row.closest(".cw-people-links-scroll");
            if (scroller)
                scroller.scrollTop = 0;
            if (row.scrollIntoView)
                row.scrollIntoView({ block: "nearest" });
        }
    };
})();