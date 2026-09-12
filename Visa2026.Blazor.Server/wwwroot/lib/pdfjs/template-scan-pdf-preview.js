import * as pdfjs from "./pdf.min.mjs";

const pdfjsLib = globalThis.pdfjsLib || pdfjs;
const getDocument = pdfjs.getDocument || pdfjsLib.getDocument;
const GlobalWorkerOptions = pdfjs.GlobalWorkerOptions || pdfjsLib.GlobalWorkerOptions;

if (GlobalWorkerOptions) {
    GlobalWorkerOptions.workerSrc = new URL("./pdf.worker.min.mjs", import.meta.url).href;
}

const hosts = new WeakMap();

function waitWidth(element) {
    return new Promise(function (resolve) {
        var tries = 0;
        function tick() {
            const width = element.clientWidth || 0;
            if (width > 32 || tries >= 12) {
                resolve(width || 480);
                return;
            }
            tries += 1;
            requestAnimationFrame(tick);
        }
        tick();
    });
}

function toUint8(byteArray) {
    const data = byteArray instanceof Uint8Array ? byteArray : new Uint8Array(byteArray);
    return data.slice();
}

function fold(value) {
    return String(value || "")
        .normalize("NFKC")
        .replace(/\s+/g, "")
        .toLowerCase();
}

function itemRect(item, viewport) {
    const m = item.transform || [1, 0, 0, 1, 0, 0];
    const x = m[4] || 0;
    const y = m[5] || 0;
    const fontHeight = Math.hypot(m[2] || 0, m[3] || 0) || (item.height || 12);
    const width = item.width || 8;
    const p1 = viewport.convertToViewportPoint(x, y);
    const p2 = viewport.convertToViewportPoint(x + width, y + fontHeight);
    const left = Math.min(p1[0], p2[0]);
    const top = Math.min(p1[1], p2[1]);
    const right = Math.max(p1[0], p2[0]);
    const bottom = Math.max(p1[1], p2[1]);
    return {
        left,
        top,
        width: Math.max(right - left, 8),
        height: Math.max(bottom - top, 10)
    };
}

function unionRects(rects) {
    let left = Infinity;
    let top = Infinity;
    let right = -Infinity;
    let bottom = -Infinity;
    for (const rect of rects) {
        left = Math.min(left, rect.left);
        top = Math.min(top, rect.top);
        right = Math.max(right, rect.left + rect.width);
        bottom = Math.max(bottom, rect.top + rect.height);
    }
    return {
        left: left - 2,
        top: top - 2,
        width: Math.max(right - left + 4, 12),
        height: Math.max(bottom - top + 4, 12)
    };
}

function findLabelSpan(entries, label, startIndex) {
    const needle = fold(label);
    if (!needle || !entries.length) {
        return null;
    }

    let folded = "";
    const map = [];
    for (let i = startIndex; i < entries.length; i++) {
        const chunk = fold(entries[i].item.str);
        for (let c = 0; c < chunk.length; c++) {
            folded += chunk[c];
            map.push(i);
        }
        const at = folded.indexOf(needle);
        if (at >= 0) {
            const from = map[at];
            const to = map[at + needle.length - 1];
            return { from, to, next: to + 1 };
        }
    }

    return null;
}

function ensureLayer(pageDiv) {
    let layer = pageDiv.querySelector(".tas-pdf-marks");
    if (!layer) {
        layer = document.createElement("div");
        layer.className = "tas-pdf-marks";
        pageDiv.appendChild(layer);
    }
    return layer;
}

function appendMark(pageDiv, mark, box, dotnetRef) {
    const layer = ensureLayer(pageDiv);
    const button = document.createElement("button");
    button.type = "button";
    button.className = mark.isGap ? "tas-pdf-mark tas-pdf-mark--gap" : "tas-pdf-mark";
    button.dataset.fieldId = mark.fieldId || "";
    button.style.left = box.left + "px";
    button.style.top = box.top + "px";
    button.style.width = box.width + "px";
    button.style.height = box.height + "px";
    button.title = (mark.order || "") + " " + (mark.label || "");
    const badge = document.createElement("span");
    badge.className = "tas-mark__n";
    badge.textContent = String(mark.order || "");
    button.appendChild(badge);
    if (dotnetRef) {
        button.addEventListener("click", function (event) {
            event.preventDefault();
            event.stopPropagation();
            dotnetRef.invokeMethodAsync("OnMarkActivate", mark.fieldId);
        });
    }
    layer.appendChild(button);
}

function pageSize(pageDiv, viewport) {
    return {
        width: parseFloat(pageDiv.style.width) || (viewport && viewport.width) || 0,
        height: parseFloat(pageDiv.style.height) || (viewport && viewport.height) || 0
    };
}

