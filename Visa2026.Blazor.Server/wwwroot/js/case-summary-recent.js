(function () {
    "use strict";

    window.visaCaseSummaryRecent = window.visaCaseSummaryRecent || {};

    window.visaCaseSummaryRecent.load = function (storageKey) {
        try {
            if (!storageKey) {
                return "[]";
            }
            var raw = localStorage.getItem(String(storageKey));
            return raw && raw.length ? raw : "[]";
        } catch (e) {
            return "[]";
        }
    };

    window.visaCaseSummaryRecent.save = function (storageKey, json) {
        try {
            if (!storageKey) {
                return false;
            }
            if (!json) {
                localStorage.removeItem(String(storageKey));
                return true;
            }
            localStorage.setItem(String(storageKey), String(json));
            return true;
        } catch (e) {
            return false;
        }
    };
})();