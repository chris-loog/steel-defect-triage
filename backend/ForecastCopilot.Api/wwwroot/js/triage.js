// Triage page: the threshold slider moves the line on the dot strip and updates headline, table and fault mix chart.
// Razor renders the starting state (works without JS); this script only switches classes and text.
(() => {
    const faultMixEl = document.getElementById("fault-mix-data");
    const statsEl = document.getElementById("threshold-stats-data");
    const slider = document.getElementById("threshold");
    if (!faultMixEl || !statsEl || !slider) return;

    // FaultMix is a C# record -> camelCase; ThresholdStat uses the Python snake_case names (Models/DefectApiModels.cs).
    const faultMix = JSON.parse(faultMixEl.textContent);
    const thresholdStats = JSON.parse(statsEl.textContent);

    const strip = document.querySelector(".strip");
    const dots = [...document.querySelectorAll(".strip-plot .dot")];
    const rows = [...document.querySelectorAll("tr[data-confidence]")];
    const percent = x => `${(x * 100).toFixed(1)} %`; // same format as Razor's "0.0 %"
    const setText = (id, value) => { document.getElementById(id).textContent = value; };

    // Same rule as Python and TriageFacts: confidence >= threshold -> classified automatically.
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

        // Strip: line + hatched zone follow the CSS variable; axis runs from 0.30 to 1.00.
        strip.style.setProperty("--threshold", `${((t - 0.3) / 0.7) * 100}%`);
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

        // Thresholds are floats -> match with a tolerance, not ===.
        const stat = thresholdStats.find(s => Math.abs(s.threshold - t) < 1e-6);
        setText("n-manual", rows.length - auto);
        setText("n-auto", auto);
        setText("threshold-value", t.toFixed(2));
        setText("error-rate", stat && stat.error_rate !== null ? percent(stat.error_rate) : "n/a");
        slider.setAttribute("aria-valuetext", `${t.toFixed(2)}: ${rows.length - auto} defects to manual review`);

        if (chart) {
            chart.data.datasets[0].data = faultMix.map(f => manualByFault[f.fault]);
            chart.data.datasets[1].data = faultMix.map(f => f.count - manualByFault[f.fault]);
            chart.update();
        }
    }

    slider.addEventListener("input", update);
    update(); // fills the chart with the starting split
})();