function groupPages(entries) {
    const pages = [];
    const seen = [];
    for (let i = 0; i < entries.length; i++) {
        const pageDiv = entries[i].pageDiv;
        if (seen.indexOf(pageDiv) >= 0) {
            continue;
        }
        seen.push(pageDiv);
        const items = entries.filter(function (entry) {
            return entry.pageDiv === pageDiv;
        });
        pages.push({
            pageDiv: pageDiv,
            viewport: entries[i].viewport,
            items: items
        });
    }
    return pages;
}

function contentCluster(items, viewport, pageW, pageH) {
    const rects = [];
    for (let i = 0; i < items.length; i++) {
        const rect = itemRect(items[i].item, viewport);
        if (rect.width < 2 && rect.height < 2) {
            continue;
        }
        if (rect.left < 2 && rect.top < 2 && rect.width < 16 && rect.height < 16) {
            continue;
        }
        rects.push(rect);
    }
    if (rects.length < 2) {
        return null;
    }
    const box = unionRects(rects);
    if (box.width < pageW * 0.18 || box.height < 12) {
        return null;
    }
    if (box.left < 1 && box.top < 1 && box.width < pageW * 0.2) {
        return null;
    }
    return box;
}

function printableFrame(pageW, pageH, aspect) {
    const landscape = pageW > pageH;
    const left = landscape ? pageW * 0.06 : pageW * 0.085;
    const top = landscape ? pageH * 0.09 : pageH * 0.064;
    const width = Math.max(pageW - left * 2, pageW * 0.7);
    const height = Math.min(Math.max(width * Math.max(aspect, 0.08), 20), pageH - top * 1.4);
    return { left: left, top: top, width: width, height: height };
}

function excelFrames(entries, aspect, pages) {
    const source = pages && pages.length ? pages : groupPages(entries);
    if (!source.length) {
        return [];
    }

    const frames = [];
    for (let i = 0; i < source.length; i++) {
        const page = source[i];
        const items = page.items || entries.filter(function (entry) {
            return entry.pageDiv === page.pageDiv;
        });
        const size = pageSize(page.pageDiv, page.viewport);
        const cluster = contentCluster(items, page.viewport, size.width, size.height);
        const fallback = printableFrame(size.width, size.height, aspect || 0.28);
        const useCluster = cluster
            && !(aspect > 0 && aspect < 0.7 && cluster.height > size.height * 0.75);
        frames.push({
            pageDiv: page.pageDiv,
            frame: useCluster ? cluster : fallback
        });
    }
    return frames;
}

function hasExcelBox(mark) {
    return mark
        && typeof mark.l === "number"
        && typeof mark.t === "number"
        && typeof mark.w === "number"
        && typeof mark.h === "number";
}

function placeExcelMark(mark, frames, dotnetRef) {
    if (!frames.length) {
        return false;
    }

    const totalH = frames.reduce(function (sum, item) {
        return sum + item.frame.height;
    }, 0);
    if (totalH <= 0) {
        return false;
    }

    const y = (mark.t / 100) * totalH;
    let acc = 0;
    let target = frames[0];
    let localY = y;
    for (let i = 0; i < frames.length; i++) {
        const next = acc + frames[i].frame.height;
        if (y < next || i === frames.length - 1) {
            target = frames[i];
            localY = Math.max(0, y - acc);
            break;
        }
        acc = next;
    }

    const width = target.frame.width;
    appendMark(target.pageDiv, mark, {
        left: target.frame.left + (mark.l / 100) * width,
        top: target.frame.top + localY,
        width: Math.max((mark.w / 100) * width, 16),
        height: Math.max((mark.h / 100) * totalH, 14)
    }, dotnetRef);
    return true;
}

function placeMarks(entries, marks, dotnetRef, pages) {
    const excelAspect = marks.reduce(function (value, mark) {
        return typeof mark.aspect === "number" && mark.aspect > 0 ? mark.aspect : value;
    }, 0);
    const frames = excelAspect > 0 || marks.some(hasExcelBox)
        ? excelFrames(entries, excelAspect, pages)
        : [];

    let next = 0;
    for (const mark of marks) {
        if (hasExcelBox(mark) && placeExcelMark(mark, frames, dotnetRef)) {
            continue;
        }

        const hit = findLabelSpan(entries, mark.label, next);
        if (!hit) {
            continue;
        }
        next = hit.next;
        const slice = entries.slice(hit.from, hit.to + 1);
        const pageDiv = slice[0].pageDiv;
        const viewport = slice[0].viewport;
        const samePage = slice.filter(function (entry) {
            return entry.pageDiv === pageDiv;
        });
        appendMark(pageDiv, mark, unionRects(samePage.map(function (entry) {
            return itemRect(entry.item, viewport);
        })), dotnetRef);
    }
}

async function destroyHost(container) {
    const prev = hosts.get(container);
    hosts.delete(container);
    if (prev?.loadingTask) {
        try {
            await prev.loadingTask.destroy?.();
        } catch {
        }
    }
    if (container) {
        container.replaceChildren();
    }
}

