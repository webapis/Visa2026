let hostRef = null;
let panelObserver = null;
let resizeObserver = null;
let visibilityObserver = null;
let observedPanel = null;
let observedIframe = null;

export function attachHostListener(dotNetRef) {
    detachHostListener();
    hostRef = dotNetRef;
    window.addEventListener("message", onMessage);
}

export function attachSpreadsheetPanel(panelElement, iframeElement, iframeSrc) {
    if (!panelElement || !iframeElement) {
        return;
    }

    if (observedPanel === panelElement && observedIframe === iframeElement) {
        if (iframeSrc) {
            iframeElement.dataset.src = iframeSrc;
            iframeElement.setAttribute("data-src", iframeSrc);
        }
        tryLoadSpreadsheetIframe(iframeElement);
        return;
    }

    detachSpreadsheetPanelObservers();

    observedPanel = panelElement;
    observedIframe = iframeElement;

    if (iframeSrc) {
        iframeElement.dataset.src = iframeSrc;
        iframeElement.setAttribute("data-src", iframeSrc);
    }

    panelObserver = new IntersectionObserver((entries) => {
        for (const entry of entries) {
            if (entry.isIntersecting && entry.intersectionRatio > 0) {
                tryLoadSpreadsheetIframe(iframeElement);
            }
        }
    }, { threshold: [0.01, 0.1, 0.25] });

    panelObserver.observe(iframeElement);

    resizeObserver = new ResizeObserver(() => {
        notifySpreadsheetResize(iframeElement);
    });
    resizeObserver.observe(iframeElement);

    visibilityObserver = new MutationObserver(() => {
        const rect = iframeElement.getBoundingClientRect();
        if (rect.width > 0 && rect.height > 0) {
            tryLoadSpreadsheetIframe(iframeElement);
        }
    });

    let node = panelElement.parentElement;
    while (node) {
        visibilityObserver.observe(node, {
            attributes: true,
            attributeFilter: ["style", "class", "hidden", "aria-hidden"],
        });
        node = node.parentElement;
    }

    tryLoadSpreadsheetIframe(iframeElement);
}

export function reloadSpreadsheetIframe(iframeElement, iframeSrc) {
    if (!iframeElement || !iframeSrc) {
        return;
    }

    iframeElement.dataset.src = iframeSrc;
    iframeElement.setAttribute("data-src", iframeSrc);

    const onLoad = () => scheduleSpreadsheetResize(iframeElement);
    iframeElement.addEventListener("load", onLoad, { once: true });

    // Force a full navigation even when only the query string changes.
    iframeElement.src = "about:blank";
    window.setTimeout(() => {
        iframeElement.src = iframeSrc;
    }, 0);
}

export function detachHostListener() {
    window.removeEventListener("message", onMessage);
    hostRef = null;
    detachSpreadsheetPanelObservers();
}

export function postToSpreadsheetIframe(iframeElement, messageType) {
    if (!iframeElement || !iframeElement.contentWindow) {
        return;
    }

    iframeElement.contentWindow.postMessage({ type: messageType }, window.location.origin);
}

function detachSpreadsheetPanelObservers() {
    if (panelObserver) {
        panelObserver.disconnect();
        panelObserver = null;
    }

    if (resizeObserver) {
        resizeObserver.disconnect();
        resizeObserver = null;
    }

    if (visibilityObserver) {
        visibilityObserver.disconnect();
        visibilityObserver = null;
    }

    observedPanel = null;
    observedIframe = null;
}

function onMessage(event) {
    if (!hostRef || !event || !event.data) {
        return;
    }

    if (event.origin !== window.location.origin) {
        return;
    }

    const payload = event.data;
    const type = payload.type;
    if (!type || !type.startsWith("urt-spreadsheet-")) {
        return;
    }

    if (type === "urt-spreadsheet-save-request" || type === "urt-spreadsheet-reload-request") {
        return;
    }

    hostRef.invokeMethodAsync(
        "OnSpreadsheetMessageAsync",
        type,
        payload.message || "",
        !!payload.dirty);
}

function getPendingIframeSrc(iframeElement) {
    return iframeElement?.dataset?.src || iframeElement?.getAttribute("data-src") || "";
}

function tryLoadSpreadsheetIframe(iframeElement) {
    const pendingSrc = getPendingIframeSrc(iframeElement);
    if (!pendingSrc || pendingSrc === "about:blank") {
        return;
    }

    const rect = iframeElement.getBoundingClientRect();
    if (rect.width <= 0 || rect.height <= 0) {
        return;
    }

    const currentSrc = iframeElement.getAttribute("src") || "";
    if (!currentSrc || currentSrc === "about:blank") {
        iframeElement.src = pendingSrc;
        iframeElement.addEventListener("load", () => scheduleSpreadsheetResize(iframeElement), { once: true });
        return;
    }

    scheduleSpreadsheetResize(iframeElement);
}

function notifySpreadsheetResize(iframeElement) {
    postToSpreadsheetIframe(iframeElement, "urt-spreadsheet-resize");
}

function scheduleSpreadsheetResize(iframeElement) {
    [0, 80, 200, 500, 1000].forEach((delay) => {
        setTimeout(() => notifySpreadsheetResize(iframeElement), delay);
    });
}
