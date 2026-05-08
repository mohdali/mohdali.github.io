const headingSelector = [
    '.post-content h1',
    '.post-content h2',
    '.post-content h3',
    '.post-content h4',
    '.post-content h5',
    '.post-content h6',
    '.content-page h1',
    '.content-page h2',
    '.content-page h3',
    '.content-page h4',
    '.content-page h5',
    '.content-page h6',
    '.archive-page .page-header h1',
    '.archive-page .archive-year > h2'
].join(',');

const imageSelector = '.post-content img:not(.post-preview-image):not(.image-lightbox-image)';

let observer: MutationObserver | undefined;
let enhanceScheduled = false;
let imageLightboxEventsInitialized = false;
let activeImageTrigger: HTMLElement | null = null;
let lastFragmentScrollKey = '';

function getHeadingText(heading: HTMLElement) {
    const clone = heading.cloneNode(true) as HTMLElement;
    clone.querySelectorAll('.heading-anchor').forEach(anchor => anchor.remove());

    return clone.textContent?.trim() || '';
}

function slugify(value: string) {
    const slug = value
        .trim()
        .toLowerCase()
        .normalize('NFKD')
        .replace(/[\u0300-\u036f]/g, '')
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-+|-+$/g, '');

    return slug || 'section';
}

function reserveHeadingId(heading: HTMLElement, usedIds: Set<string>) {
    if (heading.id) {
        usedIds.add(heading.id);
        return heading.id;
    }

    const baseId = slugify(getHeadingText(heading));
    let id = baseId;
    let counter = 2;

    while (usedIds.has(id)) {
        id = `${baseId}-${counter}`;
        counter++;
    }

    heading.id = id;
    usedIds.add(id);

    return id;
}

function getDirectHeadingAnchor(heading: HTMLElement) {
    return Array
        .from(heading.children)
        .find(child => child.classList.contains('heading-anchor')) as HTMLAnchorElement | undefined;
}

function getHeadingAnchorHref(id: string) {
    return `${window.location.pathname}${window.location.search}#${encodeURIComponent(id)}`;
}

export function enhanceHeadingAnchors(root: ParentNode = document) {
    const headings = Array.from(root.querySelectorAll<HTMLElement>(headingSelector));

    if (headings.length === 0) {
        return;
    }

    const usedIds = new Set(
        Array
            .from(document.querySelectorAll<HTMLElement>('[id]'))
            .map(element => element.id)
            .filter(Boolean)
    );

    headings.forEach(heading => {
        const id = reserveHeadingId(heading, usedIds);
        const label = getHeadingText(heading);
        let anchor = getDirectHeadingAnchor(heading);

        heading.classList.add('heading-anchor-target');

        if (!anchor) {
            anchor = document.createElement('a');
            anchor.className = 'heading-anchor';
            anchor.textContent = '#';
            heading.appendChild(anchor);
        }

        anchor.setAttribute('href', getHeadingAnchorHref(id));
        anchor.setAttribute('aria-label', label ? `Link to ${label}` : 'Link to this section');
        anchor.title = 'Link to this section';
    });
}

function isEnlargeableImage(image: HTMLImageElement) {
    if (!image.src && !image.currentSrc) {
        return false;
    }

    if (image.closest('a, button, .image-lightbox')) {
        return false;
    }

    return image.dataset.enlarge !== 'false';
}

export function enhancePostImages(root: ParentNode = document) {
    const images = Array.from(root.querySelectorAll<HTMLImageElement>(imageSelector));

    images.forEach(image => {
        if (!isEnlargeableImage(image)) {
            return;
        }

        image.classList.add('enlargeable-image');
        image.dataset.imageLightbox = 'true';
        image.tabIndex = image.tabIndex < 0 ? 0 : image.tabIndex;
        image.setAttribute('role', 'button');
        image.setAttribute('aria-label', image.alt ? `View larger image: ${image.alt}` : 'View larger image');
    });
}

function getLightbox() {
    const existingLightbox = document.querySelector<HTMLElement>('.image-lightbox');

    if (existingLightbox) {
        return existingLightbox;
    }

    const lightbox = document.createElement('div');
    lightbox.className = 'image-lightbox';
    lightbox.hidden = true;
    lightbox.setAttribute('role', 'dialog');
    lightbox.setAttribute('aria-modal', 'true');
    lightbox.setAttribute('aria-label', 'Image preview');

    const backdrop = document.createElement('div');
    backdrop.className = 'image-lightbox-backdrop';

    const panel = document.createElement('div');
    panel.className = 'image-lightbox-panel';

    const closeButton = document.createElement('button');
    closeButton.type = 'button';
    closeButton.className = 'image-lightbox-close';
    closeButton.setAttribute('aria-label', 'Close enlarged image');

    const image = document.createElement('img');
    image.className = 'image-lightbox-image';
    image.alt = '';

    const caption = document.createElement('p');
    caption.className = 'image-lightbox-caption';
    caption.hidden = true;

    panel.appendChild(closeButton);
    panel.appendChild(image);
    panel.appendChild(caption);
    lightbox.appendChild(backdrop);
    lightbox.appendChild(panel);
    document.body.appendChild(lightbox);

    return lightbox;
}

