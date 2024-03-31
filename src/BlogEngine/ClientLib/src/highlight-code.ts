import hljs from 'highlight.js'
import CopyButtonPlugin from './highlight-copy'
import '../node_modules/highlight.js/styles/dark.css'
import '../node_modules/highlightjs-copy/dist/highlightjs-copy.min.css'

export function highlightCode() {
    hljs.addPlugin(new CopyButtonPlugin());

    document.querySelectorAll('pre code').forEach((el : HTMLElement) => {
        hljs.highlightElement(el);
    });
}