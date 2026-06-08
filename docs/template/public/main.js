// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

export default {
    start: () => {
        applyTheme();
        buildQtMasthead();
        buildSidebarNavigation();
        buildQtFooter();
        setupSidebarFooterOverlap();
    },
};

function buildQtMasthead() {
    if (document.querySelector(".qt-docs-masthead")) {
        return;
    }

    const rel = document.querySelector('meta[name="docfx:rel"]')?.getAttribute("content") || "";
    const masthead = document.createElement("div");
    masthead.className = "qt-docs-masthead";
    masthead.innerHTML = `
        <div class="qt-docs-masthead-inner">
            <div class="qt-docs-masthead-top">
                <a class="qt-docs-back-link" href="https://doc.qt.io/">Back to Doc.qt.io</a>
                <nav class="qt-docs-masthead-links" aria-label="Qt links">
                    <a href="https://www.qt.io/contact-us/" target="_blank" rel="noreferrer">Contact Us</a>
                    <a href="https://blog.qt.io/" target="_blank" rel="noreferrer">Blog</a>
                    <a class="qt-docs-download" href="https://www.qt.io/download/" target="_blank" rel="noreferrer">Download Qt</a>
                </nav>
            </div>
            <div class="qt-docs-masthead-main">
                <div class="qt-docs-product-lockup" aria-label="Qt Bridge for C#">
                    <a class="qt-docs-logo" href="https://doc-snapshots.qt.io/" aria-label="Qt documentation snapshots">
                        <img src="${rel}public/qt-logo-documentation.png" alt="Qt documentation">
                    </a>
                </div>
                <nav class="qt-docs-primary-nav" aria-label="Qt documentation links">
                    <button class="qt-docs-masthead-theme" type="button" aria-label="Toggle light and dark theme"></button>
                    <a class="qt-docs-archives" href="https://doc.qt.io/archives/">Archives</a>
                </nav>
            </div>
        </div>
    `;

    document.body.insertBefore(masthead, document.body.firstChild);
    document.body.classList.add("has-qt-docs-masthead");
    setupMastheadThemeToggle(masthead);
    setupMastheadScroll();
}

function setupMastheadThemeToggle(root) {
    const button = root.querySelector(".qt-docs-masthead-theme");
    if (!button) {
        return;
    }

    button.addEventListener("click", () => {
        const current = getResolvedTheme();
        const next = current === "dark" ? "light" : "dark";
        localStorage.setItem("theme", next);
        applyTheme();
    });
}

function setupMastheadScroll() {
    const threshold = 42;
    let ticking = false;

    const sync = () => {
        document.body.classList.toggle("qt-masthead-hidden", window.scrollY > threshold);
        ticking = false;
    };

    window.addEventListener("scroll", () => {
        if (!ticking) {
            window.requestAnimationFrame(sync);
            ticking = true;
        }
    }, { passive: true });

    sync();
}

function setupSidebarFooterOverlap() {
    const footer = document.querySelector("body > footer");
    if (!footer) {
        return;
    }

    let ticking = false;

    const sync = () => {
        const footerTop = footer.getBoundingClientRect().top;
        const visibleFooterHeight = Math.max(0, window.innerHeight - footerTop);
        document.documentElement.style.setProperty(
            "--qt-footer-visible-height",
            `${Math.round(visibleFooterHeight)}px`
        );
        ticking = false;
    };

    const schedule = () => {
        if (!ticking) {
            window.requestAnimationFrame(sync);
            ticking = true;
        }
    };

    window.addEventListener("scroll", schedule, { passive: true });
    window.addEventListener("resize", schedule);
    sync();
}

