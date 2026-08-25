// Guard the cart-count observer against self-triggered mutation loops.
// The shared layout observes #cart-count and also writes back to #cart-count.
// A native MutationObserver would therefore keep triggering itself. This wrapper
// only suppresses duplicate callbacks for that one element and leaves every
// other MutationObserver on the site completely untouched.
(function () {
    const NativeMutationObserver = window.MutationObserver;
    if (!NativeMutationObserver || window.__pcCartObserverGuardInstalled) return;

    window.__pcCartObserverGuardInstalled = true;

    function PharmaCareMutationObserver(callback) {
        let observedCartCount = false;
        let lastCartValue = null;

        const observer = new NativeMutationObserver(function (mutations, nativeObserver) {
            if (!observedCartCount) {
                callback(mutations, nativeObserver);
                return;
            }

            const target = mutations && mutations.length ? mutations[0].target : null;
            const cartElement = target && target.nodeType === 3 ? target.parentElement : target;
            const currentValue = cartElement ? cartElement.textContent.trim() : '';

            if (currentValue === lastCartValue) {
                return;
            }

            lastCartValue = currentValue;
            callback(mutations, nativeObserver);
        });

        const nativeObserve = observer.observe.bind(observer);
        observer.observe = function (target, options) {
            if (target && target.id === 'cart-count') {
                observedCartCount = true;
                lastCartValue = target.textContent.trim();
            }

            return nativeObserve(target, options);
        };

        return observer;
    }

    PharmaCareMutationObserver.prototype = NativeMutationObserver.prototype;
    window.MutationObserver = PharmaCareMutationObserver;
})();

// Search functionality for PharmaCare
$(document).ready(function () {
    let searchTimeout;
    const searchInput = $('#navbar-search-input');
    const searchBtn = $('#navbar-search-btn');
    const searchResults = $('#navbar-search-results');

    function performSearch(query) {
        if (query.length < 2) {
            hideSearchResults();
            return;
        }

        showSearchResults();
        searchResults.html('<div class="navbar-search-results-wrapper"><div class="navbar-search-results-header">Searching...</div></div>');

        $.ajax({
            url: '/FrontEnd/SearchProducts',
            type: 'GET',
            data: {
                query: query,
                sort: 'relevance'
            },
            success: function (data) {
                displaySearchResults(data, query);
            },
            error: function (xhr, status, error) {
                console.error('Search error:', error);
                searchResults.html('<div class="navbar-search-results-wrapper"><div class="navbar-search-no-results"><p>Search error occurred. Please try again.</p></div></div>');
            }
        });
    }

    function displaySearchResults(products, query) {
        if (!products || products.length === 0) {
            searchResults.html(`
                <div class="navbar-search-results-wrapper">
                    <div class="navbar-search-no-results">
                        <p>No medicines found for "${query}"</p>
                        <p>Try adjusting your search terms</p>
                    </div>
                </div>
            `);
            return;
        }

        let resultsHtml = `
            <div class="navbar-search-results-wrapper">
                <div class="navbar-search-results-header">
                    Found ${products.length} medicine${products.length > 1 ? 's' : ''}
                </div>
                <div class="navbar-search-results-list">
        `;

        const limitedProducts = products.slice(0, 8);

        limitedProducts.forEach(function (product) {
            resultsHtml += `
                <a href="/FrontEnd/ShopSingle/${product.id}" class="navbar-search-result-item">
                    <div class="navbar-search-result-content">
                        <img src="${product.image}" alt="${product.name}" class="navbar-search-result-image" onerror="this.src='/assets/images/product_01.png'">
                        <div class="navbar-search-result-info">
                            <div class="navbar-search-result-name">${product.name}</div>
                            <div class="navbar-search-result-category">${product.category}</div>
                        </div>
                        <div class="navbar-search-result-price">$${product.price.toFixed(2)}</div>
                    </div>
                </a>
            `;
        });

        if (products.length > 8) {
            resultsHtml += `
                <div class="view-all-container">
                    <a href="/FrontEnd/Shop?search=${encodeURIComponent(query)}" class="view-all-link">
                        View all ${products.length} results
                    </a>
                </div>
            `;
        }

        resultsHtml += `
                </div>
            </div>
        `;

        searchResults.html(resultsHtml);
    }

    function showSearchResults() {
        searchResults.show();
    }

    function hideSearchResults() {
        searchResults.hide();
    }

    searchInput.on('input', function () {
        const query = $(this).val().trim();
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function () {
            performSearch(query);
        }, 300);
    });

    searchBtn.on('click', function (e) {
        e.preventDefault();
        const query = searchInput.val().trim();

        if (query.length >= 2) {
            performSearch(query);
        } else if (query.length === 0) {
            hideSearchResults();
        }
    });

    searchInput.on('keypress', function (e) {
        if (e.which === 13) {
            e.preventDefault();
            const query = $(this).val().trim();

            if (query.length >= 2) {
                window.location.href = `/FrontEnd/Shop?search=${encodeURIComponent(query)}`;
            }
        }
    });

    $(document).on('click', function (e) {
        if (!$(e.target).closest('.navbar-search-container').length) {
            hideSearchResults();
        }
    });

    searchResults.on('click', function (e) {
        e.stopPropagation();
    });

    $(document).on('keydown', function (e) {
        if ((e.ctrlKey || e.metaKey) && e.which === 75) {
            e.preventDefault();
            searchInput.focus();
        }
    });
});

