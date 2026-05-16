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
const revealElements = document.querySelectorAll(".reveal");
if (revealElements.length && "IntersectionObserver" in window) {
    const revealObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add("active");
                revealObserver.unobserve(entry.target);
            }
        });
    }, {
        threshold: 0.1,
        rootMargin: "0px 0px -50px 0px"
    });

    revealElements.forEach(el => revealObserver.observe(el));
} else {
    revealElements.forEach(el => el.classList.add("active"));
}

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
const faqList = document.getElementById("faq-list");
const faqItems = faqList ? Array.from(faqList.querySelectorAll(".faq-item")) : [];

function setFaqOpen(item, shouldOpen) {
    const answer = item.querySelector(".faq-answer");
    if (!answer) return;

    item.classList.toggle("open", shouldOpen);
    answer.style.height = shouldOpen ? `${answer.scrollHeight}px` : "0px";
}

if (faqList) {
    faqList.addEventListener("click", (event) => {
        const question = event.target.closest(".faq-question");
        if (!question) return;

        const item = question.closest(".faq-item");
        const willOpen = item && !item.classList.contains("open");

        faqItems.forEach(otherItem => setFaqOpen(otherItem, false));
        if (item && willOpen) setFaqOpen(item, true);
    });

    window.addEventListener("resize", () => {
        faqItems.forEach(item => {
            if (item.classList.contains("open")) setFaqOpen(item, true);
        });
    }, { passive: true });
}

document.fonts?.ready.then(() => {
    faqItems.forEach(item => {
        if (item.classList.contains("open")) setFaqOpen(item, true);
    });
});

// === FAQ FILTER BUTTONS ===
const filterBtns = document.querySelectorAll(".filter-btn");
let currentFilter = "all";

function applyFaqFilter(activeFilter) {
    faqItems.forEach(item => {
        const category = item.getAttribute("data-category") || "";
        const isVisible = activeFilter === "all" || category === activeFilter;
        item.hidden = !isVisible;
        if (!isVisible) setFaqOpen(item, false);
    });
}

filterBtns.forEach(btn => {
    btn.addEventListener("click", () => {
        filterBtns.forEach(b => b.classList.remove("active"));
        btn.classList.add("active");
        currentFilter = btn.getAttribute("data-filter") || "all";
        applyFaqFilter(currentFilter);
    });
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
            btn.innerHTML = "Message Sent!";
            btn.disabled = true;
            btn.style.background = "#059669";
            contactForm.reset();

            setTimeout(() => {
                btn.innerHTML = originalText;
                btn.style.background = "";
                btn.disabled = false;
            }, 1800);
        }
    });
}
