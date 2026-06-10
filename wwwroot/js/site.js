// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

const workspaceTabCache = new Map();
const responseDetailCache = new Map();
let pendingWorkspaceScrollY = null;
let workspaceScrollRestoreQueued = false;

const localDateFormatters = new Map();

const workspaceTabAliases = {
  "": "overview",
  "overview": "overview",
  "assessment": "assessment-answers",
  "assessmentanswers": "assessment-answers",
  "assessment-answers": "assessment-answers",
  "documents": "documents",
  "notes": "notes-transcripts",
  "notestranscripts": "notes-transcripts",
  "notes-transcripts": "notes-transcripts",
  "discovery-notes-transcripts": "notes-transcripts",
  "discoverynotestranscripts": "notes-transcripts",
  "knowledgegap": "knowledge-gap-analysis",
  "knowledgegapanalysis": "knowledge-gap-analysis",
  "knowledge-gap-analysis": "knowledge-gap-analysis",
  "analysis": "company-summary",
  "aidrafts": "company-summary",
  "ai-drafts": "company-summary",
  "summary": "company-summary",
  "companysummary": "company-summary",
  "company-summary": "company-summary",
  "gap": "knowledge-gap-analysis",
  "gapanalysis": "knowledge-gap-analysis",
  "gap-analysis": "knowledge-gap-analysis",
  "swot": "swot",
  "insights": "industry-competitors",
  "industrycompetitors": "industry-competitors",
  "industry-competitors": "industry-competitors",
  "usecases": "use-cases-scoring",
  "usecasesscoring": "use-cases-scoring",
  "use-cases-scoring": "use-cases-scoring",
  "roadmap": "roadmap",
  "reports": "reports",
  "strategicreport": "reports",
  "strategic-report": "reports",
  "tasks": "tasks-activity",
  "tasksactivity": "tasks-activity",
  "tasks-activity": "tasks-activity",
  "activity": "activity-log",
  "activitylog": "activity-log",
  "activity-log": "activity-log"
};

function normalizeWorkspaceTabKey(value) {
  const raw = (value || "")
    .toString()
    .trim()
    .replace(/^#/, "")
    .toLowerCase();
  const dashed = raw.replace(/[_\s]+/g, "-");
  const compact = dashed.replace(/-/g, "");
  return workspaceTabAliases[dashed] || workspaceTabAliases[compact] || "overview";
}

function getWorkspaceShell(element = document) {
  return element?.closest?.("[data-workspace-tabs]") || document.querySelector("[data-workspace-tabs]");
}

function getWorkspaceStorageKey(shell = getWorkspaceShell()) {
  const clientId = shell?.getAttribute("data-workspace-client-id") || window.location.pathname;
  return `ai-readiness-workspace-return:${clientId}`;
}

function getWorkspaceSessionStorage() {
  try {
    return window.sessionStorage;
  } catch {
    return null;
  }
}

function readPositiveInteger(value) {
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) && parsed >= 0 ? parsed : null;
}

function findWorkspaceTabButton(shell, tabKey) {
  const normalizedTab = normalizeWorkspaceTabKey(tabKey);
  return Array.from(shell?.querySelectorAll("[data-bs-toggle='tab'][data-workspace-tab-key]") || [])
    .find((button) => normalizeWorkspaceTabKey(button.getAttribute("data-workspace-tab-key")) === normalizedTab);
}

function findWorkspaceTabPanel(shell, tabKey) {
  const normalizedTab = normalizeWorkspaceTabKey(tabKey);
  return Array.from(shell?.querySelectorAll("[data-workspace-tab-key]") || [])
    .find((panel) => panel.id && normalizeWorkspaceTabKey(panel.getAttribute("data-workspace-tab-key")) === normalizedTab);
}

function getCurrentWorkspaceTabKey(shell = getWorkspaceShell()) {
  const activeButton = shell?.querySelector("[data-bs-toggle='tab'][data-workspace-tab-key].active");
  if (activeButton) {
    return normalizeWorkspaceTabKey(activeButton.getAttribute("data-workspace-tab-key"));
  }

  const activePanel = shell?.querySelector(".tab-pane.active[data-workspace-tab-key], .tab-pane.show[data-workspace-tab-key]");
  if (activePanel) {
    return normalizeWorkspaceTabKey(activePanel.getAttribute("data-workspace-tab-key"));
  }

  return normalizeWorkspaceTabKey(shell?.getAttribute("data-workspace-active-tab"));
}

