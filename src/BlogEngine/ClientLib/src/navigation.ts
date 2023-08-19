export function scrollToFragment(elementId: string) {
    var element = document.getElementById(elementId);

    if (element) {
        element.scrollIntoView({
            behavior: 'smooth'
        });
    }
}