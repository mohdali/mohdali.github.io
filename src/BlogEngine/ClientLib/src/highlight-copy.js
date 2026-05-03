/**
 *  @file highlight-copy.js
 *  @author Arron Hunt <arronjhunt@gmail.com>
 *  @copyright Copyright 2021. All rights reserved.
 */

/**
 * Adds a copy button to highlightjs code blocks
 */
class CopyButtonPlugin {
    /**
     * Create a new CopyButtonPlugin class instance
     * @param {Object} [options] - Functions that will be called when a copy event fires
     * @param {CopyCallback} [options.callback]
     * @param {Hook} [options.hook]
     * @param {String} [options.lang] Defaults to the document body's lang attribute and falls back to "en"
     */
    constructor(options = {}) {
      this.hook = options.hook;
      this.callback = options.callback;
      this.lang = options.lang || document.documentElement.lang || "en";
    }
    "after:highlightElement"({ el, text }) {
      this.addCopyButton(el, text);
    }

    addCopyButton(el, text = el.textContent || "") {
      if (!el.parentElement) return;

      // Prerendered pages may already contain the button markup, but event
      // listeners are not preserved in static HTML.
      let button = el.parentElement.querySelector(".hljs-copy-button");

      if (!button) {
        button = document.createElement("button");
        button.className = "hljs-copy-button";
        el.parentElement.appendChild(button);
      }

      button.textContent = "Copy";
      button.dataset.copied = "false";
      button.dataset.tooltip = "Copy";
      button.type = "button";
      button.setAttribute("aria-label", "Copy code");
      el.parentElement.classList.add("hljs-copy-wrapper");

      el.parentElement.style.setProperty(
        "--hljs-theme-background",
        window.getComputedStyle(el).backgroundColor
      );

      const hook = this.hook;
      const callback = this.callback;

      button.onclick = function () {
        if (!navigator.clipboard) return;

        let newText = text;
        if (hook && typeof hook === "function") {
          newText = hook(text, el) || text;
        }

        navigator.clipboard
          .writeText(newText)
          .then(function () {
            button.textContent = "Copied!";
            button.dataset.copied = "true";
            button.dataset.tooltip = "Copied!";
            button.setAttribute("aria-label", "Copied to clipboard");

            let alert = document.createElement("div");
            alert.role = "status";
            alert.className = "hljs-copy-alert";
            alert.textContent = "Copied to clipboard";
            el.parentElement.appendChild(alert);

            setTimeout(() => {
              button.textContent = "Copy";
              button.dataset.copied = "false";
              button.dataset.tooltip = "Copy";
              button.setAttribute("aria-label", "Copy code");
              el.parentElement.removeChild(alert);
              alert = null;
            }, 2000);
          })
          .then(function () {
            if (typeof callback === "function") return callback(newText, el);
          });
      };
    }
  }

  export default CopyButtonPlugin

  /**
   * @typedef {function} CopyCallback
   * @param {string} text - The raw text copied to the clipboard.
   * @param {HTMLElement} el - The code block element that was copied from.
   * @returns {undefined}
   */
  /**
   * @typedef {function} Hook
   * @param {string} text - The raw text copied to the clipboard.
   * @param {HTMLElement} el - The code block element that was copied from.
   * @returns {string|undefined}
   */