function getSelectedWorkspaceResponseId(shell = getWorkspaceShell()) {
  const selectedRow = shell?.querySelector("[data-assessment-response-row].selected-response-row");
  const selectedRowId = readPositiveInteger(selectedRow?.getAttribute("data-assessment-response-row"));
  if (selectedRowId !== null) {
    return selectedRowId.toString();
  }

  const selectedButton = shell?.querySelector("[data-response-detail-url].btn-primary[data-response-id]");
  const selectedButtonId = readPositiveInteger(selectedButton?.getAttribute("data-response-id"));
  if (selectedButtonId !== null) {
    return selectedButtonId.toString();
  }

  const shellValue = readPositiveInteger(shell?.getAttribute("data-workspace-selected-response-id"));
  return shellValue !== null ? shellValue.toString() : null;
}

function getWorkspaceReturnContext(shell = getWorkspaceShell()) {
  const activeTab = getCurrentWorkspaceTabKey(shell);
  const context = {
    activeTab,
    scrollY: Math.max(0, Math.round(window.scrollY || 0))
  };

  if (activeTab === "assessment-answers") {
    const selectedResponseId = getSelectedWorkspaceResponseId(shell);
    if (selectedResponseId) {
      context.selectedResponseId = selectedResponseId;
    }
  }

  return context;
}

function saveWorkspaceReturnContext(shell, context) {
  const storage = getWorkspaceSessionStorage();
  if (!shell || !storage) {
    return;
  }

  try {
    storage.setItem(getWorkspaceStorageKey(shell), JSON.stringify(context));
  } catch {
    // Non-critical: hidden form fields still carry the return context.
  }
}

function consumeStoredWorkspaceReturnContext(shell) {
  const storage = getWorkspaceSessionStorage();
  if (!shell || !storage) {
    return null;
  }

  const key = getWorkspaceStorageKey(shell);
  try {
    const raw = storage.getItem(key);
    storage.removeItem(key);
    return raw ? JSON.parse(raw) : null;
  } catch {
    try {
      storage.removeItem(key);
    } catch {
      // Ignore cleanup failure.
    }
    return null;
  }
}

function upsertWorkspaceReturnInput(form, name, value) {
  let input = form.querySelector(`input[type="hidden"][name="${name}"][data-workspace-return]`);
  if (value === null || value === undefined || value === "") {
    input?.remove();
    return;
  }

  if (!input) {
    input = document.createElement("input");
    input.type = "hidden";
    input.name = name;
    input.setAttribute("data-workspace-return", "");
    form.appendChild(input);
  }

  input.value = value.toString();
}

function appendWorkspaceReturnContext(form) {
  const shell = getWorkspaceShell(form);
  if (!shell) {
    return;
  }

  const context = getWorkspaceReturnContext(shell);
  shell.setAttribute("data-workspace-active-tab", context.activeTab);
  upsertWorkspaceReturnInput(form, "activeTab", context.activeTab);
  upsertWorkspaceReturnInput(form, "scrollY", context.scrollY);
  upsertWorkspaceReturnInput(form, "selectedResponseId", context.selectedResponseId || "");
  saveWorkspaceReturnContext(shell, context);
}

function workspaceTabFetchUrl(panel) {
  const url = panel?.getAttribute("data-workspace-tab-url");
  if (!url) {
    return null;
  }

  if (normalizeWorkspaceTabKey(panel.getAttribute("data-workspace-tab-key")) !== "assessment-answers") {
    return url;
  }

  const responseId = getSelectedWorkspaceResponseId();
  if (!responseId) {
    return url;
  }

  const nextUrl = new URL(url, window.location.href);
  nextUrl.searchParams.set("responseId", responseId);
  return `${nextUrl.pathname}${nextUrl.search}${nextUrl.hash}`;
}

function queueWorkspaceScrollRestore(scrollY) {
  const parsed = readPositiveInteger(scrollY);
  if (parsed !== null) {
    pendingWorkspaceScrollY = parsed;
  }
}

function restoreWorkspaceScroll(shell = getWorkspaceShell()) {
  if (pendingWorkspaceScrollY === null || workspaceScrollRestoreQueued) {
    return;
  }

  const scrollY = pendingWorkspaceScrollY;
  workspaceScrollRestoreQueued = true;
  window.requestAnimationFrame(() => {
    window.setTimeout(() => {
      window.scrollTo({ top: scrollY, behavior: "auto" });
      pendingWorkspaceScrollY = null;
      workspaceScrollRestoreQueued = false;
      if (shell) {
        shell.removeAttribute("data-workspace-scroll-y");
      }
    }, 80);
  });
}

