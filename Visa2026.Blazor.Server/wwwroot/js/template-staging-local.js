(function () {
    "use strict";

    window.visaTemplateStagingLocal = window.visaTemplateStagingLocal || {};

    var EXPORTS_KEY = "visa2026-template-downloads";
    var DB_NAME = "visa2026-template-edit";
    var DB_STORE = "handles";
    var HANDLE_KEY = "directory";
    var PICKER_ID = "visa2026-template-edit";
    var _dotNetRef = null;
    var _busy = false;
    var _memoryHandle = null;

    loadHandle().then(function (handle) {
        if (handle) {
            _memoryHandle = handle;
        }
    }).catch(function () {
        // IndexedDB is unavailable; the next click opens the folder picker.
    });

    window.visaTemplateStagingLocal.initDotNetRef = function (dotNetRef) {
        _dotNetRef = dotNetRef;
    };

    window.visaTemplateStagingLocal.disposeDotNetRef = function () {
        _dotNetRef = null;
    };

    function beginBusy() {
        if (_busy) {
            return false;
        }

        _busy = true;
        return true;
    }

    function endBusy() {
        _busy = false;
    }

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

    function openDb() {
        return new Promise(function (resolve, reject) {
            var request = indexedDB.open(DB_NAME, 1);
            request.onupgradeneeded = function () {
                if (!request.result.objectStoreNames.contains(DB_STORE)) {
                    request.result.createObjectStore(DB_STORE);
                }
            };
            request.onsuccess = function () { resolve(request.result); };
            request.onerror = function () { reject(request.error); };
        });
    }

    async function saveHandle(handle) {
        var db = await openDb();
        await new Promise(function (resolve, reject) {
            var tx = db.transaction(DB_STORE, "readwrite");
            tx.objectStore(DB_STORE).put(handle, HANDLE_KEY);
            tx.oncomplete = function () { resolve(); };
            tx.onerror = function () { reject(tx.error); };
        });
        db.close();
    }

    async function loadHandle() {
        var db = await openDb();
        var handle = await new Promise(function (resolve, reject) {
            var tx = db.transaction(DB_STORE, "readonly");
            var request = tx.objectStore(DB_STORE).get(HANDLE_KEY);
            request.onsuccess = function () { resolve(request.result || null); };
            request.onerror = function () { reject(request.error); };
        });
        db.close();
        return handle;
    }

    function classifyError(error) {
        if (error && error.code === "unsupported") {
            return "unsupported";
        }

        var name = error && error.name ? error.name : "";
        var message = error && error.message ? String(error.message) : "";
        if (name === "AbortError") {
            return "cancelled";
        }

        if (name === "SecurityError" || name === "NotAllowedError" || /not allowed|system file|protected/i.test(message)) {
            return "protectedRoot";
        }

        if (name === "NoModificationAllowedError" || name === "NotReadableError" || /lock/i.test(message)) {
            return "fileLocked";
        }

        return "exportFailed";
    }

    function unsupportedError() {
        var error = new Error("File System Access API is unavailable. Use HTTPS (or localhost) in Edge or Chrome.");
        error.code = "unsupported";
        return error;
    }

    // Must run before any Blazor round-trip so the browser still has the click gesture.
    async function ensureFolderFromUserGesture() {
        if (!window.isSecureContext || typeof window.showDirectoryPicker !== "function") {
            throw unsupportedError();
        }

        var handle = _memoryHandle;
        if (!handle) {
            try {
                handle = await loadHandle();
                _memoryHandle = handle;
            } catch (e) {
                handle = null;
            }
        }

        if (handle && handle.queryPermission) {
            var current = await handle.queryPermission({ mode: "readwrite" });
            if (current === "granted") {
                _memoryHandle = handle;
                return handle;
            }

            if (current === "prompt" && handle.requestPermission) {
                var requested = await handle.requestPermission({ mode: "readwrite" });
                if (requested === "granted") {
                    return handle;
                }
            }
        }

        handle = await window.showDirectoryPicker({
            id: PICKER_ID,
            mode: "readwrite"
        });
        _memoryHandle = handle;
        await saveHandle(handle);
        return handle;
    }

    async function sha256Hex(bytes) {
        var copy = bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength);
        var digest = await crypto.subtle.digest("SHA-256", copy);
        var view = new Uint8Array(digest);
        var hex = "";
        for (var i = 0; i < view.length; i++) {
            hex += view[i].toString(16).padStart(2, "0");
        }
        return hex;
    }

    async function writeBytes(dirHandle, fileName, bytes) {
        var fileHandle = await dirHandle.getFileHandle(fileName, { create: true });
        var writable = await fileHandle.createWritable();
        await writable.write(bytes);
        await writable.close();
    }

    async function readFileBytes(dirHandle, fileName) {
        var fileHandle = await dirHandle.getFileHandle(fileName);
        var file = await fileHandle.getFile();
        return new Uint8Array(await file.arrayBuffer());
    }

    function namesMatch(left, right) {
        return String(left || "").toLowerCase() === String(right || "").toLowerCase();
    }

    function openOfficePath(path, outputFormat) {
        if (!path) {
            return false;
        }

        var excel = String(outputFormat || "").toLowerCase() === "excel";
        var scheme = excel ? "ms-excel:ofe|u|" : "ms-word:ofe|u|";
        var anchor = document.createElement("a");
        anchor.setAttribute("href", scheme + String(path));
        anchor.style.display = "none";
        document.body.appendChild(anchor);
        anchor.click();
        document.body.removeChild(anchor);
        return true;
    }

    function tryOpenOffice(path, outputFormat, folderName, leafName) {
        if (!namesMatch(folderName, leafName)) {
            return false;
        }

        return openOfficePath(path, outputFormat);
    }

    function launchUrl(url) {
        var anchor = document.createElement("a");
        anchor.href = url;
        anchor.style.display = "none";
        document.body.appendChild(anchor);
        anchor.click();
        document.body.removeChild(anchor);
    }

    async function readJson(response) {
        if (!response.ok) {
            return null;
        }

        return await response.json();
    }

    async function writeOnThisPc(templateId) {
        var response = await fetch(
            "/api/user-report-templates/" + encodeURIComponent(templateId) + "/staging/write-local",
            { method: "POST", credentials: "same-origin", headers: { "Accept": "application/json" } });
        var body = await readJson(response);
        if (!body || body.available !== true) {
            return null;
        }

        if ((body.code === "exported" || body.code === "needsSync") && body.opened !== true) {
            body.opened = openOfficePath(body.suggestedLocalFilePath || body.copyPath, body.outputFormat);
        }

        return body;
    }

    async function pollDesktop(token) {
        for (var i = 0; i < 40; i++) {
            await new Promise(function (resolve) { setTimeout(resolve, 500); });
            var response = await fetch(
                "/api/user-report-templates/staging/desktop/" + encodeURIComponent(token) + "/result",
                { credentials: "same-origin", headers: { "Accept": "application/json" } });
            var body = await readJson(response);
            if (body && body.completed && body.result) {
                return body.result;
            }
        }

        return { code: "helperMissing" };
    }

    async function openWithOfficerPc(templateId) {
        var response = await fetch(
            "/api/user-report-templates/" + encodeURIComponent(templateId) + "/staging/desktop-open",
            { method: "POST", credentials: "same-origin", headers: { "Accept": "application/json" } });
        var launch = await readJson(response);
        if (!launch || !launch.launchUrl || !launch.token) {
            return { code: "helperMissing", templateId: templateId };
        }

        launchUrl(launch.launchUrl);
        var result = await pollDesktop(launch.token);
        if (!result.templateId) {
            result.templateId = templateId;
        }

        return result;
    }

    async function readExportError(response) {
        var errText = await response.text();
        try {
            var errJson = JSON.parse(errText);
            return errJson.error || errJson.title || errText;
        } catch (e) {
            return errText || ("Export failed (" + response.status + ").");
        }
    }

    async function writeExportedTemplate(dirHandle, templateId) {
        var url = "/api/user-report-templates/" + encodeURIComponent(templateId) + "/staging/export";
        var response = await fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: { "Accept": "application/json" }
        });
        if (!response.ok) {
            return {
                code: "exportFailed",
                error: await readExportError(response),
                templateId: templateId
            };
        }

        var payload = await response.json();
        if (!payload || !payload.fileBase64 || !payload.documentFileName) {
            return { code: "exportFailed", error: "Export payload is incomplete.", templateId: templateId };
        }

        var fileName = payload.documentFileName;
        var bytes = base64ToBytes(payload.fileBase64);
        var sourceHash = String(payload.sourceContentHashSha256 || "");
        var alreadyCurrent = false;
        try {
            var existing = await readFileBytes(dirHandle, fileName);
            var existingHash = await sha256Hex(existing);
            if (sourceHash && existingHash.toLowerCase() !== sourceHash.toLowerCase()) {
                return {
                    code: "needsSync",
                    displayName: payload.displayName || "",
                    fileName: fileName,
                    folderName: dirHandle.name || "",
                    templateId: templateId
                };
            }

            alreadyCurrent = !!sourceHash && existingHash.toLowerCase() === sourceHash.toLowerCase();
        } catch (readError) {
            if (readError && readError.name !== "NotFoundError") {
                return {
                    code: classifyError(readError),
                    error: readError.message || "",
                    templateId: templateId
                };
            }
        }

        if (!alreadyCurrent) {
            await writeBytes(dirHandle, fileName, bytes);
        }

        var metaName = fileName + ".meta.json";
        var meta = {
            templateId: String(payload.templateId || templateId),
            templateName: payload.displayName || "",
            documentFileName: fileName,
            outputFormat: payload.outputFormat || "",
            exportedAtUtc: new Date().toISOString(),
            sourceContentHashSha256: sourceHash
        };
        await writeBytes(dirHandle, metaName, new TextEncoder().encode(JSON.stringify(meta, null, 2)));

        var opened = tryOpenOffice(
            payload.suggestedLocalFilePath,
            payload.outputFormat,
            dirHandle.name,
            payload.folderLeafName);
        var folderMatches = namesMatch(dirHandle.name, payload.folderLeafName);

        return {
            code: "exported",
            templateId: String(payload.templateId || templateId),
            displayName: payload.displayName || "",
            fileName: fileName,
            folderName: dirHandle.name || "",
            copyPath: folderMatches ? (payload.copyPath || "") : "",
            opened: opened
        };
    }

    async function reportEdit(result) {
        if (!_dotNetRef) {
            return;
        }

        await _dotNetRef.invokeMethodAsync("OnEditTemplateResult", JSON.stringify(result));
    }

    window.visaTemplateStagingLocal.editTemplateDirect = async function (templateId) {
        if (!templateId || !beginBusy()) {
            return;
        }

        try {
            var started = _dotNetRef
                ? _dotNetRef.invokeMethodAsync("OnEditTemplateStarted", String(templateId))
                : null;
            var local = await writeOnThisPc(templateId);
            if (started) {
                try { await started; } catch (startError) { }
            }

            var outcome = local || await openWithOfficerPc(templateId);
            await reportEdit(outcome);
        } catch (e) {
            try {
                await reportEdit({
                    code: classifyError(e),
                    error: (e && e.message) || "",
                    templateId: String(templateId)
                });
            } catch (reportError) {
                // Circuit already gone.
            }
        } finally {
            endBusy();
        }
    };

    window.visaTemplateStagingLocal.chooseFolderDirect = async function () {
        if (!beginBusy()) {
            return;
        }

        try {
            if (!window.isSecureContext || typeof window.showDirectoryPicker !== "function") {
                throw unsupportedError();
            }

            var handle = await window.showDirectoryPicker({
                id: PICKER_ID,
                mode: "readwrite"
            });
            _memoryHandle = handle;
            await saveHandle(handle);
            if (_dotNetRef) {
                await _dotNetRef.invokeMethodAsync("OnTemplateFolderChosen", handle.name || "");
            }
        } catch (e) {
            if (_dotNetRef) {
                try {
                    await _dotNetRef.invokeMethodAsync("OnTemplateFolderChoiceFailed", classifyError(e));
                } catch (reportError) {
                    // Circuit already gone.
                }
            }
        } finally {
            endBusy();
        }
    };

    window.visaTemplateStagingLocal.copyPathDirect = async function (path) {
        var text = String(path || "");
        if (!text) {
            return;
        }

        try {
            await navigator.clipboard.writeText(text);
        } catch (e) {
            var area = document.createElement("textarea");
            area.value = text;
            area.style.position = "fixed";
            area.style.left = "-9999px";
            document.body.appendChild(area);
            area.select();
            document.execCommand("copy");
            document.body.removeChild(area);
        }

        if (_dotNetRef) {
            try {
                await _dotNetRef.invokeMethodAsync("OnTemplatePathCopied", text);
            } catch (reportError) {
                // Circuit already gone.
            }
        }
    };

    window.visaTemplateStagingLocal.listGrantedTemplateIds = async function () {
        try {
            var handle = await loadHandle();
            if (!handle || !handle.queryPermission) {
                return [];
            }

            var permission = await handle.queryPermission({ mode: "readwrite" });
            if (permission !== "granted") {
                return [];
            }

            var ids = [];
            for await (var entry of handle.values()) {
                if (!entry || entry.kind !== "file" || !/\.meta\.json$/i.test(entry.name || "")) {
                    continue;
                }

                try {
                    var file = await entry.getFile();
                    var meta = JSON.parse(await file.text());
                    if (meta && meta.templateId) {
                        ids.push(String(meta.templateId));
                    }
                } catch (metaError) {
                    // Skip a sidecar the browser cannot read.
                }
            }

            return ids;
        } catch (e) {
            return [];
        }
    };

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

    async function syncFolder(dirHandle) {
        var uploads = [];
        for await (var entry of dirHandle.values()) {
            if (!entry || entry.kind !== "file" || !/\.meta\.json$/i.test(entry.name || "")) {
                continue;
            }

            var metaName = entry.name;
            var meta;
            try {
                var metaFile = await entry.getFile();
                meta = JSON.parse(await metaFile.text());
            } catch (metaError) {
                uploads.push({
                    templateId: "00000000-0000-0000-0000-000000000000",
                    status: "Failed",
                    errorMessage: (metaError && metaError.message) || "Could not read template metadata.",
                    fileName: metaName
                });
                continue;
            }

            var templateId = meta && meta.templateId ? String(meta.templateId) : "";
            var fileName = meta && meta.documentFileName ? String(meta.documentFileName) : "";
            if (!templateId || !fileName) {
                continue;
            }

            try {
                var bytes = await readFileBytes(dirHandle, fileName);
                var hash = await sha256Hex(bytes);
                var sourceHash = String(meta.sourceContentHashSha256 || "");
                if (sourceHash && hash.toLowerCase() === sourceHash.toLowerCase()) {
                    uploads.push({
                        templateId: templateId,
                        status: "SkippedUnchanged",
                        errorMessage: null,
                        displayName: meta.templateName || null,
                        fileName: fileName
                    });
                    continue;
                }

                var file = new File([bytes], fileName, { type: mimeForFileName(fileName) });
                var uploaded = await uploadFile(templateId, file);
                uploads.push(uploaded);
                if (uploaded.status === "Imported" || uploaded.status === "SkippedUnchanged") {
                    meta.sourceContentHashSha256 = hash;
                    meta.lastImportedContentHashSha256 = hash;
                    await writeBytes(dirHandle, metaName, new TextEncoder().encode(JSON.stringify(meta, null, 2)));
                }
            } catch (fileError) {
                var locked = fileError && (fileError.name === "NotReadableError" || fileError.name === "NoModificationAllowedError");
                uploads.push({
                    templateId: templateId,
                    status: "Failed",
                    errorMessage: locked
                        ? "The template file is locked."
                        : ((fileError && fileError.message) || "Could not read the template file."),
                    fileName: fileName
                });
            }
        }

        return summarizeUploadResults(uploads, false);
    }

    window.visaTemplateStagingLocal.syncFromFolderDirect = async function () {
        if (!_dotNetRef || !beginBusy()) {
            return;
        }

        try {
            await _dotNetRef.invokeMethodAsync("OnSyncFromFilePickerStarted");
            var localResponse = await fetch(
                "/api/user-report-templates/staging/import-local",
                { method: "POST", credentials: "same-origin", headers: { "Accept": "application/json" } });
            if (!localResponse.ok) {
                var failText = await localResponse.text();
                await _dotNetRef.invokeMethodAsync("OnSyncFolderFailed", failText || "exportFailed");
                return;
            }

            var localImport = await readJson(localResponse);
            if (localImport && localImport.handled === true) {
                await _dotNetRef.invokeMethodAsync("OnSyncFromFilePickerResult", JSON.stringify(localImport));
                return;
            }

            var launchResponse = await fetch(
                "/api/user-report-templates/staging/desktop-sync",
                { method: "POST", credentials: "same-origin", headers: { "Accept": "application/json" } });
            var launch = await readJson(launchResponse);
            if (!launch || !launch.launchUrl || !launch.token) {
                await _dotNetRef.invokeMethodAsync("OnSyncFolderFailed", "helperMissing");
                return;
            }

            launchUrl(launch.launchUrl);
            var result = await pollDesktop(launch.token);
            if (result && result.code === "helperMissing") {
                await _dotNetRef.invokeMethodAsync("OnSyncFolderFailed", "helperMissing");
                return;
            }

            await _dotNetRef.invokeMethodAsync("OnSyncFromFilePickerResult", JSON.stringify(result));
        } catch (e) {
            var code = classifyError(e);
            try {
                if (code === "cancelled") {
                    await _dotNetRef.invokeMethodAsync("OnSyncFromFilePickerResult", JSON.stringify(emptySummaryCancelled()));
                } else {
                    await _dotNetRef.invokeMethodAsync("OnSyncFolderFailed", code);
                }
            } catch (reportError) {
                // Circuit already gone.
            }
        } finally {
            endBusy();
        }
    };

    function emptySummaryCancelled() {
        var summary = emptySummary();
        summary.cancelled = true;
        return summary;
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

    // Kept for callers that still match a downloaded file by name. Folder sync does not use this.
    window.visaTemplateStagingLocal.resolveDownloadedTemplateId = resolveTemplateId;

    window.visaTemplateStagingLocal.downloadPcSetup = async function () {
        try {
            var response = await fetch("/api/user-report-templates/staging/pc-setup", {
                credentials: "same-origin",
                headers: { "Accept": "application/zip" }
            });
            if (!response.ok) {
                return;
            }

            var blob = await response.blob();
            var url = URL.createObjectURL(blob);
            var anchor = document.createElement("a");
            anchor.href = url;
            anchor.download = "Visa2026-TemplateEdit-Setup.zip";
            anchor.style.display = "none";
            document.body.appendChild(anchor);
            anchor.click();
            document.body.removeChild(anchor);
            URL.revokeObjectURL(url);
        } catch (e) {
            // Leave the catalog open. The officer can click the button again.
        }
    };

    // Capture phase keeps the browser click gesture for showDirectoryPicker. Blazor Server
    // would otherwise spend that gesture on a circuit round-trip.
    document.addEventListener("click", function (event) {
        var target = event.target;
        if (!target || !target.closest) {
            return;
        }

        var button = target.closest("[data-visa-edit-template], [data-visa-sync-templates], [data-visa-choose-folder], [data-visa-copy-path], [data-visa-download-pc-setup]");
        if (!button || button.disabled) {
            return;
        }

        if (button.hasAttribute("data-visa-edit-template")) {
            event.preventDefault();
            window.visaTemplateStagingLocal.editTemplateDirect(button.getAttribute("data-visa-edit-template"));
        } else if (button.hasAttribute("data-visa-sync-templates")) {
            event.preventDefault();
            window.visaTemplateStagingLocal.syncFromFolderDirect();
        } else if (button.hasAttribute("data-visa-choose-folder")) {
            event.preventDefault();
            window.visaTemplateStagingLocal.chooseFolderDirect();
        } else if (button.hasAttribute("data-visa-copy-path")) {
            event.preventDefault();
            window.visaTemplateStagingLocal.copyPathDirect(button.getAttribute("data-visa-copy-path"));
        } else if (button.hasAttribute("data-visa-download-pc-setup")) {
            event.preventDefault();
            window.visaTemplateStagingLocal.downloadPcSetup();
        }
    }, true);
})();
