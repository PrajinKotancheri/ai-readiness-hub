// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

const workspaceTabCache = new Map();
const responseDetailCache = new Map();

function ensureProgressBar() {
  let bar = document.querySelector("[data-global-progress]");
  if (!bar) {
    bar = document.createElement("div");
    bar.className = "global-progress";
    bar.setAttribute("data-global-progress", "");
    document.body.appendChild(bar);
  }

  return bar;
}

function startGlobalLoading(label = "Loading...") {
  document.body.classList.add("is-loading");
  document.body.setAttribute("aria-busy", "true");
  const bar = ensureProgressBar();
  bar.hidden = false;
  bar.setAttribute("aria-label", label);
}

function stopGlobalLoading() {
  document.body.classList.remove("is-loading");
  document.body.removeAttribute("aria-busy");
  const bar = document.querySelector("[data-global-progress]");
  if (bar) {
    bar.hidden = true;
  }

  document.querySelectorAll("[data-loading-original-text]").forEach((button) => {
    button.textContent = button.getAttribute("data-loading-original-text");
    button.removeAttribute("data-loading-original-text");
    button.disabled = false;
  });
}

function loadingTextForButton(button) {
  const explicit = button.getAttribute("data-loading-text");
  if (explicit) {
    return explicit;
  }

  const text = (button.textContent || "").trim().toLowerCase();
  if (text.includes("send") || text.includes("reminder")) {
    return "Sending...";
  }

  if (text.includes("save") || text.includes("edit") || text.includes("update") || text.includes("change")) {
    return "Saving...";
  }

  if (text.includes("generate") || text.includes("run") || text.includes("score")) {
    return "Generating...";
  }

  if (text.includes("upload") || text.includes("import") || text.includes("add")) {
    return "Saving...";
  }

  return "Loading...";
}

function markSubmittingButton(button) {
  if (!button || button.disabled) {
    return;
  }

  const nextText = loadingTextForButton(button);
  button.setAttribute("data-loading-original-text", button.textContent || "");
  button.textContent = nextText;
  button.disabled = true;
}

function isPlainNavigationLink(link, event) {
  if (!link || link.hasAttribute("data-copy-target") || link.hasAttribute("download")) {
    return false;
  }

  if (link.getAttribute("data-bs-toggle") || link.getAttribute("role") === "tab") {
    return false;
  }

  if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
    return false;
  }

  const href = link.getAttribute("href");
  if (!href || href.startsWith("#") || href.startsWith("javascript:")) {
    return false;
  }

  if (link.target && link.target !== "_self") {
    return false;
  }

  const url = new URL(href, window.location.href);
  return url.origin === window.location.origin;
}

function skeleton(label) {
  return `
    <div class="tab-loading" role="status" aria-live="polite">
      <div class="loading-spinner" aria-hidden="true"></div>
      <div>${label}</div>
    </div>
    <div class="skeleton-grid" aria-hidden="true">
      <div class="skeleton-block"></div>
      <div class="skeleton-block"></div>
      <div class="skeleton-line"></div>
      <div class="skeleton-line short"></div>
    </div>`;
}

function tabErrorHtml(label, retryUrl) {
  return `
    <div class="empty-state tab-error">
      <strong>${label} could not be loaded.</strong>
      <span>Check the connection and try again.</span>
      <div class="mt-3">
        <button class="btn btn-outline-primary btn-sm" type="button" data-workspace-retry-url="${retryUrl}">Retry</button>
      </div>
    </div>`;
}

async function loadWorkspaceTab(panel, force = false) {
  const url = panel?.getAttribute("data-workspace-tab-url");
  if (!panel || !url) {
    return;
  }

  if (!force && panel.getAttribute("data-loaded") === "true") {
    return;
  }

  if (!force && workspaceTabCache.has(url)) {
    panel.innerHTML = workspaceTabCache.get(url);
    panel.setAttribute("data-loaded", "true");
    return;
  }

  const label = panel.getAttribute("data-loading-label") || "Loading...";
  panel.innerHTML = skeleton(label);

  try {
    const response = await fetch(url, {
      headers: {
        "X-Requested-With": "XMLHttpRequest"
      }
    });
    const html = await response.text();
    if (!response.ok) {
      throw new Error(`Tab request failed with ${response.status}`);
    }

    workspaceTabCache.set(url, html);
    panel.innerHTML = html;
    panel.setAttribute("data-loaded", "true");
  } catch {
    panel.removeAttribute("data-loaded");
    panel.innerHTML = tabErrorHtml(label.replace(/\.\.\.$/, ""), url);
  }
}

