import * as pdfjs from "./pdf.min.mjs";

const pdfjsLib = globalThis.pdfjsLib || pdfjs;
const getDocument = pdfjs.getDocument || pdfjsLib.getDocument;
const GlobalWorkerOptions = pdfjs.GlobalWorkerOptions || pdfjsLib.GlobalWorkerOptions;
const XfaLayer = pdfjs.XfaLayer || pdfjsLib.XfaLayer;

if (GlobalWorkerOptions) {
    GlobalWorkerOptions.workerSrc = new URL("./pdf.worker.min.mjs", import.meta.url).href;
}

const hosts = new WeakMap();
let probeCtx = null;

const previewLinkService = {
    addLinkAttributes: function (element, url, newWindow) {
        if (!element || !url) {
            return;
        }
        element.href = url;
        if (newWindow) {
            element.target = "_blank";
            element.rel = "noopener noreferrer";
        }
    }
};

function toPdfSource(doc) {
    if (typeof doc === "string" && doc.length > 0) {
        return { url: doc.split("#")[0], enableXfa: true };
    }
    if (doc instanceof Uint8Array) {
        return { data: doc, enableXfa: true };
    }
    if (doc instanceof ArrayBuffer) {
        return { data: new Uint8Array(doc), enableXfa: true };
    }
    throw new Error("Application form preview needs a PDF blob URL");
}

function xfaViewport(viewport) {
    if (viewport && typeof viewport.clone === "function") {
        return viewport.clone({ dontFlip: true });
    }
    return viewport;
}

function cssColorParts(cssColor) {
    if (cssColor == null || cssColor === "") {
        return null;
    }
    if (typeof cssColor !== "string") {
        if (Array.isArray(cssColor) && cssColor.length >= 3) {
            return {
                r: Number(cssColor[0]) > 1 ? Number(cssColor[0]) : Number(cssColor[0]) * 255,
                g: Number(cssColor[1]) > 1 ? Number(cssColor[1]) : Number(cssColor[1]) * 255,
                b: Number(cssColor[2]) > 1 ? Number(cssColor[2]) : Number(cssColor[2]) * 255,
                a: cssColor.length > 3 ? Number(cssColor[3]) : 1
            };
        }
        cssColor = String(cssColor);
    }
    const lower = cssColor.trim().toLowerCase();
    if (!lower || lower === "transparent" || lower === "none" || lower.includes("url(")) {
        return null;
    }
    try {
        probeCtx = probeCtx || document.createElement("canvas").getContext("2d", { willReadFrequently: true });
        probeCtx.fillStyle = "#ffffff";
        probeCtx.fillStyle = cssColor;
        const parsed = probeCtx.fillStyle;
        const hex = /^#([0-9a-f]{6})$/i.exec(parsed);
        if (hex) {
            return {
                r: parseInt(hex[1].slice(0, 2), 16),
                g: parseInt(hex[1].slice(2, 4), 16),
                b: parseInt(hex[1].slice(4, 6), 16),
                a: 1
            };
        }
        const m = parsed.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)(?:,\s*([0-9.]+))?\)/i);
        if (!m) {
            return null;
        }
        return {
            r: Number(m[1]),
            g: Number(m[2]),
            b: Number(m[3]),
            a: m[4] === undefined ? 1 : Number(m[4])
        };
    } catch {
        return null;
    }
}

function luminance(c) {
    return 0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b;
}

function isDarkFill(cssColor) {
    const c = cssColorParts(cssColor);
    return !!(c && c.a > 0.4 && luminance(c) < 80);
}

function isLightFill(cssColor) {
    const c = cssColorParts(cssColor);
    return !!(c && c.a > 0.4 && luminance(c) > 186);
}

function fixXfaStyleObject(style) {
    if (!style || typeof style !== "object") {
        return;
    }
    const bg = style.backgroundColor || style.background;
    if (isDarkFill(bg)) {
        style.backgroundColor = "#ffffff";
        if (style.background && typeof style.background === "string" && style.background.indexOf("url(") < 0) {
            style.background = "#ffffff";
        }
    }
    if (isDarkFill(style.fill)) {
        style.fill = "#ffffff";
    }
    if (isLightFill(style.color)) {
        style.color = "#111111";
    }
}

function walkXfaHtml(node) {
    if (!node || typeof node !== "object") {
        return;
    }
    if (node.attributes && node.attributes.style) {
        fixXfaStyleObject(node.attributes.style);
    }
    if (Array.isArray(node.children)) {
        for (const child of node.children) {
            walkXfaHtml(child);
        }
    }
}

