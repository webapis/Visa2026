(function () {
    "use strict";

    window.visaTemplateStagingLocal = window.visaTemplateStagingLocal || {};

    var EXPORTS_KEY = "visa2026-template-downloads";
    var _dotNetRef = null;

    window.visaTemplateStagingLocal.initDotNetRef = function (dotNetRef) {
        _dotNetRef = dotNetRef;
    };

    window.visaTemplateStagingLocal.disposeDotNetRef = function () {
        _dotNetRef = null;
    };

    function readExports() {
        try {
            var raw = sessionStorage.getItem(EXPORTS_KEY);
            if (!raw) {
                return [];
            }

            var parsed = JSON.parse(raw);
            return Array.isArray(parsed) ? parsed : [];
        } catch (e) {
            return [];
        }
    }

    function writeExports(list) {
        sessionStorage.setItem(EXPORTS_KEY, JSON.stringify(list));
    }

    function registerExport(record) {
        var templateId = String(record.templateId || "").toLowerCase();
        var list = readExports().filter(function (x) {
            return String(x.templateId || "").toLowerCase() !== templateId;
        });
        list.push({
            templateId: String(record.templateId),
            fileName: record.fileName,
            displayName: record.displayName || "",
            exportedAtUtc: record.exportedAtUtc || new Date().toISOString()
        });
        writeExports(list);
    }

    function removeExport(templateId) {
        var id = String(templateId || "").toLowerCase();
        writeExports(readExports().filter(function (x) {
            return String(x.templateId || "").toLowerCase() !== id;
        }));
    }

    function resolveTemplateId(fileName) {
        var lower = String(fileName || "").toLowerCase();
        var match = readExports().find(function (x) {
            return String(x.fileName || "").toLowerCase() === lower;
        });
        return match ? match.templateId : null;
    }

    function base64ToBytes(base64) {
        var binary = atob(base64);
        var bytes = new Uint8Array(binary.length);
        for (var i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        return bytes;
    }

    function mimeForFileName(fileName) {
        if (/\.xlsx$/i.test(fileName || "")) {
            return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        }

        return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    }

    function emptySummary() {
        return {
            importedCount: 0,
            skippedUnchangedCount: 0,
            skippedNotFoundCount: 0,
            failedCount: 0,
            cancelled: false,
            uploads: []
        };
    }

    function summarizeUploadResults(uploads, cancelled) {
        var imported = 0;
        var skippedUnchanged = 0;
        var skippedNotFound = 0;
        var failed = 0;

        for (var i = 0; i < uploads.length; i++) {
            var status = uploads[i].status;
            if (status === "Imported") {
                imported++;
            } else if (status === "SkippedUnchanged") {
                skippedUnchanged++;
            } else if (status === "SkippedNotFound") {
                skippedNotFound++;
            } else if (status === "Failed") {
                failed++;
            }
        }

        return {
            importedCount: imported,
            skippedUnchangedCount: skippedUnchanged,
            skippedNotFoundCount: skippedNotFound,
            failedCount: failed,
            cancelled: cancelled === true,
            uploads: uploads
        };
    }

    window.visaTemplateStagingLocal.downloadTemplate = function (payload) {
        if (!payload || !payload.fileName || !payload.fileBase64) {
            return { success: false, error: "Download payload is incomplete." };
        }

        try {
            var bytes = base64ToBytes(payload.fileBase64);
            var blob = new Blob([bytes], { type: mimeForFileName(payload.fileName) });
            var url = URL.createObjectURL(blob);
            var anchor = document.createElement("a");
            anchor.href = url;
            anchor.download = payload.fileName;
            anchor.style.display = "none";
            document.body.appendChild(anchor);
            anchor.click();
            document.body.removeChild(anchor);
            URL.revokeObjectURL(url);

            registerExport({
                templateId: payload.templateId,
                fileName: payload.fileName,
                displayName: payload.displayName,
                exportedAtUtc: payload.exportedAtUtc
            });

            return { success: true, fileName: payload.fileName };
        } catch (e) {
            return { success: false, error: (e && e.message) || "Could not download template." };
        }
    };

    function pickFilesAsync() {
        return new Promise(function (resolve) {
            var input = document.createElement("input");
            input.type = "file";
            input.accept = ".docx,.xlsx";
            input.multiple = true;
            input.style.display = "none";

            function cleanup() {
                if (input.parentNode) {
                    input.parentNode.removeChild(input);
                }
            }

            input.addEventListener("change", function () {
                var files = input.files ? Array.prototype.slice.call(input.files) : [];
                cleanup();
                resolve(files);
            });

            document.body.appendChild(input);
            input.click();
        });
    }

    async function uploadFile(templateId, file) {
        var formData = new FormData();
        formData.append("file", file, file.name);
        var url = "/api/user-report-templates/"
            + encodeURIComponent(templateId)
            + "/staging/upload";
        var response = await fetch(url, {
            method: "POST",
            body: formData,
            credentials: "same-origin"
        });

        if (!response.ok) {
            var errText = await response.text();
            var errMessage = errText;
            try {
                var errJson = JSON.parse(errText);
                errMessage = errJson.error || errJson.title || errText;
            } catch (parseError) {
                // keep raw text
            }

            return {
                templateId: templateId,
                status: "Failed",
                errorMessage: errMessage || ("Upload failed (" + response.status + ")."),
                fileName: file.name
            };
        }

        var body = await response.json();
        var apiStatus = (body && body.status) ? String(body.status) : "Failed";
        if (apiStatus === "Imported") {
            removeExport(templateId);
        }

        return {
            templateId: templateId,
            status: apiStatus,
            errorMessage: body && body.errorMessage ? body.errorMessage : null,
            displayName: body && body.displayName ? body.displayName : null,
            fileName: file.name
        };
    }

    window.visaTemplateStagingLocal.syncFromFilePicker = async function () {
        var files = await pickFilesAsync();
        if (!files.length) {
            return summarizeUploadResults([], true);
        }

        var uploads = [];
        for (var i = 0; i < files.length; i++) {
            var file = files[i];
            var templateId = resolveTemplateId(file.name);
            if (!templateId) {
                uploads.push({
                    templateId: "00000000-0000-0000-0000-000000000000",
                    status: "Failed",
                    errorMessage: "Could not match file to a downloaded template. Click Edit template first, then choose this file on sync.",
                    fileName: file.name
                });
                continue;
            }

            try {
                uploads.push(await uploadFile(templateId, file));
            } catch (e) {
                uploads.push({
                    templateId: templateId,
                    status: "Failed",
                    errorMessage: (e && e.message) || "Upload request failed.",
                    fileName: file.name
                });
            }
        }

        return summarizeUploadResults(uploads, false);
    };

    // Native onclick entry point — keeps a live user-activation token for the file picker.
    window.visaTemplateStagingLocal.syncFromFilePickerDirect = async function () {
        if (!_dotNetRef) {
            return;
        }

        try {
            await _dotNetRef.invokeMethodAsync("OnSyncFromFilePickerStarted");
            var result = await window.visaTemplateStagingLocal.syncFromFilePicker();
            await _dotNetRef.invokeMethodAsync("OnSyncFromFilePickerResult", JSON.stringify(result));
        } catch (e) {
            // Component disposed or SignalR disconnected.
        }
    };
})();
