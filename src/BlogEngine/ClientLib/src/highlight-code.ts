import hljs from 'highlight.js/lib/core'
import bash from 'highlight.js/lib/languages/bash'
import csharp from 'highlight.js/lib/languages/csharp'
import css from 'highlight.js/lib/languages/css'
import javascript from 'highlight.js/lib/languages/javascript'
import json from 'highlight.js/lib/languages/json'
import markdown from 'highlight.js/lib/languages/markdown'
import powershell from 'highlight.js/lib/languages/powershell'
import typescript from 'highlight.js/lib/languages/typescript'
import xml from 'highlight.js/lib/languages/xml'
import yaml from 'highlight.js/lib/languages/yaml'
import CopyButtonPlugin from './highlight-copy'
import '../node_modules/highlight.js/styles/dark.css'
import '../node_modules/highlightjs-copy/dist/highlightjs-copy.min.css'

// Initialize the plugin only once
let isPluginInitialized = false;
let areLanguagesRegistered = false;

function registerLanguages() {
    if (areLanguagesRegistered) {
        return;
    }

    hljs.registerLanguage('bash', bash);
    hljs.registerLanguage('shell', bash);
    hljs.registerLanguage('csharp', csharp);
    hljs.registerLanguage('css', css);
    hljs.registerLanguage('javascript', javascript);
    hljs.registerLanguage('json', json);
    hljs.registerLanguage('markdown', markdown);
    hljs.registerLanguage('powershell', powershell);
    hljs.registerLanguage('typescript', typescript);
    hljs.registerLanguage('xml', xml);
    hljs.registerLanguage('razor', xml);
    hljs.registerLanguage('yaml', yaml);
    areLanguagesRegistered = true;
}

export function highlightCode() {
    registerLanguages();

    // Only add the plugin once
    if (!isPluginInitialized) {
        hljs.addPlugin(new CopyButtonPlugin());
        isPluginInitialized = true;
    }

    document.querySelectorAll<HTMLElement>('pre code').forEach((el) => {
        // Skip if already highlighted
        if (el.classList.contains('hljs')) {
            return;
        }
        hljs.highlightElement(el);
    });
}
