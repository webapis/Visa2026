(function () {
    "use strict";

    window.visaTemplateStagingLocal = window.visaTemplateStagingLocal || {};

    var DB_NAME = "visa2026-template-staging";
    var DB_VERSION = 1;
    var STORE = "settings";
    var DIR_HANDLE_KEY = "directoryHandle";
    var PATH_HINT_KEY = "folderPathHint";
    var DEFAULT_SUBFOLDER = "TemplateEdit";
    var PROTECTED_ROOT_NAMES = [
        "documents", "desktop", "downloads", "music", "pictures", "videos",
        "belge", "documentos", "dokumente", "документы"
    ];

    function isProtectedRootFolder(name) {
        if (!name) {
            return false;
        }

        return PROTECTED_ROOT_NAMES.indexOf(String(name).trim().toLowerCase()) >= 0;
    }

    function supportsLocalFolder() {
        return typeof window.showDirectoryPicker === "function" && typeof window.crypto !== "undefined"
            && typeof window.crypto.subtle !== "undefined";
    }

    function openDb() {
        return new Promise(function (resolve, reject) {
            var request = indexedDB.open(DB_NAME, DB_VERSION);
            request.onupgradeneeded = function () {
                var db = request.result;
                if (!db.objectStoreNames.contains(STORE)) {
                    db.createObjectStore(STORE);
                }
            };
            request.onsuccess = function () { resolve(request.result); };
            request.onerror = function () { reject(request.error); };
        });
    }

    async function idbGet(key) {
        var db = await openDb();
        return new Promise(function (resolve, reject) {
            var tx = db.transaction(STORE, "readonly");
            var store = tx.objectStore(STORE);
            var req = store.get(key);
            req.onsuccess = function () { resolve(req.result); };
            req.onerror = function () { reject(req.error); };
        });
    }

    async function idbSet(key, value) {
        var db = await openDb();
        return new Promise(function (resolve, reject) {
            var tx = db.transaction(STORE, "readwrite");
            var store = tx.objectStore(STORE);
            var req = store.put(value, key);
            req.onsuccess = function () { resolve(); };
            req.onerror = function () { reject(req.error); };
        });
    }

    async function verifyPermission(handle, mode) {
        if (!handle || typeof handle.queryPermission !== "function") {
            return false;
        }

        var opts = { mode: mode || "readwrite" };
        var state = await handle.queryPermission(opts);
        if (state === "granted") {
            return true;
        }

        if (typeof handle.requestPermission === "function") {
            state = await handle.requestPermission(opts);
            return state === "granted";
        }

        return false;
    }

    async function getDirectoryHandle() {
        var handle = await idbGet(DIR_HANDLE_KEY);
        if (handle && await verifyPermission(handle, "readwrite")) {
            return handle;
        }
        return null;
    }

    function normalizeOptions(options) {
        if (!options || typeof options !== "string") {
            return {
                subfolderName: (options && options.subfolderName) || DEFAULT_SUBFOLDER,
                suggestedPathHint: (options && options.suggestedPathHint) || ""
            };
        }

        return { subfolderName: DEFAULT_SUBFOLDER, suggestedPathHint: options };
    }

    async function getStoredPathHint() {
        var hint = await idbGet(PATH_HINT_KEY);
        return (hint || "").trim();
    }

    function base64ToBytes(base64) {
        var binary = atob(base64);
        var bytes = new Uint8Array(binary.length);
        for (var i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        return bytes;
    }

    async function writeFile(handle, fileName, bytes) {
        var fileHandle = await handle.getFileHandle(fileName, { create: true });
        var writable = await fileHandle.createWritable();
        await writable.write(bytes);
        await writable.close();
    }

    async function readFileBytes(handle, fileName) {
        var fileHandle = await handle.getFileHandle(fileName);
        var file = await fileHandle.getFile();
        var buffer = await file.arrayBuffer();
        return new Uint8Array(buffer);
    }

    async function sha256Hex(bytes) {
        var digest = await window.crypto.subtle.digest("SHA-256", bytes);
        return Array.from(new Uint8Array(digest))
            .map(function (b) { return b.toString(16).padStart(2, "0"); })
            .join("")
            .toUpperCase();
    }

    function bytesToBase64(bytes) {
        var binary = "";
        var chunk = 0x8000;
        for (var i = 0; i < bytes.length; i += chunk) {
            binary += String.fromCharCode.apply(null, bytes.subarray(i, i + chunk));
        }
        return btoa(binary);
    }

    function buildMeta(payload) {
        return {
            templateId: payload.templateId,
            templateName: payload.displayName,
            outputFormat: payload.outputFormat,
            documentFileName: payload.fileName,
            exportedAtUtc: payload.exportedAtUtc,
            exportedByUserName: payload.exportedByUserName || "",
            sourceContentHashSha256: payload.sourceHash,
            lastImportedAtUtc: null,
            lastImportedContentHashSha256: payload.lastImportedContentHashSha256 || null
        };
    }

    function metaFileName(documentFileName) {
        return documentFileName + ".meta.json";
    }

    async function readMeta(handle, documentFileName) {
        try {
            var bytes = await readFileBytes(handle, metaFileName(documentFileName));
            var text = new TextDecoder().decode(bytes);
            return JSON.parse(text);
        } catch (e) {
            return null;
        }
    }

    function toFileUri(windowsPath) {
        var normalized = windowsPath.replace(/\\/g, "/");
        if (/^[a-zA-Z]:\//.test(normalized)) {
            return "file:///" + normalized;
        }

        return "file://" + normalized;
    }

    function buildLocalOfficeUrl(folderPathHint, fileName, outputFormat) {
        if (!folderPathHint || !fileName) {
            return "";
        }

        var trimmed = folderPathHint.trim().replace(/[\\/]+$/, "");
        var fullPath = trimmed + "\\" + fileName;
        var protocol = (outputFormat || "").toLowerCase() === "excel" ? "ms-excel" : "ms-word";
        // Full schema: ofe|u|file:///C:/... (abbreviated ms-word:C:\... is invalid — Office parses "C" as the command).
        return protocol + ":ofe|u|" + toFileUri(fullPath);
    }

    function tryOpenOfficeUrl(url) {
        if (!url) {
            return false;
        }

        try {
            var anchor = document.createElement("a");
            anchor.href = url;
            anchor.style.display = "none";
            anchor.setAttribute("rel", "noopener noreferrer");
            document.body.appendChild(anchor);
            anchor.click();
            document.body.removeChild(anchor);
            return true;
        } catch (e) {
            return false;
        }
    }

    window.visaTemplateStagingLocal.isSupported = function () {
        return supportsLocalFolder();
    };

    window.visaTemplateStagingLocal.isSecureContext = function () {
        return window.isSecureContext === true;
    };

    window.visaTemplateStagingLocal.hasFolder = async function () {
        if (!supportsLocalFolder()) {
            return false;
        }

        var handle = await getDirectoryHandle();
        return !!handle;
    };

    window.visaTemplateStagingLocal.getFolderName = async function () {
        var handle = await getDirectoryHandle();
        return handle ? handle.name : "";
    };

    window.visaTemplateStagingLocal.chooseFolder = async function (options) {
        if (!supportsLocalFolder()) {
            return { success: false, error: "File System Access API is not available in this browser." };
        }

        if (!window.isSecureContext) {
            return {
                success: false,
                error: "Local template folder requires HTTPS (or localhost). Open Visa2026 with https://."
            };
        }

        var opts = normalizeOptions(options);

        try {
            var handle = await window.showDirectoryPicker({
                id: "visa2026-template-edit",
                mode: "readwrite"
            });

            if (isProtectedRootFolder(handle.name)) {
                return {
                    success: false,
                    needsSubfolder: true,
                    error: "Select the Visa2026 TemplateEdit folder under AppData\\Local, not a system folder."
                };
            }

            if (!await verifyPermission(handle, "readwrite")) {
                return { success: false, error: "Write permission was not granted for the template folder." };
            }

            await idbSet(DIR_HANDLE_KEY, handle);

            return {
                success: true,
                folderName: handle.name
            };
        } catch (e) {
            if (e && e.name === "AbortError") {
                return { success: false, error: "Folder selection was cancelled." };
            }

            var message = (e && e.message) || "Could not choose template folder.";
            if (/system files/i.test(message)) {
                return {
                    success: false,
                    needsSubfolder: true,
                    error: message
                };
            }

            return { success: false, error: message };
        }
    };

    window.visaTemplateStagingLocal.setFolderPathHint = async function (pathHint) {
        if (!pathHint) {
            return false;
        }

        await idbSet(PATH_HINT_KEY, pathHint.trim());
        return true;
    };

    window.visaTemplateStagingLocal.exportDocument = async function (payload) {
        if (!payload || !payload.fileName || !payload.fileBase64) {
            return { success: false, error: "Export payload is incomplete." };
        }

        if (!window.isSecureContext) {
            return {
                success: false,
                error: "Local template folder requires HTTPS (or localhost)."
            };
        }

        var handle = await getDirectoryHandle();
        if (!handle) {
            return {
                success: false,
                needsFolder: true,
                error: "Choose template folder first (button below), then click Edit template."
            };
        }

        try {
            var pathHint = await getStoredPathHint();
            var bytes = base64ToBytes(payload.fileBase64);
            await writeFile(handle, payload.fileName, bytes);
            var meta = buildMeta(payload);
            var metaJson = JSON.stringify(meta, null, 2);
            await writeFile(handle, metaFileName(payload.fileName), new TextEncoder().encode(metaJson));

            var officeUrl = buildLocalOfficeUrl(pathHint, payload.fileName, payload.outputFormat);
            var opened = tryOpenOfficeUrl(officeUrl);
            var fullPath = pathHint
                ? pathHint.trim().replace(/[\\/]+$/, "") + "\\" + payload.fileName
                : "";

            return {
                success: true,
                folderName: handle.name,
                fileName: payload.fileName,
                fullPath: fullPath,
                opened: opened,
                officeUrl: officeUrl,
                needsPathHint: !pathHint
            };
        } catch (e) {
            return { success: false, error: (e && e.message) || "Could not write template to local folder." };
        }
    };

    async function loadMetaByTemplateId(handle) {
        var map = {};
        for await (var entry of handle.values()) {
            if (entry.kind !== "file" || !entry.name.endsWith(".meta.json")) {
                continue;
            }

            try {
                var file = await entry.getFile();
                var text = await file.text();
                var meta = JSON.parse(text);
                if (meta && meta.templateId) {
                    map[String(meta.templateId).toLowerCase()] = meta;
                }
            } catch (e) {
                // skip invalid meta
            }
        }

        return map;
    }

    window.visaTemplateStagingLocal.collectChangedUploads = async function (templateIds) {
        var uploads = [];
        if (!templateIds || !templateIds.length) {
            return {
                importedCount: 0,
                skippedUnchangedCount: 0,
                skippedNotFoundCount: 0,
                failedCount: 0,
                uploads: uploads
            };
        }

        var handle = await getDirectoryHandle();
        if (!handle) {
            return {
                importedCount: 0,
                skippedUnchangedCount: 0,
                skippedNotFoundCount: 0,
                failedCount: templateIds.length,
                uploads: templateIds.map(function (id) {
                    return {
                        templateId: id,
                        status: "Failed",
                        errorMessage: "Template folder is not configured."
                    };
                })
            };
        }

        var metaByTemplateId = await loadMetaByTemplateId(handle);
        var targetIds = templateIds && templateIds.length
            ? templateIds
            : Object.keys(metaByTemplateId);

        if (!targetIds.length) {
            return {
                importedCount: 0,
                skippedUnchangedCount: 0,
                skippedNotFoundCount: 0,
                failedCount: 0,
                uploads: uploads
            };
        }

        for (var i = 0; i < targetIds.length; i++) {
            var templateId = String(targetIds[i]);
            var knownMeta = metaByTemplateId[templateId.toLowerCase()];
            if (!knownMeta || !knownMeta.documentFileName) {
                uploads.push({ templateId: templateId, status: "SkippedNotFound" });
                continue;
            }

            try {
                var contentBytes = await readFileBytes(handle, knownMeta.documentFileName);
                var hash = await sha256Hex(contentBytes);
                if (knownMeta.lastImportedContentHashSha256
                    && String(knownMeta.lastImportedContentHashSha256).toUpperCase() === hash) {
                    uploads.push({ templateId: templateId, status: "SkippedUnchanged" });
                    continue;
                }

                uploads.push({
                    templateId: templateId,
                    status: "Pending",
                    fileName: knownMeta.documentFileName,
                    fileBase64: bytesToBase64(contentBytes),
                    contentHash: hash
                });
            } catch (e) {
                uploads.push({
                    templateId: templateId,
                    status: "Failed",
                    errorMessage: (e && e.message) || "Could not read local template file."
                });
            }
        }

        var skippedUnchangedCount = uploads.filter(function (u) { return u.status === "SkippedUnchanged"; }).length;
        var skippedNotFoundCount = uploads.filter(function (u) { return u.status === "SkippedNotFound"; }).length;
        var failedCount = uploads.filter(function (u) { return u.status === "Failed"; }).length;

        return {
            importedCount: 0,
            skippedUnchangedCount: skippedUnchangedCount,
            skippedNotFoundCount: skippedNotFoundCount,
            failedCount: failedCount,
            uploads: uploads
        };
    };

    window.visaTemplateStagingLocal.markImported = async function (documentFileName, contentHash) {
        var handle = await getDirectoryHandle();
        if (!handle || !documentFileName) {
            return false;
        }

        var meta = await readMeta(handle, documentFileName);
        if (!meta) {
            return false;
        }

        meta.lastImportedAtUtc = new Date().toISOString();
        meta.lastImportedContentHashSha256 = contentHash;
        var metaJson = JSON.stringify(meta, null, 2);
        await writeFile(handle, metaFileName(documentFileName), new TextEncoder().encode(metaJson));
        return true;
    };

    window.visaTemplateStagingLocal.copyPath = async function (filePath) {
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
