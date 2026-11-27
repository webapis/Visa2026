/* People-column +N on application lists. Toggle stays in the browser so the
   grid does not treat the click as a row load (that leaves the Loading panel up). */
(function () {
    if (window.visaListPeopleExpand)
        return;

    function moreLabel(event) {
        var node = event.target;
        if (!node || !node.closest)
            return null;
        return node.closest(".lv-people__more");
    }

    function toggle(label) {
        var id = label.getAttribute("for");
        var input = id ? document.getElementById(id) : null;
        if (!input)
            return;
        input.checked = !input.checked;
        label.setAttribute("aria-expanded", input.checked ? "true" : "false");
    }

    document.addEventListener("pointerdown", function (event) {
        var label = moreLabel(event);
        if (!label)
            return;
        event.preventDefault();
        event.stopPropagation();
        toggle(label);
    }, true);

    function swallow(event) {
        if (!moreLabel(event))
            return;
        event.preventDefault();
        event.stopPropagation();
    }

    document.addEventListener("mousedown", swallow, true);
    document.addEventListener("click", swallow, true);

    window.visaListPeopleExpand = true;
})();