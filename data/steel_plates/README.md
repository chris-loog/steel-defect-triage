# Steel Plates Faults (real data)

- **Source:** UCI Machine Learning Repository – <https://archive.ics.uci.edu/dataset/198/steel+plates+faults>
- **Donors:** M. Buscema, S. Terzi, W. Tastle (Semeion, Research Center of Sciences of Communication, Rome), 2010
- **License:** [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) – free to use and share with attribution.
  Files are unchanged from the original zip (only file permissions reset).
- **Citation:** Buscema, M., Terzi, S., & Tastle, W. (2010). Steel Plates Faults [Dataset].
  UCI Machine Learning Repository. https://doi.org/10.24432/C5J88N

## Files
- `Faults.NNA` – 1,941 rows, tab-separated, no header, no missing values. One row = one detected surface defect.
- `Faults27x7_var` – the 34 column names, one per line (27 features, then 7 fault classes).

## Columns
- **27 features** (from the inspection system): defect bounding box (`X_Minimum` … `Y_Maximum`), size
  (`Pixels_Areas`, perimeters, `LogOfAreas`), brightness (`Sum/Minimum/Maximum_of_Luminosity`, `Luminosity_Index`),
  plate context (`Length_of_Conveyer`, `TypeOfSteel_A300/A400`, `Steel_Plate_Thickness`) and shape indices
  (`Edges_Index`, `Empty_Index`, `Square_Index`, `Orientation_Index`, …).
- **7 fault classes** (one-hot, exactly one = 1 per row):

  | Class | Rows |
  |---|---|
  | Pastry | 158 |
  | Z_Scratch | 190 |
  | K_Scatch (sic) | 391 |
  | Stains | 72 |
  | Dirtiness | 55 |
  | Bumps | 402 |
  | Other_Faults | 673 |

No timestamps → no time series; this is a classification dataset.
