(function () {
    "use strict";

    window.rdScrollSpy = {
        _observer: null,
        _dotNetRef: null,

        init: function (dotNetRef, containerId) {
            this.dispose();
            this._dotNetRef = dotNetRef;
            var self = this;

            // Wait for DOM to settle after Blazor render
            requestAnimationFrame(function () {
                var container = document.getElementById(containerId);
                if (!container) return;

                var sections = container.querySelectorAll('[data-rd-cat]');
                if (!sections.length) return;

                // Determine the XAF scroll container or fall back to null (viewport)
                var scrollRoot = null;
                var parent = container.parentElement;
                while (parent) {
                    var style = getComputedStyle(parent);
                    if (style.overflow === 'auto' || style.overflow === 'scroll' ||
                        style.overflowY === 'auto' || style.overflowY === 'scroll') {
                        scrollRoot = parent;
                        break;
                    }
                    parent = parent.parentElement;
                }

                self._observer = new IntersectionObserver(function (entries) {
                    // Pick the section nearest the top of the viewport / scroll root
                    var best = null;
                    var bestTop = Infinity;
                    entries.forEach(function (entry) {
                        if (entry.isIntersecting) {
                            var top = entry.boundingClientRect.top;
                            if (top >= 0 && top < bestTop) {
                                bestTop = top;
                                best = entry.target;
                            }
                        }
                    });
                    if (best && self._dotNetRef) {
                        self._dotNetRef.invokeMethodAsync('OnSectionInView', best.getAttribute('data-rd-cat'));
                    }
                }, {
                    root: scrollRoot,
                    rootMargin: '0px 0px -60% 0px',
                    threshold: [0, 0.05, 0.1]
                });

                sections.forEach(function (s) { self._observer.observe(s); });
            });
        },

        scrollToSection: function (categoryKey) {
            var el = document.getElementById('rd-cat-' + categoryKey);
            if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
        },

        scrollToTop: function (containerId) {
            var el = document.getElementById(containerId);
            if (el) { el.scrollTop = 0; return; }
            // Walk up to find the scroll container
            var root = document.getElementById('rd-all-sections');
            if (!root) return;
            var p = root.parentElement;
            while (p) {
                var s = getComputedStyle(p);
                if (s.overflow === 'auto' || s.overflow === 'scroll' ||
                    s.overflowY === 'auto' || s.overflowY === 'scroll') {
                    p.scrollTop = 0;
                    return;
                }
                p = p.parentElement;
            }
            window.scrollTo({ top: 0, behavior: 'smooth' });
        },

        dispose: function () {
            if (this._observer) { this._observer.disconnect(); this._observer = null; }
            this._dotNetRef = null;
        }
    };
})();