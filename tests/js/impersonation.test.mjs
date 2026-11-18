// impersonation.js（接收端 JS 桥）的自动化测试。零依赖，用 Node 内置测试运行器：
//
//   node --test "tests/js/*.test.mjs"
//
// 被测的是仓库里「真实的」wwwroot/js/impersonation.js：把源码以 data: URL 的形式动态 import，
// 每个用例拿到一份全新的模块实例（订阅表、id 计数器互不影响），window 用手写的假对象替身。

import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const source = readFileSync(
    resolve(here, '../../src/Masa.Stack.Components/wwwroot/js/impersonation.js'),
    'utf8');

let instance = 0;
async function loadModule() {
    const text = `${source}\n//# instance=${instance++}`;
    return import('data:text/javascript;base64,' + Buffer.from(text).toString('base64'));
}

function memoryStorage(initial = {}) {
    const data = new Map(Object.entries(initial));
    return {
        getItem: (k) => (data.has(k) ? data.get(k) : null),
        setItem: (k, v) => { data.set(k, String(v)); },
        removeItem: (k) => { data.delete(k); },
        _data: data,
    };
}

// 构造一个假 window 并挂到 globalThis。fire(e) 向已注册的 storage 监听器派发事件。
function installWindow({ hash = '', pathname = '/', search = '', fetch, storage = memoryStorage() } = {}) {
    const storageListeners = new Set();
    const replaceCalls = [];
    const w = {
        location: { hash, pathname, search },
        history: {
            state: null,
            replaceState(_state, _title, url) {
                replaceCalls.push(url);
                const i = url.indexOf('#');
                w.location.hash = i >= 0 ? url.substring(i) : '';
            },
        },
        localStorage: storage,
        addEventListener(type, handler) { if (type === 'storage') storageListeners.add(handler); },
        removeEventListener(type, handler) { if (type === 'storage') storageListeners.delete(handler); },
        fetch,
    };
    globalThis.window = w;
    return {
        w,
        replaceCalls,
        storage,
        listenerCount: () => storageListeners.size,
        fire: (event) => { for (const handler of [...storageListeners]) handler(event); },
    };
}

const KEY = 'masa.impersonation.session';
const tick = (ms = 20) => new Promise((resolve) => setTimeout(resolve, ms));

// ---------------------------------------------------------------- captureFragment

test('captureFragment: 识别 OAuth 标准参数名 access_token / expires_in，并立刻抹除地址栏', async () => {
    const mod = await loadModule();
    const env = installWindow({ hash: '#access_token=AAA.BBB.CCC&expires_in=600', pathname: '/pm/projects' });

    const result = mod.captureFragment();

    assert.deepEqual(result, { accessToken: 'AAA.BBB.CCC', expiresIn: 600 });
    assert.equal(env.w.location.hash, '');
    assert.deepEqual(env.replaceCalls, ['/pm/projects']);
});

test('captureFragment: 不带 expires_in 时默认 900 秒', async () => {
    const mod = await loadModule();
    installWindow({ hash: '#access_token=AAA' });

    assert.deepEqual(mod.captureFragment(), { accessToken: 'AAA', expiresIn: 900 });
});

test('captureFragment: 只抹除自己的参数，保留 query 与 hash 路由里的其它参数', async () => {
    const mod = await loadModule();
    const env = installWindow({
        hash: '#/orders?tab=2&access_token=AAA&expires_in=60',
        pathname: '/app',
        search: '?lang=zh',
    });

    const result = mod.captureFragment();

    assert.deepEqual(result, { accessToken: 'AAA', expiresIn: 60 });
    assert.equal(env.w.location.hash, '#/orders?tab=2');
    assert.deepEqual(env.replaceCalls, ['/app?lang=zh#/orders?tab=2']);
});

for (const bad of ['abc', '-5', '0']) {
    test(`captureFragment: expires_in=${bad} 非法时返回 null，但 token 仍必须从地址栏抹除`, async () => {
        const mod = await loadModule();
        const env = installWindow({ hash: `#access_token=SECRET&expires_in=${bad}` });

        const result = mod.captureFragment();

        assert.equal(result, null);
        assert.equal(env.w.location.hash, '', 'token 不能留在地址栏里');
        assert.equal(env.replaceCalls.length, 1);
    });
}

test('captureFragment: OIDC 回调路由（/authentication/*）一律不消费，也不改写 URL', async () => {
    const mod = await loadModule();
    const env = installWindow({
        hash: '#access_token=AAA&expires_in=600',
        pathname: '/Authentication/login-callback',
    });

    assert.equal(mod.captureFragment(), null);
    assert.equal(env.replaceCalls.length, 0);
});

