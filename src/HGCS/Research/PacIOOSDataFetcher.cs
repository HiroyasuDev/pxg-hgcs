// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// REAL INTEGRATION: PacIOOS ERDDAP Live Ocean Data Fetcher
// Replaces synthetic noise with actual buoy sensor readings.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================
//
// DATA SOURCE: PacIOOS ERDDAP (Pacific Islands Ocean Observing System)
// Endpoint: https://pae-paha.pacioos.hawaii.edu/erddap/tabledap/
//
// Known dataset IDs (verified from PacIOOS catalog):
//   aws_himb       — HIMB Weather Station (Coconut Island, Kaneohe Bay)
//   nss_01_agg     — NS01 Nearshore Sensor, Waikiki
//   nss_04_agg     — NS04 Nearshore Sensor, Hilo Bay
//   wqb_04_agg     — Water Quality Buoy 04, Hilo Bay
//   wqb_05_agg     — Water Quality Buoy 05, Ala Wai Canal
//   swan_oahu      — SWAN Wave Model, Oahu
//
// API format: {base}/tabledap/{datasetId}.json?{columns}&{constraints}
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace PXG.HGCS.Research
{
    /// <summary>
    /// Live data fetcher for PacIOOS ERDDAP ocean observation datasets.
    /// Uses <see cref="UnityWebRequest"/> to query real buoy sensor data
    /// and convert it into HGCS grid-mapped <see cref="ResearchDataProvider.DataPoint"/> instances.
    ///
    /// This replaces the synthetic Random-based data in <see cref="OceanScienceDataProvider"/>.
    /// </summary>
    public class PacIOOSDataFetcher
    {
        // ── ERDDAP Configuration ────────────────────────────────────────────

        /// <summary>PacIOOS ERDDAP base URL.</summary>
        public const string BaseUrl = "https://pae-paha.pacioos.hawaii.edu/erddap/tabledap";

        /// <summary>Known PacIOOS dataset identifiers.</summary>
        public static class DatasetIds
        {
            public const string HIMB_Weather      = "aws_himb";
            public const string Waikiki_Nearshore = "nss_01_agg";
            public const string Hilo_Nearshore    = "nss_04_agg";
            public const string Hilo_WaterQuality = "wqb_04_agg";
            public const string AlaWai_WaterQual  = "wqb_05_agg";
            public const string SWAN_Oahu_Wave    = "swan_oahu";
        }

        // ── Station Coordinates (real GPS positions) ────────────────────────

        /// <summary>Real geographic coordinates for PacIOOS stations.</summary>
        public static readonly Dictionary<string, (float lat, float lon, string name)> StationCoords = new()
        {
            { DatasetIds.HIMB_Weather,      (21.4316f, -157.7867f, "HIMB Coconut Island") },
            { DatasetIds.Waikiki_Nearshore, (21.2647f, -157.8234f, "Waikiki Nearshore") },
            { DatasetIds.Hilo_Nearshore,    (19.7352f, -155.0608f, "Hilo Nearshore") },
            { DatasetIds.Hilo_WaterQuality, (19.7310f, -155.0580f, "Hilo Bay WQ Buoy") },
            { DatasetIds.AlaWai_WaterQual,  (21.2882f, -157.8432f, "Ala Wai Canal WQ") },
        };

        // ── Fetch State ─────────────────────────────────────────────────────

        /// <summary>Whether a fetch is currently in progress.</summary>
        public bool IsFetching { get; private set; }

        /// <summary>Last error message (null if last fetch succeeded).</summary>
        public string LastError { get; private set; }

        /// <summary>Last successful response time in milliseconds.</summary>
        public float LastResponseTimeMs { get; private set; }

        /// <summary>Total successful fetches.</summary>
        public int TotalFetches { get; private set; }

        // ── ERDDAP Response Model ───────────────────────────────────────────

        /// <summary>
        /// Parsed ERDDAP tabledap JSON response.
        /// ERDDAP JSON format:
        /// {
        ///   "table": {
        ///     "columnNames": ["time", "temperature", ...],
        ///     "columnTypes": ["String", "float", ...],
        ///     "rows": [["2026-03-23T00:00:00Z", 25.3, ...], ...]
        ///   }
        /// }
        /// </summary>
        [Serializable]
        public class ErddapResponse
        {
            public ErddapTable table;
        }

        [Serializable]
        public class ErddapTable
        {
            public string[] columnNames;
            public string[] columnTypes;
            public string[][] rows;
        }

        // ── Fetch Methods ───────────────────────────────────────────────────

        /// <summary>
        /// Fetches the latest water temperature data from a PacIOOS station.
        /// Returns data points mapped to an HGCS grid.
        ///
        /// Usage (from a MonoBehaviour):
        ///   StartCoroutine(fetcher.FetchTemperature("aws_himb", grid, 24, callback));
        /// </summary>
        /// <param name="datasetId">ERDDAP dataset ID (use DatasetIds constants).</param>
        /// <param name="grid">HGCS grid for coordinate mapping.</param>
        /// <param name="hoursBack">How many hours of data to retrieve.</param>
        /// <param name="onComplete">Callback with fetched data points.</param>
        public IEnumerator FetchTemperature(
            string datasetId,
            DeterministicGrid grid,
            int hoursBack,
            Action<List<ResearchDataProvider.DataPoint>> onComplete)
        {
            string timeConstraint = BuildTimeConstraint(hoursBack);

            // ERDDAP REST query: select time + temperature columns
            string url = $"{BaseUrl}/{datasetId}.json" +
                         $"?time,sea_water_temperature" +
                         $"&time>={timeConstraint}" +
                         $"&orderBy(%22time%22)";

            yield return FetchAndParse(url, datasetId, grid, "Temperature", "°C", onComplete);
        }

        /// <summary>
        /// Fetches multi-parameter water quality data (temp, salinity, DO, pH).
        /// </summary>
        public IEnumerator FetchWaterQuality(
            string datasetId,
            DeterministicGrid grid,
            int hoursBack,
            Action<List<ResearchDataProvider.DataPoint>> onComplete)
        {
            string timeConstraint = BuildTimeConstraint(hoursBack);

            string url = $"{BaseUrl}/{datasetId}.json" +
                         $"?time,sea_water_temperature,sea_water_salinity," +
                         $"mass_concentration_of_oxygen_in_sea_water,sea_water_ph_reported_on_total_scale" +
                         $"&time>={timeConstraint}" +
                         $"&orderBy(%22time%22)";

            yield return FetchAndParse(url, datasetId, grid, "WaterQuality", "", onComplete);
        }

        /// <summary>
        /// Generic fetch: retrieves any columns from any ERDDAP dataset.
        /// </summary>
        /// <param name="datasetId">Dataset identifier.</param>
        /// <param name="columns">Comma-separated column names.</param>
        /// <param name="constraints">Additional ERDDAP constraints (URL-encoded).</param>
        /// <param name="grid">HGCS grid for mapping.</param>
        /// <param name="onComplete">Callback with raw parsed response.</param>
        public IEnumerator FetchRaw(
            string datasetId,
            string columns,
            string constraints,
            Action<ErddapResponse> onComplete)
        {
            string url = $"{BaseUrl}/{datasetId}.json?{columns}&{constraints}";

            IsFetching = true;
            LastError = null;
            float startTime = Time.realtimeSinceStartup;

            using var request = UnityWebRequest.Get(url);
            request.timeout = 30;
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            LastResponseTimeMs = (Time.realtimeSinceStartup - startTime) * 1000f;

            if (request.result != UnityWebRequest.Result.Success)
            {
                LastError = $"ERDDAP fetch failed: {request.error} (HTTP {request.responseCode})";
                Debug.LogError($"[PXG.HGCS.Research] {LastError}");
                Debug.LogError($"[PXG.HGCS.Research] URL: {url}");
                onComplete?.Invoke(null);
            }
            else
            {
                try
                {
                    var response = JsonUtility.FromJson<ErddapResponse>(request.downloadHandler.text);
                    TotalFetches++;
                    Debug.Log($"[PXG.HGCS.Research] ERDDAP: {response.table.rows.Length} rows " +
                              $"from {datasetId} in {LastResponseTimeMs:F0}ms");
                    onComplete?.Invoke(response);
                }
                catch (Exception ex)
                {
                    LastError = $"JSON parse error: {ex.Message}";
                    Debug.LogError($"[PXG.HGCS.Research] {LastError}");
                    onComplete?.Invoke(null);
                }
            }

            IsFetching = false;
        }

        // ── Internal Fetch + Grid Mapping ───────────────────────────────────

        private IEnumerator FetchAndParse(
            string url,
            string datasetId,
            DeterministicGrid grid,
            string category,
            string unit,
            Action<List<ResearchDataProvider.DataPoint>> onComplete)
        {
            IsFetching = true;
            LastError = null;
            float startTime = Time.realtimeSinceStartup;

            using var request = UnityWebRequest.Get(url);
            request.timeout = 30;
            request.SetRequestHeader("Accept", "application/json");

            Debug.Log($"[PXG.HGCS.Research] Fetching: {url}");

            yield return request.SendWebRequest();

            LastResponseTimeMs = (Time.realtimeSinceStartup - startTime) * 1000f;

            if (request.result != UnityWebRequest.Result.Success)
            {
                LastError = $"ERDDAP fetch failed: {request.error} (HTTP {request.responseCode})";
                Debug.LogError($"[PXG.HGCS.Research] {LastError}");
                onComplete?.Invoke(new List<ResearchDataProvider.DataPoint>());
                IsFetching = false;
                yield break;
            }

            // Parse ERDDAP JSON
            List<ResearchDataProvider.DataPoint> dataPoints;
            try
            {
                var response = JsonUtility.FromJson<ErddapResponse>(request.downloadHandler.text);
                dataPoints = MapToGrid(response, datasetId, grid, category, unit);
                TotalFetches++;

                Debug.Log($"[PXG.HGCS.Research] ERDDAP success: {dataPoints.Count} data points " +
                          $"from {datasetId} in {LastResponseTimeMs:F0}ms");
            }
            catch (Exception ex)
            {
                LastError = $"Parse error: {ex.Message}";
                Debug.LogError($"[PXG.HGCS.Research] {LastError}");
                dataPoints = new List<ResearchDataProvider.DataPoint>();
            }

            IsFetching = false;
            onComplete?.Invoke(dataPoints);
        }

        // ── Grid Mapping ────────────────────────────────────────────────────

        /// <summary>
        /// Maps ERDDAP response rows to HGCS grid data points.
        /// X-axis = time (first column), Y-axis = primary value (second column).
        /// </summary>
        private List<ResearchDataProvider.DataPoint> MapToGrid(
            ErddapResponse response,
            string datasetId,
            DeterministicGrid grid,
            string category,
            string unit)
        {
            var points = new List<ResearchDataProvider.DataPoint>();
            var rows = response.table.rows;
            var colNames = response.table.columnNames;

            if (rows == null || rows.Length == 0) return points;

            int gridW = grid.Dimensions.x * grid.CellSize;
            int gridH = grid.Dimensions.y * grid.CellSize;

            // Get station name
            string stationName = StationCoords.ContainsKey(datasetId)
                ? StationCoords[datasetId].name
                : datasetId;

            // Find value range for Y-axis normalization
            float minVal = float.MaxValue, maxVal = float.MinValue;
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i].Length > 1 && float.TryParse(rows[i][1], out float v))
                {
                    minVal = Mathf.Min(minVal, v);
                    maxVal = Mathf.Max(maxVal, v);
                }
            }

            float valRange = Mathf.Max(maxVal - minVal, 0.01f);

            for (int i = 0; i < rows.Length; i++)
            {
                string[] row = rows[i];
                if (row.Length < 2) continue;

                // X = linear time distribution across grid width
                int x = Mathf.RoundToInt((i / (float)Mathf.Max(rows.Length - 1, 1)) * gridW);

                // Y = normalized value mapped to grid height
                float rawValue = 0f;
                if (float.TryParse(row[1], out float val))
                    rawValue = val;
                int y = Mathf.RoundToInt(((rawValue - minVal) / valRange) * gridH);

                var dp = new ResearchDataProvider.DataPoint
                {
                    Id = $"LIVE_{datasetId}_{i:D4}",
                    GridPosition = grid.Snap(new Vector2(x, y)),
                    Label = $"{stationName}: {rawValue:F2}{unit}",
                    Value = Mathf.Clamp(((rawValue - minVal) / valRange) * 3f + 0.5f, 0.5f, 4f),
                    SecondaryValue = rawValue,
                    Category = category
                };

                // Populate metadata from all columns
                for (int c = 0; c < colNames.Length && c < row.Length; c++)
                {
                    dp.Metadata[colNames[c]] = row[c];
                }
                dp.Metadata["source"] = "PacIOOS ERDDAP";
                dp.Metadata["dataset"] = datasetId;
                dp.Metadata["station"] = stationName;
                dp.Metadata["live"] = "true";

                points.Add(dp);
            }

            return points;
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        /// <summary>
        /// Builds an ISO 8601 time constraint for ERDDAP queries.
        /// </summary>
        private string BuildTimeConstraint(int hoursBack)
        {
            DateTime utcNow = DateTime.UtcNow;
            DateTime start = utcNow.AddHours(-hoursBack);
            return start.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        /// <summary>
        /// Returns the status of the fetcher for debug display.
        /// </summary>
        public string GetStatus() =>
            $"[PacIOOS] Fetches: {TotalFetches} | " +
            $"Fetching: {IsFetching} | " +
            $"Last: {LastResponseTimeMs:F0}ms | " +
            $"Error: {LastError ?? "none"}";
    }
}
