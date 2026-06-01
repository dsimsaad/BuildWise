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
const mobileToggle = document.getElementById("mobile-toggle");
const mobileMenu = document.getElementById("mobile-menu");

if (mobileToggle && mobileMenu) {
    mobileToggle.addEventListener("click", () => {
        mobileMenu.classList.toggle("active");
        mobileToggle.classList.toggle("active");
    });
    const mobileLinks = mobileMenu.querySelectorAll("a");
    mobileLinks.forEach(link => {
        link.addEventListener("click", () => {
            mobileMenu.classList.remove("active");
            mobileToggle.classList.remove("active");
        });
    });
}
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
const lordIcons = document.querySelectorAll("lord-icon");
if (lordIcons.length) {
    const loadLordIconRuntime = () => {
        if (document.querySelector("script[data-lordicon-runtime]")) return;

        const script = document.createElement("script");
        script.src = "https://cdn.lordicon.com/lordicon.js";
        script.defer = true;
        script.dataset.lordiconRuntime = "true";
        document.head.appendChild(script);
    };

    if ("IntersectionObserver" in window) {
        const iconObserver = new IntersectionObserver(entries => {
            if (entries.some(entry => entry.isIntersecting)) {
                loadLordIconRuntime();
                iconObserver.disconnect();
            }
        }, { rootMargin: "300px 0px" });

        lordIcons.forEach(icon => iconObserver.observe(icon));
    } else {
        if ("requestIdleCallback" in window) {
            window.requestIdleCallback(loadLordIconRuntime, { timeout: 1200 });
        } else {
            window.setTimeout(loadLordIconRuntime, 600);
        }
    }
}
const backToTopBtn = document.getElementById("backToTop");

if (backToTopBtn) {
    let backToTopFrame = 0;
    window.addEventListener("scroll", () => {
        if (backToTopFrame) return;
        backToTopFrame = requestAnimationFrame(() => {
            backToTopBtn.classList.toggle("show", window.scrollY > 400);
            backToTopFrame = 0;
        });
    }, { passive: true });

    backToTopBtn.addEventListener("click", () => {
        window.scrollTo({
            top: 0,
            behavior: "smooth"
        });
    });
}
const demoModal = document.getElementById("demoModal");
const openDemoModalBtn = document.getElementById("openDemoModal");
const demoVideo = document.getElementById("demoVideo");

if (demoModal && openDemoModalBtn && demoVideo) {
    const demoLanguageBtns = demoModal.querySelectorAll("[data-demo-src]");
    const closeDemoBtns = demoModal.querySelectorAll("[data-demo-close]");
    const getActiveDemoSrc = () => demoModal.querySelector(".demo-language-btn.is-active")?.getAttribute("data-demo-src") || "";
    const getAutoplaySrc = src => src.includes("?") ? `${src}&autoplay=1` : `${src}?autoplay=1`;

    const openDemo = () => {
        demoModal.classList.add("is-open");
        demoModal.setAttribute("aria-hidden", "false");
        document.body.style.overflow = "hidden";
        const src = getActiveDemoSrc();
        if (src) {
            demoVideo.setAttribute("src", getAutoplaySrc(src));
        }
    };

    const closeDemo = () => {
        demoModal.classList.remove("is-open");
        demoModal.setAttribute("aria-hidden", "true");
        document.body.style.overflow = "";
        demoVideo.removeAttribute("src");
    };

    openDemoModalBtn.addEventListener("click", openDemo);
    closeDemoBtns.forEach(button => button.addEventListener("click", closeDemo));

    demoLanguageBtns.forEach(button => {
        button.addEventListener("click", () => {
            const src = button.getAttribute("data-demo-src");
            if (!src || demoVideo.getAttribute("src")?.startsWith(src)) return;

            demoLanguageBtns.forEach(item => item.classList.remove("is-active"));
            button.classList.add("is-active");
            demoVideo.setAttribute("title", button.getAttribute("data-demo-label") || "BuildWise demo");
            demoVideo.setAttribute("src", getAutoplaySrc(src));
        });
    });

    document.addEventListener("keydown", event => {
        if (event.key === "Escape" && demoModal.classList.contains("is-open")) {
            closeDemo();
        }
    });
}
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

    let scrollAnimationFrame = 0;
    const scheduleScrollAnimation = () => {
        if (scrollAnimationFrame) return;
        scrollAnimationFrame = requestAnimationFrame(() => {
            updateScrollAnimation();
            scrollAnimationFrame = 0;
        });
    };

    window.addEventListener('scroll', scheduleScrollAnimation, { passive: true });
    window.addEventListener('resize', scheduleScrollAnimation, { passive: true });
    updateScrollAnimation();
}
const navLinks = document.querySelectorAll(".nav-links a, .mobile-nav-overlay a");
const currentPath = window.location.pathname.split("/").pop() || "index.html";

navLinks.forEach(link => {
    const href = link.getAttribute("href") || "";
    const linkPath = href.split("/").filter(Boolean).pop() || "index.html";
    if (linkPath === currentPath) {
        link.classList.add("active");
    } else {
        link.classList.remove("active");
    }
});
