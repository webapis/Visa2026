(function () {
    "use strict";

    window.visaTemplateStaging = window.visaTemplateStaging || {};

    function tryProtocolUrl(url) {
        if (!url) {
            return;
        }

        try {
            var anchor = document.createElement("a");
            anchor.href = url;
            anchor.style.display = "none";
            anchor.setAttribute("rel", "noopener noreferrer");
            document.body.appendChild(anchor);
            anchor.click();
            document.body.removeChild(anchor);
        } catch (e) {
            try {
                window.open(url, "_blank", "noopener,noreferrer");
            } catch (e2) {
                // Shell-open on server (dev) or manual open from copied path.
            }
        }
    }

    window.visaTemplateStaging.openExported = function (filePath, officeOpenUrl) {
        if (officeOpenUrl) {
            tryProtocolUrl(officeOpenUrl);
        }

        return filePath || "";
    };

    window.visaTemplateStaging.copyPath = async function (filePath) {
        if (!filePath) {
            return false;
        }

        try {
            if (navigator.clipboard && navigator.clipboard.writeText) {
                await navigator.clipboard.writeText(filePath);
                return true;
            }
        } catch (e) {
            // fall through to legacy copy
        }

        try {
            var textarea = document.createElement("textarea");
            textarea.value = filePath;
            textarea.setAttribute("readonly", "");
            textarea.style.position = "absolute";
            textarea.style.left = "-9999px";
            document.body.appendChild(textarea);
            textarea.select();
            var copied = document.execCommand("copy");
            document.body.removeChild(textarea);
            return copied;
        } catch (e) {
            return false;
        }
    };
})();
