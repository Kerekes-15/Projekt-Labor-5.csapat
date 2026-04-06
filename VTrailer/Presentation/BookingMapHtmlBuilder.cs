using System.Globalization;

namespace VTrailer.Presentation;

internal static class BookingMapHtmlBuilder
{
    public static string Build(double depotLongitude, double depotLatitude)
    {
        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="referrer" content="origin" />
    <title>Delivery map</title>
    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
    <style>
        html, body {
            margin: 0;
            height: 100%;
            font-family: "Segoe UI", sans-serif;
            background: #ffffff;
            color: #0f172a;
        }

        .layout {
            display: grid;
            grid-template-rows: auto 1fr auto;
            height: 100%;
        }

        .toolbar {
            display: flex;
            gap: 8px;
            align-items: center;
            padding: 12px;
            background: rgba(255,255,255,0.98);
            border-bottom: 1px solid #d8dee9;
        }

        .toolbar input {
            flex: 1;
            padding: 10px 12px;
            border-radius: 10px;
            border: 1px solid #c4ccda;
            font-size: 14px;
            color: #0f172a;
            background: #ffffff;
        }

        .toolbar button {
            border: 0;
            border-radius: 10px;
            background: #0078d7;
            color: white;
            padding: 10px 14px;
            cursor: pointer;
            font-size: 14px;
        }

        #map {
            height: 100%;
            width: 100%;
        }

        .footer {
            padding: 10px 12px;
            background: rgba(255,255,255,0.98);
            border-top: 1px solid #d8dee9;
            font-size: 13px;
            color: #0f172a;
        }

        .hint {
            color: #334155;
        }

        .status {
            margin-top: 4px;
            color: #0f172a;
            font-weight: 600;
        }
    </style>
</head>
<body>
    <div class="layout">
        <div class="toolbar">
            <input id="searchInput" type="text" placeholder="Search for an address or area" />
            <button type="button" onclick="searchLocation()">Search</button>
        </div>
        <div id="map"></div>
        <div class="footer">
            <div class="hint">Click anywhere on the map to choose the delivery location, or search first and fine-tune with a click.</div>
            <div id="selectionStatus" class="status">No delivery point selected yet.</div>
        </div>
    </div>

    <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
    <script>
        const depot = {
            latitude: {{depotLatitude.ToString("0.#####", CultureInfo.InvariantCulture)}},
            longitude: {{depotLongitude.ToString("0.#####", CultureInfo.InvariantCulture)}}
        };

        const map = L.map("map").setView([depot.latitude, depot.longitude], 11);

        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: "&copy; OpenStreetMap contributors"
        }).addTo(map);

        L.circleMarker([depot.latitude, depot.longitude], {
            radius: 10,
            color: "#0078d7",
            fillColor: "#0078d7",
            fillOpacity: 0.95
        }).addTo(map).bindTooltip("Depot", { permanent: true, direction: "top" });

        let selectedMarker = null;

        function postWebViewMessage(message) {
            try {
                if (window.hasOwnProperty("chrome") && typeof chrome.webview !== undefined) {
                    chrome.webview.postMessage(message);
                } else if (window.hasOwnProperty("unoWebView")) {
                    unoWebView.postMessage(JSON.stringify(message));
                } else if (window.hasOwnProperty("webkit") && typeof webkit.messageHandlers !== undefined) {
                    webkit.messageHandlers.unoWebView.postMessage(JSON.stringify(message));
                }
            } catch (ex) {
                console.error("Failed to post message to host", ex);
            }
        }

        function setSelectionLabel(label) {
            document.getElementById("selectionStatus").textContent = label;
        }

        function publishSelection(latitude, longitude, label) {
            if (selectedMarker) {
                map.removeLayer(selectedMarker);
            }

            selectedMarker = L.circleMarker([latitude, longitude], {
                radius: 9,
                color: "#ef4444",
                fillColor: "#ef4444",
                fillOpacity: 0.9
            }).addTo(map);

            selectedMarker.bindTooltip("Delivery point", { permanent: true, direction: "top" });
            setSelectionLabel(label);

            postWebViewMessage({
                type: "locationSelected",
                latitude: latitude,
                longitude: longitude,
                label: label
            });
        }

        function selectLocation(latitude, longitude, label) {
            const defaultLabel = label || `Selected point: ${latitude.toFixed(5)}, ${longitude.toFixed(5)}`;
            publishSelection(latitude, longitude, defaultLabel);
        }

        map.on("click", function (event) {
            selectLocation(event.latlng.lat, event.latlng.lng);
        });

        async function searchLocation() {
            const input = document.getElementById("searchInput");
            const query = input.value.trim();
            if (!query) {
                setSelectionLabel("Enter an address or place name to search.");
                return;
            }

            setSelectionLabel("Searching...");

            try {
                const response = await fetch(`https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&q=${encodeURIComponent(query)}`, {
                    referrerPolicy: "origin"
                });
                const matches = await response.json();

                if (!Array.isArray(matches) || matches.length === 0) {
                    setSelectionLabel("No matching location was found.");
                    return;
                }

                const match = matches[0];
                const latitude = Number(match.lat);
                const longitude = Number(match.lon);
                map.setView([latitude, longitude], 14);
                selectLocation(latitude, longitude, match.display_name || query);
            } catch (error) {
                console.error(error);
                setSelectionLabel("Search failed. Try clicking the map instead.");
            }
        }
    </script>
</body>
</html>
""";
    }
}
