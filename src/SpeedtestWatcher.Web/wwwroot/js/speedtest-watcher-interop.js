window.speedtestWatcherInterop = {
    getLocalStorage: function (key) {
        return localStorage.getItem(key);
    },

    setLocalStorage: function (key, value) {
        localStorage.setItem(key, value);
    },

    downloadFileFromBytes: function (filename, contentType, bytesBase64) {
        const link = document.createElement('a');
        link.download = filename;
        link.href = `data:${contentType};base64,${bytesBase64}`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    },

    downloadFileFromStream: async function (filename, streamReference) {
        const buffer = await streamReference.arrayBuffer();
        const url = URL.createObjectURL(new Blob([buffer]));
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    },

    copyText: async function (text) {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
            return true;
        }
        const area = document.createElement('textarea');
        area.value = text;
        area.style.position = 'fixed';
        area.style.opacity = '0';
        document.body.appendChild(area);
        area.select();
        const copied = document.execCommand('copy');
        document.body.removeChild(area);
        return copied;
    },

    observeIntersection: function (dotnetHelper, elementId, threshold) {
        const el = document.getElementById(elementId);
        if (!el) return;
        const observer = new IntersectionObserver(entries => {
            entries.forEach(e => {
                if (e.isIntersecting) {
                    dotnetHelper.invokeMethodAsync('OnIntersect');
                }
            });
        }, { rootMargin: '200px', threshold: threshold || 0 });
        observer.observe(el);
    }
};

