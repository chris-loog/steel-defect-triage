// Loads the LLM planner summary after the page has rendered (the model can take several seconds).
// textContent (not innerHTML): LLM output is treated as plain text, never as HTML → no XSS.
(async () => {
    const el = document.getElementById("forecast-summary");
    if (!el) return;
    try {
        const res = await fetch("/Home/Summary");
        const body = await res.json();
        el.textContent = res.ok ? body.summary : body.error;
        el.classList.toggle("text-muted", !res.ok);
    } catch {
        el.textContent = "The AI summary could not be loaded.";
    }
})();