for (const marker of ['id_token=X', 'state=Y']) {
    test(`captureFragment: 带 ${marker.split('=')[0]} 的 fragment 是 implicit 回调形态，不消费`, async () => {
        const mod = await loadModule();
        const env = installWindow({ hash: `#access_token=AAA&${marker}` });

        assert.equal(mod.captureFragment(), null);
        assert.equal(env.replaceCalls.length, 0);
    });
}

test('captureFragment: 没有 fragment 或没有 access_token 时返回 null，不改写 URL', async () => {
    const mod = await loadModule();

    let env = installWindow({ hash: '' });
    assert.equal(mod.captureFragment(), null);
    assert.equal(env.replaceCalls.length, 0);

    env = installWindow({ hash: '#section-2' });
    assert.equal(mod.captureFragment(), null);
    assert.equal(env.replaceCalls.length, 0);
});

// ---------------------------------------------------------------- probeUserInfo

function fakeResponse(status, body) {
    return {
        status,
        ok: status >= 200 && status < 300,
        json: async () => {
            if (body instanceof Error) throw body;
            return body;
        },
    };
}

test('probeUserInfo: 200 返回 status 与 userinfo 的 sub；请求只带 Bearer，不带 cookie', async () => {
    const mod = await loadModule();
    const requests = [];
    installWindow({
        fetch: async (url, init) => {
            requests.push({ url, init });
            return fakeResponse(200, { sub: 'target-guid', name: 'x' });
        },
    });

    const result = await mod.probeUserInfo('https://sso.test/connect/userinfo', 'TOKEN', 5000);

    assert.deepEqual(result, { status: 200, sub: 'target-guid' });
    assert.equal(requests.length, 1);
    assert.equal(requests[0].url, 'https://sso.test/connect/userinfo');
    assert.equal(requests[0].init.method, 'GET');
    assert.equal(requests[0].init.headers.Authorization, 'Bearer TOKEN');
    assert.equal(requests[0].init.credentials, 'omit');
});

test('probeUserInfo: 非 200 原样返回状态码，sub 为空', async () => {
    const mod = await loadModule();
    installWindow({ fetch: async () => fakeResponse(401, { sub: 'ignored' }) });

    assert.deepEqual(await mod.probeUserInfo('u', 'T', 5000), { status: 401, sub: '' });
});

test('probeUserInfo: 200 但响应不是 JSON / 没有 sub 时 sub 为空', async () => {
    const mod = await loadModule();

    installWindow({ fetch: async () => fakeResponse(200, new SyntaxError('not json')) });
    assert.deepEqual(await mod.probeUserInfo('u', 'T', 5000), { status: 200, sub: '' });

    installWindow({ fetch: async () => fakeResponse(200, { name: 'no sub' }) });
    assert.deepEqual(await mod.probeUserInfo('u', 'T', 5000), { status: 200, sub: '' });

    installWindow({ fetch: async () => fakeResponse(200, { sub: 123 }) });
    assert.deepEqual(await mod.probeUserInfo('u', 'T', 5000), { status: 200, sub: '' });
});

test('probeUserInfo: 网络 / CORS 错误统一返回 status 0，不抛异常', async () => {
    const mod = await loadModule();
    installWindow({ fetch: async () => { throw new TypeError('Failed to fetch'); } });

    assert.deepEqual(await mod.probeUserInfo('u', 'T', 5000), { status: 0, sub: '' });
});

test('probeUserInfo: 超时会中止请求并返回 status 0', async () => {
    const mod = await loadModule();
    let aborted = false;
    installWindow({
        fetch: (_url, init) => new Promise((_resolve, reject) => {
            init.signal.addEventListener('abort', () => {
                aborted = true;
                reject(new DOMException('aborted', 'AbortError'));
            });
        }),
    });

    const result = await mod.probeUserInfo('u', 'T', 30);

    assert.deepEqual(result, { status: 0, sub: '' });
    assert.equal(aborted, true);
});

// ---------------------------------------------------------------- 跨 tab 订阅

