(() => {
    const faqList = document.getElementById("faq-list");
    const faqItems = faqList ? Array.from(faqList.querySelectorAll(".faq-item")) : [];

    function setFaqOpen(item, shouldOpen) {
        const answer = item.querySelector(".faq-answer");
        if (!answer) return;

        item.classList.toggle("open", shouldOpen);
        answer.style.height = shouldOpen ? `${answer.scrollHeight}px` : "0px";
    }

    if (faqList) {
        faqList.addEventListener("click", event => {
            const question = event.target.closest(".faq-question");
            if (!question) return;

            const item = question.closest(".faq-item");
            const willOpen = item && !item.classList.contains("open");

            faqItems.forEach(otherItem => setFaqOpen(otherItem, false));
            if (item && willOpen) setFaqOpen(item, true);
        });

        let resizeFrame = 0;
        window.addEventListener("resize", () => {
            cancelAnimationFrame(resizeFrame);
            resizeFrame = requestAnimationFrame(() => {
                faqItems.forEach(item => {
                    if (item.classList.contains("open")) setFaqOpen(item, true);
                });
            });
        }, { passive: true });
    }

    document.fonts?.ready.then(() => {
        faqItems.forEach(item => {
            if (item.classList.contains("open")) setFaqOpen(item, true);
        });
    });

    const filterBtns = document.querySelectorAll(".filter-btn");

    filterBtns.forEach(btn => {
        btn.addEventListener("click", () => {
            const filter = btn.dataset.filter || "all";

            filterBtns.forEach(item => item.classList.remove("active"));
            btn.classList.add("active");

            faqItems.forEach(item => {
                const visible = filter === "all" || item.dataset.category === filter;
                item.hidden = !visible;
                if (!visible) setFaqOpen(item, false);
            });
        });
    });

    document.querySelector(".contact-form")?.addEventListener("submit", event => {
        event.preventDefault();

        const form = event.currentTarget;
        const btn = form.querySelector(".btn-submit");
        if (!btn) return;

        const originalText = btn.innerHTML;
        btn.innerHTML = "Message Sent!";
        btn.disabled = true;
        btn.style.background = "#059669";
        form.reset();

        setTimeout(() => {
            btn.innerHTML = originalText;
            btn.style.background = "";
            btn.disabled = false;
        }, 1800);
    });
})();
