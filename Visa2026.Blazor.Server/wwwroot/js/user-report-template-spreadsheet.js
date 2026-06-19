(function () {
    function getConfig() {
        return window.__urtSpreadsheetPageConfig || {};
    }

    window.urtSpreadsheet_onInit = function (spreadsheet) {
        window.__urtSpreadsheetInstance = spreadsheet;
        scheduleSpreadsheetResize();
    };

    window.urtSpreadsheet_onBeforeSend = function (_spreadsheet, e) {
        var cfg = getConfig();
        if (cfg.antiforgeryToken) {
            e.request.setRequestHeader("RequestVerificationToken", cfg.antiforgeryToken);
        }
    };

    window.urtSpreadsheet_onDocumentChanged = function () {
        var cfg = getConfig();
        if (!cfg.canEdit) {
            return;
        }
        window.__urtSpreadsheetDirty = true;
        notifyParent({ type: "urt-spreadsheet-dirty", dirty: true });

        var status = document.getElementById("urt-spreadsheet-status");
        if (!status) {
            return;
        }
        status.textContent = cfg.statusUnsaved || "Unsaved changes";
        status.classList.remove("urt-spreadsheet-status--saved");
        status.classList.add("urt-spreadsheet-status--unsaved");
    };

    function notifyParent(payload) {
        try {
            if (window.parent && window.parent !== window) {
                window.parent.postMessage(payload, window.location.origin);
            }
        } catch (_) { }
    }

    function setSavedStatus() {
        var cfg = getConfig();
        window.__urtSpreadsheetDirty = false;
        notifyParent({ type: "urt-spreadsheet-dirty", dirty: false });

        var status = document.getElementById("urt-spreadsheet-status");
        if (!status) {
            return;
        }
        status.textContent = cfg.statusSaved || "Saved";
        status.classList.remove("urt-spreadsheet-status--unsaved");
        status.classList.add("urt-spreadsheet-status--saved");
    }

    function getSpreadsheetInstance() {
        return window.__urtSpreadsheetInstance;
    }

    function resizeSpreadsheet() {
        var host = document.querySelector(".urt-spreadsheet-host");
        if (!host || host.clientHeight < 80) {
            return;
        }

        var spreadsheet = getSpreadsheetInstance();
        if (!spreadsheet) {
            window.dispatchEvent(new Event("resize"));
            return;
        }

        if (typeof spreadsheet.AdjustControl === "function") {
            spreadsheet.AdjustControl();
        } else if (typeof spreadsheet.adjustControl === "function") {
            spreadsheet.adjustControl();
        } else if (typeof spreadsheet.updateDimensions === "function") {
            spreadsheet.updateDimensions();
        } else if (typeof spreadsheet.repaint === "function") {
            spreadsheet.repaint();
        }

        window.dispatchEvent(new Event("resize"));
    }

    function scheduleSpreadsheetResize() {
        [0, 80, 200, 500, 1000, 1800].forEach(function (delay) {
            setTimeout(resizeSpreadsheet, delay);
        });
    }

    async function saveToTemplate() {
        var cfg = getConfig();
        var spreadsheet = getSpreadsheetInstance();
        if (!spreadsheet || !cfg.saveUrl) {
            return;
        }

        var state = spreadsheet.getSpreadsheetState();
        var body = new URLSearchParams();
        body.append("templateId", cfg.templateId);
        body.append("spreadsheetState", JSON.stringify(state));

        var response = await fetch(cfg.saveUrl, {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded",
                "RequestVerificationToken": cfg.antiforgeryToken || ""
            },
            body: body.toString()
        });

        var payload = null;
        try {
            payload = await response.json();
        } catch (_) {
            payload = { success: false, message: cfg.saveFailed };
        }

        if (payload && payload.success) {
            setSavedStatus();
            notifyParent({ type: "urt-spreadsheet-saved", message: payload.message || cfg.saveSuccess });
            return;
        }

        var message = (payload && payload.message) || cfg.saveFailed || "Save failed.";
        notifyParent({ type: "urt-spreadsheet-error", message: message });
        window.alert(message);
    }

    function reloadFromDatabase() {
        var cfg = getConfig();
        if (!cfg.reloadUrl) {
            return;
        }

        if (window.__urtSpreadsheetDirty) {
            var confirmed = window.confirm(cfg.reloadConfirm || "Reload and discard unsaved changes?");
            if (!confirmed) {
                return;
            }
        }

        window.location.replace(cfg.reloadUrl);
    }

    function wireToolbar() {
        var cfg = getConfig();
        var saveButton = document.getElementById("urt-spreadsheet-save");
        if (saveButton) {
            saveButton.addEventListener("click", function () {
                saveToTemplate();
            });
        }

        var reloadButton = document.getElementById("urt-spreadsheet-reload");
        if (reloadButton && cfg.reloadUrl) {
            reloadButton.addEventListener("click", reloadFromDatabase);
        }

        window.addEventListener("beforeunload", function (event) {
            if (window.__urtSpreadsheetDirty) {
                event.preventDefault();
                event.returnValue = "";
            }
        });
    }

    window.addEventListener("message", function (event) {
        if (event.origin !== window.location.origin || !event.data) {
            return;
        }

        var type = event.data.type;
        if (type === "urt-spreadsheet-save-request") {
            saveToTemplate();
        } else if (type === "urt-spreadsheet-reload-request") {
            reloadFromDatabase();
        } else if (type === "urt-spreadsheet-resize") {
            scheduleSpreadsheetResize();
        }
    });

    window.addEventListener("resize", function () {
        resizeSpreadsheet();
    });

    document.addEventListener("DOMContentLoaded", function () {
        wireToolbar();
        scheduleSpreadsheetResize();
    });
})();
