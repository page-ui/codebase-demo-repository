'use strict';

const trainFrames = window.trainFrames;
const smokeFrames = window.smokeFrames;
const trainHeight = window.trainHeight;
const trainWidth = window.trainWidth;

const codeBlock = document.getElementById("sl");
const canvas = document.getElementById("canvas");

var trainPosition = { x: 0, y: 0 };
var trainFrameIndex = 0;

var smokePosition = { x: 0, y: 0 };
var smokeFrameIndex = 0;

var frameCounter = 0;

var metrics = {
	characterSize: { width: 0, height: 0 },
	width: 0,
	height: 0,
	lineCount: 0,
	columnCount: 0
};

function clearCanvas() {
	codeBlock.innerText = "".padEnd(metrics.lineCount, "\n");
}

var lastResizeDraw = new Date(0);

function resetMetrics() {
	const computedBody = window.getComputedStyle(document.body, null);
	const width = parseFloat(computedBody.width);
	const height = parseFloat(computedBody.height);

	const computedCodeBlock = window.getComputedStyle(codeBlock, null);
	const context = canvas.getContext("2d");
	context.font = computedCodeBlock.font;

	const measurement = context.measureText("A");
	const charHeight = measurement.actualBoundingBoxAscent -
		measurement.actualBoundingBoxDescent + 4;
	const charWidth = measurement.width;

	metrics = {
		characterSize: { width: charWidth, height: charHeight },
		width: width + charWidth,
		height: height,
		lineCount: Math.floor(height / charHeight) + 1,
		columnCount: Math.floor(width / charWidth) + 1
	};
	codeBlock.style.lineHeight = charHeight + "px";

	if ((new Date() - lastResizeDraw) >= 100) {
		if (lastResizeDraw.getTime() !== 0) {
			draw();
		}
		lastResizeDraw = new Date();
	}
}

window.onresize = function() {
	resetMetrics();
	smokePosition.y = Math.floor((metrics.lineCount / 2) -
		((trainFrames[0].length + smokeFrames[0].length) / 2)) - 3;
	trainPosition.y = smokePosition.y + smokeFrames[0].length;
};

function writeLine(x, y, lines) {
	if (typeof lines === 'string') {
		lines = [lines];
	}
	if (y < 0) return;
	if (x < 0) {
		lines = lines.map(function(text) { return text.slice(-x); });
		x = 0;
	}
	const data = codeBlock.innerText.split("\n");
	if (y >= data.length) return;
	for (let i = 0; i < lines.length; i++) {
		if (data[y + i] == null) break;
		data[y + i] = "".padEnd(x, " ") + lines[i].slice(0, metrics.columnCount - x);
	}
	codeBlock.innerText = data.join("\n");
}

function update() {
	if (metrics.columnCount < 20) return;
	if ((trainPosition.x + trainWidth) < -5) {
		resetTrain();
	}
	trainPosition.x -= 1;
	frameCounter = (frameCounter + 1) % 12;
	trainFrameIndex = (trainFrameIndex + 1) % trainFrames.length;
	if (frameCounter % 4 === 0) {
		smokePosition.x = trainPosition.x + 2;
		smokeFrameIndex = (smokeFrameIndex + 1) % smokeFrames.length;
	}
}

function draw() {
	clearCanvas();
	writeLine(smokePosition.x, smokePosition.y, smokeFrames[smokeFrameIndex]);
	writeLine(trainPosition.x, trainPosition.y, trainFrames[trainFrameIndex]);
}

function resetTrain() {
	trainPosition.x = metrics.columnCount;
	smokePosition.x = trainPosition.x + 2;
}

function initialize() {
	window.onresize();
	resetTrain();
	setInterval(() => {
		update();
		draw();
	}, 50);
}

window.onload = function() {
	initialize();
};
