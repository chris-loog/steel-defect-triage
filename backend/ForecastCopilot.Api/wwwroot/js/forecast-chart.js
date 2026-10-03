// Draws the 24 h forecast chart on the home page.
// Data comes from the <script id="forecast-data"> JSON block rendered by Razor (same data as the table).
(() => {
    const dataEl = document.getElementById("forecast-data");
    const canvas = document.getElementById("forecast-chart");
    if (!dataEl || !canvas || typeof Chart === "undefined") return;

    // JSON property names follow the Python contract (snake_case), see Models/ForecastResponse.cs.
    const points = JSON.parse(dataEl.textContent);

    const weekday = new Intl.DateTimeFormat("en", { weekday: "short" });
    const labels = points.map(p => {
        const d = new Date(p.timestamp);
        return `${weekday.format(d)} ${String(d.getHours()).padStart(2, "0")}:00`;
    });

    new Chart(canvas, {
        type: "line",
        data: {
            labels,
            datasets: [
                {
                    label: "Forecast",
                    data: points.map(p => p.units_predicted),
                    borderColor: "#0d6efd",
                    backgroundColor: "rgba(13, 110, 253, 0.12)",
                    fill: true,
                    tension: 0.25,
                    pointRadius: 2,
                },
                {
                    label: "Same hour yesterday",
                    data: points.map(p => p.units_same_hour_yesterday),
                    borderColor: "#6c757d",
                    borderDash: [6, 4],
                    fill: false,
                    tension: 0.25,
                    pointRadius: 0,
                },
            ],
        },
        options: {
            responsive: true,
            maintainAspectRatio: false, // height comes from .chart-container in site.css
            interaction: { mode: "index", intersect: false }, // tooltip shows both series for the hovered hour
            plugins: { legend: { position: "bottom" } },
            scales: {
                x: { grid: { display: false }, ticks: { maxRotation: 0, autoSkip: true, maxTicksLimit: 12 } },
                y: { beginAtZero: true, title: { display: true, text: "units / hour" } },
            },
        },
    });
})();
