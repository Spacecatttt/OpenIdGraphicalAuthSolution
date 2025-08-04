// This function sets up a global click listener to detect clicks outside the specified element.
export function initializeDropdown(dotNetHelper, containerId) {
    const handler = (event) => {
        const container = document.getElementById(containerId);
        // If the dropdown container exists and the click was outside of it,
        // call the .NET method to close the dropdown.
        if (container && !container.contains(event.target)) {
            dotNetHelper.invokeMethodAsync('CloseDropdown');
        }
    };

    document.addEventListener('click', handler, true);
    window.blazorDropdownHandlers = window.blazorDropdownHandlers || {};
    window.blazorDropdownHandlers[containerId] = handler;
}

export function cleanupDropdown(containerId) {
    const handler = window.blazorDropdownHandlers[containerId];
    if (handler) {
        document.removeEventListener('click', handler, true);
        delete window.blazorDropdownHandlers[containerId];
    }
}