async function render(container, pdfBytes, marks, dotnetRef) {
    if (!container || typeof getDocument !== "function") {
        throw new Error("pdf.js is not loaded");
    }

    await destroyHost(container);
    const loadingTask = getDocument({ data: toUint8(pdfBytes) });
    hosts.set(container, { loadingTask: loadingTask, entries: [], pages: [] });
    const pdf = await loadingTask.promise;
    const widthPx = Math.max(await waitWidth(container) - 24, 280);
    const list = Array.isArray(marks) ? marks : [];
    const entries = [];
    const pages = [];

    for (let n = 1; n <= pdf.numPages; n++) {
        const page = await pdf.getPage(n);
        const base = page.getViewport({ scale: 1 });
        const scale = Math.max(0.55, widthPx / Math.max(base.width, 1));
        const viewport = page.getViewport({ scale: scale });
        const outputScale = window.devicePixelRatio || 1;

        const pageDiv = document.createElement("div");
        pageDiv.className = "tas-pdf-page";
        pageDiv.style.width = Math.floor(viewport.width) + "px";
        pageDiv.style.height = Math.floor(viewport.height) + "px";
        container.appendChild(pageDiv);

        const canvas = document.createElement("canvas");
        canvas.width = Math.floor(viewport.width * outputScale);
        canvas.height = Math.floor(viewport.height * outputScale);
        canvas.style.width = Math.floor(viewport.width) + "px";
        canvas.style.height = Math.floor(viewport.height) + "px";
        pageDiv.appendChild(canvas);

        const ctx = canvas.getContext("2d");
        const renderParams = { canvasContext: ctx, viewport: viewport };
        if (outputScale !== 1) {
            renderParams.transform = [outputScale, 0, 0, outputScale, 0, 0];
        }
        await page.render(renderParams).promise;

        const text = await page.getTextContent();
        const items = (text.items || []).filter(function (item) {
            return item && typeof item.str === "string" && item.str.length > 0;
        });
        for (let i = 0; i < items.length; i++) {
            entries.push({ item: items[i], pageDiv: pageDiv, viewport: viewport });
        }
        pages.push({ pageDiv: pageDiv, viewport: viewport, items: entries.filter(function (entry) {
            return entry.pageDiv === pageDiv;
        }) });
    }

    hosts.set(container, { loadingTask: loadingTask, entries: entries, pages: pages });
    placeMarks(entries, list, dotnetRef, pages);
}

function updateMarks(container, marks, dotnetRef) {
    const prev = hosts.get(container);
    if (!container || !prev || !prev.entries) {
        return;
    }

    container.querySelectorAll(".tas-pdf-marks").forEach(function (layer) {
        layer.replaceChildren();
    });
    placeMarks(prev.entries, Array.isArray(marks) ? marks : [], dotnetRef, prev.pages);
}

function fieldMatches(fid, id) {
    if (!id || !fid) {
        return false;
    }
    if (fid === id) {
        return true;
    }
    return fid.indexOf(id + ":") === 0;
}

function setActive(container, fieldId, scroll) {
    if (!container) {
        return;
    }
    const id = fieldId || "";
    let first = null;
    container.querySelectorAll(".tas-pdf-mark").forEach(function (el) {
        const on = fieldMatches(el.dataset.fieldId || "", id);
        el.classList.toggle("is-hovered", on);
        if (on && !first) {
            first = el;
        }
    });
    if (!scroll || !first || typeof first.scrollIntoView !== "function") {
        return;
    }
    first.scrollIntoView({ block: "center", behavior: "smooth" });
}

async function clear(container) {
    await destroyHost(container);
}

function exportPagePngs(maxPages, maxWidth) {
    const root = document.querySelector(".tas-office-pdf--pages");
    if (!root) {
        return [];
    }

    const limit = Math.max(1, Math.min(maxPages || 2, 5));
    const maxW = Math.max(320, Math.min(maxWidth || 1280, 1600));
    const canvases = root.querySelectorAll("canvas");
    const out = [];
    for (let i = 0; i < canvases.length && out.length < limit; i++) {
        const src = canvases[i];
        if (!src || !src.width || !src.height) {
            continue;
        }

        let canvas = src;
        if (src.width > maxW) {
            const scale = maxW / src.width;
            canvas = document.createElement("canvas");
            canvas.width = maxW;
            canvas.height = Math.max(1, Math.round(src.height * scale));
            const ctx = canvas.getContext("2d");
            if (!ctx) {
                continue;
            }
            ctx.drawImage(src, 0, 0, canvas.width, canvas.height);
        }

        const dataUrl = canvas.toDataURL("image/jpeg", 0.72);
        const comma = dataUrl.indexOf(",");
        out.push({
            mime: "image/jpeg",
            b64: comma >= 0 ? dataUrl.slice(comma + 1) : dataUrl,
            width: canvas.width,
            height: canvas.height
        });
    }

    return out;
}

const api = { render, clear, setActive, updateMarks, exportPagePngs };
window.visaTemplateScanPdfPreview = api;
export { render, clear, setActive, updateMarks, exportPagePngs };