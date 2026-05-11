'use strict';

const fs = require('fs');
const path = require('path');
const { pathToFileURL } = require('url');
const esbuild = require('esbuild');
const { performance } = require('perf_hooks');
const { execFile } = require('child_process');
const { promisify } = require('util');
const { JSDOM } = require('jsdom');

const execFileAsync = promisify(execFile);

let compile;
let render;
const WORKER_DIR = path.resolve(__dirname);

async function ensureSvelteLoaded() {
    if (!compile || !render) {
        const compiler = await import('svelte/compiler');
        const server = await import('svelte/server');
        compile = compiler.compile;
        render = server.render;
    }
}

const svelteResolverPlugin = (isSsr = false) => ({
    name: 'svelte-resolver',
    setup(build) {
        build.onResolve({ filter: /^svelte$/ }, () => ({
            path: path.resolve(WORKER_DIR, 'node_modules', 'svelte', 'src', isSsr ? 'index-server.js' : 'index-client.js')
        }));
    }
});

function forceString(val) { return typeof val === 'string' ? val : ''; }
function clean(val) { return forceString(val).trim(); }

function normalizePagePath(value) {
    const pagePath = clean(value || 'index');
    if (!/^[a-z0-9-]+$/.test(pagePath) || pagePath === 'preview') {
        throw new Error(`Invalid page path: ${pagePath}`);
    }

    return pagePath;
}

function normalizePages(input) {
    const sourcePages = Array.isArray(input?.pages) && input.pages.length > 0
        ? input.pages
        : [{ path: 'index', html: input?.html, css: input?.css, js: input?.js }];
    const seen = new Set();
    return sourcePages.map(page => {
        const pagePath = normalizePagePath(page?.path);
        if (seen.has(pagePath)) {
            throw new Error(`Duplicate page path: ${pagePath}`);
        }

        seen.add(pagePath);
        return {
            path: pagePath,
            html: clean(page?.html),
            css: clean(page?.css),
            js: clean(page?.js)
        };
    }).sort((a, b) => a.path.localeCompare(b.path));
}

function extractHtmlDocumentParts(rawHtml) {
    const headMatch = rawHtml.match(/<head[^>]*>([\s\S]*?)<\/head>/i);
    const bodyMatch = rawHtml.match(/<body([^>]*)>([\s\S]*?)<\/body>/i);
    return {
        headHtml: headMatch ? headMatch[1] : '',
        bodyAttrs: bodyMatch ? bodyMatch[1] : '',
        bodyHtml: bodyMatch ? bodyMatch[2] : (headMatch ? rawHtml.replace(/<head[\s\S]*?<\/head>/i, '') : rawHtml)
    };
}

function extractTrailingDocumentSource(rawHtml, logs) {
    const match = forceString(rawHtml).match(/<\/html\s*>\s*([\s\S]+)$/i);
    const trailingSource = match ? match[1].trim() : '';
    if (!trailingSource) {
        return { js: '', css: '' };
    }

    const cssStart = findTrailingCssStart(trailingSource);
    if (cssStart >= 0) {
        const js = trailingSource.slice(0, cssStart).trim();
        const css = trailingSource.slice(cssStart).trim();
        if (js) logs.push('Recovered trailing JavaScript after closing </html>.');
        if (css) logs.push('Recovered trailing CSS after closing </html>.');
        return { js, css };
    }

    if (looksLikeJavaScriptSource(trailingSource)) {
        logs.push('Recovered trailing JavaScript after closing </html>.');
        return { js: trailingSource, css: '' };
    }

    if (looksLikeCssSource(trailingSource)) {
        logs.push('Recovered trailing CSS after closing </html>.');
        return { js: '', css: trailingSource };
    }

    logs.push('Ignored trailing content after closing </html> because it was not recognized as CSS or JavaScript.');
    return { js: '', css: '' };
}

