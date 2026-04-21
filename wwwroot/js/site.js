// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(function () {
    const csrfCookieName = "XSRF-TOKEN";
    const csrfHeaderName = "X-CSRF-TOKEN";

    function getCookieValue(name) {
        const cookies = document.cookie ? document.cookie.split('; ') : [];
        for (let i = 0; i < cookies.length; i++) {
            const parts = cookies[i].split('=');
            const key = decodeURIComponent(parts.shift() || '');
            if (key === name) {
                return decodeURIComponent(parts.join('='));
            }
        }
        return null;
    }

    async function ensureCsrfToken() {
        let token = getCookieValue(csrfCookieName);
        if (token) return token;

        try {
            const resp = await fetch('/antiforgery/token', {
                method: 'GET',
                credentials: 'same-origin'
            });

            if (!resp.ok) return null;
            token = getCookieValue(csrfCookieName);
            return token;
        } catch {
            return null;
        }
    }

    function isUnsafeMethod(method) {
        const m = (method || 'GET').toUpperCase();
        return m === 'POST' || m === 'PUT' || m === 'PATCH' || m === 'DELETE';
    }

    const originalFetch = window.fetch;
    window.fetch = async function (input, init) {
        const requestInit = init ? { ...init } : {};
        const method = requestInit.method || 'GET';

        if (isUnsafeMethod(method)) {
            const token = await ensureCsrfToken();
            if (token) {
                const headers = new Headers(requestInit.headers || {});
                if (!headers.has(csrfHeaderName)) {
                    headers.set(csrfHeaderName, token);
                }
                requestInit.headers = headers;
            }
        }

        return originalFetch(input, requestInit);
    };

    if (window.jQuery) {
        window.jQuery.ajaxSetup({
            beforeSend: function (xhr, settings) {
                if (!isUnsafeMethod(settings.type)) return;
                const token = getCookieValue(csrfCookieName);
                if (token) {
                    xhr.setRequestHeader(csrfHeaderName, token);
                }
            }
        });
    }
})();
