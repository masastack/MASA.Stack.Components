/*
 * MASA Stack impersonation (token-exchange) receiver-side bridge.
 *
 * 加载方式：由 ImpersonationSession 通过 IJSRuntime 模块导入
 *   await JS.InvokeAsync<IJSObjectReference>("import",
 *       "./_content/Masa.Stack.Components/js/impersonation.js")
 * 不需要宿主页额外添加 <script> 标签（与 js/http.js 的用法一致）。
 *
 * 发送端（Auth 管理后台）以 URL fragment 追加
 *   #access_token=...&expires_in=...
 * 参数名用 OAuth 2.0 标准名（RFC 6749 §4.2.2 / RFC 8693 响应字段），与后端文档 docs/sso-impersonation.md 一致。
 * fragment 从不发送到服务器；接收端读取后立即抹除，避免 token 留在地址栏/历史/书签里。
 *
 * 标准名与 OIDC implicit 回调 fragment 同名，所以只在「不是 OIDC 回调」时消费：
 * 回调路由（/authentication/*）和带有 id_token / state 的 fragment 一律跳过，交给 oidc-client 处理。
 * 本应用走 authorization code 流程，正常回调里不会出现 #access_token，这里只是防御。
 *
 * 自动化测试：tests/js/impersonation.test.mjs（node --test "tests/js/*.test.mjs"）。
 */

const ACCESS_TOKEN_KEY = 'access_token';
const EXPIRES_IN_KEY = 'expires_in';
const DEFAULT_EXPIRES_IN_SECONDS = 900;

// 解析 location.hash 中的查询串。同时支持
// "#access_token=x" 与 hash 路由型 SPA "#/path?foo=1&access_token=x"。
function parseHashQuery() {
    const hash = window.location.hash || '';
    const raw = hash.startsWith('#') ? hash.substring(1) : hash;
    if (!raw) return null;
    const qIndex = raw.indexOf('?');
    const path = qIndex >= 0 ? raw.substring(0, qIndex) : '';
    const pairs = qIndex >= 0 ? raw.substring(qIndex + 1) : raw;
    return { path: path, params: new URLSearchParams(pairs) };
}

// 读取并从当前 URL 中移除模拟 token（仅 fragment）。
// 返回 { accessToken, expiresIn } 或 null。URL 用 history.replaceState 就地重写，
// token 不会残留在地址栏 / 浏览器历史 / 收藏夹中。
//
// 顺序有讲究：只要识别出这是一次模拟 token 交付，就**先抹除 URL，再校验参数**。
// 否则 expires_in 非法（手工拼的畸形链接）时会返回 null 却把 token 留在地址栏里。
export function captureFragment() {
    // OIDC 回调路由：fragment 归 oidc-client 管，绝不消费
    if ((window.location.pathname || '').toLowerCase().includes('/authentication/')) return null;

    const parsed = parseHashQuery();
    if (!parsed) return null;
    // implicit 回调形态（IdP 一并返回 id_token / state）：不是模拟交付，跳过
    if (parsed.params.has('id_token') || parsed.params.has('state')) return null;
    const accessToken = parsed.params.get(ACCESS_TOKEN_KEY);
    if (!accessToken) return null;
    const expiresInRaw = parsed.params.get(EXPIRES_IN_KEY);
    parsed.params.delete(ACCESS_TOKEN_KEY);
    parsed.params.delete(EXPIRES_IN_KEY);
    const qs = parsed.params.toString();
    const newRaw = qs ? (parsed.path ? parsed.path + '?' + qs : qs) : parsed.path;
    const newHash = newRaw ? '#' + newRaw : '';
    window.history.replaceState(
        window.history.state,
        '',
        window.location.pathname + window.location.search + newHash);

    const expiresIn = parseInt(expiresInRaw || String(DEFAULT_EXPIRES_IN_SECONDS), 10);
    if (isNaN(expiresIn) || expiresIn <= 0) return null;
    return { accessToken: accessToken, expiresIn: expiresIn };
}

// 向 SSO 的 userinfo 端点出示 token，由 SSO 校验签名/有效期/act（WASM 里无法本地验签）。
// 只返回「事实」，不做裁决：{ status, sub }。
//  - status：HTTP 状态码；网络错误、CORS 失败、超时统一为 0
//  - sub：200 时 userinfo 响应里的 sub，其余为空串
// credentials:'omit'：只靠 Bearer，不携带任何 cookie。
export async function probeUserInfo(url, accessToken, timeoutMs) {
    const controller = typeof AbortController === 'function' ? new AbortController() : null;
    const timer = controller && timeoutMs > 0 ? setTimeout(() => controller.abort(), timeoutMs) : null;
    try {
        const response = await window.fetch(url, {
            method: 'GET',
            headers: { Authorization: 'Bearer ' + accessToken, Accept: 'application/json' },
            credentials: 'omit',
            cache: 'no-store',
            signal: controller ? controller.signal : undefined,
        });
        let sub = '';
        if (response.ok) {
            try {
                const body = await response.json();
                sub = body && typeof body.sub === 'string' ? body.sub : '';
            } catch (e) {
                // 200 但不是 JSON：sub 留空，由调用方判定为校验失败
            }
        }
        return { status: response.status, sub: sub };
    } catch (e) {
        return { status: 0, sub: '' };
    } finally {
        if (timer) clearTimeout(timer);
    }
}

export function readSession(key) {
    try {
        const json = window.localStorage.getItem(key);
        return json ? JSON.parse(json) : null;
    } catch (e) {
        return null;
    }
}

export function writeSession(key, value) {
    try {
        window.localStorage.setItem(key, JSON.stringify(value));
    } catch (e) {
        // 配额不足 / 存储被禁用：会话是 best-effort，不抛给 .NET
    }
}

export function clearSession(key) {
    try {
        window.localStorage.removeItem(key);
    } catch (e) {
    }
}

// ---- 跨 tab 同步：用 storage 事件替代每秒轮询 localStorage ----
// storage 事件只会在「其它」标签页写入/删除时触发（本 tab 的写入不会回调自己），
// 正好符合「其它 tab 结束了模拟，本 tab 跟着回落」的语义。
//
// 每个订阅者有自己的 id：多个订阅者（同一文档里叠挂的多个布局）互不顶替，
// 退订时只摘掉自己的监听器。
const storageSubscriptions = new Map(); // id -> handler
let nextSubscriptionId = 1;

// 返回订阅 id（>0），退订时回传。
export function subscribeStorage(key, dotNetRef) {
    const handler = (e) => {
        // e.key === null 表示 localStorage.clear() 被调用
        if (e.key !== key && e.key !== null) return;
        try {
            // invokeMethodAsync 在对象已释放 / circuit 断开时返回 rejected Promise（不是同步抛），
            // 不接住就会变成 unhandled rejection。
            Promise.resolve(dotNetRef.invokeMethodAsync('OnStorageChanged')).catch(() => { });
        } catch (err) {
            // 同步抛出的情形：同样无需处理
        }
    };
    const id = nextSubscriptionId++;
    window.addEventListener('storage', handler);
    storageSubscriptions.set(id, handler);
    return id;
}

export function unsubscribeStorage(id) {
    const handler = storageSubscriptions.get(id);
    if (handler) {
        window.removeEventListener('storage', handler);
        storageSubscriptions.delete(id);
    }
}