async function loadAssessmentResponseDetails(button) {
  const url = button.getAttribute("data-response-detail-url");
  const responseId = button.getAttribute("data-response-id");
  const panel = document.querySelector("[data-response-detail-panel]");
  if (!url || !panel) {
    return;
  }

  panel.innerHTML = skeleton("Loading response details...");

  try {
    let html = responseDetailCache.get(url);
    if (!html) {
      const response = await fetch(url, {
        headers: {
          "X-Requested-With": "XMLHttpRequest"
        }
      });
      html = await response.text();
      if (!response.ok) {
        throw new Error(`Response detail request failed with ${response.status}`);
      }

      responseDetailCache.set(url, html);
    }

    panel.innerHTML = html;
    document.querySelectorAll("[data-assessment-response-row]").forEach((row) => {
      const selected = row.getAttribute("data-assessment-response-row") === responseId;
      row.classList.toggle("selected-response-row", selected);
      const rowButton = row.querySelector("[data-response-detail-url]");
      if (rowButton) {
        rowButton.classList.toggle("btn-primary", selected);
        rowButton.classList.toggle("btn-outline-primary", !selected);
      }
    });
  } catch {
    panel.innerHTML = tabErrorHtml("Response details", url);
  }
}

document.addEventListener("click", async (event) => {
  const copyButton = event.target.closest("[data-copy-target]");
  if (copyButton) {
    const target = document.getElementById(copyButton.getAttribute("data-copy-target"));
    if (!target || !target.value) {
      return;
    }

    try {
      await navigator.clipboard.writeText(target.value);
      const originalText = copyButton.textContent;
      copyButton.textContent = "Copied";
      window.setTimeout(() => {
        copyButton.textContent = originalText;
      }, 1400);
    } catch {
      target.focus();
      target.select();
    }
    return;
  }

  const responseButton = event.target.closest("[data-response-detail-url]");
  if (responseButton) {
    event.preventDefault();
    await loadAssessmentResponseDetails(responseButton);
    return;
  }

  const retryButton = event.target.closest("[data-workspace-retry-url]");
  if (retryButton) {
    event.preventDefault();
    const panel = retryButton.closest("[data-workspace-tab-panel]");
    if (panel) {
      panel.setAttribute("data-workspace-tab-url", retryButton.getAttribute("data-workspace-retry-url"));
      await loadWorkspaceTab(panel, true);
      return;
    }

    const responsePanel = retryButton.closest("[data-response-detail-panel]");
    if (responsePanel) {
      const retryUrl = retryButton.getAttribute("data-workspace-retry-url");
      responsePanel.innerHTML = skeleton("Loading response details...");
      try {
        const response = await fetch(retryUrl, { headers: { "X-Requested-With": "XMLHttpRequest" } });
        const html = await response.text();
        if (!response.ok) {
          throw new Error("Retry failed");
        }
        responsePanel.innerHTML = html;
      } catch {
        responsePanel.innerHTML = tabErrorHtml("Response details", retryUrl);
      }
      return;
    }
  }

  const link = event.target.closest("a[href]");
  if (isPlainNavigationLink(link, event)) {
    startGlobalLoading("Loading...");
  }
});

document.addEventListener("submit", (event) => {
  const form = event.target;
  if (!(form instanceof HTMLFormElement)) {
    return;
  }

  if (form.dataset.submitting === "true") {
    event.preventDefault();
    return;
  }

  if (typeof form.checkValidity === "function" && !form.checkValidity()) {
    stopGlobalLoading();
    return;
  }

  form.dataset.submitting = "true";
  const submitter = event.submitter || form.querySelector("button[type='submit'], input[type='submit']");
  if (submitter?.name && !form.querySelector(`input[type="hidden"][data-submit-proxy="${submitter.name}"]`)) {
    const proxy = document.createElement("input");
    proxy.type = "hidden";
    proxy.name = submitter.name;
    proxy.value = submitter.value;
    proxy.setAttribute("data-submit-proxy", submitter.name);
    form.appendChild(proxy);
  }

  markSubmittingButton(submitter);
  startGlobalLoading(loadingTextForButton(submitter || form));
});

document.addEventListener("input", (event) => {
  const input = event.target.closest("#answerFilter");
  if (!input) {
    return;
  }

  const query = input.value.trim().toLowerCase();
  document.querySelectorAll("[data-answer-search]").forEach((item) => {
    const haystack = item.getAttribute("data-answer-search")?.toLowerCase() ?? "";
    item.hidden = query.length > 0 && !haystack.includes(query);
  });
});

document.addEventListener("shown.bs.tab", (event) => {
  const targetSelector = event.target?.getAttribute("data-bs-target");
  if (!targetSelector) {
    return;
  }

  const panel = document.querySelector(targetSelector);
  if (panel?.hasAttribute("data-workspace-tab-panel")) {
    loadWorkspaceTab(panel);
  }
});

window.addEventListener("pageshow", stopGlobalLoading);
window.addEventListener("beforeunload", () => startGlobalLoading("Loading..."));

document.addEventListener("DOMContentLoaded", () => {
  document.querySelectorAll("[data-workspace-tab-panel].active, [data-workspace-tab-panel].show").forEach((panel) => {
    loadWorkspaceTab(panel);
  });
});
