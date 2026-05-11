'use strict';

const fs = require('fs');
const path = require('path');
const net = require('net');
const { compileRender } = require('./compile');

const PORT = process.env.PORT || 4000;
const WORK_DIR = process.env.WORK_DIR || '/work';

async function prewarm() {
    try {
        await import('svelte/compiler');
        await import('svelte/server');
    } catch (err) {}
}

function readLengthPrefixed(socket, callback) {
    let lengthBuffer = Buffer.alloc(4);
    let lengthReceived = 0;
    let dataBuffer = null;
    let dataReceived = 0;
    let readingLength = true;

    socket.on('data', (chunk) => {
        let offset = 0;
        while (offset < chunk.length) {
            if (readingLength) {
                const needed = 4 - lengthReceived;
                const available = chunk.length - offset;
                const toRead = Math.min(needed, available);
                chunk.copy(lengthBuffer, lengthReceived, offset, offset + toRead);
                lengthReceived += toRead;
                offset += toRead;

                if (lengthReceived === 4) {
                    const length = lengthBuffer.readInt32LE(0);
                    if (length <= 0 || length > 50 * 1024 * 1024) {
                        callback(new Error('Invalid length'), null);
                        socket.destroy();
                        return;
                    }
                    dataBuffer = Buffer.alloc(length);
                    dataReceived = 0;
                    readingLength = false;
                }
            }
            else {
                const needed = dataBuffer.length - dataReceived;
                const available = chunk.length - offset;
                const toRead = Math.min(needed, available);
                chunk.copy(dataBuffer, dataReceived, offset, offset + toRead);
                dataReceived += toRead;
                offset += toRead;

                if (dataReceived === dataBuffer.length) {
                    const json = dataBuffer.toString('utf8');
                    callback(null, json);
                    lengthBuffer = Buffer.alloc(4);
                    lengthReceived = 0;
                    dataBuffer = null;
                    dataReceived = 0;
                    readingLength = true;
                }
            }
        }
    });
}

function sendLengthPrefixed(socket, json) {
    const data = Buffer.from(json, 'utf8');
    const length = Buffer.alloc(4);
    length.writeInt32LE(data.length, 0);
    socket.write(length);
    socket.write(data);
}

async function handleRequest(json, socket) {
    let payload;
    try {
        payload = JSON.parse(json);
    } catch (e) {
        sendLengthPrefixed(socket, JSON.stringify({ errors: ['Invalid JSON'] }));
        return;
    }

    try {
        const result = await compileRender({
            html: payload?.html,
            css: payload?.css,
            js: payload?.js,
            pages: payload?.pages,
            runId: payload?.runId || `run_${Date.now()}`,
            publicRunBasePath: payload?.publicRunBasePath,
            outputDir: path.join(WORK_DIR, payload?.runId || `run_${Date.now()}`)
        });


        const response = {
            runId: result.runId,
            ssrHtml: result.ssrHtml,
            clientJsUrl: result.clientJsUrl,
            cssUrl: result.cssUrl,
            previewUrl: result.previewUrl,
            previewUrls: result.previewUrls || {},
            ssrHtmls: result.ssrHtmls || {},
            logs: result.logs,
            errors: result.errors,
            artifacts: {},
            previewHtml: null,
            previewHtmls: {}
        };

        const artifactsDir = path.join(result.outputDir, 'artifacts');
        if (fs.existsSync(artifactsDir)) {
            for (const file of fs.readdirSync(artifactsDir)) {
                response.artifacts[file] = fs.readFileSync(path.join(artifactsDir, file), 'utf8');
            }
        }

        for (const name of Object.keys(response.previewUrls)) {
            const htmlPath = path.join(result.outputDir, `${name}.html`);
            if (fs.existsSync(htmlPath)) {
                response.previewHtmls[name] = fs.readFileSync(htmlPath, 'utf8');
            }
        }

        const previewPath = path.join(result.outputDir, 'preview.html');
        if (fs.existsSync(previewPath)) {
            response.previewHtml = fs.readFileSync(previewPath, 'utf8');
        } else if (response.previewHtmls.index) {
            response.previewHtml = response.previewHtmls.index;
        }

        sendLengthPrefixed(socket, JSON.stringify(response));
    } catch (err) {
        sendLengthPrefixed(socket, JSON.stringify({
            runId: payload?.runId || '',
            errors: [err.message]
        }));
    }
}

async function main() {
    await prewarm();

    try {
        const pkgJson = '/app/NodeWorker/package.json';
        const targetPkgJson = path.join(WORK_DIR, 'package.json');
        if (!fs.existsSync(targetPkgJson)) {
            fs.copyFileSync(pkgJson, targetPkgJson);
        }
        
        const nmTarget = path.join(WORK_DIR, 'node_modules');
        if (!fs.existsSync(nmTarget)) {
            fs.symlinkSync('/app/NodeWorker/node_modules', nmTarget, 'dir');
        }
    } catch (err) {}

    const server = net.createServer((socket) => {
        readLengthPrefixed(socket, async (err, json) => {
            if (err) {
                sendLengthPrefixed(socket, JSON.stringify({ errors: [err.message] }));
                return;
            }
            await handleRequest(json, socket);
        });
    });

    server.listen(PORT, '0.0.0.0');

    process.on('SIGTERM', () => {
        server.close(() => process.exit(0));
    });

    process.on('SIGINT', () => {
        server.close(() => process.exit(0));
    });
}

main().catch(err => {
    process.exit(1);
});