function findTrailingCssStart(source) {
    const candidates = [];
    const markerRegexes = [
        /\/\*\s*=+[\s\S]{0,500}?(?:STYLESHEET|STYLE GUIDE|DESIGN SYSTEM|CSS)\b/i,
        /\/\*[\s\S]{0,500}?\*\/\s*:root\s*\{/i,
        /^\s*:root\s*\{/im
    ];

    for (const regex of markerRegexes) {
        const match = regex.exec(source);
        if (match) {
            candidates.push(match.index);
        }
    }

    return candidates.length > 0 ? Math.min(...candidates) : -1;
}

function looksLikeJavaScriptSource(source) {
    return /\b(?:document|window)\s*\.|\baddEventListener\s*\(|\b(?:const|let|var|function)\s+/.test(source);
}

function looksLikeCssSource(source) {
    return /(?:^|\n)\s*(?:\:root|html\b|body\b|\*|[.#][-_a-zA-Z])[\s\S]{0,160}\{/.test(source);
}

function stripLocalResourceLinks(html) {
    return html.replace(/<link[^>]*(?:href=["'](?!(?:https?:|data:|(?:\/\/)))[^"']*["'])[^>]*>/gi, '')
               .replace(/<script[^>]*(?:src=["'](?!(?:https?:|data:|(?:\/\/)))[^"']*["'])[^>]*><\/script>/gi, '');
}

function allowedStylesheetHosts() {
    return forceString(process.env.PAGE_UI_ALLOWED_STYLESHEET_HOSTS)
        .split(',')
        .map(host => host.trim().toLowerCase())
        .filter(Boolean);
}

function getAttributeRaw(tag, name) {
    const match = tag.match(new RegExp(`\\b${name}\\s*=\\s*["']([^"']+)["']`, 'i'));
    return match ? match[1].trim() : '';
}

function isExternalReference(value) {
    const ref = forceString(value).trim();
    return /^https?:\/\//i.test(ref) || /^\/\//.test(ref) || /^data:/i.test(ref);
}

function isTailwindCdnUrl(value) {
    const ref = forceString(value).trim().toLowerCase();
    return /^https?:\/\/cdn\.tailwindcss\.com(?:\/|$)/.test(ref)
        || /^https?:\/\/cdn\.jsdelivr\.net\/npm\/@tailwindcss\/browser(?:@|\/|$)/.test(ref)
        || /^https?:\/\/unpkg\.com\/@tailwindcss\/browser(?:@|\/|$)/.test(ref);
}

function isGoogleFontsStylesheet(value) {
    const ref = forceString(value).trim().toLowerCase();
    return /^https:\/\/fonts\.googleapis\.com\/css2?(?:\?|$)/.test(ref);
}

function isAllowedExternalStylesheet(value) {
    const hosts = allowedStylesheetHosts();
    if (hosts.length === 0) return false;

    try {
        const url = new URL(value.startsWith('//') ? `https:${value}` : value);
        return hosts.includes(url.hostname.toLowerCase());
    } catch (_) {
        return false;
    }
}

function sanitizeResourceTags(html, logs) {
    let tailwindCdnRequested = false;
    let cleanHtml = forceString(html);

    cleanHtml = cleanHtml.replace(/<script\b[^>]*\bsrc\s*=\s*["']([^"']+)["'][^>]*>\s*<\/script>/gi, (_tag, src) => {
        if (isTailwindCdnUrl(src)) {
            tailwindCdnRequested = true;
            logs.push(`Removed Tailwind CDN script: ${src}`);
        } else if (isExternalReference(src)) {
            logs.push(`Removed external script: ${src}`);
        } else {
            logs.push(`Removed local script reference after source resolution: ${src}`);
        }

        return '';
    });

    cleanHtml = cleanHtml.replace(/<link\b[^>]*>/gi, tag => {
        const href = getAttributeRaw(tag, 'href');
        const rel = getAttributeRaw(tag, 'rel').toLowerCase();
        if (!href || !rel.includes('stylesheet')) return tag;

        if (isTailwindCdnUrl(href)) {
            tailwindCdnRequested = true;
            logs.push(`Removed Tailwind stylesheet reference: ${href}`);
            return '';
        }

        if (isGoogleFontsStylesheet(href)) {
            logs.push(`Kept Google Fonts stylesheet: ${href}`);
            return tag;
        }

        if (isExternalReference(href)) {
            if (isAllowedExternalStylesheet(href)) {
                logs.push(`Kept allowlisted external stylesheet: ${href}`);
                return tag;
            }

            logs.push(`Removed external stylesheet: ${href}`);
            return '';
        }

        logs.push(`Removed local stylesheet reference after source resolution: ${href}`);
        return '';
    });

    return { cleanHtml, tailwindCdnRequested };
}

function extractTailwindConfigScripts(html, logs) {
    const configs = [];
    const cleanHtml = forceString(html).replace(/<script\b(?![^>]*\bsrc\s*=)[^>]*>([\s\S]*?)<\/script>/gi, (match, code) => {
        if (!/\btailwind\s*\.\s*config\s*=/.test(code)) return match;

        configs.push(code);
        logs.push('Removed Tailwind CDN config script after converting supported theme tokens.');
        return '';
    });

    return { cleanHtml, configs };
}

function sanitizeCssTokenName(name) {
    return forceString(name)
        .trim()
        .toLowerCase()
        .replace(/[^a-z0-9-]/g, '-')
        .replace(/-+/g, '-')
        .replace(/^-|-$/g, '');
}

function extractTailwindThemeCss(configScripts, logs) {
    const colors = new Map();
    const colorRegex = /['"]([^'"]+)['"]\s*:\s*['"](#(?:[0-9a-fA-F]{3,8}))['"]/g;
    const fonts = new Map();
    const fontFamilyBlockRegex = /fontFamily\s*:\s*\{([\s\S]*?)\n\s*\}/g;
    const fontFamilyRegex = /['"]?([A-Za-z0-9_-]+)['"]?\s*:\s*\[([^\]]+)\]/g;
    const sizes = new Map();
    const sizeBlockRegex = /\b(spacing|borderRadius|fontSize)\s*:\s*\{([\s\S]*?)\n\s*\}/g;
    const sizeRegex = /['"]?([A-Za-z0-9_-]+)['"]?\s*:\s*['"]([^'"]+)['"]/g;
    const sizeTokenPrefix = new Map([
        ['spacing', '--spacing-'],
        ['borderRadius', '--radius-'],
        ['fontSize', '--text-']
    ]);

    for (const script of configScripts || []) {
        let match;
        while ((match = colorRegex.exec(script)) !== null) {
            const token = sanitizeCssTokenName(match[1]);
            if (!token) continue;

            colors.set(token, match[2]);
        }

        let fontBlockMatch;
        while ((fontBlockMatch = fontFamilyBlockRegex.exec(script)) !== null) {
            let fontMatch;
            while ((fontMatch = fontFamilyRegex.exec(fontBlockMatch[1])) !== null) {
                const token = sanitizeCssTokenName(fontMatch[1]);
                if (!token) continue;

                const family = fontMatch[2]
                    .split(',')
                    .map(part => part.trim())
                    .filter(Boolean)
                    .join(', ');
                if (family) {
                    fonts.set(token, family);
                }
            }
        }

        let sizeBlockMatch;
        while ((sizeBlockMatch = sizeBlockRegex.exec(script)) !== null) {
            const prefix = sizeTokenPrefix.get(sizeBlockMatch[1]);
            if (!prefix) continue;

            let sizeMatch;
            while ((sizeMatch = sizeRegex.exec(sizeBlockMatch[2])) !== null) {
                const token = sanitizeCssTokenName(sizeMatch[1]);
                const value = sizeMatch[2].trim();
                if (!token || !value) continue;

                sizes.set(`${prefix}${token}`, value);
            }
        }
    }

    if (colors.size === 0 && fonts.size === 0 && sizes.size === 0) return '';

    const declarations = [
        ...Array.from(colors.entries()).map(([name, value]) => `  --color-${name}: ${value};`),
        ...Array.from(fonts.entries()).map(([name, value]) => `  --font-${name}: ${value};`),
        ...Array.from(sizes.entries()).map(([name, value]) => `  ${name}: ${value};`)
    ];
    logs.push(`Converted ${colors.size} explicit Tailwind theme color token(s), ${fonts.size} font family token(s), and ${sizes.size} size token(s) from CDN config.`);
    return [
        '@theme {',
        ...declarations,
        '}'
    ].join('\n');
}

function getAttribute(tag, name) {
    const match = tag.match(new RegExp(`\\b${name}\\s*=\\s*["']?([^"'\\s>]+)`, 'i'));
    return match ? match[1].trim().toLowerCase() : '';
}

function isExtractableScript(tag) {
    if (/\bsrc\s*=/i.test(tag)) return false;
    const type = getAttribute(tag, 'type');
    return type === '' || type === 'text/javascript' || type === 'application/javascript';
}

function extractScriptsAndStyles(html) {
    let scriptContent = '';
    let styleContent = '';

    let cleanHtml = html.replace(/<script\b[^>]*>([\s\S]*?)<\/script>/gi, (match, code) => {
        if (!isExtractableScript(match)) return match;
        scriptContent += code.trim() + '\n';
        return '';
    });

    cleanHtml = cleanHtml.replace(/<style\b[^>]*>([\s\S]*?)<\/style>/gi, (_match, css) => {
        styleContent += css.trim() + '\n';
        return '';
    });

    return { cleanHtml, scriptContent, styleContent };
}

function convertInlineHandlers(html) {
    return html.replace(/\bon(\w+)=["']([\s\S]*?)["']/gi, (match, event, handler) => {
        const h = handler.trim();
        if (h.startsWith('{') && h.endsWith('}')) return match;
        const eventName = event.toLowerCase();
        return ` on${eventName}={() => { ${h} }}`;
    });
}

function repairReactStyleAttributeNames(html, logs) {
    const attrMap = new Map([
        ['className', 'class'],
        ['htmlFor', 'for'],
        ['strokeLinecap', 'stroke-linecap'],
        ['strokeLinejoin', 'stroke-linejoin'],
        ['strokeWidth', 'stroke-width'],
        ['strokeMiterlimit', 'stroke-miterlimit'],
        ['fillRule', 'fill-rule'],
        ['clipRule', 'clip-rule'],
        ['clipPath', 'clip-path']
    ]);
    let repaired = forceString(html);
    const repairedAttrs = new Set();

    for (const [from, to] of attrMap.entries()) {
        const regex = new RegExp(`\\b${from}\\s*=`, 'g');
        if (regex.test(repaired)) {
            repairedAttrs.add(`${from}->${to}`);
            repaired = repaired.replace(regex, `${to}=`);
        }
    }

    if (repairedAttrs.size > 0) {
        logs.push(`Normalized React-style attribute(s): ${Array.from(repairedAttrs).join(', ')}.`);
    }

    return repaired;
}

function repairMalformedClassNames(document, logs) {
    let repairedCount = 0;
    document.querySelectorAll('[class]').forEach(element => {
        const original = element.getAttribute('class') || '';
        const repaired = original
            .replace(/\btabular-plan\b/g, 'tabular-nums')
            .replace(/\b(text|bg|border|decoration|outline|ring)-color-/g, '$1-')
            .replace(/\s+/g, ' ')
            .trim();

        if (repaired !== original) {
            element.setAttribute('class', repaired);
            repairedCount += 1;
        }
    });

    if (repairedCount > 0) {
        logs.push(`Normalized malformed class attribute(s) on ${repairedCount} element(s).`);
    }
}

function repairEmptyInteractiveLabels(document, logs) {
    let repairedCount = 0;
    document.querySelectorAll('button, a, [role="button"]').forEach((element, index) => {
        const text = forceString(element.textContent).trim();
        const ariaLabel = forceString(element.getAttribute('aria-label')).trim();
        const title = forceString(element.getAttribute('title')).trim();
        if (text || ariaLabel || title || element.querySelector('svg,img')) return;

        element.textContent = 'Open';
        repairedCount += 1;
        logs.push(`Added fallback text to empty interactive element #${index + 1}.`);
    });

    if (repairedCount > 0) {
        logs.push(`Added fallback labels to ${repairedCount} empty interactive element(s).`);
    }
}

function escapeSvelteTextAndAttributeBraces(document, logs) {
    let textRepairs = 0;
    let attrRepairs = 0;
    const walker = document.createTreeWalker(document.body, document.defaultView.NodeFilter.SHOW_TEXT);
    const textNodes = [];
    while (walker.nextNode()) {
        textNodes.push(walker.currentNode);
    }

    textNodes.forEach(node => {
        const original = node.nodeValue || '';
        if (!/[{}]/.test(original)) return;

        node.nodeValue = original.replace(/\{/g, '&#123;').replace(/\}/g, '&#125;');
        textRepairs += 1;
    });

    document.body.querySelectorAll('*').forEach(element => {
        for (const attr of Array.from(element.attributes)) {
            if (!/[{}]/.test(attr.value)) continue;
            element.setAttribute(attr.name, attr.value.replace(/\{/g, '&#123;').replace(/\}/g, '&#125;'));
            attrRepairs += 1;
        }
    });

    if (textRepairs > 0 || attrRepairs > 0) {
        logs.push(`Escaped Svelte-sensitive brace literal(s) in ${textRepairs} text node(s) and ${attrRepairs} attribute(s).`);
    }
}

function normalizeHtmlForSvelte(html, logs) {
    const attrRepairedHtml = repairReactStyleAttributeNames(html, logs);
    let dom;
    try {
        dom = new JSDOM(`<!DOCTYPE html><body>${attrRepairedHtml}</body>`);
    } catch (error) {
        logs.push(`Skipped DOM HTML repair: ${error.message}`);
        return attrRepairedHtml;
    }

    const { document } = dom.window;
    const before = attrRepairedHtml.trim();

    repairMalformedClassNames(document, logs);
    repairEmptyInteractiveLabels(document, logs);
    escapeSvelteTextAndAttributeBraces(document, logs);

    const normalized = document.body.innerHTML
        .replace(/&amp;#123;/g, '&#123;')
        .replace(/&amp;#125;/g, '&#125;');
    if (normalized.trim() !== before) {
        logs.push('Normalized malformed HTML fragment before Svelte compilation.');
    }

    dom.window.close();
    return normalized;
}

function isAuthorGlobalSelector(selector) {
    const normalized = selector.trim().toLowerCase();
    if (!normalized) return false;

    return normalized === ':root'
        || normalized === 'html'
        || normalized === 'body'
        || normalized === '*'
        || normalized === '*::before'
        || normalized === '*::after'
        || normalized === '*:before'
        || normalized === '*:after'
        || normalized === '::before'
        || normalized === '::after'
        || normalized === '::backdrop'
        || /^(?:button|input|select|textarea|optgroup)(?:\b|:|\[|$)/.test(normalized);
}

function extractAuthorGlobalCss(css, logs) {
    const source = forceString(css);
    const rules = [];
    const ruleRegex = /(^|})\s*([^{}@]+)\s*\{([^{}]*)\}/g;
    let match;

    while ((match = ruleRegex.exec(source)) !== null) {
        const selectors = match[2]
            .split(',')
            .map(part => part.trim())
            .filter(Boolean);
        if (selectors.length === 0 || !selectors.every(isAuthorGlobalSelector)) continue;

        const selector = selectors.join(', ');
        const body = match[3].trim();
        if (!selector || !body) continue;

        rules.push(`${selector} {\n${body}\n}`);
    }

    if (rules.length === 0) return '';

    logs.push(`Preserved ${rules.length} author-global style rule(s) outside CSS layers.`);
    return uniqueCssBlocks(rules);
}

function validateJavaScript(js) {
    if (!forceString(js).trim()) return null;

    try {
        esbuild.transformSync(js, {
            loader: 'js',
            format: 'iife',
            target: 'es2020',
            logLevel: 'silent'
        });
        return null;
    } catch (error) {
        return error;
    }
}

function repairCommonJavaScriptSyntax(js, logs) {
    const source = forceString(js);
    const firstError = validateJavaScript(source);
    if (!firstError) return source;

    let repaired = source;
    repaired = repaired.replace(/([}\]])(\s*\r?\n\s*)(['"`]?[A-Za-z_$][\w$-]*['"`]?\s*:)/g, '$1,$2$3');
    repaired = repaired.replace(/([}\]])(\s*)(['"`]?[A-Za-z_$][\w$-]*['"`]?\s*:)/g, '$1,$2$3');

    if (repaired !== source && !validateJavaScript(repaired)) {
        logs.push('Repaired common JavaScript object-literal comma omission.');
        return repaired;
    }

    logs.push(`JavaScript preflight found unrepaired syntax: ${firstError.message}`);
    return source;
}

function buildBodyAttributes(attrs) {
    const cleanAttrs = attrs.trim();
    return cleanAttrs ? ' ' + cleanAttrs : '';
}

function modernizeSource(html, js) {
    let modernJs = js;
    const modernHtml = html;

    if (modernJs.includes('let') || modernJs.includes('var') || modernJs.includes('const')) {
        if (!modernJs.includes('$state') && !modernJs.includes('$derived')) {
            modernJs = modernJs.replace(/(?:let|var)\s+(\w+)\s*=\s*([^;]+)/g, 'let $1 = $state($2)');
        }
    }

    return { html: modernHtml, js: modernJs };
}

function buildSvelteSource(html, js, css) {
    let svelteSource = '';
    if (js.trim().length > 0 || js.includes('untrack(')) {
        const functionNames = new Set();
        const funcRegexes = [
            /function\s+([a-zA-Z0-9_$]+)\s*\(/g,
            /(?:const|let|var)\s+([a-zA-Z0-9_$]+)\s*=\s*(?:async\s*)?(?:\([^)]*\)|[a-zA-Z0-9_$]+)\s*=>/g,
            /(?:const|let|var)\s+([a-zA-Z0-9_$]+)\s*=\s*(?:async\s*)?function/g
        ];

        funcRegexes.forEach(regex => {
            let match;
            while ((match = regex.exec(js)) !== null) {
                functionNames.add(match[1]);
            }
        });

        const exports = Array.from(functionNames)
            .map(name => `      if (typeof ${name} !== 'undefined') window.${name} = ${name};`)
            .join('\n');
        svelteSource += '<script>\n';
        svelteSource += `  import { untrack, onMount } from 'svelte';\n`;
        svelteSource += `  onMount(() => {\n`;
        svelteSource += `    const originalAddEventListener = document.addEventListener.bind(document);\n`;
        svelteSource += `    document.addEventListener = (type, listener, options) => {\n`;
        svelteSource += `      if (type === 'DOMContentLoaded' && document.readyState !== 'loading' && typeof listener === 'function') {\n`;
        svelteSource += `        queueMicrotask(() => listener.call(document, new Event('DOMContentLoaded')));\n`;
        svelteSource += `        return;\n`;
        svelteSource += `      }\n`;
        svelteSource += `      return originalAddEventListener(type, listener, options);\n`;
        svelteSource += `    };\n`;
        svelteSource += `    try {\n`;
        svelteSource += js + '\n';
        svelteSource += `    } finally {\n`;
        svelteSource += `      document.addEventListener = originalAddEventListener;\n`;
        svelteSource += `    }\n`;
        svelteSource += exports + '\n';
        svelteSource += `  });\n`;
        svelteSource += '</script>\n';
    }

    svelteSource += html + '\n';
    svelteSource += '<style>\n';
    svelteSource += css;
    svelteSource += '\n</style>';
    return svelteSource;
}

function cssHasTailwindDirectives(css) {
    return /@import\s+["']tailwindcss["']/i.test(css)
        || /@tailwind\s+(base|components|utilities)/i.test(css);
}

function removeTailwindDirectives(css) {
    return forceString(css)
        .replace(/@import\s+["']tailwindcss["']\s*;?/gi, '')
        .replace(/@tailwind\s+(base|components|utilities)\s*;?/gi, '');
}

function uniqueCssBlocks(blocks) {
    const seen = new Set();
    return blocks
        .map(block => forceString(block).trim())
        .filter(block => {
            if (!block || seen.has(block)) return false;
            seen.add(block);
            return true;
        })
        .join('\n');
}

function wrapCssLayer(layerName, css) {
    const content = forceString(css).trim();
    return content ? `@layer ${layerName} {\n${content}\n}` : '';
}

function hasTailwindUtilityClasses(text) {
    const utilityRegex = /\b(?:sm:|md:|lg:|xl:|2xl:)?(?:flex|grid|block|hidden|inline-flex|items-(?:center|start|end)|justify-(?:center|between|around|end|start)|min-h-screen|h-screen|w-full|max-w-[a-z0-9-]+|p[trblxy]?-\d+|m[trblxy]?-\d+|gap-\d+|rounded(?:-[a-z0-9]+)?|shadow(?:-[a-z0-9]+)?|bg-[a-z]+-\d+|text-[a-z]+-\d+|font-(?:bold|semibold|medium)|border(?:-[a-z]+-\d+)?|space-[xy]-\d+)\b/g;
    const matches = new Set();
    const classRegex = /\bclass\s*=\s*["']([^"']+)["']/gi;
    let classMatch;
    while ((classMatch = classRegex.exec(text)) !== null) {
        let match;
        while ((match = utilityRegex.exec(classMatch[1])) !== null) {
            matches.add(match[0]);
            if (matches.size >= 4) {
                return true;
            }
        }
    }

    return /\bclass\s*=\s*["'][^"']*(?:sm:|md:|lg:|xl:|2xl:|\[[^\]]+\])/.test(text) && matches.size >= 2;
}

function getTailwindCliPath() {
    return path.join(WORKER_DIR, 'node_modules', '@tailwindcss', 'cli', 'dist', 'index.mjs');
}

async function compileTailwindCss(page, pageInputDir, sourceText, css, logs, explicitTailwind, themeCss = '') {
    const tailwindSignal = explicitTailwind || cssHasTailwindDirectives(css) || hasTailwindUtilityClasses(sourceText);
    if (!tailwindSignal) return '';

    const cliPath = getTailwindCliPath();
    if (!fs.existsSync(cliPath)) {
        const message = 'Tailwind was requested but @tailwindcss/cli is not installed in the render sandbox.';
        if (explicitTailwind || cssHasTailwindDirectives(css)) {
            throw new Error(message);
        }

        logs.push(message);
        return '';
    }

    const tailwindInputCss = path.join(pageInputDir, 'tailwind.input.css');
    const tailwindOutputCss = path.join(pageInputDir, 'tailwind.output.css');
    const tailwindContentHtml = path.join(pageInputDir, 'tailwind.content.html');
    const tailwindContentJs = path.join(pageInputDir, 'tailwind.content.js');
    const tailwindCssPath = path.join(WORKER_DIR, 'node_modules', 'tailwindcss', 'index.css').replace(/\\/g, '/');
    const inputCss = `${themeCss}\n@import "${tailwindCssPath}";\n${removeTailwindDirectives(css)}`;

    fs.writeFileSync(tailwindInputCss, inputCss, 'utf8');
    fs.writeFileSync(tailwindContentHtml, sourceText, 'utf8');
    fs.writeFileSync(tailwindContentJs, page.js || '', 'utf8');
    const contentHtmlPath = tailwindContentHtml.replace(/\\/g, '/');
    const contentJsPath = tailwindContentJs.replace(/\\/g, '/');
    fs.writeFileSync(
        tailwindInputCss,
        `${inputCss}\n@source "${contentHtmlPath}";\n@source "${contentJsPath}";\n`,
        'utf8');

    try {
        const tailwindEnv = { ...process.env };
        delete tailwindEnv.NODE_OPTIONS;
        await execFileAsync(process.execPath, [cliPath, '-i', tailwindInputCss, '-o', tailwindOutputCss, '--minify'], {
            cwd: WORKER_DIR,
            env: tailwindEnv,
            timeout: 15000,
            maxBuffer: 1024 * 1024
        });
    } catch (error) {
        const output = [error.stdout, error.stderr].filter(Boolean).join('\n').trim();
        throw new Error(`Tailwind compilation failed${output ? `: ${output}` : '.'}`);
    }

    const generatedCss = fs.existsSync(tailwindOutputCss)
        ? fs.readFileSync(tailwindOutputCss, 'utf8')
        : '';
    const maxBytes = Number(process.env.PAGE_UI_TAILWIND_MAX_CSS_BYTES || 512000);
    if (Buffer.byteLength(generatedCss, 'utf8') > maxBytes) {
        throw new Error(`Tailwind output exceeded ${maxBytes} bytes.`);
    }

    logs.push(`Compiled Tailwind CSS for page '${page.path}'.`);
    return generatedCss;
}

function extractPublicRunToken(publicRunBasePath) {
    const parts = forceString(publicRunBasePath).split('/').filter(Boolean);
    const runsIndex = parts.indexOf('runs');
    if (runsIndex >= 0 && parts.length > runsIndex + 1) {
        return decodeURIComponent(parts[runsIndex + 1]);
    }

    return parts.length > 0 ? decodeURIComponent(parts[parts.length - 1]) : '';
}

function buildDiagnosticsScript(pagePath, publicRunToken) {
    return `
(function () {
  'use strict';
  var publicRunToken = ${JSON.stringify(publicRunToken)};
  var pagePath = ${JSON.stringify(pagePath)};
  if (!publicRunToken || window.__pageUiRenderDiagnosticsInstalled) return;
  window.__pageUiRenderDiagnosticsInstalled = true;
  var queue = [];
  var seen = new Set();
  var timer = 0;
  var maxEntries = 25;

  function text(value) {
    if (value == null) return '';
    if (value instanceof Error) return value.stack || value.message || String(value);
    if (typeof value === 'object') {
      try { return JSON.stringify(value); } catch (_) { return String(value); }
    }
    return String(value);
  }

  function enqueue(entry) {
    entry.timestamp = new Date().toISOString();
    entry.url = location.href;
    var key = [entry.severity, entry.message, entry.stack, entry.source, entry.line, entry.column].join('|').slice(0, 600);
    if (seen.has(key)) return;
    seen.add(key);
    queue.push(entry);
    if (queue.length > maxEntries) queue.splice(0, queue.length - maxEntries);
    schedule();
  }

  function schedule() {
    if (timer) return;
    timer = window.setTimeout(flush, 1000);
  }

  function flush() {
    timer = 0;
    if (queue.length === 0) return;
    var entries = queue.splice(0, queue.length);
    try {
      fetch('/api/render-diagnostics/report', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'same-origin',
        keepalive: true,
        body: JSON.stringify({ publicRunToken: publicRunToken, pagePath: pagePath, entries: entries })
      }).catch(function () {});
    } catch (_) {}
  }

  window.addEventListener('error', function (event) {
    enqueue({
      severity: 'error',
      message: text(event.message || event.error),
      stack: event.error && event.error.stack ? text(event.error.stack) : '',
      source: event.filename || '',
      line: event.lineno || null,
      column: event.colno || null
    });
  });

  window.addEventListener('unhandledrejection', function (event) {
    enqueue({
      severity: 'unhandledrejection',
      message: text(event.reason),
      stack: event.reason && event.reason.stack ? text(event.reason.stack) : ''
    });
  });

  ['error', 'warn'].forEach(function (level) {
    var original = console[level];
    console[level] = function () {
      var args = Array.prototype.slice.call(arguments);
      enqueue({ severity: level === 'error' ? 'error' : 'log', message: args.map(text).join(' ') });
      return original && original.apply(console, arguments);
    };
  });

  window.__pageUiReportRenderDiagnostic = enqueue;
  window.addEventListener('beforeunload', flush);
}());
`.trim();
}

async function compilePage(page, context) {
    const { outputDir, artifactsDir, publicRunBasePath } = context;
    const pageInputDir = path.join(outputDir, 'input', page.path);
    fs.mkdirSync(pageInputDir, { recursive: true });
    const logs = [];

    const extracted = extractHtmlDocumentParts(page.html);
    const trailingSource = extractTrailingDocumentSource(page.html, logs);
    const sanitizedHead = sanitizeResourceTags(stripLocalResourceLinks(extracted.headHtml), logs);
    const sanitizedBody = sanitizeResourceTags(stripLocalResourceLinks(extracted.bodyHtml), logs);
    const headTailwindConfig = extractTailwindConfigScripts(sanitizedHead.cleanHtml, logs);
    const bodyTailwindConfig = extractTailwindConfigScripts(sanitizedBody.cleanHtml, logs);
    const tailwindThemeCss = extractTailwindThemeCss(
        [...headTailwindConfig.configs, ...bodyTailwindConfig.configs],
        logs);
    const headExtracted = extractScriptsAndStyles(headTailwindConfig.cleanHtml);
    const bodyExtracted = extractScriptsAndStyles(convertInlineHandlers(bodyTailwindConfig.cleanHtml));
    const normalizedBodyHtml = normalizeHtmlForSvelte(bodyExtracted.cleanHtml, logs);
    const cleanedPageCss = removeTailwindDirectives(page.css);
    const combinedJs = [page.js, trailingSource.js, headExtracted.scriptContent, bodyExtracted.scriptContent]
        .filter(Boolean)
        .join('\n');
    const repairedJs = repairCommonJavaScriptSyntax(combinedJs, logs);
    const { html: modernHtml, js: modernJs } = modernizeSource(
        normalizedBodyHtml,
        repairedJs
    );
    const extractedCss = [headExtracted.styleContent, bodyExtracted.styleContent].filter(Boolean).join('\n');
    const svelteCss = [cleanedPageCss, trailingSource.css, extractedCss].filter(Boolean).join('\n');
    const authorGlobalCss = extractAuthorGlobalCss(svelteCss, logs);
    const svelteSource = buildSvelteSource(modernHtml, modernJs, svelteCss);
    const svelteFile = path.resolve(pageInputDir, 'App.svelte');
    fs.writeFileSync(svelteFile, svelteSource, 'utf8');

    let renderedHtml = '';
    let head = '';
    let ssrCss = '';

    if (!process.env.SSR_DISABLED) {
        const ssrResult = compile(svelteSource, {
            filename: `${page.path}.svelte`,
            generate: 'server',
            css: 'external'
        });

        if (ssrResult.errors && ssrResult.errors.length > 0) {
            const errs = ssrResult.errors.map(e => `${e.code}: ${e.message}`).join(', ');
            throw new Error(`Svelte Compiler Errors (${page.path}): ${errs}`);
        }

        ssrCss = forceString(ssrResult.css?.code);
        const ssrEsmFile = path.resolve(pageInputDir, 'ssr.esm.mjs');
        fs.writeFileSync(ssrEsmFile, forceString(ssrResult.js.code), 'utf8');

        const ssrModuleUrl = `${pathToFileURL(ssrEsmFile).href}?v=${Date.now()}`;
        const ssrModule = await import(ssrModuleUrl);
        const App = ssrModule.default || ssrModule;

        const renderResult = render(App, { props: {} });
        renderedHtml = renderResult.html;
        head = renderResult.head;
    }

    const domResult = compile(svelteSource, {
        filename: `${page.path}.svelte`,
        generate: 'client',
        css: 'external'
    });

    const domEsmFile = path.resolve(pageInputDir, 'dom.esm.mjs');
    const clientJsFile = path.resolve(artifactsDir, `${page.path}.client.js`);
    const clientCssFile = path.resolve(artifactsDir, `${page.path}.client.css`);
    const diagnosticsJsFile = path.resolve(artifactsDir, `${page.path}.diagnostics.js`);
    fs.writeFileSync(domEsmFile, forceString(domResult.js.code), 'utf8');

    const tailwindCss = await compileTailwindCss(
        page,
        pageInputDir,
        [extracted.bodyAttrs, modernHtml, headExtracted.cleanHtml, svelteSource].join('\n'),
        [page.css, trailingSource.css].filter(Boolean).join('\n'),
        logs,
        sanitizedHead.tailwindCdnRequested || sanitizedBody.tailwindCdnRequested,
        tailwindThemeCss);
    const componentCss = wrapCssLayer('components', uniqueCssBlocks([
        domResult.css?.code,
        ssrCss
    ]));
    const finalCss = [tailwindCss, authorGlobalCss, componentCss].filter(Boolean).join('\n');
    fs.writeFileSync(clientCssFile, finalCss, 'utf8');
    fs.writeFileSync(diagnosticsJsFile, buildDiagnosticsScript(page.path, extractPublicRunToken(publicRunBasePath)), 'utf8');

    const clientEntry = path.resolve(pageInputDir, 'client-entry.js');
    fs.writeFileSync(clientEntry, [
        `import { hydrate, mount } from 'svelte';`,
        `import App from ${JSON.stringify(domEsmFile)};`,
        `const target = document.getElementById('svelte-root');`,
        `if (target) {`,
        `    try {`,
        `        hydrate(App, { target });`,
        `    } catch (err) {`,
        `        if (window.__pageUiReportRenderDiagnostic) window.__pageUiReportRenderDiagnostic({ severity: 'error', message: 'Svelte hydration failed: ' + (err && err.message ? err.message : String(err)), stack: err && err.stack ? err.stack : '' });`,
        `        try {`,
        `            target.innerHTML = '';`,
        `            mount(App, { target });`,
        `        } catch (inner) {`,
        `            if (window.__pageUiReportRenderDiagnostic) window.__pageUiReportRenderDiagnostic({ severity: 'error', message: 'Svelte mount failed: ' + (inner && inner.message ? inner.message : String(inner)), stack: inner && inner.stack ? inner.stack : '' });`,
        `            const errDiv = document.createElement('div');`,
        `            errDiv.style.cssText = 'position:fixed;top:0;left:0;background:red;color:white;padding:10px;z-index:9999;';`,
        `            errDiv.innerText = 'Svelte Hydration Error: ' + err.message;`,
        `            document.body.appendChild(errDiv);`,
        `        }`,
        `    }`,
        `}`
    ].join('\n'), 'utf8');

    await esbuild.build({
        absWorkingDir: WORKER_DIR,
        entryPoints: [clientEntry],
        bundle: true,
        outfile: clientJsFile,
        format: 'iife',
        platform: 'browser',
        conditions: ['svelte', 'browser', 'import', 'default'],
        mainFields: ['browser', 'module', 'main'],
        plugins: [svelteResolverPlugin()],
        logLevel: 'silent'
    });

    const cssRelPath = `artifacts/${page.path}.client.css`;
    const diagnosticsRelPath = `artifacts/${page.path}.diagnostics.js`;
    const jsRelPath = `artifacts/${page.path}.client.js`;
    const bodyAttrString = buildBodyAttributes(extracted.bodyAttrs);
    const finalHtml = `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>UI Placeholder</title>
  <base href="${publicRunBasePath}/">
  <link rel="stylesheet" href="${cssRelPath}">
  ${forceString(head)}
  ${forceString(headExtracted.cleanHtml)}
</head>
<body${bodyAttrString}>
  <div id="svelte-root">${forceString(renderedHtml)}</div>
  <script src="${diagnosticsRelPath}" defer></script>
  <script src="${jsRelPath}" defer></script>
</body>
</html>`;

    const previewHtmlPath = path.join(outputDir, `${page.path}.html`);
    fs.writeFileSync(previewHtmlPath, finalHtml, 'utf8');

    return {
        path: page.path,
        ssrHtml: renderedHtml,
        previewHtml: finalHtml,
        previewUrl: `${publicRunBasePath}/${page.path}.html`,
        clientJsUrl: `${publicRunBasePath}/artifacts/${page.path}.client.js`,
        cssUrl: `${publicRunBasePath}/artifacts/${page.path}.client.css`,
        logs
    };
}

async function compileRender(input) {
    const start = performance.now();
    const runId = path.basename(forceString(input?.runId || `run_${Date.now()}`));
    const outputDir = path.resolve(forceString(input?.outputDir || path.join(WORKER_DIR, 'runs', runId)));
    const artifactsDir = path.join(outputDir, 'artifacts');
    const publicRunBasePath = clean(input?.publicRunBasePath) || `/runs/${encodeURIComponent(runId)}`;
    const pages = normalizePages(input);

    await ensureSvelteLoaded();

    fs.mkdirSync(path.join(outputDir, 'input'), { recursive: true });
    fs.mkdirSync(artifactsDir, { recursive: true });

    const response = {
        runId,
        outputDir,
        ssrHtml: '',
        clientJsUrl: '',
        cssUrl: '',
        previewUrl: '',
        previewUrls: {},
        ssrHtmls: {},
        logs: [],
        errors: []
    };

    for (const page of pages) {
        const result = await compilePage(page, { outputDir, artifactsDir, publicRunBasePath });
        response.logs.push(...result.logs);
        response.previewUrls[result.path] = result.previewUrl;
        response.ssrHtmls[result.path] = result.ssrHtml;

        if (result.path === 'index' || !response.previewUrl) {
            response.ssrHtml = result.ssrHtml;
            response.clientJsUrl = result.path === 'index' ? `${publicRunBasePath}/artifacts/client.js` : result.clientJsUrl;
            response.cssUrl = result.path === 'index' ? `${publicRunBasePath}/artifacts/client.css` : result.cssUrl;
            response.previewUrl = result.path === 'index' ? `${publicRunBasePath}/preview.html` : result.previewUrl;
        }
    }

    const indexHtmlPath = path.join(outputDir, 'index.html');
    const previewHtmlPath = path.join(outputDir, 'preview.html');
    const indexClientJs = path.join(artifactsDir, 'index.client.js');
    const indexClientCss = path.join(artifactsDir, 'index.client.css');
    if (fs.existsSync(indexHtmlPath)) {
        fs.copyFileSync(indexHtmlPath, previewHtmlPath);
    }
    if (fs.existsSync(indexClientJs)) {
        fs.copyFileSync(indexClientJs, path.join(artifactsDir, 'client.js'));
    }
    if (fs.existsSync(indexClientCss)) {
        fs.copyFileSync(indexClientCss, path.join(artifactsDir, 'client.css'));
    }

    response.logs.push(`Compiled ${pages.length} page(s) in ${(performance.now() - start).toFixed(1)} ms`);
    return response;
}

module.exports = { compileRender };
