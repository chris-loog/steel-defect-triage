// Triage page: the threshold slider moves the line on the dot strip and updates headline, table and fault mix chart.
// Razor renders the starting state (works without JS); this script only switches classes and text.
(() => {
    const faultMixEl = document.getElementById("fault-mix-data");
    const errorRatesEl = document.getElementById("error-rates-data");
    const slider = document.getElementById("threshold");
    if (!faultMixEl || !errorRatesEl || !slider) return;

    // Both from Razor: FaultMix (camelCase C# record) and ThresholdGrid.ErrorRates ("0.70" -> rate or null).
    const faultMix = JSON.parse(faultMixEl.textContent);
    const errorRates = JSON.parse(errorRatesEl.textContent);

    const strip = document.querySelector(".strip");
    const axisMin = parseFloat(strip.dataset.axisMin); // lowest threshold; the axis runs from there to 1.00
    const dots = [...document.querySelectorAll(".strip-plot .dot")];
    const rows = [...document.querySelectorAll("tr[data-confidence]")];
    const percent = x => `${(x * 100).toFixed(1)} %`; // same format as Razor's "0.0 %"
    const setText = (id, value) => { document.getElementById(id).textContent = value; };

    // Same rule as Python and BatchDefect.IsAutoAt: confidence >= threshold -> classified automatically.
    const isAuto = (el, t) => parseFloat(el.dataset.confidence) >= t;

    const css = getComputedStyle(document.documentElement);
    const color = name => css.getPropertyValue(name).trim();

    let chart = null;
    const canvas = document.getElementById("fault-mix-chart");
    if (canvas && typeof Chart !== "undefined") {
        Chart.defaults.font.family = css.getPropertyValue("--font-body");
        Chart.defaults.color = color("--steel");
        chart = new Chart(canvas, {
            type: "bar",
            data: {
                labels: faultMix.map(f => f.fault),
                datasets: [
                    { label: "Manual review", data: [], backgroundColor: color("--marker"),
                      borderColor: color("--marker-edge"), borderWidth: 1 },
                    { label: "Automatic", data: [], backgroundColor: color("--steel") },
                ],
            },
            options: {
                indexAxis: "y", // horizontal bars: fault names are long
                responsive: true,
                maintainAspectRatio: false, // height comes from .chart-container in site.css
                animation: { duration: 0 },
                plugins: { legend: { position: "bottom", labels: { boxWidth: 12, color: color("--blued-steel") } } },
                scales: {
                    x: { stacked: true, beginAtZero: true, ticks: { precision: 0 }, grid: { color: color("--mill-scale") } },
                    y: { stacked: true, grid: { display: false }, ticks: { color: color("--blued-steel") } },
                },
            },
        });
    }

    function update() {
        const t = parseFloat(slider.value);

        // Strip: line + hatched zone follow the CSS variable.
        strip.style.setProperty("--threshold", `${((t - axisMin) / (1 - axisMin)) * 100}%`);
        for (const dot of dots) {
            const auto = isAuto(dot, t);
            dot.classList.toggle("is-auto", auto);
            dot.classList.toggle("is-manual", !auto);
        }

        // Table rows + per-fault counts for the chart.
        const manualByFault = Object.fromEntries(faultMix.map(f => [f.fault, 0]));
        let auto = 0;
        for (const row of rows) {
            const rowAuto = isAuto(row, t);
            if (rowAuto) auto++; else manualByFault[row.dataset.fault]++;
            row.classList.toggle("is-auto", rowAuto);
            row.classList.toggle("is-manual", !rowAuto);
            row.querySelector(".triage-status").textContent = rowAuto ? "Automatic" : "Manual review";
        }

        const errorRate = errorRates[t.toFixed(2)]; // same "0.00" key format as ThresholdGrid.ErrorRates
        setText("n-manual", rows.length - auto);
        setText("n-auto", auto);
        setText("threshold-value", t.toFixed(2));
        setText("error-rate", errorRate == null ? "n/a" : percent(errorRate));
        slider.setAttribute("aria-valuetext", `${t.toFixed(2)}: ${rows.length - auto} defects to manual review`);

        if (chart) {
            chart.data.datasets[0].data = faultMix.map(f => manualByFault[f.fault]);
            chart.data.datasets[1].data = faultMix.map(f => f.count - manualByFault[f.fault]);
            chart.update();
        }
    }

    slider.addEventListener("input", update);
    update(); // fills the chart with the starting split

    // Handover note: one LLM call per click (slow), for the slider's current threshold.
    const summaryButton = document.getElementById("summary-button");
    const summaryText = document.getElementById("summary-text");
    const summaryNote = document.getElementById("summary-note");
    const summaryWarning = document.getElementById("summary-warning");
    const summaryPanel = summaryButton.closest(".summary-panel");
    let summaryThreshold = null; // threshold of the summary on screen

    async function writeSummary() {
        const threshold = slider.value;
        summaryButton.disabled = true;
        summaryButton.textContent = "Writing…";
        summaryWarning.hidden = true;
        try {
            const response = await fetch(`/Home/Summary?seed=${summaryButton.dataset.seed}&threshold=${threshold}`);
            const body = await response.json().catch(() => ({})); // e.g. an HTML error page
            if (!response.ok) throw new Error(body.error ?? "The summary could not be written.");

            // textContent, never innerHTML: LLM output is untrusted text.
            summaryText.textContent = body.note.headline;
            for (const dd of summaryNote.querySelectorAll("dd[data-field]")) dd.textContent = body.note[dd.dataset.field];
            summaryText.classList.remove("is-placeholder");
            summaryPanel.classList.remove("is-outdated");
            summaryNote.hidden = false;
            summaryThreshold = threshold;
            if (body.unverifiedNumbers.length > 0) {
                summaryWarning.textContent = `Check this text: it contains numbers that are not in the facts (${body.unverifiedNumbers.join(", ")}).`;
                summaryWarning.hidden = false;
            }
        } catch (err) {
            summaryText.textContent = err.message;
            summaryText.classList.add("is-placeholder");
            summaryPanel.classList.remove("is-outdated");
            summaryNote.hidden = true;
            summaryThreshold = null;
        } finally {
            summaryButton.disabled = false;
            summaryButton.textContent = "Write summary";
        }
    }

    summaryButton.addEventListener("click", writeSummary);
    slider.addEventListener("input", () => {
        if (summaryThreshold !== null) summaryPanel.classList.toggle("is-outdated", slider.value !== summaryThreshold);
    });
})();
