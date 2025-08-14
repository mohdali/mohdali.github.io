import hljs from 'highlight.js'
import CopyButtonPlugin from './highlight-copy'
import '../node_modules/highlight.js/styles/dark.css'
import '../node_modules/highlightjs-copy/dist/highlightjs-copy.min.css'

// Initialize the plugin only once
let isPluginInitialized = false;

export function highlightCode() {
    // Only add the plugin once
    if (!isPluginInitialized) {
        hljs.addPlugin(new CopyButtonPlugin());
        isPluginInitialized = true;
    }

    document.querySelectorAll('pre code').forEach((el : HTMLElement) => {
        // Skip if already highlighted
        if (el.classList.contains('hljs')) {
            return;
        }
        hljs.highlightElement(el);
    });
}