function restoreWorkspaceScrollAfterTabLoad(panel) {
  if (pendingWorkspaceScrollY === null) {
    return;
  }

  const shell = getWorkspaceShell(panel);
  const activeTab = getCurrentWorkspaceTabKey(shell);
  const panelTab = normalizeWorkspaceTabKey(panel?.getAttribute("data-workspace-tab-key"));
  if (activeTab === panelTab) {
    restoreWorkspaceScroll(shell);
  }
}

function formatterOptions(format) {
  switch ((format || "datetime").toLowerCase()) {
    case "date":
      return { day: "2-digit", month: "short", year: "numeric" };
    case "time":
      return { hour: "2-digit", minute: "2-digit" };
    default:
      return { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" };
  }
}

function localDateFormatter(format) {
  const key = (format || "datetime").toLowerCase();
  if (!localDateFormatters.has(key)) {
    localDateFormatters.set(key, new Intl.DateTimeFormat(undefined, formatterOptions(key)));
  }

  return localDateFormatters.get(key);
}

function relativeLocalTime(date) {
  const seconds = Math.round((date.getTime() - Date.now()) / 1000);
  const divisions = [
    { amount: 60, unit: "second" },
    { amount: 60, unit: "minute" },
    { amount: 24, unit: "hour" },
    { amount: 7, unit: "day" },
    { amount: 4.345, unit: "week" },
    { amount: 12, unit: "month" },
    { amount: Number.POSITIVE_INFINITY, unit: "year" }
  ];
  let value = seconds;
  for (const division of divisions) {
    if (Math.abs(value) < division.amount) {
      if (typeof Intl.RelativeTimeFormat === "function") {
        return new Intl.RelativeTimeFormat(undefined, { numeric: "auto" }).format(Math.round(value), division.unit);
      }

      return localDateFormatter("datetime").format(date);
    }

    value /= division.amount;
  }

  return localDateFormatter("datetime").format(date);
}

function utcTooltipText(date) {
  return `UTC: ${new Intl.DateTimeFormat(undefined, {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    timeZone: "UTC",
    timeZoneName: "short"
  }).format(date)}`;
}

function formatLocalTimestamps(root = document) {
  const selector = "[data-utc], [data-utc-date], [data-utc-datetime]";
  const items = [
    ...(root.matches?.(selector) ? [root] : []),
    ...(root.querySelectorAll?.(selector) || [])
  ];
  items.forEach((item) => {
    const utcValue = item.getAttribute("data-utc") ||
      item.getAttribute("data-utc-datetime") ||
      item.getAttribute("data-utc-date");
    if (!utcValue) {
      return;
    }

    const date = new Date(utcValue);
    if (Number.isNaN(date.getTime())) {
      return;
    }

    const inferredFormat = item.hasAttribute("data-utc-date")
      ? "date"
      : item.hasAttribute("data-utc-datetime")
        ? "datetime"
        : "datetime";
    const format = item.getAttribute("data-format") || inferredFormat;
    item.textContent = format.toLowerCase() === "relative"
      ? relativeLocalTime(date)
      : localDateFormatter(format).format(date);

    item.setAttribute("title", item.getAttribute("title") || utcTooltipText(date));
  });
}

function firstPresentValue(...values) {
  return values.find((value) => value !== null && value !== undefined && value !== "");
}

function getInitialWorkspaceReturnContext(shell) {
  const params = new URLSearchParams(window.location.search);
  const stored = consumeStoredWorkspaceReturnContext(shell) || {};
  const activeTab = normalizeWorkspaceTabKey(firstPresentValue(
    params.get("activeTab"),
    stored.activeTab,
    shell?.getAttribute("data-workspace-active-tab")
  ));
  const selectedResponseId = readPositiveInteger(firstPresentValue(
    params.get("selectedResponseId"),
    params.get("responseId"),
    stored.selectedResponseId,
    shell?.getAttribute("data-workspace-selected-response-id")
  ));
  const scrollY = readPositiveInteger(firstPresentValue(
    params.get("scrollY"),
    stored.scrollY,
    shell?.getAttribute("data-workspace-scroll-y")
  ));

  return {
    activeTab,
    selectedResponseId: selectedResponseId === null ? null : selectedResponseId.toString(),
    scrollY
  };
}

function initializeWorkspaceReturnContext() {
  const shell = getWorkspaceShell();
  if (!shell) {
    return null;
  }

  const context = getInitialWorkspaceReturnContext(shell);
  shell.setAttribute("data-workspace-active-tab", context.activeTab);
  if (context.selectedResponseId) {
    shell.setAttribute("data-workspace-selected-response-id", context.selectedResponseId);
  }

  queueWorkspaceScrollRestore(context.scrollY);

  const button = findWorkspaceTabButton(shell, context.activeTab);
  if (button && !button.classList.contains("active")) {
    if (window.bootstrap?.Tab) {
      window.bootstrap.Tab.getOrCreateInstance(button).show();
    } else {
      button.click();
    }
    return shell;
  }

  const activePanel = findWorkspaceTabPanel(shell, context.activeTab);
  if (activePanel?.hasAttribute("data-workspace-tab-panel")) {
    loadWorkspaceTab(activePanel);
  } else {
    restoreWorkspaceScroll(shell);
  }

  return shell;
}

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
  const url = workspaceTabFetchUrl(panel);
  if (!panel || !url) {
    return;
  }

  if (!force && panel.getAttribute("data-loaded") === "true") {
    restoreWorkspaceScrollAfterTabLoad(panel);
    return;
  }

  if (!force && panel.getAttribute("data-loading") === "true") {
    return;
  }

  if (!force && workspaceTabCache.has(url)) {
    panel.innerHTML = workspaceTabCache.get(url);
    panel.setAttribute("data-loaded", "true");
    formatLocalTimestamps(panel);
    restoreWorkspaceScrollAfterTabLoad(panel);
    return;
  }

  const label = panel.getAttribute("data-loading-label") || "Loading...";
  panel.innerHTML = skeleton(label);
  panel.setAttribute("data-loading", "true");

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
    formatLocalTimestamps(panel);
    restoreWorkspaceScrollAfterTabLoad(panel);
  } catch {
    panel.removeAttribute("data-loaded");
    panel.innerHTML = tabErrorHtml(label.replace(/\.\.\.$/, ""), url);
    restoreWorkspaceScrollAfterTabLoad(panel);
  } finally {
    panel.removeAttribute("data-loading");
  }
}

