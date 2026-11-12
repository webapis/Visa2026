(function () {
    "use strict";

    window.visaProgressColorTabs = window.visaProgressColorTabs || {};

    window.visaProgressColorTabs.load = function (storageKey) {
        try {
            if (!storageKey) {
                return "";
            }
            var raw = localStorage.getItem(String(storageKey));
            return raw && raw.length ? String(raw) : "";
        } catch (e) {
            return "";
        }
    };

    window.visaProgressColorTabs.save = function (storageKey, value) {
        try {
            if (!storageKey) {
                return false;
            }
            if (!value) {
                localStorage.removeItem(String(storageKey));
                return true;
            }
            localStorage.setItem(String(storageKey), String(value));
            return true;
        } catch (e) {
            return false;
        }
    };
})();