function lightenSvgRects(root) {
    root.querySelectorAll("rect").forEach((el) => {
        const fill = el.style.fill || el.getAttribute("fill");
        const computed = window.getComputedStyle(el).fill;
        if (isDarkFill(fill) || isDarkFill(computed)) {
            el.style.setProperty("fill", "#ffffff", "important");
            el.setAttribute("fill", "#ffffff");
        }
    });
}

function lightenXfaPaper(root) {
    if (!root) {
        return;
    }
    lightenSvgRects(root);
    const nodes = [root, ...root.querySelectorAll("*")];
    for (const el of nodes) {
        if (el.tagName === "IMG" || el.namespaceURI === "http://www.w3.org/2000/svg") {
            continue;
        }
        const cs = window.getComputedStyle(el);
        if (cs.backgroundImage && cs.backgroundImage !== "none") {
            continue;
        }
        const inlineBg = el.style.backgroundColor || el.style.background;
        if (isDarkFill(inlineBg) || isDarkFill(cs.backgroundColor)) {
            el.style.setProperty("background-color", "#ffffff", "important");
        }
        const inlineFg = el.style.color;
        if (isLightFill(inlineFg) || isLightFill(cs.color)) {
            el.style.setProperty("color", "#111111", "important");
        }
    }
}

function renderXfaLayer(div, viewport, xfaHtml, annotationStorage) {
    walkXfaHtml(xfaHtml);
    XfaLayer.render({
        viewport: xfaViewport(viewport),
        div,
        xfaHtml,
        annotationStorage,
        intent: "display",
        linkService: previewLinkService,
    });
    lightenXfaPaper(div);
}

async function destroyHost(container) {
    const prev = hosts.get(container);
    hosts.delete(container);
    if (prev?.loadingTasks) {
        for (const task of prev.loadingTasks) {
            try {
                await task.destroy?.();
            } catch {
            }
        }
    }
    if (container) {
        container.replaceChildren();
    }
}

async function renderOne(container, doc, widthPx) {
    const loadingTask = getDocument(toPdfSource(doc));
    const pdf = await loadingTask.promise;
    const tasks = hosts.get(container)?.loadingTasks || [];
    tasks.push(loadingTask);
    hosts.set(container, { loadingTasks: tasks });

    for (let n = 1; n <= pdf.numPages; n++) {
        const page = await pdf.getPage(n);
        const base = page.getViewport({ scale: 1 });
        const scale = Math.max(0.5, widthPx / Math.max(base.width, 1));
        const viewport = page.getViewport({ scale });

        const pageDiv = document.createElement("div");
        pageDiv.className = "visa-xfa-preview__page";
        pageDiv.style.width = Math.floor(viewport.width) + "px";
        pageDiv.style.height = Math.floor(viewport.height) + "px";
        container.appendChild(pageDiv);

        const xfaHtml = await page.getXfa();
        if (xfaHtml && XfaLayer) {
            const xfaDiv = document.createElement("div");
            xfaDiv.className = "xfaLayer";
            pageDiv.appendChild(xfaDiv);
            renderXfaLayer(xfaDiv, viewport, xfaHtml, pdf.annotationStorage);
            continue;
        }

        const canvas = document.createElement("canvas");
        const outputScale = window.devicePixelRatio || 1;
        canvas.width = Math.floor(viewport.width * outputScale);
        canvas.height = Math.floor(viewport.height * outputScale);
        canvas.style.width = Math.floor(viewport.width) + "px";
        canvas.style.height = Math.floor(viewport.height) + "px";
        pageDiv.appendChild(canvas);
        const ctx = canvas.getContext("2d");
        const renderParams = { canvasContext: ctx, viewport };
        if (outputScale !== 1) {
            renderParams.transform = [outputScale, 0, 0, outputScale, 0, 0];
        }
        await page.render(renderParams).promise;
    }
}

async function renderAll(container, documents) {
    if (!container || typeof getDocument !== "function") {
        throw new Error("pdf.js is not loaded");
    }
    await destroyHost(container);
    hosts.set(container, { loadingTasks: [] });
    const list = Array.isArray(documents) ? documents : [documents];
    const widthPx = Math.max(container.clientWidth || 0, 480) - 16;
    for (const doc of list) {
        if (!doc) continue;
        await renderOne(container, doc, widthPx);
    }
}

async function clear(container) {
    if (!container) return;
    await destroyHost(container);
}

const api = { renderAll, clear };
window.visaXfaPreview = api;
export { renderAll, clear };