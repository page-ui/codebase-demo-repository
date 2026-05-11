using System.Net;

namespace Page.Ui.Worker.Ai.Services;

internal static class WorkerRenderTemplates
{
    public static string BuildRetroCliHtml(string promptText, string createdAtUtc, string shortChatId, string shortMessageId)
    {
        var safePrompt = WebUtility.HtmlEncode(promptText);
        return $"""
<main class="retro-scene">
  <div class="retro-grid" aria-hidden="true"></div>
  <div class="retro-glow" aria-hidden="true"></div>

  <header class="top-shell">
    <div class="title-wrap">
      <span class="badge">PAGE.UI // AI CONSOLE</span>
      <h1>Model_Error</h1>
    </div>
    <div class="meta-wrap">
      <span>CHAT {shortChatId}</span>
      <span>MSG {shortMessageId}</span>
      <span>{createdAtUtc}</span>
    </div>
  </header>

  <section class="terminal-card">
    <div class="terminal-head">
      <span class="dot dot-red"></span>
      <span class="dot dot-amber"></span>
      <span class="dot dot-green"></span>
      <span class="terminal-label">/usr/bin/page-ui-ai</span>
    </div>
    <div class="terminal-body">
      <p class="line"><span class="token">$</span> boot --profile=runtime --mode=interactive</p>
      <p class="line"><span class="token">$</span> attach chat://{shortChatId}</p>
      <p class="line"><span class="token">$</span> decode --source=user.prompt</p>
      <p class="line command"><span class="token">&gt;</span> PROMPT_PAYLOAD</p>
      <pre class="prompt">{safePrompt}</pre>
      <p class="line status"><span class="token">#</span> status: compiled preview ready</p>
    </div>
  </section>

  <section class="status-deck">
    <article class="status-card">
      <h2>PIPELINE</h2>
      <p>worker-ai -> svelte-render -> /runs</p>
    </article>
    <article class="status-card">
      <h2>RENDER MODE</h2>
      <p>SSR + hydrate (artifact split)</p>
    </article>
    <article class="status-card">
      <h2>TRANSPORT</h2>
      <p>MassTransit / RabbitMQ event chain</p>
    </article>
  </section>
</main>
""";
    }

