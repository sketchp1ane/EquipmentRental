/* ==========================================================================
   EquipmentRental — Global client helpers
   ========================================================================== */
(function () {
    'use strict';

    // -----------------------------------------------------------------
    // Toast helper: window.erToast('success' | 'danger' | 'warning' | 'info', message)
    // -----------------------------------------------------------------
    function ensureToastContainer() {
        var c = document.getElementById('er-toast-container');
        if (!c) {
            c = document.createElement('div');
            c.id = 'er-toast-container';
            c.className = 'er-toast-container';
            document.body.appendChild(c);
        }
        return c;
    }

    var ICONS = {
        success: 'bi-check-circle-fill',
        danger:  'bi-x-octagon-fill',
        warning: 'bi-exclamation-triangle-fill',
        info:    'bi-info-circle-fill'
    };
    var BG = {
        success: 'text-bg-success',
        danger:  'text-bg-danger',
        warning: 'text-bg-warning',
        info:    'text-bg-info'
    };

    window.erToast = function (type, message, delay) {
        type = ICONS[type] ? type : 'info';
        delay = delay || 4000;
        var container = ensureToastContainer();
        var el = document.createElement('div');
        el.className = 'toast align-items-center border-0 ' + BG[type];
        el.setAttribute('role', 'alert');
        el.setAttribute('aria-live', 'assertive');
        el.setAttribute('aria-atomic', 'true');
        el.innerHTML =
            '<div class="d-flex">' +
              '<div class="toast-body d-flex align-items-center gap-2">' +
                '<i class="bi ' + ICONS[type] + '"></i>' +
                '<span></span>' +
              '</div>' +
              '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>' +
            '</div>';
        el.querySelector('.toast-body span').textContent = message;
        container.appendChild(el);
        var toast = bootstrap.Toast.getOrCreateInstance(el, { delay: delay });
        el.addEventListener('hidden.bs.toast', function () { el.remove(); });
        toast.show();
    };

    // -----------------------------------------------------------------
    // data-er-confirm — replaces onclick="return confirm(..)" and bare forms.
    // Usage:
    //   <button data-er-confirm="确定删除？" data-er-confirm-variant="danger" ... >
    //   <form data-er-confirm="确定提交？" ...>
    // -----------------------------------------------------------------
    function handleConfirm(e, el, message) {
        if (el.dataset.erConfirmed === '1') { return true; }
        e.preventDefault();

        var variant = el.dataset.erConfirmVariant || 'danger';
        var title   = el.dataset.erConfirmTitle   || '请确认';
        var okText  = el.dataset.erConfirmOk      || '确定';
        var cancel  = el.dataset.erConfirmCancel  || '取消';

        var modalId = 'er-confirm-modal';
        var modalEl = document.getElementById(modalId);
        if (!modalEl) {
            modalEl = document.createElement('div');
            modalEl.id = modalId;
            modalEl.className = 'modal fade';
            modalEl.tabIndex = -1;
            modalEl.innerHTML =
                '<div class="modal-dialog modal-dialog-centered"><div class="modal-content">' +
                  '<div class="modal-header er-variant-danger">' +
                    '<h5 class="modal-title"></h5>' +
                    '<button type="button" class="btn-close" data-bs-dismiss="modal"></button>' +
                  '</div>' +
                  '<div class="modal-body"></div>' +
                  '<div class="modal-footer">' +
                    '<button type="button" class="btn btn-light" data-bs-dismiss="modal"></button>' +
                    '<button type="button" class="btn btn-danger er-confirm-ok"></button>' +
                  '</div>' +
                '</div></div>';
            document.body.appendChild(modalEl);
        }
        var header = modalEl.querySelector('.modal-header');
        header.className = 'modal-header er-variant-' + variant;
        modalEl.querySelector('.modal-title').textContent = title;
        modalEl.querySelector('.modal-body').textContent = message;
        modalEl.querySelector('[data-bs-dismiss="modal"]').textContent = cancel;
        var okBtn = modalEl.querySelector('.er-confirm-ok');
        okBtn.textContent = okText;
        okBtn.className = 'btn er-confirm-ok btn-' + (variant === 'info' ? 'info' : variant === 'warning' ? 'warning' : 'danger');

        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        okBtn.onclick = function () {
            el.dataset.erConfirmed = '1';
            modal.hide();
            if (el.tagName === 'FORM') { el.submit(); }
            else if (el.tagName === 'A') { window.location.href = el.getAttribute('href'); }
            else { el.click(); }
        };
        modal.show();
        return false;
    }

    document.addEventListener('click', function (e) {
        var el = e.target.closest('[data-er-confirm]');
        if (!el || el.tagName === 'FORM') return;
        handleConfirm(e, el, el.dataset.erConfirm);
    }, true);

    document.addEventListener('submit', function (e) {
        var el = e.target;
        if (!el.matches || !el.matches('form[data-er-confirm]')) return;
        handleConfirm(e, el, el.dataset.erConfirm);
    }, true);

    // -----------------------------------------------------------------
    // Image lightbox — anchors with .er-lightbox open the href in a modal.
    //   <a href="/files/photo.jpg" class="er-lightbox"><img src="..."></a>
    // -----------------------------------------------------------------
    function ensureLightbox() {
        var id = 'er-lightbox-modal';
        var modal = document.getElementById(id);
        if (modal) return modal;
        modal = document.createElement('div');
        modal.id = id;
        modal.className = 'modal fade';
        modal.tabIndex = -1;
        modal.innerHTML =
            '<div class="modal-dialog modal-dialog-centered modal-xl">' +
              '<div class="modal-content bg-transparent border-0">' +
                '<div class="modal-header border-0 pb-0">' +
                  '<button type="button" class="btn-close btn-close-white ms-auto" data-bs-dismiss="modal" aria-label="关闭"></button>' +
                '</div>' +
                '<div class="modal-body text-center p-0">' +
                  '<img class="img-fluid rounded" style="max-height:85vh" alt="预览" />' +
                '</div>' +
              '</div>' +
            '</div>';
        document.body.appendChild(modal);
        return modal;
    }
    document.addEventListener('click', function (e) {
        var a = e.target.closest('a.er-lightbox');
        if (!a) return;
        var href = a.getAttribute('href');
        if (!href) return;
        e.preventDefault();
        var modal = ensureLightbox();
        modal.querySelector('img').src = href;
        bootstrap.Modal.getOrCreateInstance(modal).show();
    });

    // -----------------------------------------------------------------
    // Enable Bootstrap tooltips globally for [data-bs-toggle="tooltip"]
    // -----------------------------------------------------------------
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
            bootstrap.Tooltip.getOrCreateInstance(el);
        });
    });
})();
