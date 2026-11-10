window.visaPeopleActionMenu = window.visaPeopleActionMenu || (function () {
    var _dotNetRef = null;
    var _rootIds = [];

    function place(buttonId, panelId) {
        var btn = document.getElementById(buttonId);
        var panel = document.getElementById(panelId);
        if (!btn || !panel)
            return;

        var rect = btn.getBoundingClientRect();
        var vh = window.innerHeight || document.documentElement.clientHeight || 800;
        var vw = window.innerWidth || document.documentElement.clientWidth || 1200;
        var maxHeight = Math.max(160, Math.floor(vh * 0.9));
        var width = Math.max(Math.floor(rect.width), 280);
        width = Math.min(width, Math.max(240, vw - 16));

        panel.style.position = "fixed";
        panel.style.zIndex = "80";
        panel.style.width = width + "px";
        panel.style.minWidth = width + "px";
        panel.style.left = Math.max(8, Math.min(rect.left, vw - width - 8)) + "px";
        panel.style.right = "auto";
        panel.style.bottom = "auto";
        // Measure natural content height first (uncapped).
        panel.style.height = "auto";
        panel.style.maxHeight = "none";
        panel.style.overflowY = "visible";
        var contentHeight = Math.ceil(panel.scrollHeight || panel.getBoundingClientRect().height || 0);
        if (contentHeight < 48)
            contentHeight = 48;

        var spaceBelow = Math.max(0, vh - rect.bottom - 8);
        var spaceAbove = Math.max(0, rect.top - 8);
        var openUp = contentHeight > spaceBelow && spaceAbove > spaceBelow;
        var available = openUp ? spaceAbove : spaceBelow;
        if (available < 80)
            available = Math.max(spaceBelow, spaceAbove, 80);

        var height = Math.min(contentHeight, available, maxHeight);
        panel.style.height = height + "px";
        panel.style.maxHeight = height + "px";
        panel.style.overflowY = contentHeight > height ? "auto" : "hidden";
        panel.classList.toggle("is-up", openUp);

        if (openUp) {
            panel.style.top = Math.max(8, rect.top - height - 4) + "px";
        } else {
            panel.style.top = Math.min(rect.bottom + 4, Math.max(8, vh - height - 8)) + "px";
        }
    }

    function isInsideRoots(target) {
        if (!target || !_rootIds || _rootIds.length === 0)
            return false;
        for (var i = 0; i < _rootIds.length; i++) {
            var el = document.getElementById(_rootIds[i]);
            if (el && el.contains(target))
                return true;
        }
        return false;
    }

    function onPointerDown(event) {
        if (!_dotNetRef)
            return;
        if (isInsideRoots(event.target))
            return;
        try {
            _dotNetRef.invokeMethodAsync("ClosePeoplePreviewMenusFromJs");
        } catch (e) {
            // Circuit may be gone.
        }
    }

    function onKeyDown(event) {
        if (!_dotNetRef || event.key !== "Escape")
            return;
        try {
            _dotNetRef.invokeMethodAsync("ClosePeoplePreviewMenusFromJs");
        } catch (e) {
        }
    }

    function bindDismiss(dotNetRef, rootIds) {
        unbindDismiss();
        _dotNetRef = dotNetRef || null;
        _rootIds = Array.isArray(rootIds) ? rootIds.slice() : [];
        if (!_dotNetRef)
            return;
        document.addEventListener("pointerdown", onPointerDown, true);
        document.addEventListener("keydown", onKeyDown, true);
    }

    function unbindDismiss() {
        document.removeEventListener("pointerdown", onPointerDown, true);
        document.removeEventListener("keydown", onKeyDown, true);
        _dotNetRef = null;
        _rootIds = [];
    }

    return {
        place: place,
        bindDismiss: bindDismiss,
        unbindDismiss: unbindDismiss
    };
})();