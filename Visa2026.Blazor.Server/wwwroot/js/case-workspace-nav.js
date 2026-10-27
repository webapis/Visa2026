window.visaCaseWorkspaceNav = window.visaCaseWorkspaceNav || (function () {
    var holds = 0;

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
        }
    };
})();
