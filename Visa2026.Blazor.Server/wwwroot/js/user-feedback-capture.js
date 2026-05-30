window.visaUserFeedback = window.visaUserFeedback || {};
window.visaUserFeedback.pendingScreenshot = null;

window.visaUserFeedback.clearPendingScreenshot = function () {
    window.visaUserFeedback.pendingScreenshot = null;
    var img = document.getElementById("user-feedback-screenshot-preview");
    if (img) {
        img.removeAttribute("src");
        img.style.display = "none";
    }
};

window.visaUserFeedback._dataUrlToBlob = function (dataUrl) {
    if (!dataUrl || dataUrl.indexOf(",") < 0) {
        return null;
    }
    var parts = dataUrl.split(",");
    var mime = parts[0].match(/:(.*?);/);
    var mimeType = mime && mime[1] ? mime[1] : "image/png";
    var binary = atob(parts[1]);
    var len = binary.length;
    var bytes = new Uint8Array(len);
    for (var i = 0; i < len; i++) {
        bytes[i] = binary.charCodeAt(i);
    }
    return new Blob([bytes], { type: mimeType });
};

window.visaUserFeedback._loadHtml2Canvas = function () {
    if (typeof html2canvas === "function") {
        return Promise.resolve();
    }
    return new Promise(function (resolve, reject) {
        var script = document.createElement("script");
        script.src = "https://cdnjs.cloudflare.com/ajax/libs/html2canvas/1.4.1/html2canvas.min.js";
        script.onload = resolve;
        script.onerror = function () { reject(new Error("library")); };
        document.head.appendChild(script);
    });
};

window.visaUserFeedback._removeCaptureChrome = function (doc) {
    var selectors = [
        ".user-feedback-dialog-backdrop",
        ".user-feedback-header-host",
        ".bo-state-header-badge-host",
        "#blazor-error-ui"
    ];
    selectors.forEach(function (sel) {
        doc.querySelectorAll(sel).forEach(function (el) { el.remove(); });
    });
};

window.visaUserFeedback._prepareClone = function (doc) {
    var app = doc.querySelector("app");
    if (app) {
        app.classList.remove("d-none");
        app.style.display = "block";
        app.style.visibility = "visible";
    }
    window.visaUserFeedback._removeCaptureChrome(doc);
};

window.visaUserFeedback._findCaptureTarget = function () {
    var selectors = [
        ".xaf-detail-view",
        ".xaf-list-view",
        ".xaf-navigation-list"
    ];
    for (var i = 0; i < selectors.length; i++) {
        var el = document.querySelector(selectors[i]);
        if (el) {
            var rect = el.getBoundingClientRect();
            if (rect.width >= 100 && rect.height >= 100) {
                return el;
            }
        }
    }
    return null;
};

window.visaUserFeedback._canvasToDataUrl = function (canvas) {
    return new Promise(function (resolve) {
        if (!canvas || canvas.width < 10 || canvas.height < 10) {
            resolve(null);
            return;
        }
        canvas.toBlob(function (blob) {
            if (!blob) {
                resolve(null);
                return;
            }
            var reader = new FileReader();
            reader.onloadend = function () { resolve(reader.result); };
            reader.readAsDataURL(blob);
        }, "image/png", 0.85);
    });
};

window.visaUserFeedback._captureViaHtml2Canvas = async function () {
    await window.visaUserFeedback._loadHtml2Canvas();
    var target = window.visaUserFeedback._findCaptureTarget();
    if (!target) {
        return null;
    }

    var canvas = await Promise.race([
        html2canvas(target, {
            useCORS: true,
            allowTaint: true,
            logging: false,
            backgroundColor: "#ffffff",
            scale: 1,
            width: Math.min(target.scrollWidth, 1400),
            height: Math.min(target.scrollHeight, 900),
            ignoreElements: function (el) {
                return el && (
                    el.id === "user-feedback-header-host" ||
                    (el.classList && (
                        el.classList.contains("user-feedback-dialog-backdrop") ||
                        el.classList.contains("user-feedback-header-host") ||
                        el.classList.contains("bo-state-header-badge-host")
                    ))
                );
            },
            onclone: function (doc) {
                window.visaUserFeedback._prepareClone(doc);
            }
        }),
        new Promise(function (_, reject) {
            setTimeout(function () { reject(new Error("timeout")); }, 6000);
        })
    ]);

    return window.visaUserFeedback._canvasToDataUrl(canvas);
};

