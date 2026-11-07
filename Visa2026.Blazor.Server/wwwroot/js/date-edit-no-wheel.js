/**
 * Ignore mouse wheel on officer date editors (mask sections + open calendar).
 * Keyboard typing and Up/Down remain enabled.
 */
(function () {
    if (window.__visaDateEditNoWheel) {
        return;
    }
    window.__visaDateEditNoWheel = true;

    function isDateEditWheelTarget(target) {
        if (!target) {
            return false;
        }
        var el = target.nodeType === 1 ? target : target.parentElement;
        if (!el || typeof el.closest !== "function") {
            return false;
        }
        if (el.closest(".dxbl-date-edit, .officer-date-edit, .dxbl-calendar, .dxbl-date-edit-dropdown")) {
            return true;
        }
        // Masked date input chrome (XAF / Dx) sometimes exposes text-edit classes only.
        if (el.matches && el.matches("input.dxbl-date-edit-input, input.dxbl-text-edit-input")) {
            var v = el.value || "";
            if (/^\d{2}[./]\d{2}[./]\d{4}/.test(v) || el.getAttribute("inputmode") === "numeric") {
                // Prefer ancestor markers when present; still block focused masked dates.
                if (el.closest(".dxbl-date-edit, .officer-date-edit, .dxbl-dropdown-box")) {
                    return true;
                }
            }
        }
        return false;
    }

    function blockWheel(e) {
        if (!isDateEditWheelTarget(e.target)) {
            return;
        }
        e.preventDefault();
        e.stopPropagation();
        if (typeof e.stopImmediatePropagation === "function") {
            e.stopImmediatePropagation();
        }
    }

    // Window capture so we run before DevExpress MaskedInput OnWheel handlers.
    window.addEventListener("wheel", blockWheel, { capture: true, passive: false });
    document.addEventListener("wheel", blockWheel, { capture: true, passive: false });

    function bindInput(input) {
        if (!input || input.__visaNoWheelBound) {
            return;
        }
        input.__visaNoWheelBound = true;
        input.addEventListener("wheel", blockWheel, { capture: true, passive: false });
    }

    function scan(root) {
        var scope = root && root.querySelectorAll ? root : document;
        scope.querySelectorAll(
            ".dxbl-date-edit input, .officer-date-edit input, input.dxbl-date-edit-input"
        ).forEach(bindInput);
        scope.querySelectorAll(".dxbl-calendar").forEach(function (cal) {
            if (cal.__visaNoWheelBound) {
                return;
            }
            cal.__visaNoWheelBound = true;
            cal.addEventListener("wheel", blockWheel, { capture: true, passive: false });
        });
    }

    function startObserver() {
        scan(document);
        if (!window.MutationObserver) {
            return;
        }
        var obs = new MutationObserver(function (mutations) {
            for (var i = 0; i < mutations.length; i++) {
                var m = mutations[i];
                for (var j = 0; j < m.addedNodes.length; j++) {
                    var n = m.addedNodes[j];
                    if (n && n.nodeType === 1) {
                        scan(n);
                    }
                }
            }
        });
        obs.observe(document.documentElement, { childList: true, subtree: true });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", startObserver);
    } else {
        startObserver();
    }
})();