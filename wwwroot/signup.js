(function () {
    'use strict';

    var captchaToken = null;
    var widgetId = null;

    // Called by hCaptcha script once loaded
    window.onHcaptchaLoad = function () {
        var config = window.QueryBotConfig || {};
        if (config.devMode) {
            // Skip captcha widget on localhost — server verification is also bypassed in Development
            captchaToken = 'dev';
            updateButton();
            return;
        }
        widgetId = hcaptcha.render('captchaWidget', {
            sitekey: config.captchaSiteKey || '',
            callback: 'onCaptchaSuccess',
            'expired-callback': 'onCaptchaExpired'
        });
    };

    // Called by hCaptcha when user completes the challenge
    window.onCaptchaSuccess = function (token) {
        captchaToken = token;
        updateButton();
    };

    // Called by hCaptcha when the token expires
    window.onCaptchaExpired = function () {
        captchaToken = null;
        updateButton();
    };

    function isValidEmail(v) {
        return v.length > 0 && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v);
    }

    function formValid() {
        var email = document.getElementById('email').value.trim();
        var nickname = document.getElementById('displayName').value.trim();
        var password = document.getElementById('password').value;
        var confirm = document.getElementById('confirmPassword').value;
        return isValidEmail(email) && nickname.length > 0 && password.length > 0 && password === confirm;
    }

    function updateButton() {
        document.getElementById('signupBtn').disabled = !(formValid() && captchaToken !== null);
    }

    function setStatus(msg) {
        var el = document.getElementById('signupStatus');
        el.textContent = msg || '';
        el.style.display = msg ? '' : 'none';
    }

    document.getElementById('email').addEventListener('input', updateButton);
    document.getElementById('displayName').addEventListener('input', updateButton);
    document.getElementById('password').addEventListener('input', updateButton);
    document.getElementById('confirmPassword').addEventListener('input', updateButton);

    document.getElementById('signupBtn').addEventListener('click', function () {
        if (!formValid() || !captchaToken) return;

        document.getElementById('signupBtn').disabled = true;
        setStatus('');

        fetch('signup', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                email: document.getElementById('email').value.trim(),
                nickname: document.getElementById('displayName').value.trim(),
                password: document.getElementById('password').value,
                captchaToken: captchaToken
            })
        })
        .then(function (res) {
            if (res.ok) {
                document.getElementById('signupForm').style.display = 'none';
                document.getElementById('signupSuccess').style.display = 'block';
            } else {
                setStatus('Something went wrong. Please try again.');
                if (widgetId !== null) hcaptcha.reset(widgetId);
                captchaToken = null;
                updateButton();
            }
        })
        .catch(function () {
            setStatus('Unable to connect. Please check your connection and try again.');
            if (widgetId !== null) hcaptcha.reset(widgetId);
            captchaToken = null;
            updateButton();
        });
    });
}());
