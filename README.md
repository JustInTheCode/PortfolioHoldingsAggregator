# Portfolio Holdings Aggregator

A simple CLI tool to aggregate holdings from one or more CSV files.

---

## Use Cases

This tool is useful if you want to:

- Combine holdings from multiple accounts into a single aggregated view.
- Understand your exposure to specific assets, even if they are held across different sources — such as multiple accounts or funds.
- Explore how changing your allocations (e.g., increasing QQQ or gold) affects your overall exposure to specific assets (e.g., NVDA).

### Example:
1. **Comparing overlapping ETFs**  
   Suppose you're invested in QQQ, IVV, and VTV. Since IVV already covers about 95% of VTV’s holdings, you're considering closing VTV.  
   However, VTV differs in weighting and excludes certain growth stocks like NVDA — reducing single-stock exposure.  
   For example, a 50/50 allocation to QQQ and IVV results in an 8.42% portfolio weight in NVDA.  
   Brokers like Interactive Brokers typically show only your top 25 holdings and don’t allow allocation simulations — so you'd normally do this manually in Excel.  
   With this tool, you can:
   - Import CSVs from ETF providers (e.g., via a free trial on sites like etfdb.com),
   - Simulate different allocations,
   - View exposure across your entire portfolio — not just the top 10–25 holdings,
   - Quickly iterate and compare different ETF combinations and weights.

2. **Aggregating holdings across accounts**  
   You hold assets across several brokerages (e.g., IBKR, Schwab, and Fidelity). While each platform may show your top holdings or even the full list, none gives a complete picture across all accounts.  
   By exporting each account’s holdings to CSV and importing them into this tool, you can:
   - View your consolidated portfolio in one place,
   - Understand your true aggregate exposure to individual stocks,
   - Detect overconcentration (e.g., holding NVDA through both a tech ETF and a thematic AI fund).

---

## How to Download & Run

1. Navigate to the [download](./download) folder in this repository.
2. Select the latest version (e.g., `v1.0.0`).
3. Pick the ZIP file for your operating system and CPU architecture. *(If you’re unsure and on Windows, choose `win-x64.zip`.)*
4. Download and unzip the archive.
5. Run the included `PortfolioHoldingsAggregator.exe`.

No installation required — just unzip and run.

---

## CSV Format

Each file must contain the following fields (**no header row**):

- **Name** of the holding (e.g., `Apple Inc`)
- **Symbol** (e.g., `AAPL`)
- **Weight** — either:
  - A **percentage** like `7.8%`
  - Or a **fraction** like `0.078`

---

## How to Use

1. Launch the application (`PortfolioHoldingsAggregator.exe`).
2. For each file you'd like to include, enter its full file path.
3. Enter the total value of all holdings in the file.
4. Specify the **delimiter** used in the file (`,` or `;`).
5. Specify the **decimal separator** used in the file (`.` or `,`).
6. Specify whether the weights are given as **percentages** (e.g., `7.58%`) or **fractions** (e.g., `0.0758`).
7. Repeat this process for each file.

> 💡 **Tip:** To update an already added file, simply enter the same file path again and choose to overwrite it when prompted.

Once all files have been added, the tool will automatically aggregate the holdings and generate a CSV file with the combined results.
After each aggregation, you can choose to exit or rerun — either with a fresh start by clearing the previously added sources, or by keeping them.

---

## Output

- The aggregated results are saved as a CSV file in the **same folder** as the executable.
- By default, the file is named `aggregated_portfolio.csv`, but you can also choose a custom name.
- If the file already exists, a new file will be created with an **incremented number suffix**  
  (e.g., `aggregated_portfolio_1.csv`, `aggregated_portfolio_2.csv`, etc.).
- Existing files are **never overwritten**.
- The `Weight` column is always expressed as a fraction (e.g., `0.0758` for `7.58%`), regardless of how weights were originally entered.

### Output formats
There are two output formats to choose from:

#### Compact
Includes only the **total value** and **total weight** of each holding, aggregated across all sources — plus a final total row. 

##### Example:

| Name               | Symbol | Total Weight | Total Value |
|--------------------|--------|--------------|-------------|
| NVIDIA Corporation | NVDA   | 0.0561...    | 168.4       |
| ...                | ...    | ...          | ...         |
| Total              | N/A    | 1            | 3000        |

#### Detailed 
Includes the **value** and **weight** of each holding **per source**, as well as the total value and total weight for each holding — plus a final total row.

In the detailed output, each source gets its own column for both weight and value. Column names are derived from the original filename. For example, if the file was named 
`VTV-holdings.csv`, the columns will be named `VTV-holdings Weight` and `VTV-holdings Value`.

If multiple files share the same name but are located in different directories (e.g., `C:\QQQ\holdings.csv` and `C:\IVV\holdings.csv`), the folder name will be appended for uniqueness.  

##### Example:

