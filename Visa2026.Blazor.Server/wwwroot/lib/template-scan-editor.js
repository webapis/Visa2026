// Selection inside the Review Excel grid. The cell under the pointer is the placeholder.

export function selectedText() {
    const selection = window.getSelection();
    if (!selection || selection.isCollapsed || selection.rangeCount === 0)
        return "";
    return selection.toString();
}

export function cellOfSelection(root) {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0 || !root)
        return "";
    const node = selection.anchorNode;
    const element = node && node.nodeType === 1 ? node : node && node.parentElement;
    if (!element || !root.contains(element))
        return "";
    const cell = element.closest("[data-cell]");
    return cell ? (cell.getAttribute("data-cell") || "") : "";
}
