import { initializeApp } from "https://www.gstatic.com/firebasejs/12.12.1/firebase-app.js";
import { getAuth, createUserWithEmailAndPassword, signInWithPopup, GoogleAuthProvider, updateProfile } from "https://www.gstatic.com/firebasejs/12.12.1/firebase-auth.js";

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
function showError(message) {
    if (errorBox) {
        errorBox.textContent = message;
        errorBox.style.display = 'block';
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
const signupForm = document.getElementById('signupForm');
const submitBtn = document.getElementById('submitBtn');
const googleBtn = document.querySelector('.glass-btn-google');

async function sendTokenToBackend(idToken, fullName = null) {
    try {
        const response = await fetch('/Account/FirebaseLogin', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                idToken: idToken,
                name: fullName,
                fullName: fullName
            })
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

if (signupForm && submitBtn) {
    signupForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        hideError();

        const fullname = document.getElementById('fullname').value;
        const email = document.getElementById('email').value;
        const password = document.getElementById('password').value;

        const btnContent = submitBtn.innerHTML;
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<div class="loading-spinner"></div>';

        try {
            const userCredential = await createUserWithEmailAndPassword(auth, email, password);
            await updateProfile(userCredential.user, {
                displayName: fullname
            });
            const idToken = await userCredential.user.getIdToken(true);
            await sendTokenToBackend(idToken, fullname);

        } catch (error) {
            console.error(error);
            let msg = error.message;
            if (error.code === 'auth/email-already-in-use') {
                msg = 'This email is already registered. Please sign in instead.';
            } else if (error.code === 'auth/weak-password') {
                msg = 'Password should be at least 6 characters.';
            } else if (error.code === 'auth/invalid-email') {
                msg = 'Please enter a valid email address.';
            }
            showError(msg);
        } finally {
            submitBtn.disabled = false;
            submitBtn.innerHTML = btnContent;
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
            await sendTokenToBackend(idToken);

        } catch (error) {
            console.error(error);
            showError('Google Sign Up Error: ' + error.message);
        } finally {
            googleBtn.disabled = false;
            googleBtn.innerHTML = originalHtml;
        }
    });
}
