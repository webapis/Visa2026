window.visaPersonRoster = (function () {
    var observer = null;
    var timer = 0;
    var dotNet = null;
    var canvas = null;
    var resizeArmed = false;
    var resizeStartX = 0;
    var resizeColumnIndex = -1;

    function rosterGrid() {
        var nodes = document.querySelectorAll(".visa-person-roster");
        var best = null;
        var bestWidth = 0;
        for (var i = 0; i < nodes.length; i++) {
            var rect = nodes[i].getBoundingClientRect();
            if (rect.width < 200 || rect.height < 40)
                continue;
            if (rect.width > bestWidth) {
                best = nodes[i];
                bestWidth = rect.width;
            }
        }
        return best;
    }

    function headerRow(grid) {
        var rows = grid.querySelectorAll("thead tr");
        var best = null;
        var bestCount = 0;
        for (var i = 0; i < rows.length; i++) {
            var count = rows[i].querySelectorAll("th").length;
            if (count > bestCount) {
                best = rows[i];
                bestCount = count;
            }
        }
        return best || grid.querySelector(".dxbl-grid-header-row");
    }

    function bodyRows(grid) {
        return grid.querySelectorAll("tbody tr");
    }

    function textWidth(el) {
        if (!el)
            return 0;
        var style = window.getComputedStyle(el);
        var font = style.font;
        if (!font || font === "")
            font = (style.fontWeight || "400") + " " + (style.fontSize || "13px") + " " + (style.fontFamily || "sans-serif");
        if (!canvas)
            canvas = document.createElement("canvas");
        var ctx = canvas.getContext("2d");
        if (!ctx)
            return el.scrollWidth || 0;
        ctx.font = font;
        var text = (el.textContent || "").replace(/\s+/g, " ").trim();
        if (!text)
            return 0;
        return Math.ceil(ctx.measureText(text).width);
    }

    function cellPreferred(cell) {
        if (!cell)
            return 0;
        var lines = cell.querySelectorAll(".person-doc-number, .person-doc-line");
        if (lines.length) {
            var maxLine = 0;
            for (var i = 0; i < lines.length; i++)
                maxLine = Math.max(maxLine, textWidth(lines[i]));
            return maxLine;
        }
        return textWidth(cell);
    }

    function paneWidth(grid) {
        var viewer = grid.querySelector(".dxbl-scroll-viewer");
        var width = viewer ? viewer.clientWidth : 0;
        if (width < 240)
            width = grid.clientWidth;
        return Math.max(0, Math.floor(width));
    }

    function rowCells(row) {
        var cells = [];
        if (!row)
            return cells;
        var children = row.children;
        for (var i = 0; i < children.length; i++) {
            var cell = children[i];
            if (cell.hasAttribute
                && (cell.hasAttribute("dxbl-left-virtual-spacer-element")
                    || cell.hasAttribute("dxbl-right-virtual-spacer-element")))
                continue;
            cells.push(cell);
        }
        return cells;
    }

    function measure(dataColumnCount) {
        var grid = rosterGrid();
        if (!grid)
            return { available: 0, viewport: 0, preferred: [] };

        var viewport = paneWidth(grid);
        var header = headerRow(grid);
        if (!header)
            return { available: Math.max(0, viewport - 12), viewport: viewport, preferred: [] };

        var heads = rowCells(header);
        var start = 0;
        var reserved = 0;
        var expected = typeof dataColumnCount === "number" ? dataColumnCount : 0;
        if (expected > 0 && heads.length > expected) {
            start = heads.length - expected;
            for (var h = 0; h < start; h++)
                reserved += Math.ceil(heads[h].getBoundingClientRect().width);
        }

        var rows = bodyRows(grid);
        var preferred = [];
        for (var c = start; c < heads.length; c++) {
            var width = 0;
            var seen = 0;
            for (var r = 0; r < rows.length && seen < 40; r++) {
                var cells = rowCells(rows[r]);
                var cell = cells[c];
                if (!cell)
                    continue;
                seen++;
                width = Math.max(width, cellPreferred(cell));
            }
            // Headers wrap. A missing body must not size the column from the caption.
            if (seen === 0)
                width = 48;
            preferred.push(width);
        }

        return {
            available: Math.max(0, viewport - reserved - 12),
            viewport: viewport,
            preferred: preferred
        };
    }

    function headerPixelWidths(grid) {
        var header = headerRow(grid);
        if (!header)
            return [];
        var cells = rowCells(header);
        var widths = [];
        for (var i = 0; i < cells.length; i++)
            widths.push(Math.max(1, Math.round(cells[i].getBoundingClientRect().width)));
        return widths;
    }

    function edgeColumnIndex(header, clientX) {
        var cells = rowCells(header);
        var best = -1;
        var bestDist = 1e9;
        for (var i = 0; i < cells.length; i++) {
            var dist = Math.abs(cells[i].getBoundingClientRect().right - clientX);
            if (dist < bestDist) {
                bestDist = dist;
                best = i;
            }
        }
        return bestDist <= 14 ? best : -1;
    }

    function armColumnResize(event) {
        resizeArmed = false;
        resizeColumnIndex = -1;
        var header = event.target && event.target.closest
            ? event.target.closest(".visa-person-roster thead, .visa-person-roster .dxbl-grid-header-row")
            : null;
        if (!header)
            return;
        var index = edgeColumnIndex(header, event.clientX);
        if (index < 0)
            return;
        resizeArmed = true;
        resizeColumnIndex = index;
        resizeStartX = event.clientX;
        if (dotNet)
            dotNet.invokeMethodAsync("OnColumnResizeStarting");
    }

    function commitColumnResize(event) {
        if (!resizeArmed)
            return;
        var index = resizeColumnIndex;
        resizeArmed = false;
        resizeColumnIndex = -1;
        if (!dotNet)
            return;
        var moved = Math.abs(event.clientX - resizeStartX) >= 4;
        window.setTimeout(function () {
            if (!dotNet)
                return;
            if (!moved || index < 0) {
                dotNet.invokeMethodAsync("OnColumnResizeCancelled");
                return;
            }
            var grid = rosterGrid();
            var header = grid ? headerRow(grid) : null;
            var cells = header ? rowCells(header) : [];
            if (index >= cells.length) {
                dotNet.invokeMethodAsync("OnColumnResizeCancelled");
                return;
            }
            var pixels = Math.max(1, Math.round(cells[index].getBoundingClientRect().width));
            dotNet.invokeMethodAsync("OnOneColumnResized", index, cells.length, pixels);
        }, 250);
    }

    function observe(ref) {
        window.clearTimeout(timer);
        if (observer)
            observer.disconnect();
        observer = null;
        dotNet = ref;
        var grid = rosterGrid();
        if (!grid || typeof ResizeObserver === "undefined")
            return;
        var scroller = grid.querySelector(".dxbl-scroll-viewer") || grid;
        observer = new ResizeObserver(function () {
            window.clearTimeout(timer);
            timer = window.setTimeout(function () {
                if (dotNet)
                    dotNet.invokeMethodAsync("OnRosterWidthChanged");
            }, 180);
        });
        observer.observe(scroller);
    }

    function release() {
        window.clearTimeout(timer);
        if (observer)
            observer.disconnect();
        observer = null;
        dotNet = null;
    }

    function findNumber(target) {
        return target && target.closest ? target.closest(".person-doc-number") : null;
    }

    function markPending() {
        if (window.visaPreviewDrawer && window.visaPreviewDrawer._hostRef)
            window.visaPreviewDrawer._hostRef.invokeMethodAsync("MarkPersonDocumentCopiesLinkClickPending");
    }

    function blockRow(event) {
        if (!findNumber(event.target))
            return;
        markPending();
        event.preventDefault();
        event.stopPropagation();
        event.stopImmediatePropagation();
    }

    function openNumber(event) {
        var button = findNumber(event.target);
        if (!button)
            return;
        event.preventDefault();
        event.stopPropagation();
        event.stopImmediatePropagation();
        var personId = button.getAttribute("data-person-id") || "";
        var recordKey = button.getAttribute("data-record-key") || "";
        if (!personId || !recordKey)
            return;
        markPending();
        var ref = window.visaPreviewDrawer && window.visaPreviewDrawer._hostRef;
        if (!ref)
            return;
        ref.invokeMethodAsync(
            "OpenPersonDocumentCopyFromJsAsync",
            personId,
            recordKey,
            button.getAttribute("data-display-name") || "",
            button.getAttribute("data-owner-view") || "");
    }

    if (!window.visaPersonRoster || !window.visaPersonRoster._numberCapture) {
        document.addEventListener("pointerdown", blockRow, true);
        document.addEventListener("pointerdown", armColumnResize, true);
        document.addEventListener("pointerup", commitColumnResize, true);
        document.addEventListener("mousedown", blockRow, true);
        document.addEventListener("click", openNumber, true);
        document.addEventListener("keydown", function (event) {
            if (event.key !== "Enter" && event.key !== " ")
                return;
            var button = findNumber(event.target);
            if (!button || document.activeElement !== button)
                return;
            openNumber(event);
        }, true);
    }

    return {
        _numberCapture: true,
        measure: measure,
        observe: observe,
        release: release
    };
})();