function handleShopSearch() {
    const searchForm = $('#search-form');
    if (searchForm.length) {
        searchForm.on('submit', function (e) {
            e.preventDefault();
            const query = $('#search-input').val().trim();
            if (query) {
                window.location.href = `/FrontEnd/Shop?search=${encodeURIComponent(query)}`;
            }
        });
    }
}

// Global Help & Support widget for every storefront page.
(function () {
    if (window.__pcSupportWidgetInstalled) return;
    window.__pcSupportWidgetInstalled = true;

    function injectSupportStyles() {
        if (document.getElementById('pc-support-styles')) return;

        const style = document.createElement('style');
        style.id = 'pc-support-styles';
        style.textContent = `
            .pc-support-launcher{position:fixed;right:24px;bottom:102px;z-index:1090;width:58px;height:58px;border:0;border-radius:50%;background:linear-gradient(135deg,#0d9488,#0f766e);color:#fff;box-shadow:0 16px 34px rgba(13,148,136,.3);display:grid;place-items:center;font-size:20px;cursor:pointer;transition:.2s ease}.pc-support-launcher:hover{transform:translateY(-3px) scale(1.03);box-shadow:0 20px 40px rgba(13,148,136,.36)}.pc-support-launcher:focus{outline:3px solid rgba(45,212,191,.24);outline-offset:3px}.pc-support-launcher .pc-support-dot{position:absolute;right:2px;top:2px;width:12px;height:12px;border-radius:50%;background:#34d399;border:2px solid #fff}
            .pc-support-panel{position:fixed;right:24px;bottom:172px;z-index:1095;width:min(390px,calc(100vw - 28px));background:#fff;color:#18242a;border:1px solid #dce8e6;border-radius:22px;box-shadow:0 26px 70px rgba(17,44,51,.22);overflow:hidden;opacity:0;visibility:hidden;transform:translateY(12px) scale(.98);transition:.22s ease}.pc-support-panel.is-open{opacity:1;visibility:visible;transform:none}.pc-support-head{padding:20px 20px 17px;background:linear-gradient(135deg,#073b3c,#0d766f);color:#fff;display:flex;align-items:flex-start;justify-content:space-between;gap:14px}.pc-support-head h3{margin:0 0 4px;font-size:18px;font-weight:800}.pc-support-head p{margin:0;color:#cce2df;font-size:12px;line-height:1.5}.pc-support-close{width:34px;height:34px;border:1px solid rgba(255,255,255,.18);border-radius:10px;background:rgba(255,255,255,.08);color:#fff;display:grid;place-items:center;cursor:pointer}.pc-support-body{padding:18px}.pc-support-status{display:none;margin-bottom:12px;padding:11px 12px;border-radius:12px;font-size:12px;line-height:1.5}.pc-support-status.success{display:block;background:#e8f8f1;color:#126a49;border:1px solid #bce7d2}.pc-support-status.error{display:block;background:#fff0f0;color:#a03b3b;border:1px solid #f1c6c6}.pc-support-types{display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-bottom:14px}.pc-support-type{border:1px solid #dce6e4;border-radius:11px;background:#f8fbfa;color:#425159;padding:10px 9px;font-size:11px;font-weight:700;cursor:pointer;transition:.18s}.pc-support-type:hover,.pc-support-type.active{border-color:#0d9488;background:#eaf8f6;color:#087b71}.pc-support-label{display:block;margin:0 0 6px;font-size:11px;font-weight:800;color:#52636a;text-transform:uppercase;letter-spacing:.5px}.pc-support-input,.pc-support-message{width:100%;border:1px solid #d8e3e1;border-radius:11px;background:#fbfdfd;color:#172329;outline:none;transition:.18s}.pc-support-input{height:44px;padding:0 12px;margin-bottom:11px}.pc-support-message{min-height:112px;padding:11px 12px;resize:vertical;margin-bottom:12px}.pc-support-input:focus,.pc-support-message:focus{border-color:#0d9488;box-shadow:0 0 0 3px rgba(13,148,136,.08);background:#fff}.pc-support-send{width:100%;height:46px;border:0;border-radius:12px;background:#0d9488;color:#fff;font-size:13px;font-weight:800;display:flex;align-items:center;justify-content:center;gap:8px;cursor:pointer;transition:.18s}.pc-support-send:hover{background:#0f766e}.pc-support-send:disabled{opacity:.6;cursor:not-allowed}.pc-support-meta{margin-top:10px;text-align:center;color:#849196;font-size:10px}.pc-support-page{display:none}
            body.dark-mode .pc-support-panel{background:#20282b;color:#eef4f4;border-color:#344043}.dark-mode .pc-support-body{background:#20282b}.dark-mode .pc-support-label{color:#c3cecf}.dark-mode .pc-support-input,.dark-mode .pc-support-message{background:#171e20;border-color:#384548;color:#f4f7f7}.dark-mode .pc-support-type{background:#252e31;border-color:#394649;color:#dfe7e8}.dark-mode .pc-support-type:hover,.dark-mode .pc-support-type.active{background:#173a36;border-color:#2fb6aa;color:#7ee0d5}
            @media(max-width:575px){.pc-support-launcher{right:16px;bottom:88px;width:52px;height:52px}.pc-support-panel{left:14px;right:14px;bottom:152px;width:auto;max-height:calc(100vh - 180px);overflow:auto}.pc-support-types{grid-template-columns:1fr 1fr}}
        `;
        document.head.appendChild(style);
    }

    function buildSupportWidget() {
        if (document.getElementById('pc-support-launcher')) return;

        const launcher = document.createElement('button');
        launcher.type = 'button';
        launcher.id = 'pc-support-launcher';
        launcher.className = 'pc-support-launcher';
        launcher.setAttribute('aria-label', 'Open help and support');
        launcher.setAttribute('title', 'Need help?');
        launcher.innerHTML = '<i class="fas fa-headset"></i><span class="pc-support-dot"></span>';

        const panel = document.createElement('section');
        panel.id = 'pc-support-panel';
        panel.className = 'pc-support-panel';
        panel.setAttribute('aria-hidden', 'true');
        panel.innerHTML = `
            <div class="pc-support-head">
                <div><h3>How can we help?</h3><p>Send a quick message to the PharmaCare team without leaving this page.</p></div>
                <button type="button" class="pc-support-close" aria-label="Close support"><i class="fas fa-times"></i></button>
            </div>
            <div class="pc-support-body">
                <div id="pc-support-status" class="pc-support-status"></div>
                <div class="pc-support-types" role="group" aria-label="Support topic">
                    <button type="button" class="pc-support-type active" data-type="General support"><i class="far fa-comments"></i> General</button>
                    <button type="button" class="pc-support-type" data-type="Order help"><i class="fas fa-shopping-bag"></i> Order help</button>
                    <button type="button" class="pc-support-type" data-type="Prescription help"><i class="fas fa-prescription-bottle-alt"></i> Prescription</button>
                    <button type="button" class="pc-support-type" data-type="Product question"><i class="fas fa-capsules"></i> Product question</button>
                </div>
                <label class="pc-support-label" for="pc-support-name">Your name</label>
                <input id="pc-support-name" class="pc-support-input" type="text" autocomplete="name" placeholder="Your name">
                <label class="pc-support-label" for="pc-support-email">Email</label>
                <input id="pc-support-email" class="pc-support-input" type="email" autocomplete="email" placeholder="you@example.com">
                <label class="pc-support-label" for="pc-support-message">Message</label>
                <textarea id="pc-support-message" class="pc-support-message" placeholder="Tell us what you need help with..."></textarea>
                <input id="pc-support-page" class="pc-support-page" type="hidden">
                <button id="pc-support-send" type="button" class="pc-support-send"><i class="fas fa-paper-plane"></i> Send message</button>
                <div class="pc-support-meta"><i class="fas fa-shield-alt"></i> Your message goes to the PharmaCare support team.</div>
            </div>
        `;

        document.body.appendChild(launcher);
        document.body.appendChild(panel);

        let supportType = 'General support';
        let contextLoaded = false;

        function setStatus(message, type) {
            const status = document.getElementById('pc-support-status');
            status.className = 'pc-support-status ' + type;
            status.textContent = message;
        }

        function clearStatus() {
            const status = document.getElementById('pc-support-status');
            status.className = 'pc-support-status';
            status.textContent = '';
        }

        async function loadContext() {
            if (contextLoaded) return;
            contextLoaded = true;

            try {
                const response = await fetch('/Support/Context', {
                    method: 'GET',
                    credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                });

                if (!response.ok) return;
                const data = await response.json();
                if (!data || !data.isLoggedIn) return;

                const fullName = [data.firstName, data.lastName].filter(Boolean).join(' ').trim();
                if (fullName) document.getElementById('pc-support-name').value = fullName;
                if (data.email) document.getElementById('pc-support-email').value = data.email;
            } catch (error) {
                console.debug('Support context unavailable.', error);
            }
        }

        function openPanel() {
            panel.classList.add('is-open');
            panel.setAttribute('aria-hidden', 'false');
            document.getElementById('pc-support-page').value = window.location.pathname + window.location.search;
            clearStatus();
            loadContext();
            setTimeout(function () { document.getElementById('pc-support-message').focus(); }, 180);
        }

        function closePanel() {
            panel.classList.remove('is-open');
            panel.setAttribute('aria-hidden', 'true');
        }

        launcher.addEventListener('click', function () {
            panel.classList.contains('is-open') ? closePanel() : openPanel();
        });

        panel.querySelector('.pc-support-close').addEventListener('click', closePanel);

        panel.querySelectorAll('.pc-support-type').forEach(function (button) {
            button.addEventListener('click', function () {
                panel.querySelectorAll('.pc-support-type').forEach(function (item) { item.classList.remove('active'); });
                button.classList.add('active');
                supportType = button.getAttribute('data-type') || 'General support';
            });
        });

        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape' && panel.classList.contains('is-open')) closePanel();
        });

        document.getElementById('pc-support-send').addEventListener('click', async function () {
            clearStatus();
            const sendButton = this;
            const name = document.getElementById('pc-support-name').value.trim();
            const email = document.getElementById('pc-support-email').value.trim();
            const message = document.getElementById('pc-support-message').value.trim();
            const pageUrl = document.getElementById('pc-support-page').value;

            if (!email || !message) {
                setStatus('Please enter your email and message.', 'error');
                return;
            }

            const nameParts = name.split(/\s+/).filter(Boolean);
            const firstName = nameParts.shift() || 'Customer';
            const lastName = nameParts.join(' ');

            sendButton.disabled = true;
            sendButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Sending...';

            try {
                const response = await fetch('/Support/Send', {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: JSON.stringify({
                        firstName: firstName,
                        lastName: lastName,
                        email: email,
                        supportType: supportType,
                        message: message,
                        pageUrl: pageUrl
                    })
                });

                const data = await response.json().catch(function () { return null; });
                if (!response.ok || !data || !data.success) {
                    throw new Error(data && data.message ? data.message : 'Unable to send your message.');
                }

                setStatus(data.message || 'Your message was sent successfully.', 'success');
                document.getElementById('pc-support-message').value = '';
                setTimeout(closePanel, 1800);
            } catch (error) {
                setStatus(error.message || 'Unable to send your message. Please try again.', 'error');
            } finally {
                sendButton.disabled = false;
                sendButton.innerHTML = '<i class="fas fa-paper-plane"></i> Send message';
            }
        });
    }

    function initSupportWidget() {
        injectSupportStyles();
        buildSupportWidget();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initSupportWidget, { once: true });
    } else {
        initSupportWidget();
    }
})();
