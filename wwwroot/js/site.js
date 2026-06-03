// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener("click", async (event) => {
  const button = event.target.closest("[data-copy-target]");
  if (!button) {
    return;
  }

  const target = document.getElementById(button.getAttribute("data-copy-target"));
  if (!target || !target.value) {
    return;
  }

  try {
    await navigator.clipboard.writeText(target.value);
    const originalText = button.textContent;
    button.textContent = "Copied";
    window.setTimeout(() => {
      button.textContent = originalText;
    }, 1400);
  } catch {
    target.focus();
    target.select();
  }
});
