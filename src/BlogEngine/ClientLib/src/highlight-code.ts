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

let isCopyPluginRegistered = false;
let areLanguagesRegistered = false;
let copyButtonPlugin: CopyButtonPlugin | null = null;

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

    if (!isCopyPluginRegistered) {
        copyButtonPlugin = new CopyButtonPlugin();
        hljs.addPlugin(copyButtonPlugin);
        isCopyPluginRegistered = true;
    }

    document.querySelectorAll<HTMLElement>('pre code').forEach((el) => {
        if (el.classList.contains('hljs')) {
            copyButtonPlugin?.addCopyButton(el);
            return;
        }
        hljs.highlightElement(el);
    });
}
