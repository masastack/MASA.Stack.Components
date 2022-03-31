window.MasaStackComponents = {}
window.MasaStackComponents.scrollTo = scrollTo;
window.MasaStackComponents.waterFull = waterFull;

function scrollTo(target, inside = 'window') {
    const targetElement = document.querySelector(target)
    if (!targetElement) return;

    const targetRect = targetElement.getBoundingClientRect();

    if (inside === 'window') {
        const scrollTop = document.documentElement.scrollTop;

        let top = targetRect.top + scrollTop;

        window.scrollTo({top, left: 0, behavior: "smooth"});
    } else {
        const insideElement = document.querySelector(inside);
        if (!insideElement) return;

        const scrollTop = insideElement.scrollTop;
        const insideRect = insideElement.getBoundingClientRect();

        let top = targetRect.top + scrollTop - insideRect.top;

        insideElement.scrollTo({top, left: 0, behavior: "smooth"});
    }
}

function waterFull(containerSelector, selectors, columns = 4) {
    const container = document.querySelector(containerSelector);
    const items = document.querySelectorAll(`${containerSelector} ${selectors}`);
    const arr = []
    let width = 0;
    for (let index = 0; index < items.length; index++) {
        const item = items[index];

        if (width === 0) width = item.clientWidth;

        if (index < columns) {
            item.style['top'] = "0px";
            const left = width * index;
            item.style['left'] = `${left}px`;
            arr.push({height: item.clientHeight, left})
        } else {
            arr.sort((x, y) => x.height - y.height);
            const res = arr[0];
            item.style['top'] = `${res.height}px`;
            item.style['left'] = `${res.left}px`;
            res.height = res.height + item.clientHeight;
        }
    }

    arr.sort((x, y) => x.height - y.height)

    const maxHeight = arr[arr.length - 1];
    return maxHeight.height;
}