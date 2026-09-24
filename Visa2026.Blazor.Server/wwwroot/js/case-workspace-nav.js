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
        var collapse = holds > 0 && caseVisible();
        el.classList.toggle("visa-app-shell--nav-collapsed-for-case", collapse);
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
        }
    };
})();