function openImageLightbox(image: HTMLImageElement) {
    const source = image.currentSrc || image.src;

    if (!source) {
        return;
    }

    const lightbox = getLightbox();
    const lightboxImage = lightbox.querySelector<HTMLImageElement>('.image-lightbox-image');
    const caption = lightbox.querySelector<HTMLElement>('.image-lightbox-caption');
    const closeButton = lightbox.querySelector<HTMLButtonElement>('.image-lightbox-close');
    const captionText = image.alt?.trim() || '';

    if (!lightboxImage || !caption || !closeButton) {
        return;
    }

    activeImageTrigger = image;
    lightboxImage.src = source;
    lightboxImage.alt = image.alt || '';
    caption.textContent = captionText;
    caption.hidden = !captionText;
    lightbox.hidden = false;
    document.body.classList.add('image-lightbox-open');

    window.requestAnimationFrame(() => closeButton.focus({ preventScroll: true }));
}

function closeImageLightbox() {
    const lightbox = document.querySelector<HTMLElement>('.image-lightbox');

    if (!lightbox || lightbox.hidden) {
        return;
    }

    const lightboxImage = lightbox.querySelector<HTMLImageElement>('.image-lightbox-image');

    lightbox.hidden = true;
    document.body.classList.remove('image-lightbox-open');
    lightboxImage?.removeAttribute('src');

    activeImageTrigger?.focus({ preventScroll: true });
    activeImageTrigger = null;
}

function findImageTarget(target: EventTarget | null) {
    if (!(target instanceof Element)) {
        return null;
    }

    return target.closest<HTMLImageElement>('img[data-image-lightbox="true"]');
}

function initializeImageLightboxEvents() {
    if (imageLightboxEventsInitialized) {
        return;
    }

    document.addEventListener('click', event => {
        const lightboxTarget = event.target instanceof Element
            ? event.target.closest('.image-lightbox-backdrop, .image-lightbox-close')
            : null;

        if (lightboxTarget) {
            closeImageLightbox();
            return;
        }

        const image = findImageTarget(event.target);

        if (!image || !isEnlargeableImage(image)) {
            return;
        }

        event.preventDefault();
        openImageLightbox(image);
    });

    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') {
            closeImageLightbox();
            return;
        }

        if (event.key !== 'Enter' && event.key !== ' ') {
            return;
        }

        const image = findImageTarget(event.target);

        if (!image || !isEnlargeableImage(image)) {
            return;
        }

        event.preventDefault();
        openImageLightbox(image);
    });

    imageLightboxEventsInitialized = true;
}

function getCurrentFragmentId() {
    const hash = window.location.hash;

    if (!hash || hash.length <= 1) {
        return '';
    }

    try {
        return decodeURIComponent(hash.substring(1));
    } catch {
        return hash.substring(1);
    }
}

function scrollToCurrentFragment() {
    const elementId = getCurrentFragmentId();

    if (!elementId) {
        lastFragmentScrollKey = '';
        return;
    }

    const scrollKey = `${window.location.pathname}${window.location.search}${window.location.hash}`;

    if (lastFragmentScrollKey === scrollKey) {
        return;
    }

    const element = document.getElementById(elementId);

    if (!element) {
        return;
    }

    lastFragmentScrollKey = scrollKey;
    element.scrollIntoView({
        behavior: 'smooth'
    });
}

export function enhanceContent(root: ParentNode = document) {
    enhanceHeadingAnchors(root);
    enhancePostImages(root);
    initializeImageLightboxEvents();
    scrollToCurrentFragment();
}

function scheduleEnhancement() {
    if (enhanceScheduled) {
        return;
    }

    enhanceScheduled = true;

    window.requestAnimationFrame(() => {
        enhanceScheduled = false;
        enhanceContent();
    });
}

function initializeContentEnhancements() {
    enhanceContent();

    if (observer) {
        return;
    }

    const target = document.getElementById('app') || document.body;

    observer = new MutationObserver(scheduleEnhancement);
    observer.observe(target, {
        childList: true,
        subtree: true
    });
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeContentEnhancements, { once: true });
} else {
    initializeContentEnhancements();
}

window.addEventListener('hashchange', () => {
    lastFragmentScrollKey = '';
    scheduleEnhancement();
});
