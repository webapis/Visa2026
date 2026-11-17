window.visaStatePreviewPrint = function () {
    document.body.classList.add("state-preview-printing");
    window.print();
};

window.addEventListener("afterprint", function () {
    document.body.classList.remove("state-preview-printing");
});
