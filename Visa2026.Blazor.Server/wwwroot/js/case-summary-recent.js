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

    window.visaCaseSummaryRecent.clearPrefix = function (prefix) {
        try {
            if (!prefix) {
                return 0;
            }
            var p = String(prefix);
            var removed = 0;
            var keys = [];
            for (var i = 0; i < localStorage.length; i++) {
                var k = localStorage.key(i);
                if (k && k.indexOf(p) === 0) {
                    keys.push(k);
                }
            }
            for (var j = 0; j < keys.length; j++) {
                localStorage.removeItem(keys[j]);
                removed++;
            }
            return removed;
        } catch (e) {
            return 0;
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