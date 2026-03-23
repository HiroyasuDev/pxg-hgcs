// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// Research Module: Ocean Science Data Provider
// Bathymetric grid overlay, current vectors, buoy sensor panels.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PXG.HGCS.Research
{
    /// <summary>
    /// Ocean sciences data visualization provider for Pacific region
    /// research. Maps bathymetric depth data, ocean current vector fields,
    /// and buoy sensor networks to the HGCS grid.
    ///
    /// Relevant UH programs: School of Ocean and Earth Science and
    /// Technology (SOEST), Pacific Islands Ocean Observing System (PacIOOS),
    /// Hawai'i Institute of Marine Biology (HIMB).
    /// </summary>
    public class OceanScienceDataProvider : ResearchDataProvider
    {
        public override string DomainName => "Ocean";
        public override string DomainIcon => "🌊";

        // ── Ocean Data Categories ───────────────────────────────────────────

        private static readonly string[] SensorTypes = {
            "Temperature", "Salinity", "Dissolved_O2", "pH",
            "Chlorophyll", "Turbidity", "Wave_Height", "Current_Speed"
        };

        private static readonly string[] BuoyStations = {
            "Waikiki", "Kaneohe_Bay", "Pearl_Harbor", "Makapuu",
            "Hilo_Bay", "Kawaihae", "Lahaina", "Maalaea",
            "PacIOOS_N", "PacIOOS_S", "ALOHA_Station", "HOTS_Deep"
        };

        // ── Buoy Sensor Data ────────────────────────────────────────────────

        /// <summary>
        /// Generates simulated buoy sensor data points mapped to the grid.
        /// Geographic positions approximate Hawaiian archipelago locations.
        /// </summary>
        public override List<DataPoint> GenerateDataPoints(DeterministicGrid grid, int count)
        {
            var points = new List<DataPoint>();
            var rng = new System.Random(42);

            int gridW = grid.Dimensions.x * grid.CellSize;
            int gridH = grid.Dimensions.y * grid.CellSize;

            // Hawaiian archipelago approximate bounds: 
            // Lat: 18.9°N to 22.2°N, Lon: -160.5°W to -154.8°W
            float latMin = 18.9f, latMax = 22.2f;
            float lonMin = -160.5f, lonMax = -154.8f;

            for (int i = 0; i < count; i++)
            {
                string station = BuoyStations[i % BuoyStations.Length];
                string sensor = SensorTypes[rng.Next(SensorTypes.Length)];

                // Simulate buoy positions within Hawaiian waters
                float lat = latMin + (float)rng.NextDouble() * (latMax - latMin);
                float lon = lonMin + (float)rng.NextDouble() * (lonMax - lonMin);

                // Map geographic coordinates to grid
                int x = Mathf.RoundToInt(((lon - lonMin) / (lonMax - lonMin)) * gridW);
                int y = Mathf.RoundToInt(((lat - latMin) / (latMax - latMin)) * gridH);

                // Sensor reading
                float reading = sensor switch
                {
                    "Temperature"   => 22f + (float)rng.NextDouble() * 6f,    // 22–28°C
                    "Salinity"      => 34f + (float)rng.NextDouble() * 2f,    // 34–36 PSU
                    "Dissolved_O2"  => 4f + (float)rng.NextDouble() * 4f,     // 4–8 mg/L
                    "pH"            => 7.8f + (float)rng.NextDouble() * 0.5f, // 7.8–8.3
                    "Chlorophyll"   => (float)rng.NextDouble() * 5f,          // 0–5 µg/L
                    "Turbidity"     => (float)rng.NextDouble() * 10f,         // 0–10 NTU
                    "Wave_Height"   => 0.5f + (float)rng.NextDouble() * 4f,   // 0.5–4.5 m
                    "Current_Speed" => (float)rng.NextDouble() * 2f,          // 0–2 m/s
                    _ => (float)rng.NextDouble() * 10f
                };

                var dp = new DataPoint
                {
                    Id = $"BUOY_{station}_{sensor}_{i:D3}",
                    GridPosition = grid.Snap(new Vector2(x, y)),
                    Label = $"{station}: {sensor}",
                    Value = Mathf.Clamp(reading / 10f, 0.3f, 3f),
                    SecondaryValue = reading,
                    Category = sensor,
                    Metadata =
                    {
                        ["station"] = station,
                        ["sensor_type"] = sensor,
                        ["reading"] = $"{reading:F2}",
                        ["latitude"] = $"{lat:F4}°N",
                        ["longitude"] = $"{Mathf.Abs(lon):F4}°W",
                        ["depth_m"] = $"{rng.Next(1, 200)}",
                        ["network"] = "PacIOOS"
                    }
                };

                points.Add(dp);
            }

            return points;
        }

        // ── Dashboard Layout ────────────────────────────────────────────────

        /// <summary>
        /// Creates a multi-panel ocean science dashboard:
        /// - Bathymetric Map (main, 65% width)
        /// - Current Vector Field (overlay on main)
        /// - Sensor Readings Panel (right sidebar, 35%)
        /// - Buoy Status Strip (bottom)
        /// </summary>
        public override List<PanelRegion> CreateDashboardLayout(DeterministicGrid grid)
        {
            int totalW = grid.Dimensions.x * grid.CellSize;
            int totalH = grid.Dimensions.y * grid.CellSize;

            int mainW = Mathf.RoundToInt(totalW * 0.65f);
            int sideW = totalW - mainW;
            int mainH = Mathf.RoundToInt(totalH * 0.85f);
            int barH = totalH - mainH;

            return new List<PanelRegion>
            {
                new PanelRegion
                {
                    Name = "Bathymetric Map",
                    Origin = grid.Snap(Vector2.zero),
                    Size = new Vector2Int(mainW, mainH)
                },
                new PanelRegion
                {
                    Name = "Sensor Readings",
                    Origin = grid.Snap(new Vector2(mainW, 0)),
                    Size = new Vector2Int(sideW, mainH / 2)
                },
                new PanelRegion
                {
                    Name = "Tide / Wave Timeline",
                    Origin = grid.Snap(new Vector2(mainW, mainH / 2)),
                    Size = new Vector2Int(sideW, mainH / 2)
                },
                new PanelRegion
                {
                    Name = "Buoy Network Status",
                    Origin = grid.Snap(new Vector2(0, mainH)),
                    Size = new Vector2Int(totalW, barH)
                }
            };
        }

        // ── Bathymetric Depth Grid ──────────────────────────────────────────

        /// <summary>
        /// Generates a simulated bathymetric depth grid (values in meters).
        /// Returns a 2D array [columns × rows] of depth values.
        /// Negative = below sea level, positive = above (island peaks).
        /// </summary>
        public float[,] GenerateBathymetricGrid(DeterministicGrid grid)
        {
            int cols = grid.Dimensions.x;
            int rows = grid.Dimensions.y;
            var depths = new float[cols, rows];
            var rng = new System.Random(42);

            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    // Simulate ocean floor with island peaks
                    float baseDepth = -4000f; // deep ocean
                    float cx = cols * 0.5f, cy = rows * 0.5f;
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float islandInfluence = Mathf.Max(0, 1f - dist / (cols * 0.3f));
                    float noise = (float)(rng.NextDouble() - 0.5) * 500f;

                    depths[x, y] = baseDepth + islandInfluence * 5000f + noise;
                }
            }

            return depths;
        }

        // ── Current Vector Field ────────────────────────────────────────────

        /// <summary>
        /// Generates simulated ocean current vectors at each grid cell.
        /// Returns a 2D array of direction vectors [columns × rows].
        /// </summary>
        public Vector2[,] GenerateCurrentField(DeterministicGrid grid)
        {
            int cols = grid.Dimensions.x;
            int rows = grid.Dimensions.y;
            var currents = new Vector2[cols, rows];
            var rng = new System.Random(42);

            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    // North Pacific Current approximation + local perturbation
                    float baseAngle = Mathf.PI * 0.75f; // NW flow
                    float perturbation = (float)(rng.NextDouble() - 0.5) * 0.5f;
                    float speed = 0.1f + (float)rng.NextDouble() * 1.5f;

                    float angle = baseAngle + perturbation;
                    currents[x, y] = new Vector2(
                        Mathf.Cos(angle) * speed,
                        Mathf.Sin(angle) * speed
                    );
                }
            }

            return currents;
        }
    }
}
