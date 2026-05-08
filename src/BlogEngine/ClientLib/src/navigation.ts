import { enhanceContent } from './content-enhancements';

export function scrollToFragment(elementId: string) {
    enhanceContent();

    var element = document.getElementById(elementId);

    if (element) {
        element.scrollIntoView({
            behavior: 'smooth'
        });
    }
}