window.visaUserFeedback._captureViaDisplayMedia = async function () {
    if (!navigator.mediaDevices || typeof navigator.mediaDevices.getDisplayMedia !== "function") {
        return null;
    }

    var stream = null;
    try {
        stream = await navigator.mediaDevices.getDisplayMedia({
            video: { displaySurface: "browser" },
            preferCurrentTab: true,
            selfBrowserSurface: "include"
        });

        var video = document.createElement("video");
        video.srcObject = stream;
        video.muted = true;
        await video.play();
        await new Promise(function (r) { setTimeout(r, 300); });

        var canvas = document.createElement("canvas");
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        canvas.getContext("2d").drawImage(video, 0, 0);
        return window.visaUserFeedback._canvasToDataUrl(canvas);
    } finally {
        if (stream) {
            stream.getTracks().forEach(function (t) { t.stop(); });
        }
    }
};

window.visaUserFeedback.capturePage = async function (options) {
    options = options || {};
    var preferDisplayMedia = options.preferDisplayMedia !== false;

    if (preferDisplayMedia) {
        try {
            var picked = await window.visaUserFeedback._captureViaDisplayMedia();
            if (picked) {
                return { ok: true, dataUrl: picked, method: "displayMedia" };
            }
        } catch (e) {
            if (e && (e.name === "NotAllowedError" || e.name === "AbortError")) {
                return { ok: false, error: "picker" };
            }
        }
    }

    try {
        var dataUrl = await window.visaUserFeedback._captureViaHtml2Canvas();
        if (dataUrl) {
            return { ok: true, dataUrl: dataUrl, method: "html2canvas" };
        }
    } catch (e) {
        // ignore
    }

    return { ok: false, error: "capture" };
};

window.visaUserFeedback.startPageCapture = async function (dotNetRef) {
    document.body.classList.add("user-feedback-capturing");
    try {
        var result = await window.visaUserFeedback.capturePage({ preferDisplayMedia: true });
        if (!result.ok || !result.dataUrl) {
            await dotNetRef.invokeMethodAsync("OnPageCaptureFinished", false, result.error || "capture");
            return;
        }

        window.visaUserFeedback.pendingScreenshot = {
            dataUrl: result.dataUrl,
            fileName: "page-screenshot.png"
        };

        var img = document.getElementById("user-feedback-screenshot-preview");
        if (img) {
            img.src = result.dataUrl;
            img.style.display = "block";
        }

        await dotNetRef.invokeMethodAsync("OnPageCaptureFinished", true, null);
    } catch (e) {
        try {
            await dotNetRef.invokeMethodAsync("OnPageCaptureFinished", false, "capture");
        } catch (e2) {
            // circuit may already be gone
        }
    } finally {
        document.body.classList.remove("user-feedback-capturing");
    }
};

window.visaUserFeedback.submitForm = async function (payload) {
    var form = new FormData();
    form.append("Type", payload.type || "Bug");
    form.append("Severity", payload.severity || "Medium");
    form.append("Summary", payload.summary || "");
    form.append("Description", payload.description || "");
    form.append("PageUrl", payload.pageUrl || "");
    form.append("ViewId", payload.viewId || "");
    form.append("ContextBoTypeName", payload.contextBoTypeName || "");
    form.append("ContextBoId", payload.contextBoId || "");

    var pending = window.visaUserFeedback.pendingScreenshot;
    if (pending && pending.dataUrl) {
        var blob = window.visaUserFeedback._dataUrlToBlob(pending.dataUrl);
        if (blob) {
            form.append("Screenshot", blob, pending.fileName || "screenshot.png");
        }
    } else if (payload.screenshotBase64) {
        var bytes = Uint8Array.from(atob(payload.screenshotBase64), function (c) { return c.charCodeAt(0); });
        form.append("Screenshot", new Blob([bytes], { type: "image/png" }), payload.screenshotFileName || "screenshot.png");
    }

    if (payload.attachmentBase64) {
        var attachBytes = Uint8Array.from(atob(payload.attachmentBase64), function (c) { return c.charCodeAt(0); });
        form.append("Attachment", new Blob([attachBytes], { type: payload.attachmentContentType || "application/octet-stream" }), payload.attachmentFileName || "attachment.bin");
    }

    var resp = await fetch("/api/UserFeedback/submit", {
        method: "POST",
        body: form,
        credentials: "include"
    });

    var text = await resp.text();
    var data = null;
    try { data = JSON.parse(text); } catch (e) { }
    return { ok: resp.ok, status: resp.status, error: data && data.error ? data.error : null, text: text };
};

window.visaUserFeedback.parseContext = function () {
    var path = window.location.pathname || "";
    var segments = path.split("/").filter(Boolean);
    var viewId = "";
    var contextBoId = "";
    if (segments.length >= 1) {
        viewId = decodeURIComponent(segments[0]);
    }
    if (segments.length >= 2) {
        var candidate = decodeURIComponent(segments[1]);
        if (/^[0-9a-fA-F-]{36}$/.test(candidate)) {
            contextBoId = candidate;
        }
    }
    return {
        pageUrl: window.location.href,
        viewId: viewId,
        contextBoId: contextBoId
    };
};