function buildQtFooter() {
    const footer = document.querySelector("body > footer");
    if (!footer || footer.classList.contains("qt-docs-footer")) {
        return;
    }

    const rel = document.querySelector('meta[name="docfx:rel"]')?.getAttribute("content") || "";

    footer.classList.add("qt-docs-footer", "l-footer");
    footer.innerHTML = `
        <div class="l-footer__container">
            <div class="l-footer__row l-footer__row--no-padding-bottom">
                <div class="l-footer__column l-footer__company">
                    <div class="l-footer__logo">
                        <a href="https://www.qt.io/?hsLang=en" class="c-logo-footer">
                            <img src="${rel}public/qtgroup.svg" alt="Qt Group">
                        </a>
                    </div>
                    <div class="c-social-media-links" aria-label="Social media">
                        <a href="https://twitter.com/qtproject" target="_blank" rel="noopener" class="fm_button fm_twitter"><span></span></a>
                        <a href="https://www.facebook.com/qt/" target="_blank" rel="noopener" class="fm_button fm_facebook"><span></span></a>
                        <a href="https://www.youtube.com/user/QtStudios" target="_blank" rel="noopener" class="fm_button fm_youtube"><span></span></a>
                        <a href="https://www.linkedin.com/company/qtgroup/" target="_blank" rel="noopener" class="fm_button fm_linkedin"><span></span></a>
                    </div>
                    <div class="l-footer__contact">
                        <a class="c-btn" href="https://www.qt.io/contact-us?hsLang=en">Contact Us</a>
                    </div>
                </div>
                <div class="l-footer__column l-footer__navigation">
                    <nav class="c-footer-navigation" aria-label="Qt footer links">
                        <div class="hs-menu-wrapper">
                            <ul role="menu">
                    ${footerMenuItem("Qt Group", [
                        ["Our Story", "https://www.qt.io/group"],
                        ["Brand", "https://www.qt.io/brand"],
                        ["News", "https://www.qt.io/newsroom"],
                        ["Careers", "https://www.qt.io/careers"],
                        ["Investors", "https://www.qt.io/investors"],
                        ["Qt Products", "https://www.qt.io/product"],
                        ["Software Quality Products", "https://www.qt.io/product/quality-assurance"],
                    ])}
                    ${footerMenuItem("Licensing", [
                        ["License Agreement", "https://www.qt.io/terms-conditions"],
                        ["Open Source", "https://www.qt.io/licensing/open-source-lgpl-obligations"],
                        ["Plans and pricing", "https://www.qt.io/pricing"],
                        ["Download", "https://www.qt.io/download"],
                        ["FAQ", "https://www.qt.io/faq/overview"],
                    ])}
                    ${footerMenuItem("Learn Qt", [
                        ["For Learners", "https://www.qt.io/academy"],
                        ["For Students and Teachers", "https://www.qt.io/qt-educational-license"],
                        ["Qt Documentation", "https://doc.qt.io/"],
                        ["Qt Forum", "https://forum.qt.io/"],
                    ])}
                    ${footerMenuItem("Support & Services", [
                        ["Professional Services", "https://www.qt.io/qt-professional-services"],
                        ["Customer Success", "https://www.qt.io/customer-success"],
                        ["Support Services", "https://www.qt.io/qt-support/"],
                        ["Partners", "https://www.qt.io/contact-us/partners"],
                        ["Qt World", "https://www.qt.io/qt-world"],
                    ])}
                            </ul>
                        </div>
                    </nav>
                </div>
            </div>
        </div>
        <div class="qt-docs-footer-bottom">
            <div class="qt-docs-footer-bottom-inner">
                <div class="qt-docs-footer-bottom-top">
                    <a href="https://www.qt.io/?hsLang=en">&copy; 2026 The Qt Company</a>
                    <a href="mailto:feedback@qt.io?Subject=Feedback%20about%20doc.qt.io%20site">Feedback</a>
                </div>
                <p>Qt Group includes The Qt Company Oy and its global subsidiaries and affiliates.</p>
            </div>
        </div>
    `;
}

function footerMenuItem(title, links) {
    return `
        <li class="hs-menu-item hs-menu-depth-1 hs-item-has-children" role="none">
            <a href="javascript:;" aria-haspopup="true" aria-expanded="false" role="menuitem">${title}</a>
            <ul role="menu" class="hs-menu-children-wrapper">
                ${links.map(([label, href]) => `<li class="hs-menu-item hs-menu-depth-2" role="none"><a href="${href}" role="menuitem">${label}</a></li>`).join("")}
            </ul>
        </li>
    `;
}

