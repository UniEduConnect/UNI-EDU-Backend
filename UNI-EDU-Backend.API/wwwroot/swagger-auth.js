(function () {
    const TOKEN_ENDPOINTS = ['/api/login', '/api/refresh-token'];
    const SECURITY_SCHEME = 'Bearer';

    function applyToken(token, source) {
        if (!token) return;
        if (!window.ui || typeof window.ui.preauthorizeApiKey !== 'function') {
            console.warn('[Swagger] ui.preauthorizeApiKey not available yet.');
            return;
        }
        window.ui.preauthorizeApiKey(SECURITY_SCHEME, token);
        try { localStorage.setItem('swagger_bearer_token', token); } catch (e) { /* ignore */ }
        console.log('[Swagger] Bearer token auto-applied from', source);
    }

    function isTokenEndpoint(url) {
        if (!url) return false;
        return TOKEN_ENDPOINTS.some(p => url.indexOf(p) !== -1);
    }

    const originalFetch = window.fetch;
    window.fetch = async function (...args) {
        const response = await originalFetch.apply(this, args);
        try {
            const reqUrl = typeof args[0] === 'string' ? args[0] : (args[0] && args[0].url);
            if (response.ok && isTokenEndpoint(reqUrl)) {
                const cloned = response.clone();
                const body = await cloned.json();
                const token = body && body.data && body.data.accessToken;
                if (token) applyToken(token, reqUrl);
            }
        } catch (e) { /* non-JSON or parse error — ignore */ }
        return response;
    };

    // Re-apply persisted token after page reloads so Authorize stays filled.
    function rehydrate() {
        try {
            const cached = localStorage.getItem('swagger_bearer_token');
            if (cached) applyToken(cached, 'localStorage');
        } catch (e) { /* ignore */ }
    }
    if (document.readyState === 'complete') {
        setTimeout(rehydrate, 300);
    } else {
        window.addEventListener('load', () => setTimeout(rehydrate, 300));
    }
})();
