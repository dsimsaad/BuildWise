// === LOGIN PAGE TOGGLE ===
const container = document.getElementById("container");
const signUpBtn = document.getElementById("signUp");
const signInBtn = document.getElementById("signIn");

if (container && signUpBtn && signInBtn) {
    signUpBtn.addEventListener("click", () => {
        container.classList.add("right-panel-active");
    });

    signInBtn.addEventListener("click", () => {
        container.classList.remove("right-panel-active");
    });
}

// === MOBILE MENU ===
const mobileToggle = document.getElementById("mobile-toggle");
const mobileMenu = document.getElementById("mobile-menu");

if (mobileToggle && mobileMenu) {
    mobileToggle.addEventListener("click", () => {
        mobileMenu.classList.toggle("active");
        mobileToggle.classList.toggle("active");
    });

    // Close menu when clicking a link
    const mobileLinks = mobileMenu.querySelectorAll("a");
    mobileLinks.forEach(link => {
        link.addEventListener("click", () => {
            mobileMenu.classList.remove("active");
            mobileToggle.classList.remove("active");
        });
    });
}

// === SCROLL REVEAL ANIMATION ===
const revealObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            entry.target.classList.add("active");
        }
    });
}, {
    threshold: 0.1,
    rootMargin: "0px 0px -50px 0px"
});

const revealElements = document.querySelectorAll(".reveal");
revealElements.forEach(el => revealObserver.observe(el));

// === BACK TO TOP BUTTON ===
const backToTopBtn = document.getElementById("backToTop");

if (backToTopBtn) {
    window.addEventListener("scroll", () => {
        if (window.scrollY > 400) {
            backToTopBtn.classList.add("show");
        } else {
            backToTopBtn.classList.remove("show");
        }
    });

    backToTopBtn.addEventListener("click", () => {
        window.scrollTo({
            top: 0,
            behavior: "smooth"
        });
    });
}

// === FAQ ACCORDION ===
const faqItems = document.querySelectorAll(".faq-item");
faqItems.forEach(item => {
    const question = item.querySelector(".faq-question");
    if (question) {
        question.addEventListener("click", () => {
            const isActive = item.classList.contains("active");
            
            // Close all other items
            faqItems.forEach(otherItem => {
                otherItem.classList.remove("active");
            });

            // Toggle current item
            if (!isActive) {
                item.classList.add("active");
            }
        });
    }
});

// === LORDICON HOVER TRIGGER ===
document.querySelectorAll('.feature-card, .feat-card').forEach(card => {
    const icon = card.querySelector('lord-icon');
    if (icon) {
        card.addEventListener('mouseenter', () => icon.play());
    }
});

// === DASHBOARD SCROLL ANIMATION (index.html) ===
const scrollContainer = document.getElementById('scroll-container');
const scrollWrapper = document.getElementById('scroll-wrapper');
const heroHeader = document.getElementById('hero-header');

if (scrollContainer && scrollWrapper) {
    const updateScrollAnimation = () => {
        const rect = scrollContainer.getBoundingClientRect();
        const windowHeight = window.innerHeight;
        
        let progress = 0;
        const start = windowHeight;
        const end = windowHeight * 0.2;
        
        if (rect.top <= start && rect.top >= end) {
            progress = (start - rect.top) / (start - end);
        } else if (rect.top < end) {
            progress = 1;
        }

        const easeProgress = 1 - Math.pow(1 - progress, 3);
        const rotateX = 20 - (easeProgress * 20);
        
        const isMobile = window.innerWidth <= 768;
        const startScale = isMobile ? 0.7 : 1.05;
        const endScale = isMobile ? 0.9 : 1.0;
        const scale = startScale + (endScale - startScale) * easeProgress;
        
        const translateY = 0 - (easeProgress * 100);

        scrollWrapper.style.transform = `translateY(${translateY}px) rotateX(${rotateX}deg) scale(${scale})`;
        
        if (heroHeader) {
            heroHeader.style.transform = `translateY(${translateY}px)`;
        }
    };

    window.addEventListener('scroll', () => requestAnimationFrame(updateScrollAnimation));
    window.addEventListener('resize', () => requestAnimationFrame(updateScrollAnimation));
    updateScrollAnimation();
}

// === HIGHLIGHT ACTIVE NAV LINK ===
const navLinks = document.querySelectorAll(".nav-links a, .mobile-nav-overlay a");
const currentPath = window.location.pathname.split("/").pop() || "index.html";

navLinks.forEach(link => {
    const linkPath = link.getAttribute("href");
    if (linkPath === currentPath) {
        link.classList.add("active");
    } else {
        link.classList.remove("active");
    }
});

// === CONTACT FORM SUBMISSION ===
const contactForm = document.querySelector(".contact-form");
if (contactForm) {
    contactForm.addEventListener("submit", (e) => {
        e.preventDefault();
        const btn = contactForm.querySelector(".btn-submit");
        if (btn) {
            const originalText = btn.innerHTML;
            btn.innerHTML = "Sending...";
            btn.disabled = true;
            
            setTimeout(() => {
                btn.innerHTML = "Message Sent! ✓";
                btn.style.background = "#059669";
                contactForm.reset();
                setTimeout(() => {
                    btn.innerHTML = originalText;
                    btn.style.background = "";
                    btn.disabled = false;
                }, 3000);
            }, 1500);
        }
    });
}
