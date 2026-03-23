// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// Spec 5: Version Control
// Lightweight binary architecture minimizes "meta-file bloat" and storage.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PXG.HGCS
{
    /// <summary>
    /// Lightweight binary serialization for HGCS layout data. Designed to
    /// minimize "meta-file bloat" and fit within Unity's 25GB free Version
    /// Control tier.
    ///
    /// Poster ref: "Lightweight binary architecture minimizes 'meta-file
    /// bloat' and storage footprint."
    ///
    /// Layout files are stored as compact binary (.hgcs) rather than
    /// text-based YAML/JSON, reducing repo size and merge-conflict surface
    /// area for binary assets.
    /// </summary>
    public static class BinaryVersionControl
    {
        // ── Constants ───────────────────────────────────────────────────────

        /// <summary>Magic bytes identifying an HGCS binary layout file.</summary>
        private static readonly byte[] MagicBytes = { 0x48, 0x47, 0x43, 0x53 }; // "HGCS"

        /// <summary>Current binary format version.</summary>
        public const ushort FormatVersion = 1;

        /// <summary>File extension for HGCS layout files.</summary>
        public const string FileExtension = ".hgcs";

        // ── Serialization ───────────────────────────────────────────────────

        /// <summary>
        /// Serializes a set of anchor points to a compact binary layout file.
        /// Format: [HGCS magic 4B][version 2B][count 4B][anchors...]
        /// Each anchor: [id-length 1B][id UTF8][x 4B][y 4B][scale 4B]
        /// </summary>
        public static byte[] Serialize(IReadOnlyList<AnchorPoint> anchors)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // Header
            writer.Write(MagicBytes);
            writer.Write(FormatVersion);
            writer.Write(anchors.Count);

            // Anchor data
            foreach (var anchor in anchors)
            {
                byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(anchor.Id);
                writer.Write((byte)idBytes.Length);
                writer.Write(idBytes);
                writer.Write(Mathf.RoundToInt(anchor.Position.x));
                writer.Write(Mathf.RoundToInt(anchor.Position.y));
                writer.Write(anchor.Scale);
            }

            return ms.ToArray();
        }

        /// <summary>
        /// Deserializes a binary layout file back into anchor points.
        /// </summary>
        public static List<AnchorPoint> Deserialize(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            // Validate magic
            byte[] magic = reader.ReadBytes(4);
            for (int i = 0; i < 4; i++)
            {
                if (magic[i] != MagicBytes[i])
                    throw new InvalidDataException("Not a valid HGCS layout file.");
            }

            ushort version = reader.ReadUInt16();
            if (version > FormatVersion)
                throw new InvalidDataException($"Unsupported HGCS format version: {version}");

            int count = reader.ReadInt32();
            var anchors = new List<AnchorPoint>(count);

            for (int i = 0; i < count; i++)
            {
                byte idLen = reader.ReadByte();
                string id = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(idLen));
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                float scale = reader.ReadSingle();

                anchors.Add(new AnchorPoint(id, new Vector2(x, y), scale));
            }

            return anchors;
        }

        // ── File I/O ────────────────────────────────────────────────────────

        /// <summary>Writes anchor data to a .hgcs file on disk.</summary>
        public static void SaveToFile(string path, IReadOnlyList<AnchorPoint> anchors)
        {
            byte[] data = Serialize(anchors);
            File.WriteAllBytes(path, data);
            Debug.Log($"[PXG.HGCS] Saved {anchors.Count} anchors to {path} ({data.Length} bytes)");
        }

        /// <summary>Reads anchor data from a .hgcs file on disk.</summary>
        public static List<AnchorPoint> LoadFromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"HGCS layout file not found: {path}");

            byte[] data = File.ReadAllBytes(path);
            return Deserialize(data);
        }

        // ── Storage Metrics ─────────────────────────────────────────────────

        /// <summary>
        /// Estimates storage size for N anchors in binary vs. JSON format.
        /// Used for reporting meta-file bloat reduction.
        /// </summary>
        public static (long binaryBytes, long estimatedJsonBytes, float reductionPercent) EstimateStorageSavings(int anchorCount, int avgIdLength = 16)
        {
            // Binary: 4 magic + 2 version + 4 count + N * (1 idLen + avgId + 4 x + 4 y + 4 scale)
            long binary = 10 + anchorCount * (1 + avgIdLength + 12);

            // JSON estimate: {"id":"...","x":0000,"y":0000,"scale":1.00},\n per anchor + overhead
            long json = 64 + anchorCount * (avgIdLength + 48);

            float reduction = 1f - (binary / (float)json);
            return (binary, json, reduction * 100f);
        }
    }
}
