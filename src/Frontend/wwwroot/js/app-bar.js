// Handle scroll events for AppBar
let scrollHandler = null;

window.addScrollListener = (dotNetRef) => {
    scrollHandler = () => {
        const scrollY = window.scrollY;
        dotNetRef.invokeMethodAsync('HandleScroll', scrollY);
    };
    
    window.addEventListener('scroll', scrollHandler);
};

window.removeScrollListener = () => {
    if (scrollHandler) {
        window.removeEventListener('scroll', scrollHandler);
    }
};

// Handle keyboard shortcuts
let keyboardHandler = null;

window.addKeyboardShortcut = (dotNetRef) => {
    keyboardHandler = (event) => {
        // Ctrl+K for search
        if (event.ctrlKey && event.key === 'k') {
            event.preventDefault();
            dotNetRef.invokeMethodAsync('HandleKeyboardShortcut', 'ctrl+k');
        }
        // Ctrl+Shift+U for upload
        else if (event.ctrlKey && event.shiftKey && event.key === 'u') {
            event.preventDefault();
            dotNetRef.invokeMethodAsync('HandleKeyboardShortcut', 'ctrl+shift+u');
        }
    };
    
    document.addEventListener('keydown', keyboardHandler);
};

window.removeKeyboardShortcut = () => {
    if (keyboardHandler) {
        document.removeEventListener('keydown', keyboardHandler);
    }
};

// Lazy loading for avatar images
window.lazyLoadImages = () => {
    if ('loading' in HTMLImageElement.prototype) {
        // Browser supports native lazy loading
        const images = document.querySelectorAll('img[data-src]');
        images.forEach(img => {
            img.src = img.dataset.src;
        });
    } else {
        // Fallback for browsers that don't support lazy loading
        const lazyImages = [].slice.call(document.querySelectorAll("img[data-src]"));
        
        if ("IntersectionObserver" in window) {
            let lazyImageObserver = new IntersectionObserver(function(entries, observer) {
                entries.forEach(function(entry) {
                    if (entry.isIntersecting) {
                        let lazyImage = entry.target;
                        lazyImage.src = lazyImage.dataset.src;
                        lazyImage.removeAttribute("data-src");
                        lazyImageObserver.unobserve(lazyImage);
                    }
                });
            });
            
            lazyImages.forEach(function(lazyImage) {
                lazyImageObserver.observe(lazyImage);
            });
        }
    }
};
