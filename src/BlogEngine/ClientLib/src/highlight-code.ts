import hljs from 'highlight.js'
import '../node_modules/highlight.js/styles/dark.css'

export function highlightCode() {
    document.querySelectorAll('pre code').forEach((el : HTMLElement) => {
        hljs.highlightElement(el);
    });
}