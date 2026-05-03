type MermaidModule = typeof import('mermaid').default;

let mermaidPromise: Promise<MermaidModule> | undefined;
let configuredTheme = '';
let renderPromise = Promise.resolve();

function getResolvedTheme() {
    return document.documentElement.dataset.theme === 'dark' ? 'dark' : 'light';
}

function getMermaidTheme(theme: string) {
    return theme === 'dark' ? 'dark' : 'default';
}

async function getMermaid(theme: string) {
    if (!mermaidPromise) {
        mermaidPromise = import('mermaid').then(module => module.default);
    }

    const mermaid = await mermaidPromise;

    if (configuredTheme !== theme) {
        mermaid.initialize({
            startOnLoad: false,
            securityLevel: 'strict',
            theme: getMermaidTheme(theme),
            flowchart: {
                useMaxWidth: true,
                htmlLabels: true
            },
            sequence: {
                useMaxWidth: true
            }
        });
        configuredTheme = theme;
    }

    return mermaid;
}

function resetRenderedDiagram(node: HTMLElement) {
    const source = node.dataset.mermaidSource;

    if (!source) {
        node.dataset.mermaidSource = node.textContent?.trim() || '';
        return;
    }

    node.removeAttribute('data-processed');
    node.textContent = source;
}

async function renderMermaidDiagramsCore(force = false) {
    const nodes = Array.from(document.querySelectorAll<HTMLElement>('.mermaid'));

    if (nodes.length === 0) {
        return;
    }

    const theme = getResolvedTheme();
    const mermaid = await getMermaid(theme);
    const nodesToRender = nodes.filter(node => {
        if (force) {
            resetRenderedDiagram(node);
            return true;
        }

        if (node.dataset.processed === 'true') {
            return false;
        }

        resetRenderedDiagram(node);
        return true;
    });

    if (nodesToRender.length === 0) {
        return;
    }

    await mermaid.run({
        nodes: nodesToRender,
        suppressErrors: true
    });

    nodesToRender.forEach(node => {
        node.dataset.mermaidTheme = theme;
    });
}

export function renderMermaidDiagrams(options?: { force?: boolean }) {
    renderPromise = renderPromise
        .then(() => renderMermaidDiagramsCore(options?.force === true))
        .catch(error => {
            console.error('Mermaid rendering failed', error);
        });

    return renderPromise;
}

function renderExistingMermaidDiagrams() {
    if (document.querySelector('.mermaid')) {
        renderMermaidDiagrams({ force: true });
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', renderExistingMermaidDiagrams, { once: true });
} else {
    renderExistingMermaidDiagrams();
}