| Name    | Symbol | VTV-holdings Weight | VTV-holdings Value | holdings (QQQ) Weight | holdings (QQQ) Value | holdings (IVV) Weight | holdings (IVV) Value | Total Weight | Total Value |
|---------|--------|---------------------|--------------------|-----------------------|----------------------|-----------------------|----------------------|--------------|-------------|
| NVIDIA  | NVDA   | 0                   | 0                  | 0.0936                | 93.6                 | 0.0748                | 74.8                 | 0.0561...    | 168.4       |
| ...     | ...    | ...                 | ...                | ...                   | ...                  | ...                   | ...                  | ...          | ...         |
| Total   | N/A    | 0.333...            | 1000               | 0.333...              | 1000                 | 0.333...              | 1000                 | 1            | 3000        |


This format makes it easier to fine-tune data directly in Excel for faster iteration and feedback — without needing to rerun the tool.  
For example, you can convert raw values to formulas and manually adjust the total value per source to instantly see how allocation changes affect your overall portfolio.

➡️ See the [example-output](./example-output) folder for sample CSV files generated by the tool, including versions that have been imported into Excel with formatting and 
calculations applied.

---

## How It Works

For each file you add:

- The tool multiplies the **weight of each holding** by the **total value** you specify for that file.
- This results in the **absolute value** of each holding within that file.
- Holdings with the same symbol across multiple files are combined by summing their absolute values.
- The sum of all holdings from all files is then used to calculate final weights.

---

## Handling Duplicate Symbols

If the same symbol appears more than once within a single source (e.g., a source has multiple `Other` holdings), those entries are **merged**—their values and weights are combined into a single row for that source.

This does **not** affect the overall result, since holdings are always grouped by symbol across all sources. It only affects the **detailed output** of sources that contained the symbol multiple times.

To retain traceability, the name of each individual holding is preserved and suffixed with the source(s) it came from—while the symbol itself remains unchanged. 

### Example:

If `VTV` contains:
- `Other` for `U.S. Dollar` with a value of `1`
- `Other` for `Futures` with a value of `2`

And `QQQ` and `IVV` each contain `Other` for `U.S. Dollar` with a value of `4`,  
the final output will be (*weights omitted for brevity*):

| Name                                                                           | Symbol | VTV Value | QQQ Value | IVV Value | Total Value |
|--------------------------------------------------------------------------------|--------|-----------|-----------|-----------|-------------|
| U.S. Dollar (VTV-holdings, IVV-holdings, QQQ-holdings), FUTURES (VTV-holdings) | Other  | 3         | 4         | 4         | 11          |

Since `QQQ` and `IVV` each had only a single `Other` entry, it's clear their value of `4` corresponds to `U.S. Dollar`.  
In `VTV`, however, it's no longer possible to tell how much of the value `3` came from `U.S. Dollar` versus `Futures`.

---

## Inputted vs Actual Value Discrepancy

When importing a holding source, you're asked to provide the **total value** of the portfolio — this is referred to as the *inputted value*.  
Each holding’s value is calculated by multiplying this inputted value by the holding’s specified weight.

However, some files include **imprecise weights** that don’t exactly sum to 100%, often due to rounding.  
This can lead to a small difference between the *inputted* total value and the *actual* value (i.e., the sum of all calculated holding values).

### Example:

Suppose a CSV contains the following holdings:

| Name      | Symbol | Weight  |
|-----------|--------|---------|
| Apple     | AAPL   | 33.33%  |
| Microsoft | MSFT   | 33.33%  |
| Google    | GOOGL  | 33.33%  |

The weights sum to **99.99%**, not 100%.

If you input a total value of **1,000**, then each holding is assigned: `33.33% × 1,000 = 333.30`

So the **actual** total value becomes: `333.30 + 333.30 + 333.30 = 999.90`

### Aggregation Behavior

Now imagine a second source is added that also has a total inputted value of **1,000**, and contains:

| Name  | Symbol | Weight |
|-------|--------|--------|
| Apple | AAPL   | 10%    |
| ...   | ...    | ...    |

This means `AAPL` from the second source contributes: `10% × 1,000 = 100.00`

- The aggregated **value** for `AAPL` becomes: `333.30 (from first source) + 100.00 (from second source) = 433.30`
- The aggregated **total value** across all sources is: `999.90 + 1,000.00 = 1,999.90`
- The aggregated **weight** of `AAPL` is calculated as: `433.30 / 1,999.90 ≈ 21.666%` (*Precision will be higher in the actual output; shortened here for readability.*)

### Why It Works This Way

- Individual values from each source remain **unmodified**, preserving a clear audit trail.
- Aggregated weight percentages are always **normalized against the actual total value**, ensuring they reflect reality.
- This makes the output **easier to validate**:
  - You can compare the result to the original files.
  - The total row in the final table is **accurate**, because it's the true sum of the calculated values — not artificially normalized.
  - If you import the CSV into Excel and apply your own calculations, the totals and fractions will still match.