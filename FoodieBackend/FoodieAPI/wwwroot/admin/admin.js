// Foodie Admin: tiện ích dùng chung cho các trang quản trị.
(function () {
    'use strict';

    const csrfToken = document.querySelector('meta[name="csrf-token"]')?.content;

    /**
     * fetch kèm Anti-forgery token (header X-CSRF-TOKEN) cho các request AJAX thay đổi dữ liệu.
     * Server kiểm tra token này (AutoValidateAntiforgeryToken), nên trang khác không giả mạo được request.
     */
    window.adminFetch = function (url, options = {}) {
        const headers = new Headers(options.headers || {});
        headers.set('X-Requested-With', 'XMLHttpRequest');
        if (csrfToken) headers.set('X-CSRF-TOKEN', csrfToken);
        return fetch(url, { credentials: 'same-origin', ...options, headers });
    };
})();