    public static string BuildRetroCliCss()
    {
        return """
:root {
  --bg: #05070b;
  --panel: #0d121a;
  --line: #19f58c;
  --line-soft: #7de9b7;
  --muted: #7f8ea3;
  --accent: #3cf2ff;
  --warn: #ffd166;
  --border: rgba(60, 242, 255, 0.24);
}

* {
  box-sizing: border-box;
}

html,
body {
  margin: 0;
  min-height: 100%;
  background: radial-gradient(circle at 20% 10%, #10212f 0%, var(--bg) 46%, #030409 100%);
  color: var(--line-soft);
  font-family: "Cascadia Mono", "JetBrains Mono", "Consolas", "SFMono-Regular", Menlo, Monaco, monospace;
}

.retro-scene {
  position: relative;
  min-height: 100vh;
  padding: clamp(16px, 3vw, 32px);
  display: grid;
  gap: 18px;
}

.retro-grid {
  position: fixed;
  inset: 0;
  background-image:
    linear-gradient(rgba(26, 255, 167, 0.07) 1px, transparent 1px),
    linear-gradient(90deg, rgba(60, 242, 255, 0.06) 1px, transparent 1px);
  background-size: 100% 28px, 32px 100%;
  pointer-events: none;
  z-index: 0;
  animation: grid-drift 16s linear infinite;
}

.retro-glow {
  position: fixed;
  inset: -20%;
  background:
    radial-gradient(circle at 15% 20%, rgba(60, 242, 255, 0.12), transparent 40%),
    radial-gradient(circle at 85% 25%, rgba(25, 245, 140, 0.16), transparent 38%),
    radial-gradient(circle at 40% 85%, rgba(255, 209, 102, 0.12), transparent 42%);
  pointer-events: none;
  z-index: 0;
}

.top-shell,
.terminal-card,
.status-deck {
  position: relative;
  z-index: 1;
}

.top-shell {
  border: 1px solid var(--border);
  background: linear-gradient(180deg, rgba(13, 18, 26, 0.9), rgba(8, 12, 18, 0.9));
  border-radius: 14px;
  padding: clamp(14px, 2vw, 20px);
  display: flex;
  gap: 12px;
  justify-content: space-between;
  align-items: flex-end;
  box-shadow: 0 12px 38px rgba(0, 0, 0, 0.4), inset 0 0 0 1px rgba(125, 233, 183, 0.08);
}

.badge {
  display: inline-block;
  font-size: 12px;
  letter-spacing: 0.14em;
  color: var(--accent);
  margin-bottom: 8px;
}

h1 {
  margin: 0;
  font-size: clamp(24px, 4.4vw, 42px);
  line-height: 1;
  letter-spacing: 0.06em;
  color: var(--line);
  text-shadow: 0 0 14px rgba(25, 245, 140, 0.3);
}

.meta-wrap {
  display: grid;
  gap: 4px;
  text-align: right;
  font-size: 12px;
  color: var(--muted);
}

.terminal-card {
  border: 1px solid rgba(125, 233, 183, 0.25);
  border-radius: 14px;
  overflow: hidden;
  background: linear-gradient(180deg, rgba(8, 13, 17, 0.95), rgba(6, 9, 14, 0.95));
  box-shadow: 0 16px 38px rgba(0, 0, 0, 0.46);
}

.terminal-head {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 14px;
  background: rgba(255, 255, 255, 0.03);
  border-bottom: 1px solid rgba(125, 233, 183, 0.18);
}

.dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  display: inline-block;
}

.dot-red { background: #ff6b6b; }
.dot-amber { background: #ffd166; }
.dot-green { background: #7bd88f; }

.terminal-label {
  margin-left: 6px;
  font-size: 12px;
  letter-spacing: 0.08em;
  color: var(--muted);
}

.terminal-body {
  padding: clamp(14px, 2vw, 24px);
}

.line {
  margin: 0 0 10px;
  font-size: clamp(12px, 1.7vw, 14px);
  letter-spacing: 0.02em;
  color: #b8f8d7;
}

.line.command {
  margin-top: 14px;
  color: var(--accent);
}

.line.status {
  margin-top: 12px;
  color: var(--warn);
}

.token {
  color: var(--line);
  display: inline-block;
  min-width: 14px;
}

.prompt {
  margin: 10px 0 0;
  padding: 12px;
  border-radius: 10px;
  border: 1px solid rgba(60, 242, 255, 0.22);
  background: rgba(4, 7, 12, 0.72);
  color: #d9ffe9;
  line-height: 1.55;
  white-space: pre-wrap;
  word-break: break-word;
  text-shadow: 0 0 8px rgba(25, 245, 140, 0.18);
}

.status-deck {
  display: grid;
  gap: 12px;
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.status-card {
  border: 1px solid rgba(60, 242, 255, 0.22);
  border-radius: 12px;
  padding: 12px 14px;
  background: rgba(7, 10, 15, 0.82);
}

.status-card h2 {
  margin: 0 0 8px;
  font-size: 12px;
  letter-spacing: 0.08em;
  color: var(--accent);
}

.status-card p {
  margin: 0;
  font-size: 13px;
  color: #b8f8d7;
}

@media (max-width: 800px) {
  .top-shell {
    align-items: flex-start;
    flex-direction: column;
  }

  .meta-wrap {
    text-align: left;
  }

  .status-deck {
    grid-template-columns: 1fr;
  }
}

@keyframes grid-drift {
  0% { transform: translateY(0); }
  100% { transform: translateY(28px); }
}
""";
    }
}
