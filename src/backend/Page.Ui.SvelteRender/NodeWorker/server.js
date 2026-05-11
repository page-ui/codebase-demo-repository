'use strict';

const express = require('express');
const fs = require('fs');
const path = require('path');
const compression = require('compression');
const { compileRender } = require('./compile');

const app = express();
const isProduction = (process.env.NODE_ENV || 'development').toLowerCase() === 'production';
app.disable('x-powered-by');
app.use(express.json({ limit: '2mb' }));
app.use(compression());

const PORT = process.env.PORT || 3000;
const WORKER_DIR = __dirname;
const RUNS_DIR = process.env.RUNS_DIR
    ? path.resolve(process.env.RUNS_DIR)
    : path.join(WORKER_DIR, 'runs');

if (!fs.existsSync(RUNS_DIR)) fs.mkdirSync(RUNS_DIR, { recursive: true });

app.get('/health', (_req, res) => res.sendStatus(200));

app.post('/compile', async (req, res) => {
    const forceString = val => {
        if (val === null || val === undefined) return '';
        if (typeof val === 'string') return val;
        return String(val);
    };

    const runId = path.basename(forceString(req.body.runId || `run_${Date.now()}`));
    const runDir = path.join(RUNS_DIR, runId);
    let dirCreated = false;

    try {
        fs.mkdirSync(runDir, { recursive: true });
        dirCreated = true;

        const result = await compileRender({
            html: req.body.html,
            css: req.body.css,
            js: req.body.js,
            pages: req.body.pages,
            runId,
            outputDir: runDir
        });

        res.json(result);
    } catch (error) {
        console.error(`[Worker:${PORT}] Fatal Error:`, error);

        if (dirCreated && runDir && fs.existsSync(runDir)) {
            try { fs.rmSync(runDir, { recursive: true, force: true }); } catch (_) { }
        }

        const message = error && error.message ? error.message : String(error);
        const stack = error && error.stack ? error.stack : '';
        const errors = [message];
        if (!isProduction && stack) {
            errors.push(stack);
        }

        res.status(500).json({
            runId,
            ssrHtml: '',
            clientJsUrl: '',
            cssUrl: '',
            previewUrl: '',
            logs: [],
            errors
        });
    }
});

app.listen(PORT, '127.0.0.1');
