# AI Model Options for HGCS Layout Optimization

## Status: No Pre-Trained `.onnx` Model Exists for This Use Case

There is no off-the-shelf ONNX model that does "UI layout optimization." The current `SentisModelLoader.cs` is a loader for a file that doesn't exist. Here are the realistic paths forward.

---

## Option A: Graph4GUI (Best Fit — Requires Work)

**Source:** [Graph4GUI (CHI 2024)](https://github.com/nicholaskgeorge/Graph4GUI)
**What it does:** GNN that learns UI element relationships and predicts positions for unplaced elements.
**Why it fits:** This is literally "AI-suggested layout autocompletion" — the closest match to what the poster claims.

### Steps to integrate:
1. Clone the Graph4GUI repo (PyTorch-based)
2. Train on the RICO UI dataset (25K Android screenshots)
3. Export via `torch.onnx.export()` to `layout_gnn.onnx`
4. Drop into Unity's `StreamingAssets/` folder
5. Load via `Unity.Sentis.ModelLoader.Load()`
6. Wire into existing `SentisModelLoader.cs`

### Estimated effort: **2-3 weeks** (training + export + integration testing)
### For the March 25 demo: **Not feasible**

---

## Option B: MLP Regression from Synthetic Data (Fast & Honest)

Train a minimal MLP (Multi-Layer Perceptron) on synthetic HGCS layout data.

### Architecture:
```
Input:  [N anchors × 5 features] → flatten → [N*5]
Hidden: Dense(256) → ReLU → Dense(128) → ReLU
Output: [N anchors × 3 deltas] (dx, dy, confidence)
```

### Training data: Generate with existing code
```python
# generate_training_data.py
# Use ConstraintSolver to produce "good" layouts
# Perturb them randomly to produce "bad" layouts
# Train: bad_layout → delta_to_good_layout
```

### ONNX export:
```python
import torch
model = LayoutMLP()
model.load_state_dict(torch.load("layout_mlp.pth"))
dummy = torch.randn(1, 256*5)
torch.onnx.export(model, dummy, "layout_mlp.onnx", opset_version=15)
```

### Estimated effort: **2-3 days** (Python script + training + export)
### Honest assessment: This will produce a model that "works" but is too simple to be meaningfully better than the heuristic fallback. It's an honest demo vs. an empty stub.

---

## Option C: Skip AI, Be Honest About Heuristics (Recommended for March 25)

The poster says "Sentis Integration" — but doesn't claim the model is trained.
The honest approach is to:

1. Keep the `AutoLayoutPipeline.cs` with its heuristic fallback
2. Label it clearly: "Heuristic-based layout optimization (Sentis-ready pipeline)"
3. Show the tensor I/O contract as the *interface* for future AI integration
4. Demo the pipeline *working* with heuristics — overlap reduction, spacing optimization

### This is defensible because:
- The architecture IS Sentis-ready (the tensor shapes, worker creation, async readback patterns are correct)
- You're showing the *engineering* that makes AI integration possible
- You're not claiming a trained model exists

---

## Verdict

| Option | Time | Honesty | Demo Impact |
|--------|------|---------|-------------|
| **A: Graph4GUI** | 2-3 weeks | ✅ Real AI | ❌ Not feasible for March 25 |
| **B: MLP Synthetic** | 2-3 days | ⚠️ Technically real but trivial | ⚠️ Marginal |
| **C: Heuristic + Honest Labels** | 0 days | ✅ Transparent | ✅ Shows architecture |

**Recommendation for March 25: Option C.** Show the pipeline architecture, demo the heuristics, and present the tensor contract as "Sentis-ready" rather than "Sentis-powered."