async function loadAssessmentResponseDetails(button) {
  const url = button.getAttribute("data-response-detail-url");
  const responseId = button.getAttribute("data-response-id");
  const panel = document.querySelector("[data-response-detail-panel]");
  if (!url || !panel) {
    return;
  }

  const shell = getWorkspaceShell(button);
  if (shell && readPositiveInteger(responseId) !== null) {
    shell.setAttribute("data-workspace-active-tab", "assessment-answers");
    shell.setAttribute("data-workspace-selected-response-id", responseId);
    saveWorkspaceReturnContext(shell, {
      activeTab: "assessment-answers",
      selectedResponseId: responseId,
      scrollY: Math.max(0, Math.round(window.scrollY || 0))
    });
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
    formatLocalTimestamps(panel);
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

async function loadWorkspaceCounts() {
  const shell = document.querySelector("[data-workspace-counts-url]");
  const url = shell?.getAttribute("data-workspace-counts-url");
  if (!shell || !url || shell.getAttribute("data-counts-loaded") === "true") {
    return;
  }

  try {
    const response = await fetch(url, {
      headers: {
        "Accept": "application/json",
        "X-Requested-With": "XMLHttpRequest"
      }
    });
    if (!response.ok) {
      throw new Error(`Counts request failed with ${response.status}`);
    }

    const counts = await response.json();
    document.querySelectorAll("[data-workspace-count]").forEach((item) => {
      const key = item.getAttribute("data-workspace-count");
      if (key && Object.prototype.hasOwnProperty.call(counts, key)) {
        item.textContent = counts[key];
      }
    });
    shell.setAttribute("data-counts-loaded", "true");
  } catch {
    shell.removeAttribute("data-counts-loaded");
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
        formatLocalTimestamps(responsePanel);
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

  appendWorkspaceReturnContext(form);
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
  const shell = getWorkspaceShell(event.target);
  const activeTab = normalizeWorkspaceTabKey(event.target?.getAttribute("data-workspace-tab-key") || targetSelector);
  if (shell) {
    shell.setAttribute("data-workspace-active-tab", activeTab);
  }

  if (!targetSelector) {
    return;
  }

  const panel = document.querySelector(targetSelector);
  if (panel?.hasAttribute("data-workspace-tab-panel")) {
    loadWorkspaceTab(panel);
    return;
  }

  restoreWorkspaceScroll(shell);
});

window.addEventListener("pageshow", stopGlobalLoading);
window.addEventListener("beforeunload", () => startGlobalLoading("Loading..."));

document.addEventListener("DOMContentLoaded", () => {
  const shell = initializeWorkspaceReturnContext();
  formatLocalTimestamps();
  loadWorkspaceCounts();

  const activeLazyPanels = document.querySelectorAll("[data-workspace-tab-panel].active, [data-workspace-tab-panel].show");
  activeLazyPanels.forEach((panel) => {
    loadWorkspaceTab(panel);
  });

  if (activeLazyPanels.length === 0) {
    restoreWorkspaceScroll(shell);
  }
});
