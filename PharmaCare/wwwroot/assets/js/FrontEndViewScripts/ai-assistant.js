(function () {
    if (window.__pcAiAssistantInstalled) return;
    window.__pcAiAssistantInstalled = true;

    const history = [];
    let statusChecked = false;
    let aiEnabled = false;
    let sending = false;

    function escapeHtml(value) {
        return String(value || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function formatMessage(value) {
        return escapeHtml(value).replace(/\n/g, '<br>');
    }

    function injectStyles() {
        if (document.getElementById('pc-ai-styles')) return;
        const style = document.createElement('style');
        style.id = 'pc-ai-styles';
        style.textContent = `
            .pc-ai-launcher{position:fixed;right:24px;bottom:230px;z-index:1091;width:58px;height:58px;border:0;border-radius:50%;background:linear-gradient(135deg,#172554,#0d9488);color:#fff;box-shadow:0 16px 36px rgba(15,118,110,.3);display:grid;place-items:center;font-size:20px;cursor:pointer;transition:.2s ease}.pc-ai-launcher:hover{transform:translateY(-3px) scale(1.035);box-shadow:0 20px 42px rgba(15,118,110,.38)}.pc-ai-launcher:focus{outline:3px solid rgba(45,212,191,.25);outline-offset:3px}.pc-ai-launcher:before{content:'AI';position:absolute;right:-3px;top:-5px;min-width:24px;height:18px;padding:0 5px;border-radius:999px;background:#fff;color:#0f766e;font-size:8px;font-weight:900;display:grid;place-items:center;border:1px solid #d9ece9}.pc-ai-spark{animation:pcAiSpark 2.4s ease-in-out infinite}@keyframes pcAiSpark{0%,100%{transform:scale(1);opacity:1}50%{transform:scale(1.12);opacity:.76}}
            .pc-ai-panel{position:fixed;right:24px;bottom:300px;z-index:1096;width:min(420px,calc(100vw - 28px));height:min(620px,calc(100vh - 330px));min-height:460px;background:#fff;color:#172129;border:1px solid #dce8e6;border-radius:24px;box-shadow:0 28px 76px rgba(17,44,51,.24);overflow:hidden;display:flex;flex-direction:column;opacity:0;visibility:hidden;transform:translateY(14px) scale(.985);transition:.22s ease}.pc-ai-panel.is-open{opacity:1;visibility:visible;transform:none}.pc-ai-head{padding:18px 18px 15px;background:linear-gradient(135deg,#0b3e43,#0f766e);color:#fff;display:flex;align-items:flex-start;gap:12px}.pc-ai-logo{width:42px;height:42px;flex:0 0 42px;border-radius:13px;background:rgba(255,255,255,.12);display:grid;place-items:center;font-size:17px}.pc-ai-head-copy{min-width:0;flex:1}.pc-ai-head h3{margin:0 0 3px;font-size:17px;font-weight:850;color:#fff}.pc-ai-head p{margin:0;color:#c9dfdc;font-size:10px;line-height:1.45}.pc-ai-status-pill{display:inline-flex;align-items:center;gap:5px;margin-top:7px;padding:4px 8px;border-radius:999px;background:rgba(255,255,255,.1);font-size:9px;color:#dff8f4}.pc-ai-status-dot{width:6px;height:6px;border-radius:50%;background:#34d399}.pc-ai-close{width:34px;height:34px;flex:0 0 34px;border:1px solid rgba(255,255,255,.17);border-radius:10px;background:rgba(255,255,255,.08);color:#fff;display:grid;place-items:center;cursor:pointer}.pc-ai-notice{padding:9px 14px;border-bottom:1px solid #edf2f1;background:#f6fbfa;color:#65757c;font-size:9px;line-height:1.5}.pc-ai-notice i{color:#0d9488;margin-right:5px}.pc-ai-messages{flex:1;overflow-y:auto;padding:16px 14px;scroll-behavior:smooth}.pc-ai-row{display:flex;margin-bottom:12px}.pc-ai-row.user{justify-content:flex-end}.pc-ai-bubble{max-width:84%;padding:11px 13px;border-radius:15px;font-size:12px;line-height:1.6;word-break:break-word}.pc-ai-row.assistant .pc-ai-bubble{background:#f0f7f6;border:1px solid #dcebea;color:#304147;border-top-left-radius:5px}.pc-ai-row.user .pc-ai-bubble{background:linear-gradient(135deg,#0d9488,#0f766e);color:#fff;border-top-right-radius:5px}.pc-ai-avatar{width:28px;height:28px;flex:0 0 28px;margin-right:8px;border-radius:9px;background:#e7f7f4;color:#0d9488;display:grid;place-items:center;font-size:11px}.pc-ai-typing{display:flex;align-items:center;gap:5px;height:18px}.pc-ai-typing span{width:5px;height:5px;border-radius:50%;background:#7b918f;animation:pcAiTyping 1s infinite alternate}.pc-ai-typing span:nth-child(2){animation-delay:.15s}.pc-ai-typing span:nth-child(3){animation-delay:.3s}@keyframes pcAiTyping{from{opacity:.35;transform:translateY(1px)}to{opacity:1;transform:translateY(-2px)}}
            .pc-ai-suggestions{display:flex;gap:7px;overflow-x:auto;padding:0 14px 10px;scrollbar-width:none}.pc-ai-suggestions::-webkit-scrollbar{display:none}.pc-ai-chip{flex:0 0 auto;border:1px solid #dce7e5;border-radius:999px;background:#fff;color:#516269;padding:7px 10px;font-size:9px;font-weight:700;cursor:pointer;transition:.18s}.pc-ai-chip:hover{border-color:#0d9488;background:#edf9f7;color:#0d7f75}.pc-ai-composer{padding:11px 12px 13px;border-top:1px solid #e7eeee;background:#fff}.pc-ai-input-wrap{display:flex;align-items:flex-end;gap:8px;border:1px solid #d8e3e1;border-radius:15px;padding:7px 7px 7px 11px;background:#fbfdfd;transition:.18s}.pc-ai-input-wrap:focus-within{border-color:#0d9488;box-shadow:0 0 0 3px rgba(13,148,136,.08);background:#fff}.pc-ai-input{flex:1;min-height:34px;max-height:92px;border:0;outline:0;background:transparent;color:#172129;resize:none;font-family:inherit;font-size:12px;line-height:1.45;padding:8px 0}.pc-ai-send{width:38px;height:38px;flex:0 0 38px;border:0;border-radius:11px;background:#0d9488;color:#fff;display:grid;place-items:center;cursor:pointer;transition:.18s}.pc-ai-send:hover{background:#0f766e}.pc-ai-send:disabled{opacity:.55;cursor:not-allowed}.pc-ai-foot{text-align:center;margin-top:7px;color:#8a979b;font-size:8px}.pc-ai-error{margin:0 14px 10px;padding:9px 10px;border-radius:10px;background:#fff1f1;border:1px solid #f0cece;color:#a34646;font-size:10px;display:none}
            body.dark-mode .pc-ai-panel{background:#20282b;color:#eef4f4;border-color:#344043}.dark-mode .pc-ai-notice{background:#182124;border-color:#344043;color:#aebbbd}.dark-mode .pc-ai-messages{background:#20282b}.dark-mode .pc-ai-row.assistant .pc-ai-bubble{background:#293437;border-color:#394749;color:#e2e9ea}.dark-mode .pc-ai-avatar{background:#183b38;color:#69ddd2}.dark-mode .pc-ai-suggestions,.dark-mode .pc-ai-composer{background:#20282b}.dark-mode .pc-ai-chip{background:#252f32;border-color:#3a474a;color:#d9e2e3}.dark-mode .pc-ai-chip:hover{background:#173a36;color:#77ddd2}.dark-mode .pc-ai-composer{border-color:#344043}.dark-mode .pc-ai-input-wrap{background:#171e20;border-color:#3a474a}.dark-mode .pc-ai-input{color:#f3f7f7}.dark-mode .pc-ai-foot{color:#89979a}
            @media(max-width:575px){.pc-ai-launcher{right:16px;bottom:216px;width:52px;height:52px}.pc-ai-panel{left:10px;right:10px;bottom:278px;width:auto;height:min(590px,calc(100vh - 298px));min-height:430px;border-radius:20px}.pc-ai-bubble{max-width:88%}}
        `;
        document.head.appendChild(style);
    }

    function buildWidget() {
        if (document.getElementById('pc-ai-launcher')) return;

        const launcher = document.createElement('button');
        launcher.type = 'button';
        launcher.id = 'pc-ai-launcher';
        launcher.className = 'pc-ai-launcher';
        launcher.setAttribute('aria-label', 'Open PharmaCare AI assistant');
        launcher.setAttribute('title', 'Ask PharmaCare AI');
        launcher.innerHTML = '<i class="fas fa-sparkles pc-ai-spark"></i>';
        if (!document.querySelector('.fa-sparkles')) launcher.innerHTML = '<i class="fas fa-comment-medical pc-ai-spark"></i>';

        const panel = document.createElement('section');
        panel.id = 'pc-ai-panel';
        panel.className = 'pc-ai-panel';
        panel.setAttribute('aria-hidden', 'true');
        panel.innerHTML = `
            <div class="pc-ai-head">
                <span class="pc-ai-logo"><i class="fas fa-comment-medical"></i></span>
                <div class="pc-ai-head-copy">
                    <h3>PharmaCare AI</h3>
                    <p>Product, order and website assistance powered by your PharmaCare data.</p>
                    <span class="pc-ai-status-pill"><span class="pc-ai-status-dot"></span><span id="pc-ai-status-text">Checking assistant...</span></span>
                </div>
                <button type="button" class="pc-ai-close" aria-label="Close AI assistant"><i class="fas fa-times"></i></button>
            </div>
            <div class="pc-ai-notice"><i class="fas fa-shield-alt"></i> For medical diagnosis, dosing or treatment decisions, please speak with a pharmacist or clinician.</div>
            <div id="pc-ai-messages" class="pc-ai-messages"></div>
            <div id="pc-ai-error" class="pc-ai-error"></div>
            <div class="pc-ai-suggestions">
                <button class="pc-ai-chip" type="button" data-prompt="What products are currently in stock?">In-stock products</button>
                <button class="pc-ai-chip" type="button" data-prompt="How do prescription reservations work?">Prescription pickup</button>
                <button class="pc-ai-chip" type="button" data-prompt="How can I track my latest order?">Track my order</button>
                <button class="pc-ai-chip" type="button" data-prompt="Help me find a product on this website.">Find a product</button>
            </div>
            <div class="pc-ai-composer">
                <div class="pc-ai-input-wrap">
                    <textarea id="pc-ai-input" class="pc-ai-input" maxlength="1200" rows="1" placeholder="Ask about products, orders or PharmaCare..."></textarea>
                    <button id="pc-ai-send" type="button" class="pc-ai-send" aria-label="Send message"><i class="fas fa-arrow-up"></i></button>
                </div>
                <div class="pc-ai-foot">PharmaCare AI can make mistakes. Verify important medical information with a professional.</div>
            </div>
        `;

        document.body.appendChild(launcher);
        document.body.appendChild(panel);

        const messages = document.getElementById('pc-ai-messages');
        const input = document.getElementById('pc-ai-input');
        const sendButton = document.getElementById('pc-ai-send');
        const statusText = document.getElementById('pc-ai-status-text');
        const errorBox = document.getElementById('pc-ai-error');

        addMessage('assistant', 'Hi! I’m PharmaCare AI. I can help you find products, explain prescription pickup, and answer questions about your orders or reservations.');

        async function checkStatus() {
            if (statusChecked) return aiEnabled;
            statusChecked = true;
            try {
                const response = await fetch('/AI/Status', { credentials: 'same-origin', cache: 'no-store' });
                const data = await response.json();
                aiEnabled = !!(response.ok && data && data.enabled);
            } catch (_) {
                aiEnabled = false;
            }
            statusText.textContent = aiEnabled ? 'AI assistant online' : 'AI assistant unavailable';
            return aiEnabled;
        }

        function addMessage(role, text) {
            const row = document.createElement('div');
            row.className = 'pc-ai-row ' + role;
            if (role === 'assistant') {
                row.innerHTML = '<span class="pc-ai-avatar"><i class="fas fa-prescription-bottle-alt"></i></span><div class="pc-ai-bubble">' + formatMessage(text) + '</div>';
            } else {
                row.innerHTML = '<div class="pc-ai-bubble">' + formatMessage(text) + '</div>';
            }
            messages.appendChild(row);
            messages.scrollTop = messages.scrollHeight;
        }

        function showTyping() {
            const row = document.createElement('div');
            row.id = 'pc-ai-typing-row';
            row.className = 'pc-ai-row assistant';
            row.innerHTML = '<span class="pc-ai-avatar"><i class="fas fa-prescription-bottle-alt"></i></span><div class="pc-ai-bubble"><span class="pc-ai-typing"><span></span><span></span><span></span></span></div>';
            messages.appendChild(row);
            messages.scrollTop = messages.scrollHeight;
        }

        function hideTyping() {
            const row = document.getElementById('pc-ai-typing-row');
            if (row) row.remove();
        }

        function showError(text) {
            errorBox.textContent = text;
            errorBox.style.display = 'block';
        }

        function clearError() {
            errorBox.textContent = '';
            errorBox.style.display = 'none';
        }

        function setBusy(value) {
            sending = value;
            sendButton.disabled = value;
            input.disabled = value;
            sendButton.innerHTML = value ? '<i class="fas fa-spinner fa-spin"></i>' : '<i class="fas fa-arrow-up"></i>';
        }

        async function sendMessage(text) {
            const clean = String(text || '').trim();
            if (!clean || sending) return;
            clearError();

            if (!(await checkStatus())) {
                showError('PharmaCare AI is not available right now. Please use the support button to contact the pharmacy team.');
                return;
            }

            addMessage('user', clean);
            input.value = '';
            input.style.height = 'auto';
            setBusy(true);
            showTyping();

            const requestHistory = history.slice(-8);
            try {
                const body = new URLSearchParams();
                body.set('message', clean);
                body.set('pagePath', window.location.pathname + window.location.search);
                body.set('pageTitle', document.title || 'PharmaCare');
                body.set('historyJson', JSON.stringify(requestHistory));

                const response = await fetch('/AI/Chat', {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: body.toString()
                });

                const data = await response.json().catch(function () { return null; });
                hideTyping();
                if (!response.ok || !data || !data.success) {
                    throw new Error(data && data.message ? data.message : 'The AI assistant could not respond right now.');
                }

                const answer = data.message || 'I could not generate a response just now.';
                addMessage('assistant', answer);
                history.push({ role: 'user', content: clean });
                history.push({ role: 'assistant', content: answer });
                if (history.length > 12) history.splice(0, history.length - 12);
            } catch (error) {
                hideTyping();
                showError(error.message || 'The AI assistant is temporarily unavailable.');
            } finally {
                setBusy(false);
                input.focus();
            }
        }

        function openPanel() {
            const supportPanel = document.getElementById('pc-support-panel');
            if (supportPanel) {
                supportPanel.classList.remove('is-open');
                supportPanel.setAttribute('aria-hidden', 'true');
            }
            panel.classList.add('is-open');
            panel.setAttribute('aria-hidden', 'false');
            checkStatus();
            setTimeout(function () { input.focus(); }, 180);
        }

        function closePanel() {
            panel.classList.remove('is-open');
            panel.setAttribute('aria-hidden', 'true');
        }

        launcher.addEventListener('click', function () {
            panel.classList.contains('is-open') ? closePanel() : openPanel();
        });
        panel.querySelector('.pc-ai-close').addEventListener('click', closePanel);

        panel.querySelectorAll('.pc-ai-chip').forEach(function (chip) {
            chip.addEventListener('click', function () { sendMessage(chip.getAttribute('data-prompt')); });
        });

        sendButton.addEventListener('click', function () { sendMessage(input.value); });
        input.addEventListener('keydown', function (event) {
            if (event.key === 'Enter' && !event.shiftKey) {
                event.preventDefault();
                sendMessage(input.value);
            }
        });
        input.addEventListener('input', function () {
            input.style.height = 'auto';
            input.style.height = Math.min(input.scrollHeight, 92) + 'px';
        });
        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape' && panel.classList.contains('is-open')) closePanel();
        });
    }

    function init() {
        injectStyles();
        buildWidget();
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init, { once: true });
    else init();
})();
