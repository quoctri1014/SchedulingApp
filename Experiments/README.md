# SA Ablation Study

The script compares four Simulated Annealing configurations on small, medium,
and large datasets. Each configuration is repeated 30 times by default.

Run the API first, then execute from the repository root:

```powershell
.\Experiments\run-sa-ablation.ps1
```

For a quick smoke run:

```powershell
.\Experiments\run-sa-ablation.ps1 -RunsPerConfiguration 2
```

Outputs are written to `Experiments/results/`:

- `sa-ablation-raw.csv`: one row per API run.
- `sa-ablation-summary.csv`: averages and min/max penalty by dataset/configuration.
- `sa-ablation-penalty.svg`: chart of average penalty scores.

The four configurations are:

- `baseline`: temperature 1000, cooling rate 0.95, 1000 iterations.
- `low-temperature`: temperature 100, other parameters unchanged.
- `fast-cooling`: cooling rate 0.80, other parameters unchanged.
- `short-search`: 100 iterations, other parameters unchanged.