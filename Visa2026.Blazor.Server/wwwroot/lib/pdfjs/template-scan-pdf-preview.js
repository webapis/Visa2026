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
        .replace(/[\u00AD\u200B\u2060\uFEFF]/g, "")
        .replace(/[\u2010-\u2015\u2212]/g, "-")
        .replace(/[\u2215\uFF0F]/g, "/")
        .replace(/[ýÿ]/gi, "y")
        .replace(/ä/gi, "a")
        .replace(/ö/gi, "o")
        .replace(/ü/gi, "u")
        .replace(/ň/gi, "n")
        .replace(/ş/gi, "s")
        .replace(/ç/gi, "c")
        .replace(/ž/gi, "z")
        .replace(/\s+/g, "")
        .toLowerCase();
}

function isAlnum(ch) {
    return ch >= "0" && ch <= "9" || ch >= "a" && ch <= "z";
}

function isDigit(ch) {
    return ch >= "0" && ch <= "9";
}

/**
 * fold() strips spaces, so "sanawdaky 15 (on bäş)" becomes "sanawdaky15(onbas)".
 * Treat letter↔digit edges as boundaries so count digits still match, and reject
 * "20" inside "2026" via the end check.
 */
function isBoundary(folded, index, length) {
    const len = Math.max(length || 0, 1);
    if (index > 0 && isAlnum(folded[index - 1])) {
        const prev = folded[index - 1];
        const cur = folded[index];
        if (!(isDigit(prev) !== isDigit(cur))) {
            return false;
        }
    }

    const end = index + len;
    if (end < folded.length && isAlnum(folded[end])) {
        const last = folded[end - 1];
        const next = folded[end];
        if (!(isDigit(last) !== isDigit(next))) {
            return false;
        }
    }

    return true;
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

function looksLikeOrderedListMarker(folded, index, length) {
    const after = folded[index + length];
    if (after !== "." && after !== ")") {
        return false;
    }
    const next = folded[index + length + 1];
    return !!next && next >= "a" && next <= "z";
}

function foldNeedles(label) {
    const primary = fold(label);
    const needles = [];
    const add = function (value) {
        if (value && needles.indexOf(value) < 0) {
            needles.push(value);
        }
    };
    add(primary);
    add(primary.replace(/^-+/, ""));
    const withoutNo = primary.replace(/[№°]/g, "").replace(/^no\.?/, "");
    add(withoutNo);
    add(withoutNo.replace(/^-+/, ""));
    return needles;
}

function findAllLabelSpans(entries, label) {
    const needles = foldNeedles(label);
    if (!needles.length || !entries.length) {
        return [];
    }

    let folded = "";
    const map = [];
    for (let i = 0; i < entries.length; i++) {
        const chunk = fold(entries[i].item.str);
        for (let c = 0; c < chunk.length; c++) {
            folded += chunk[c];
            map.push(i);
        }
    }

    const hits = [];
    const seen = {};
    for (let n = 0; n < needles.length; n++) {
        const needle = needles[n];
        const requireBoundary = needle.length <= 3 || /^\d+$/.test(needle);
        const skipList = /^\d+$/.test(needle);
        let searchFrom = 0;
        while (searchFrom < folded.length) {
            const at = folded.indexOf(needle, searchFrom);
            if (at < 0) {
                break;
            }
            if (!requireBoundary || isBoundary(folded, at, needle.length)) {
                if (!(skipList && looksLikeOrderedListMarker(folded, at, needle.length))) {
                    const from = map[at];
                    const to = map[at + needle.length - 1];
                    const key = from + ":" + to;
                    if (from != null && to != null && !seen[key]) {
                        seen[key] = true;
                        hits.push({ from: from, to: to });
                    }
                }
            }
            searchFrom = at + Math.max(needle.length, 1);
        }
    }

    return hits;
}

function hitOverlapsUsed(hit, used) {
    for (let i = hit.from; i <= hit.to; i++) {
        if (used.has(i)) {
            return true;
        }
    }
    return false;
}

function markUsed(hit, used) {
    for (let i = hit.from; i <= hit.to; i++) {
        used.add(i);
    }
}

function hitRect(hit, entries) {
    const pageDiv = entries[hit.from].pageDiv;
    const viewport = entries[hit.from].viewport;
    const samePage = [];
    for (let i = hit.from; i <= hit.to; i++) {
        if (entries[i].pageDiv === pageDiv) {
            samePage.push(entries[i]);
        }
    }
    const box = unionRects(samePage.map(function (entry) {
        return itemRect(entry.item, viewport);
    }));
    box.pageDiv = pageDiv;
    box.viewport = viewport;
    return box;
}

function pickHit(hits, used, expected, entries, band) {
    const free = hits.filter(function (hit) {
        return !hitOverlapsUsed(hit, used);
    });
    if (!free.length) {
        return null;
    }

    let candidates = free;
    if (band && band.pred && band.succ) {
        const minY = band.pred.top + band.pred.height * 0.35;
        const maxY = band.succ.top + band.succ.height * 0.25;
        const inBand = [];
        for (let i = 0; i < free.length; i++) {
            const rect = hitRect(free[i], entries);
            if (rect.pageDiv !== band.pred.pageDiv && rect.pageDiv !== band.succ.pageDiv) {
                continue;
            }
            const cy = rect.top + rect.height / 2;
            if (cy >= minY && cy <= maxY) {
                inBand.push(free[i]);
            }
        }
        if (inBand.length) {
            candidates = inBand;
        }
    }

    if (!expected) {
        return candidates[0];
    }

    let best = null;
    let bestD = Infinity;
    for (let i = 0; i < candidates.length; i++) {
        const rect = hitRect(candidates[i], entries);
        const cx = rect.left + rect.width / 2;
        const cy = rect.top + rect.height / 2;
        const ex = expected.left + expected.width / 2;
        const ey = expected.top + expected.height / 2;
        let d = Math.hypot(cx - ex, cy - ey);
        if (rect.pageDiv !== expected.pageDiv) {
            d += 10000;
        }
        if (d < bestD) {
            bestD = d;
            best = candidates[i];
        }
    }

    return best;
}

function markOrderValue(mark) {
    const n = parseFloat(mark && mark.order);
    return Number.isFinite(n) ? n : 0;
}

function placedBand(placed, order) {
    let pred = null;
    let succ = null;
    for (let i = 0; i < placed.length; i++) {
        const item = placed[i];
        if (item.order < order && (!pred || item.order > pred.order)) {
            pred = item;
        }
        if (item.order > order && (!succ || item.order < succ.order)) {
            succ = item;
        }
    }
    return { pred: pred, succ: succ };
}

function rememberPlaced(placed, mark, box) {
    if (!box) {
        return;
    }
    placed.push({
        order: markOrderValue(mark),
        top: box.top,
        height: box.height,
        left: box.left,
        width: box.width,
        pageDiv: box.pageDiv,
        label: fold(mark.label)
    });
}

function placeShortDuplicateLabels(entries, marks, used, placed, dotnetRef) {
    const groups = {};
    for (let i = 0; i < marks.length; i++) {
        const mark = marks[i];
        if (mark.kind === "excel") {
            continue;
        }
        const key = fold(mark.label).replace(/^-+/, "");
        if (!key || (key.length > 3 && !/^\d+$/.test(key)) || /^\d+$/.test(key)) {
            continue;
        }
        if (!groups[key]) {
            groups[key] = [];
        }
        groups[key].push(mark);
    }

    const done = {};
    Object.keys(groups).forEach(function (key) {
        const group = groups[key];
        if (group.length < 2) {
            return;
        }

        const hits = shortTokenHits(entries, group[0].label).filter(function (hit) {
            return !hitOverlapsUsed(hit, used);
        });
        hits.sort(function (a, b) {
            const ra = hitRect(a, entries);
            const rb = hitRect(b, entries);
            return ra.top + ra.height / 2 - (rb.top + rb.height / 2);
        });
        group.sort(function (a, b) {
            return markOrderValue(a) - markOrderValue(b);
        });

        const pairs = pairShortHits(group, hits);
        for (let i = 0; i < pairs.length; i++) {
            const mark = pairs[i].mark;
            const box = hitRect(pairs[i].hit, entries);
            markUsed(pairs[i].hit, used);
            appendMark(box.pageDiv, mark, box, dotnetRef);
            rememberPlaced(placed, mark, box);
            done[mark.fieldId] = true;
        }
    });
    return done;
}

function shortTokenHits(entries, label) {
    const spans = findAllLabelSpans(entries, label);
    const needle = fold(label).replace(/^-+/, "");
    if (!needle) {
        return spans;
    }
    const seen = {};
    for (let i = 0; i < spans.length; i++) {
        seen[spans[i].from + ":" + spans[i].to] = true;
    }
    for (let i = 0; i < entries.length; i++) {
        const exact = fold(entries[i].item.str).replace(/^-+/, "");
        if (exact !== needle) {
            continue;
        }
        if (/^\d+$/.test(needle) && looksLikeOrderedListItem(entries[i].item.str, needle)) {
            continue;
        }
        const key = i + ":" + i;
        if (seen[key]) {
            continue;
        }
        seen[key] = true;
        spans.push({ from: i, to: i });
    }
    return spans;
}

function looksLikeOrderedListItem(str, needle) {
    const raw = String(str || "").trim();
    return raw === needle + "."
        || raw === needle + ")"
        || raw === needle + ".)"
        || new RegExp("^" + needle.replace(/[.*+?^${}()|[\]\\]/g, "\\$&") + "[\\.)]\\s").test(raw);
}

function pairShortHits(group, hits) {
    if (!hits.length) {
        return [];
    }
    if (hits.length >= group.length) {
        return group.map(function (mark, i) {
            return { mark: mark, hit: hits[i] };
        });
    }
    const pairs = [{ mark: group[0], hit: hits[0] }];
    if (group.length > 1 && hits.length > 1) {
        pairs.push({ mark: group[group.length - 1], hit: hits[hits.length - 1] });
    }
    const usedHits = {};
    usedHits[0] = true;
    if (hits.length > 1) {
        usedHits[hits.length - 1] = true;
    }
    let hitIndex = 1;
    for (let i = 1; i < group.length - 1 && hitIndex < hits.length - 1; i++) {
        if (usedHits[hitIndex]) {
            break;
        }
        usedHits[hitIndex] = true;
        pairs.push({ mark: group[i], hit: hits[hitIndex] });
        hitIndex++;
    }
    return pairs;
}

function isShortDuplicateLabel(mark) {
    const key = fold(mark.label).replace(/^-+/, "");
    return !!key && (key.length <= 3 || /^\d+$/.test(key));
}

function isNumericShortLabel(mark) {
    const key = fold(mark && mark.label).replace(/^-+/, "");
    return /^\d+$/.test(key);
}

function duplicateShortKeys(marks) {
    const counts = {};
    for (let i = 0; i < marks.length; i++) {
        const mark = marks[i];
        if (mark.kind === "excel" || !isShortDuplicateLabel(mark) || isNumericShortLabel(mark)) {
            continue;
        }
        const key = fold(mark.label).replace(/^-+/, "");
        counts[key] = (counts[key] || 0) + 1;
    }
    const keys = {};
    Object.keys(counts).forEach(function (key) {
        if (counts[key] >= 2) {
            keys[key] = true;
        }
    });
    return keys;
}

function leftoverShortBox(mark, placed) {
    const order = markOrderValue(mark);
    const key = fold(mark.label);
    const sibling = placed.filter(function (item) {
        return item.label === key;
    })[0];
    let pred = null;
    let succ = null;
    for (let i = 0; i < placed.length; i++) {
        const item = placed[i];
        if (item.order < order && (!pred || item.order > pred.order)) {
            pred = item;
        }
        if (item.order > order && (!succ || item.order < succ.order)) {
            succ = item;
        }
    }
    if (key === "tur" && order >= 3 && order < 5) {
        const birth = placed.filter(function (item) {
            return item.order >= 3 && item.order < 4;
        });
        const passport = placed.filter(function (item) {
            return item.order >= 5 && item.order < 6;
        });
        if (birth.length && passport.length) {
            pred = birth.reduce(function (a, b) {
                return a.top + a.height > b.top + b.height ? a : b;
            });
            succ = passport.reduce(function (a, b) {
                return a.top < b.top ? a : b;
            });
        }
    }
    if (!pred || !succ || pred.pageDiv !== succ.pageDiv) {
        return null;
    }
    const first = pred.top <= succ.top ? pred : succ;
    const second = pred.top <= succ.top ? succ : pred;
    const top = first.top + first.height;
    const bottom = second.top;
    const left = sibling ? sibling.left : first.left;
    const width = sibling ? sibling.width : 36;
    if (bottom - top < 8) {
        return null;
    }
    return {
        pageDiv: first.pageDiv,
        left: left,
        top: top + 2,
        width: width,
        height: Math.max(bottom - top - 4, 12)
    };
}

function visualNeighbors(band) {
    if (!band || !band.pred || !band.succ) {
        return band;
    }
    if (band.pred.top <= band.succ.top) {
        return { pred: band.pred, succ: band.succ };
    }
    return { pred: band.succ, succ: band.pred };
}

function boxBetweenNeighbors(expected, band) {
    if (!band || !band.pred || !band.succ) {
        return expected;
    }
    if (band.pred.pageDiv !== band.succ.pageDiv) {
        return expected;
    }
    const top = band.pred.top + band.pred.height;
    const bottom = band.succ.top;
    if (bottom - top < 10) {
        return expected;
    }
    const left = expected ? expected.left : band.pred.left;
    const width = expected ? expected.width : Math.max(band.pred.height * 2, 28);
    return {
        pageDiv: band.pred.pageDiv,
        left: left,
        top: top + 2,
        width: width,
        height: Math.max(bottom - top - 4, 12)
    };
}

function parentFieldId(fieldId) {
    const id = String(fieldId || "");
    const colon = id.lastIndexOf(":");
    if (colon <= 0 || colon + 1 >= id.length) {
        return id;
    }
    return /^\d+$/.test(id.slice(colon + 1)) ? id.slice(0, colon) : id;
}

function compoundPartIndex(fieldId) {
    const id = String(fieldId || "");
    const parent = parentFieldId(id);
    if (parent === id) {
        return 0;
    }
    const part = parseInt(id.slice(parent.length + 1), 10);
    return Number.isFinite(part) ? part : 0;
}

function parseOrderKey(item) {
    const raw = (item && item.el && item.el.dataset.order) || "";
    const n = parseFloat(raw);
    return Number.isFinite(n) ? n : 0;
}

function readingLineSlop(items) {
    if (!items || !items.length) {
        return 16;
    }
    const heights = items.map(function (item) {
        return parseFloat(item.el && item.el.style.height) || 0;
    }).filter(function (h) {
        return h > 4;
    }).sort(function (a, b) {
        return a - b;
    });
    const mid = heights.length ? heights[Math.floor(heights.length / 2)] : 16;
    return Math.max(16, mid * 0.6);
}

function applyReadingOrder(container, dotnetRef, lockDocumentOrder) {
    if (!container) {
        return;
    }

    const items = [];
    const pages = container.querySelectorAll(".tas-pdf-page");
    for (let p = 0; p < pages.length; p++) {
        const marks = pages[p].querySelectorAll(".tas-pdf-mark");
        for (let i = 0; i < marks.length; i++) {
            const el = marks[i];
            items.push({
                el: el,
                page: p,
                top: parseFloat(el.style.top) || 0,
                left: parseFloat(el.style.left) || 0,
                fieldId: el.dataset.fieldId || "",
                partIndex: compoundPartIndex(el.dataset.fieldId || "")
            });
        }
    }

    const siblings = {};
    for (let i = 0; i < items.length; i++) {
        if (items[i].partIndex <= 0) {
            continue;
        }
        const parent = parentFieldId(items[i].fieldId);
        if (!siblings[parent]) {
            siblings[parent] = [];
        }
        siblings[parent].push(items[i]);
    }
    Object.keys(siblings).forEach(function (parent) {
        siblings[parent].sort(function (a, b) {
            return a.partIndex - b.partIndex;
        });
    });

    const lineSlop = readingLineSlop(items);
    const groups = [];
    const seen = {};
    for (let i = 0; i < items.length; i++) {
        const item = items[i];
        if (seen[item.fieldId]) {
            continue;
        }
        if (item.partIndex > 0) {
            const parent = parentFieldId(item.fieldId);
            const members = siblings[parent] || [item];
            for (let m = 0; m < members.length; m++) {
                seen[members[m].fieldId] = true;
            }
            groups.push({
                page: members[0].page,
                top: members[0].top,
                left: members[0].left,
                members: members
            });
            continue;
        }
        seen[item.fieldId] = true;
        groups.push({
            page: item.page,
            top: item.top,
            left: item.left,
            members: [item]
        });
    }

    groups.sort(function (a, b) {
        if (lockDocumentOrder) {
            const order = parseOrderKey(a.members[0]) - parseOrderKey(b.members[0]);
            if (order !== 0) {
                return order;
            }
        }
        if (a.page !== b.page) {
            return a.page - b.page;
        }
        if (Math.abs(a.top - b.top) > lineSlop) {
            return a.top - b.top;
        }
        return a.left - b.left;
    });

    const ids = [];
    let order = 0;
    for (let g = 0; g < groups.length; g++) {
        const members = groups[g].members;
        order += 1;
        for (let m = 0; m < members.length; m++) {
            const item = members[m];
            const label = item.partIndex > 0
                ? String(order) + "." + String(item.partIndex)
                : String(order);
            const badge = item.el.querySelector(".tas-mark__n");
            if (badge) {
                badge.textContent = label;
            }
            item.el.title = label + " " + (item.el.title || "").replace(/^[\d.]+\s*/, "");
            ids.push(item.fieldId);
        }
    }

    if (dotnetRef && ids.length) {
        dotnetRef.invokeMethodAsync("OnVisualOrder", ids);
    }
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
    button.dataset.order = mark.order || "";
    button.dataset.kind = mark.kind || "";
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

function excelFrames(entries, aspect, pages, wordPage) {
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
            && !wordPage
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

function excelExpectedRect(mark, frames) {
    if (!frames.length) {
        return null;
    }

    const totalH = frames.reduce(function (sum, item) {
        return sum + item.frame.height;
    }, 0);
    if (totalH <= 0) {
        return null;
    }

    const y = (mark.t / 100) * totalH;
    let acc = 0;
    let target = frames[0];
    let localY = y;
    for (let i = 0; i < frames.length; i++) {
        const nextAcc = acc + frames[i].frame.height;
        if (y < nextAcc || i === frames.length - 1) {
            target = frames[i];
            localY = Math.max(0, y - acc);
            break;
        }
        acc = nextAcc;
    }

    const width = target.frame.width;
    return {
        pageDiv: target.pageDiv,
        left: target.frame.left + (mark.l / 100) * width,
        top: target.frame.top + localY,
        width: Math.max((mark.w / 100) * width, 16),
        height: Math.max((mark.h / 100) * totalH, 14)
    };
}

function placeExcelMark(mark, frames, dotnetRef) {
    const box = excelExpectedRect(mark, frames);
    if (!box) {
        return false;
    }
    appendMark(box.pageDiv, mark, box, dotnetRef);
    return true;
}

function placeTextHit(mark, hit, entries, used, dotnetRef) {
    const box = hitRect(hit, entries);
    markUsed(hit, used);
    appendMark(box.pageDiv, mark, box, dotnetRef);
}

function tableHeavy(marks) {
    if (!marks || !marks.length) {
        return false;
    }
    let n = 0;
    for (let i = 0; i < marks.length; i++) {
        if (marks[i] && marks[i].table) {
            n += 1;
        }
    }
    return n >= Math.max(2, marks.length * 0.4);
}

function placeMarks(entries, marks, dotnetRef, pages) {
    const excelAspect = marks.reduce(function (value, mark) {
        return typeof mark.aspect === "number" && mark.aspect > 0 ? mark.aspect : value;
    }, 0);
    const wordPage = marks.some(function (item) {
        return item && item.kind === "word";
    });
    const frames = excelAspect > 0 || marks.some(hasExcelBox)
        ? excelFrames(entries, excelAspect, pages, wordPage)
        : [];
    const used = new Set();
    const placed = [];
    const placedIds = placeShortDuplicateLabels(entries, marks, used, placed, dotnetRef);
    const dupShort = duplicateShortKeys(marks);
    const queue = marks.slice().sort(function (a, b) {
        const aExcel = a.kind !== "word" && hasExcelBox(a);
        const bExcel = b.kind !== "word" && hasExcelBox(b);
        if (aExcel !== bExcel) {
            return aExcel ? -1 : 1;
        }
        return fold(b.label).length - fold(a.label).length;
    });

    for (const mark of queue) {
        if (placedIds[mark.fieldId]) {
            continue;
        }
        if (dupShort[fold(mark.label).replace(/^-+/, "")] && mark.kind !== "excel") {
            continue;
        }
        const excelOnly = hasExcelBox(mark) && mark.kind !== "word";
        if (excelOnly && placeExcelMark(mark, frames, dotnetRef)) {
            rememberPlaced(placed, mark, excelExpectedRect(mark, frames));
            placedIds[mark.fieldId] = true;
            continue;
        }

        const expected = hasExcelBox(mark) ? excelExpectedRect(mark, frames) : null;
        const band = placedBand(placed, markOrderValue(mark));
        const hit = pickHit(findAllLabelSpans(entries, mark.label), used, expected, entries, band);
        if (hit) {
            const box = hitRect(hit, entries);
            markUsed(hit, used);
            appendMark(box.pageDiv, mark, box, dotnetRef);
            rememberPlaced(placed, mark, box);
            placedIds[mark.fieldId] = true;
        } else if (expected) {
            appendMark(expected.pageDiv, mark, expected, dotnetRef);
            rememberPlaced(placed, mark, expected);
            placedIds[mark.fieldId] = true;
        } else if (!isShortDuplicateLabel(mark) && mark.table && hasExcelBox(mark)) {
            const neighbors = visualNeighbors(band);
            const fallback = boxBetweenNeighbors(expected, neighbors) || expected;
            if (fallback) {
                appendMark(fallback.pageDiv, mark, fallback, dotnetRef);
                rememberPlaced(placed, mark, fallback);
                placedIds[mark.fieldId] = true;
            }
        }
    }

    const leftovers = queue.filter(function (mark) {
        return !placedIds[mark.fieldId] && isShortDuplicateLabel(mark) && mark.kind !== "excel";
    }).sort(function (a, b) {
        return markOrderValue(a) - markOrderValue(b);
    });
    for (let i = 0; i < leftovers.length; i++) {
        const mark = leftovers[i];
        const box = leftoverShortBox(mark, placed);
        if (!box) {
            continue;
        }
        appendMark(box.pageDiv, mark, box, dotnetRef);
        rememberPlaced(placed, mark, box);
        placedIds[mark.fieldId] = true;
    }

    applyReadingOrder(
        pages && pages[0] ? pages[0].pageDiv.parentElement : null,
        dotnetRef,
        tableHeavy(marks));
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