// components.js 里与 OIDC 缓存相关的两个函数（hasOidcUser / clearOidcTokenCache）的自动化测试。
// 零依赖，用 Node 内置测试运行器：
//
//   node --test "tests/js/*.test.mjs"
//
// 被测的是仓库里「真实的」wwwroot/js/components.js：它是经典脚本，顶层只是往 window.MasaStackComponents 上挂函数，
// 没有任何 DOM 副作用，所以把源码以 data: URL 的形式动态 import 就能执行；
// window / sessionStorage 用手写的假对象替身（components.js 以裸标识符 sessionStorage 访问它）。

import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const source = readFileSync(
    resolve(here, '../../src/Masa.Stack.Components/wwwroot/js/components.js'),
    'utf8');

const OIDC_USER_KEY = 'oidc.user:https://sso.test:masa-client';

let instance = 0;

// 装一份假 sessionStorage 并执行 components.js，返回挂好的函数、底层数据和写操作计数。
async function load(initial = {}) {
    const data = new Map(Object.entries(initial));
    const writes = { count: 0 };
    Object.defineProperty(globalThis, 'sessionStorage', {
        configurable: true,
        writable: true,
        value: {
            get length() { return data.size; },
            key: (i) => [...data.keys()][i] ?? null,
            getItem: (k) => (data.has(k) ? data.get(k) : null),
            setItem: (k, v) => { writes.count++; data.set(k, String(v)); },
            removeItem: (k) => { writes.count++; data.delete(k); },
        },
    });
    globalThis.window = {};

    const text = `${source}\n//# instance=${instance++}`;
    await import('data:text/javascript;base64,' + Buffer.from(text).toString('base64'));
    return { components: globalThis.window.MasaStackComponents, data, writes };
}

test('hasOidcUser: 没有任何存储项时为 false', async () => {
    const { components } = await load();

    assert.equal(components.hasOidcUser(), false);
});

test('hasOidcUser: 存在 oidc.user:* 键时为 true', async () => {
    const { components } = await load({ [OIDC_USER_KEY]: '{"access_token":"x"}' });

    assert.equal(components.hasOidcUser(), true);
});

test('hasOidcUser: 只有不相干的键（含 oidc 的登录状态键、框架缓存的设置）时为 false', async () => {
    const { components } = await load({
        'oidc.some-state-id': '{}',
        'Microsoft.AspNetCore.Components.WebAssembly.Authentication.CachedAuthSettings': '{}',
        'user:oidc': '{}',
        'other': '1',
    });

    assert.equal(components.hasOidcUser(), false);
});

test('hasOidcUser: 只读——不增删任何存储项', async () => {
    const { components, data, writes } = await load({ [OIDC_USER_KEY]: '{}', other: '1' });
    const before = JSON.stringify([...data.entries()]);

    components.hasOidcUser();

    assert.equal(writes.count, 0);
    assert.equal(JSON.stringify([...data.entries()]), before);
});

test('clearOidcTokenCache 清掉 oidc.user:* 之后 hasOidcUser 为 false，且不动其它键', async () => {
    const { components, data } = await load({
        [OIDC_USER_KEY]: '{}',
        'oidc.user:https://sso.test:another-client': '{}',
        other: '1',
    });
    assert.equal(components.hasOidcUser(), true);

    components.clearOidcTokenCache();

    assert.equal(components.hasOidcUser(), false);
    assert.deepEqual([...data.keys()], ['other']);
});
