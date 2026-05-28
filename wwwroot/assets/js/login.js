import { initializeApp } from "https://www.gstatic.com/firebasejs/12.12.1/firebase-app.js";
import { getAuth, signInWithEmailAndPassword, signInWithPopup, GoogleAuthProvider, sendPasswordResetEmail } from "https://www.gstatic.com/firebasejs/12.12.1/firebase-auth.js";

const firebaseConfig = {
    apiKey: "AIzaSyBrbRZg68PhYZHWburcYlOvksd94hn4b44",
    authDomain: "buildwise-d92ba.firebaseapp.com",
    projectId: "buildwise-d92ba",
    storageBucket: "buildwise-d92ba.firebasestorage.app",
    messagingSenderId: "393818029564",
    appId: "1:393818029564:web:d662443d5a34e0a0ff4679",
    measurementId: "G-WVLL6PJNSG"
};
const app = initializeApp(firebaseConfig);
const auth = getAuth(app);
const inputs = document.querySelectorAll('.glass-input-wrapper input');
inputs.forEach(input => {
    input.addEventListener('focus', () => {
        input.parentElement.classList.add('focused');
    });
    input.addEventListener('blur', () => {
        input.parentElement.classList.remove('focused');
    });
});
const togglePassword = document.getElementById('togglePassword');
const passwordInput = document.getElementById('password');
const eyeIcon = document.getElementById('eyeIcon');

if (togglePassword && passwordInput) {
    togglePassword.addEventListener('click', () => {
        const type = passwordInput.getAttribute('type') === 'password' ? 'text' : 'password';
        passwordInput.setAttribute('type', type);

        if (type === 'text') {
            eyeIcon.innerHTML = `
                <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path>
                <line x1="1" y1="1" x2="23" y2="23"></line>
            `;
        } else {
            eyeIcon.innerHTML = `
                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                <circle cx="12" cy="12" r="3"></circle>
            `;
        }
    });
}
const errorBox = document.getElementById('errorBox');
function showError(message, type = 'error') {
    if (errorBox) {
        errorBox.textContent = message;
        errorBox.style.display = 'block';
        errorBox.style.background = type === 'success' ? 'rgba(16, 185, 129, 0.1)' : 'rgba(239, 68, 68, 0.1)';
        errorBox.style.borderColor = type === 'success' ? 'rgba(16, 185, 129, 0.2)' : 'rgba(239, 68, 68, 0.2)';
        errorBox.style.color = type === 'success' ? '#10b981' : '#ef4444';

        errorBox.style.animation = 'none';
        errorBox.offsetHeight;
        errorBox.style.animation = null;
    }
}
function hideError() {
    if (errorBox) {
        errorBox.style.display = 'none';
    }
}
const loginForm = document.getElementById('loginForm');
const submitBtn = document.getElementById('submitBtn');
const googleBtn = document.querySelector('.glass-btn-google');

async function sendTokenToBackend(idToken, displayName = null) {
    try {
        // Firebase signs in on the client, then the server exchanges the token for our app cookie.
        const response = await fetch('/Account/FirebaseLogin', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ idToken: idToken, name: displayName })
        });

        const data = await response.json();

        if (data.success) {
            submitBtn.innerHTML = '<span>Redirecting...</span>';
            window.location.assign(data.redirectUrl);
        } else {
            console.error("Backend error:", data.message);
            showError(data.message);
        }
    } catch (err) {
        console.error("Fetch error:", err);
        showError('Error connecting to backend server.');
    }
}

if (loginForm && submitBtn) {
    loginForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        hideError();

        const email = document.getElementById('email').value;
        const password = document.getElementById('password').value;

        const btnContent = submitBtn.innerHTML;
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<div class="loading-spinner"></div>';

        try {
            const userCredential = await signInWithEmailAndPassword(auth, email, password);
            const idToken = await userCredential.user.getIdToken();
            await sendTokenToBackend(idToken, userCredential.user.displayName);

        } catch (error) {
            console.error(error);
            let message = 'An error occurred during sign in.';
            if (error.code === 'auth/invalid-credential' || error.code === 'auth/wrong-password' || error.code === 'auth/user-not-found') {
                message = 'Invalid email or password. Please try again.';
            } else if (error.code === 'auth/too-many-requests') {
                message = 'Too many failed attempts. Please try again later.';
            } else {
                message = error.message;
            }
            showError(message);
        } finally {
            submitBtn.disabled = false;
            submitBtn.innerHTML = btnContent;
        }
    });
}
const forgotPassLink = document.querySelector('.forgot-pass');
const forgotModal      = document.getElementById('forgotModal');
const forgotClose      = document.getElementById('forgotClose');
const forgotEmailInput = document.getElementById('forgotEmail');
const forgotSubmitBtn  = document.getElementById('forgotSubmit');
const forgotMessage    = document.getElementById('forgotMessage');

function openForgotModal() {
    if (forgotModal) {
        forgotModal.classList.add('active');
        forgotEmailInput.value = '';
        forgotMessage.textContent = '';
        forgotMessage.className = 'forgot-msg';
        forgotEmailInput.focus();
    }
}
function closeForgotModal() {
    if (forgotModal) forgotModal.classList.remove('active');
}

if (forgotPassLink) {
    forgotPassLink.addEventListener('click', (e) => { e.preventDefault(); openForgotModal(); });
}
if (forgotClose) {
    forgotClose.addEventListener('click', closeForgotModal);
}
if (forgotModal) {
    forgotModal.addEventListener('click', (e) => { if (e.target === forgotModal) closeForgotModal(); });
}
document.addEventListener('keydown', (e) => { if (e.key === 'Escape') closeForgotModal(); });

if (forgotSubmitBtn) {
    forgotSubmitBtn.addEventListener('click', async () => {
        const email = forgotEmailInput.value.trim();
        // Password reset stays in Firebase, so this app never handles the raw password reset flow.
        if (!email) {
            forgotMessage.textContent = 'Please enter your email address.';
            forgotMessage.className = 'forgot-msg error';
            return;
        }
        forgotSubmitBtn.disabled = true;
        forgotSubmitBtn.textContent = 'Sending...';
        try {
            await sendPasswordResetEmail(auth, email);
            forgotMessage.textContent = '✓ Reset link sent! Check your inbox.';
            forgotMessage.className = 'forgot-msg success';
            forgotSubmitBtn.textContent = 'Sent!';
            setTimeout(closeForgotModal, 2500);
        } catch (error) {
            let msg = 'Something went wrong. Please try again.';
            if (error.code === 'auth/user-not-found' || error.code === 'auth/invalid-credential') {
                msg = 'No account found with this email address.';
            } else if (error.code === 'auth/invalid-email') {
                msg = 'Please enter a valid email address.';
            }
            forgotMessage.textContent = msg;
            forgotMessage.className = 'forgot-msg error';
            forgotSubmitBtn.disabled = false;
            forgotSubmitBtn.textContent = 'Send Reset Link';
        }
    });
}
if (googleBtn) {
    googleBtn.addEventListener('click', async () => {
        hideError();
        const originalHtml = googleBtn.innerHTML;
        googleBtn.disabled = true;
        googleBtn.innerHTML = '<span>Loading...</span>';

        try {
            const provider = new GoogleAuthProvider();
            const result = await signInWithPopup(auth, provider);

            const idToken = await result.user.getIdToken();
            await sendTokenToBackend(idToken, result.user.displayName);

        } catch (error) {
            console.error(error);
            showError('Google Login Error: ' + error.message);
        } finally {
            googleBtn.disabled = false;
            googleBtn.innerHTML = originalHtml;
        }
    });
}