async function buildSidebarNavigation() {
    const navMeta = document.querySelector('meta[name="docfx:navrel"]');
    const navPath = navMeta?.getAttribute("content");
    if (!navPath || document.querySelector(".qt-docs-sidebar")) {
        return;
    }

    const toc = await loadRootToc(navPath);
    if (!toc) {
        return;
    }

    rewriteRootTocLinks(toc);
    const brand = document.querySelector(".navbar-brand");
    const sidebar = document.createElement("aside");
    sidebar.className = "qt-docs-sidebar";
    sidebar.innerHTML = `
        <a class="qt-docs-sidebar-brand" href="${brand?.getAttribute("href") || "index.html"}">
            Qt Bridge for C#
        </a>
        <div class="qt-docs-sidebar-search"></div>
        <nav class="qt-docs-sidebar-nav" aria-label="Documentation"></nav>
    `;

    const activeLink = markCurrentLink(toc);
    moveSearchForm(sidebar);
    sidebar.querySelector(".qt-docs-sidebar-nav")?.appendChild(toc);
    addPageKicker(activeLink);
    setupThemeControls();
    document.body.insertBefore(sidebar, document.body.firstChild);
    document.body.classList.add("has-qt-docs-sidebar");
}

function moveSearchForm(sidebar) {
    const searchForm = document.querySelector("form.search#search");
    const searchHost = sidebar.querySelector(".qt-docs-sidebar-search");
    if (!searchForm || !searchHost) {
        searchHost?.remove();
        return;
    }

    searchHost.appendChild(searchForm);
}

function setupThemeControls() {
    const media = window.matchMedia("(prefers-color-scheme: dark)");

    media.addEventListener("change", () => {
        if (!localStorage.getItem("theme")) {
            applyTheme();
        }
    });

    applyTheme();
}

function applyTheme() {
    document.documentElement.setAttribute("data-bs-theme", getResolvedTheme());
}

function getResolvedTheme() {
    const selected = localStorage.getItem("theme");
    if (selected === "light" || selected === "dark") {
        return selected;
    }

    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

function rewriteRootTocLinks(nav) {
    const rel = document.querySelector('meta[name="docfx:rel"]')?.getAttribute("content") || "";

    for (const link of nav.querySelectorAll("a[href]")) {
        const href = link.getAttribute("href");
        if (!href || isExternalOrAnchored(href) || href.startsWith(rel)) {
            continue;
        }

        link.setAttribute("href", `${rel}${href}`);
    }
}

function isExternalOrAnchored(href) {
    return href.startsWith("#")
        || href.startsWith("/")
        || /^[a-z][a-z0-9+.-]*:/i.test(href);
}

async function loadRootToc(navPath) {
    try {
        const response = await fetch(navPath);
        if (!response.ok) {
            return null;
        }

        const html = await response.text();
        const doc = new DOMParser().parseFromString(html, "text/html");
        return doc.querySelector("#toc > ul.nav")?.cloneNode(true) || null;
    } catch {
        return null;
    }
}

function addPageKicker(activeLink) {
    const article = document.querySelector("article");
    if (!article || article.querySelector(".qt-page-kicker")) {
        return;
    }

    const title = activeLink?.textContent?.trim();
    const href = activeLink?.getAttribute("href");
    if (!title || !href) {
        return;
    }

    const kicker = document.createElement("a");
    kicker.className = "qt-page-kicker";
    kicker.href = href;
    kicker.textContent = title;
    article.insertBefore(kicker, article.firstChild);
}

function markCurrentLink(nav) {
    const current = normalizePath(window.location.pathname);
    let activeLink = null;

    for (const link of nav.querySelectorAll("a[href]")) {
        const url = new URL(link.getAttribute("href"), window.location.href);
        if (normalizePath(url.pathname) === current) {
            link.classList.add("active");
            activeLink = link;
        }
    }

    return activeLink;
}

function normalizePath(path) {
    const withoutTrailingSlash = path.replace(/\/$/, "");
    return withoutTrailingSlash.endsWith("/index.html")
        ? withoutTrailingSlash.slice(0, -"/index.html".length)
        : withoutTrailingSlash;
}
