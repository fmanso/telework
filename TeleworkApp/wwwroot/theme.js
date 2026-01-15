window.applyTheme = (themeClass) => {
    // Don't wipe existing classes; just toggle dark-theme.
    if (themeClass === 'dark-theme') {
        document.body.classList.add('dark-theme');
    } else {
        document.body.classList.remove('dark-theme');
    }
};

window.loadTheme = () => {
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme === 'dark') {
        document.body.classList.add('dark-theme');
    } else {
        document.body.classList.remove('dark-theme');
    }
};

// Apply theme on load
window.loadTheme();