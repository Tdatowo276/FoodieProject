// Foodie Admin: tiện ích dùng chung cho các trang quản trị.
(function () {
    'use strict';

    const csrfToken = document.querySelector('meta[name="csrf-token"]')?.content;

    /**
     * fetch kèm Anti-forgery token (header X-CSRF-TOKEN) cho các request AJAX.
     * Server kiểm tra token này (AutoValidateAntiforgeryToken), nên trang khác không giả mạo được request.
     */
    function adminFetch(url, options = {}) {
        const headers = new Headers(options.headers || {});
        headers.set('X-Requested-With', 'XMLHttpRequest');
        if (csrfToken) headers.set('X-CSRF-TOKEN', csrfToken);
        return fetch(url, { credentials: 'same-origin', ...options, headers });
    }
    window.adminFetch = adminFetch;

    // Phiên đăng nhập hết hạn => server chuyển hướng về trang đăng nhập; tải lại cả trang thay vì nhét form login vào bảng
    function redirectedToLogin(res) {
        return res.redirected && new URL(res.url).pathname.toLowerCase().startsWith('/admin/account/login');
    }

    function showToast(message, type = 'success') {
        const box = document.createElement('div');
        box.className = `alert alert-${type} alert-dismissible fade show position-fixed bottom-0 end-0 m-3 shadow`;
        box.style.zIndex = 1080;
        box.setAttribute('role', 'alert');
        box.textContent = message;
        const close = document.createElement('button');
        close.type = 'button';
        close.className = 'btn-close';
        close.setAttribute('data-bs-dismiss', 'alert');
        close.setAttribute('aria-label', 'Đóng');
        box.appendChild(close);
        document.body.appendChild(box);
        setTimeout(() => box.remove(), 4000);
    }

    function debounce(fn, ms) {
        let timer;
        return (...args) => { clearTimeout(timer); timer = setTimeout(() => fn(...args), ms); };
    }

    // ------------------------------------------------------------------
    // 1. Lọc + phân trang bằng AJAX: <form data-ajax-filter="#vung-ket-qua">
    //    Server nhận header X-Requested-With và chỉ trả về partial view (bảng + phân trang).
    // ------------------------------------------------------------------
    document.querySelectorAll('form[data-ajax-filter]').forEach(form => {
        const target = document.querySelector(form.dataset.ajaxFilter);
        if (!target) return;
        let controller;

        async function load(url) {
            controller?.abort();                      // bỏ request cũ nếu người dùng gõ tiếp
            controller = new AbortController();
            target.classList.add('opacity-50');
            try {
                const res = await adminFetch(url, { signal: controller.signal });
                if (redirectedToLogin(res)) { location.href = res.url; return; }
                if (!res.ok) throw new Error(`HTTP ${res.status}`);
                target.innerHTML = await res.text();
                history.replaceState(null, '', url);  // giữ URL để F5 / chia sẻ link vẫn đúng bộ lọc
            } catch (err) {
                if (err.name !== 'AbortError') showToast('Không tải được dữ liệu. Vui lòng thử lại.', 'danger');
            } finally {
                target.classList.remove('opacity-50');
            }
        }

        function submit() {
            const params = new URLSearchParams(new FormData(form));
            for (const [key, value] of [...params]) if (!value) params.delete(key);
            params.set('page', '1');
            load(`${form.action}?${params}`);
        }

        form.addEventListener('submit', e => { e.preventDefault(); submit(); });
        form.querySelectorAll('select').forEach(el => el.addEventListener('change', submit));
        form.querySelectorAll('input[type="search"]').forEach(el => el.addEventListener('input', debounce(submit, 400)));
        target.addEventListener('click', e => {
            const link = e.target.closest('a[data-page-link]');
            if (!link || link.closest('.disabled')) return;
            e.preventDefault();
            load(link.href);
        });
    });

    // ------------------------------------------------------------------
    // 2. Xác nhận trước khi gửi form nguy hiểm: <form data-confirm="Bạn chắc chắn?">
    // ------------------------------------------------------------------
    document.addEventListener('submit', e => {
        const message = e.target.dataset?.confirm;
        if (message && !confirm(message)) e.preventDefault();
    });

    // ------------------------------------------------------------------
    // 3. Khóa / mở khóa tài khoản bằng AJAX: <button data-toggle-lock data-url data-locked>
    // ------------------------------------------------------------------
    document.addEventListener('click', async e => {
        const btn = e.target.closest('[data-toggle-lock]');
        if (!btn) return;

        const lock = btn.dataset.locked !== 'true';
        if (lock && !confirm('Khóa tài khoản này? Người dùng sẽ bị đăng xuất ngay và không đặt hàng được.')) return;

        btn.disabled = true;
        try {
            const res = await adminFetch(btn.dataset.url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: new URLSearchParams({ locked: String(lock) })
            });
            if (redirectedToLogin(res)) { location.href = res.url; return; }
            const data = await res.json().catch(() => ({}));
            if (!res.ok) throw new Error(data.message || 'Thao tác thất bại.');

            btn.dataset.locked = String(data.isLocked);
            btn.textContent = data.isLocked ? 'Mở khóa' : 'Khóa';
            btn.className = `btn btn-sm ${data.isLocked ? 'btn-outline-success' : 'btn-outline-danger'}`;
            const badge = document.querySelector(`[data-lock-badge="${btn.dataset.userId}"]`);
            if (badge) {
                badge.textContent = data.isLocked ? 'Đã khóa' : 'Hoạt động';
                badge.className = `badge ${data.isLocked ? 'text-bg-danger' : 'text-bg-success'}`;
            }
            showToast(data.message);
        } catch (err) {
            showToast(err.message, 'danger');
        } finally {
            btn.disabled = false;
        }
    });

    // ------------------------------------------------------------------
    // 4b. Chọn ảnh có sẵn trong thư viện: xem trước ngay (<select data-preview="#img">)
    // ------------------------------------------------------------------
    document.querySelectorAll('select[data-preview]').forEach(select => {
        const preview = document.querySelector(select.dataset.preview);
        if (!preview) return;
        const original = preview.getAttribute('src');
        select.addEventListener('change', () => {
            const src = select.value || original;
            if (src) {
                preview.src = src;
                preview.classList.remove('d-none');
            } else {
                preview.classList.add('d-none');
            }
        });
    });

    // ------------------------------------------------------------------
    // 4. Chọn ảnh: kiểm tra nhanh kích thước + xem trước (server vẫn kiểm tra lại)
    // ------------------------------------------------------------------
    document.querySelectorAll('input[type="file"][data-max-bytes]').forEach(input => {
        input.addEventListener('change', () => {
            const file = input.files[0];
            const max = Number(input.dataset.maxBytes);
            input.setCustomValidity('');
            if (file && file.size > max) {
                input.value = '';
                showToast(`Ảnh vượt quá ${Math.round(max / 1024 / 1024)}MB, vui lòng chọn ảnh nhỏ hơn.`, 'danger');
                return;
            }
            const preview = document.querySelector(input.dataset.preview);
            if (preview && file) {
                preview.src = URL.createObjectURL(file);
                preview.classList.remove('d-none');
            }
        });
    });
})();
