window.MasaStackComponents = {}

window.MasaStackComponents.scrollTo = (target, inside = 'window') => {
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

window.MasaStackComponents.waterFull = (containerSelector, selectors, columns = 4) => {
    const container = document.querySelector(containerSelector);
    const items = document.querySelectorAll(`${containerSelector} ${selectors}`);
    const arr = []
    let width = 0;
    for (let index = 0; index < items.length; index++) {
        const item = items[index];

        if (width === 0) width = item.clientWidth;

        if (index < columns) {
            item.style['position'] = 'absolute';
            item.style['top'] = "0px";
            const left = width * index;
            item.style['left'] = `${left}px`;
            arr.push({height: item.clientHeight, left})
        } else {
            arr.sort((x, y) => x.height - y.height);
            const res = arr[0];
            item.style['position'] = 'absolute';
            item.style['top'] = `${res.height}px`;
            item.style['left'] = `${res.left}px`;
            res.height = res.height + item.clientHeight;
        }
    }

    arr.sort((x, y) => x.height - y.height)

    const maxHeight = arr[arr.length - 1];
    return maxHeight.height;
}

window.MasaStackComponents.listenScroll = (selector, childSelectors, dotNet) => {
    let el = document.querySelector(selector);
    if (!el) return;

    const children = document.querySelectorAll(`${selector} ${childSelectors}`);

    let fn = debounce((position) => {
        const elTop = el.offsetTop;

        const childrenTops = []
        for (const child of children) {
            childrenTops.push(child.offsetTop)
        }

        const computedChildrenTops = childrenTops.map(child => child - elTop - 8);

        let index = computedChildrenTops.findIndex(child => child >= position);

        if (index === -1) {
            index = computedChildrenTops.length - 1;
        } else if (index > 0) {
            index--;
        }

        dotNet.invokeMethodAsync("ComputeActiveCategory", index)
    }, 100)

    el.addEventListener("scroll", function (e) {
        fn(e.target.scrollTop)
    })
}

window.MasaStackComponents.resizeObserver = (selector, invoker) => {
    const resizeObserver = new ResizeObserver((entries => {
        invoker.invokeMethodAsync('Invoke');
    }));
    resizeObserver.observe(document.querySelector(selector));
}

function debounce(fn, wait) {
    let timer = null;
    return function (...args) {
        if (timer) clearTimeout(timer);
        timer = setTimeout(() => {
            fn.apply(this, args)
        }, wait)
    }
}