test('subscribeStorage: 每次订阅返回不同的正整数 id，多个订阅者都能收到通知', async () => {
    const mod = await loadModule();
    const env = installWindow();
    const calls = [];
    const refA = { invokeMethodAsync: async (m) => { calls.push(['A', m]); } };
    const refB = { invokeMethodAsync: async (m) => { calls.push(['B', m]); } };

    const idA = mod.subscribeStorage(KEY, refA);
    const idB = mod.subscribeStorage(KEY, refB);

    assert.ok(Number.isInteger(idA) && idA > 0);
    assert.ok(Number.isInteger(idB) && idB > 0);
    assert.notEqual(idA, idB);
    assert.equal(env.listenerCount(), 2);

    env.fire({ key: KEY });
    await tick();
    assert.deepEqual(calls.sort(), [['A', 'OnStorageChanged'], ['B', 'OnStorageChanged']]);
});

test('subscribeStorage: 旧订阅者退订不会摘掉新订阅者（订阅互不顶替）', async () => {
    const mod = await loadModule();
    const env = installWindow();
    const calls = [];
    const idOld = mod.subscribeStorage(KEY, { invokeMethodAsync: async () => { calls.push('old'); } });
    mod.subscribeStorage(KEY, { invokeMethodAsync: async () => { calls.push('new'); } });

    mod.unsubscribeStorage(idOld); // 旧布局的 DisposeAsync 才执行

    assert.equal(env.listenerCount(), 1);
    env.fire({ key: KEY });
    await tick();
    assert.deepEqual(calls, ['new'], '新订阅者的跨 tab 同步必须仍然有效');
});

test('unsubscribeStorage: 重复退订 / 未知 id 是空操作', async () => {
    const mod = await loadModule();
    const env = installWindow();
    const id = mod.subscribeStorage(KEY, { invokeMethodAsync: async () => { } });

    mod.unsubscribeStorage(id);
    mod.unsubscribeStorage(id);
    mod.unsubscribeStorage(99999);

    assert.equal(env.listenerCount(), 0);
});

test('subscribeStorage: 只响应自己的 key；key 为 null（localStorage.clear）也要响应', async () => {
    const mod = await loadModule();
    const env = installWindow();
    let count = 0;
    mod.subscribeStorage(KEY, { invokeMethodAsync: async () => { count++; } });

    env.fire({ key: 'some.other.key' });
    await tick();
    assert.equal(count, 0);

    env.fire({ key: null });
    await tick();
    assert.equal(count, 1);
});

test('subscribeStorage: 回调对象已释放（invokeMethodAsync 返回 rejected Promise）不会产生 unhandled rejection', async () => {
    const mod = await loadModule();
    const env = installWindow();
    let unhandled = 0;
    const onUnhandled = () => { unhandled++; };
    process.on('unhandledRejection', onUnhandled);
    try {
        mod.subscribeStorage(KEY, {
            invokeMethodAsync: () => Promise.reject(new Error('There is no tracked object with id 1')),
        });

        env.fire({ key: KEY });
        await tick(50);

        assert.equal(unhandled, 0);
    } finally {
        process.off('unhandledRejection', onUnhandled);
    }
});

test('subscribeStorage: invokeMethodAsync 同步抛出也被吞掉，不影响其它订阅者', async () => {
    const mod = await loadModule();
    const env = installWindow();
    let healthy = 0;
    mod.subscribeStorage(KEY, { invokeMethodAsync: () => { throw new Error('disposed'); } });
    mod.subscribeStorage(KEY, { invokeMethodAsync: async () => { healthy++; } });

    assert.doesNotThrow(() => env.fire({ key: KEY }));
    await tick();
    assert.equal(healthy, 1);
});

// ---------------------------------------------------------------- localStorage 读写

test('readSession / writeSession / clearSession 往返；损坏的 JSON 读成 null', async () => {
    const mod = await loadModule();
    const env = installWindow();

    assert.equal(mod.readSession(KEY), null);

    mod.writeSession(KEY, { accessToken: 'AAA', expiresAtUnixSeconds: 1 });
    assert.deepEqual(mod.readSession(KEY), { accessToken: 'AAA', expiresAtUnixSeconds: 1 });

    env.storage.setItem(KEY, '{not json');
    assert.equal(mod.readSession(KEY), null);

    mod.clearSession(KEY);
    assert.equal(env.storage.getItem(KEY), null);
});

test('writeSession: 存储被禁用 / 配额不足时不抛给 .NET', async () => {
    const mod = await loadModule();
    installWindow({
        storage: {
            getItem: () => { throw new Error('denied'); },
            setItem: () => { throw new Error('quota'); },
            removeItem: () => { throw new Error('denied'); },
        },
    });

    assert.doesNotThrow(() => mod.writeSession(KEY, { a: 1 }));
    assert.doesNotThrow(() => mod.clearSession(KEY));
    assert.equal(mod.readSession(KEY), null);
});
