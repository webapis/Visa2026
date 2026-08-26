window.visaIssuedFieldCue = window.visaIssuedFieldCue || (function () {
    var attached = 0;
    var observer = null;
    var scanTimer = 0;
    var CUE = ["visa-field-cue--needs", "visa-field-cue--default", "visa-field-cue--confirmed", "visa-field-cue--sourced"];

    function isPreviewSlot(el) {
        return !!(el && el.closest && el.closest("#visa-preview-slot"));
    }

    function findRoots() {
        var list = [];
        document.querySelectorAll(".xaf-detail-view").forEach(function (el) {
            if (isPreviewSlot(el)) return;
            var rect = el.getBoundingClientRect();
            if (rect.width < 80 || rect.height < 80) return;
            list.push(el);
        });
        return list;
    }

    function editorHost(item) {
        return item.querySelector(".dxbl-fl-ctrl") || item;
    }

    function shouldSkip(item) {
        if (!item || item.querySelector(".dxbl-grid, .dxbl-grid-container, .xaf-list-view"))
            return true;
        if (item.classList.contains("xaf-optional-fields-toggle"))
            return true;
        var host = editorHost(item);
        if (host.querySelector("input[type=checkbox]") && !host.querySelector(".dxbl-text-edit, textarea, select, .dxbl-date-edit"))
            return true;
        return !host.querySelector(".dxbl-text-edit, .dxbl-date-edit, .dxbl-dropdown-box, .dxbl-combobox, textarea, select, input:not([type=checkbox]):not([type=hidden]):not([type=file])");
    }

    function readValue(item) {
        var host = editorHost(item);
        var input = host.querySelector("input:not([type=checkbox]):not([type=hidden]):not([type=file]), textarea, select, .dxbl-text-edit-input");
        if (!input)
            return "";
        if (input.tagName === "SELECT")
            return (input.value || "").trim();
        return (input.value || input.textContent || "").trim();
    }

    function isReadOnly(item) {
        var host = editorHost(item);
        var input = host.querySelector("input, textarea, select");
        if (input && (input.disabled || input.readOnly))
            return true;
        return !!host.querySelector(".dxbl-text-edit.dxbl-disabled, [aria-disabled=true]");
    }

    function clearCue(item) {
        CUE.forEach(function (c) { item.classList.remove(c); });
    }

    function applyCue(item) {
        if (shouldSkip(item)) {
            clearCue(item);
            return;
        }
        var value = readValue(item);
        var reviewed = item.getAttribute("data-visa-cue-reviewed") === "1";
        var state;
        if (isReadOnly(item))
            state = value ? "sourced" : "needs";
        else if (!value)
            state = "needs";
        else if (reviewed)
            state = "confirmed";
        else
            state = "default";
        clearCue(item);
        item.classList.add("visa-field-cue--" + state);
    }

    function scanRoot(root) {
        root.classList.add("visa-issued-new-cues");
        root.querySelectorAll(".dxbl-fl-item").forEach(applyCue);
    }

    function scanAll() {
        findRoots().forEach(scanRoot);
    }

    function scheduleScan() {
        if (scanTimer)
            window.clearTimeout(scanTimer);
        scanTimer = window.setTimeout(function () {
            scanTimer = 0;
            scanAll();
        }, 120);
    }

    function onFocusOut(e) {
        var item = e.target && e.target.closest ? e.target.closest(".visa-issued-new-cues .dxbl-fl-item") : null;
        if (!item || shouldSkip(item))
            return;
        item.setAttribute("data-visa-cue-reviewed", "1");
        applyCue(item);
    }

    function bind() {
        if (observer)
            return;
        document.addEventListener("focusout", onFocusOut, true);
        observer = new MutationObserver(scheduleScan);
        observer.observe(document.body, { childList: true, subtree: true, characterData: false });
    }

    function unbind() {
        document.removeEventListener("focusout", onFocusOut, true);
        if (observer) {
            observer.disconnect();
            observer = null;
        }
        if (scanTimer) {
            window.clearTimeout(scanTimer);
            scanTimer = 0;
        }
        document.querySelectorAll(".visa-issued-new-cues").forEach(function (el) {
            el.classList.remove("visa-issued-new-cues");
            el.querySelectorAll(".dxbl-fl-item").forEach(function (item) {
                clearCue(item);
                item.removeAttribute("data-visa-cue-reviewed");
            });
        });
    }

    return {
        attach: function () {
            attached += 1;
            bind();
            window.setTimeout(scanAll, 80);
            window.setTimeout(scanAll, 400);
        },
        detach: function () {
            attached = Math.max(0, attached - 1);
            if (attached === 0)
                unbind();
        }
    };
})();