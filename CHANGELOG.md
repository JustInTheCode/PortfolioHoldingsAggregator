# Changelog

All notable changes to this project will be documented in this file.

## [1.0.0] - 2025-08-01

Initial release.

### Features

- Aggregate holdings from multiple CSV files.
- Accepts weights as either **percentages** (e.g., `7.8%`) or **fractions** (e.g., `0.078`).
- Supports custom **delimiter** (`,` or `;`) and **decimal separator** (`.` or `,`).
- Option to **reuse file settings** (delimiter, decimal separator, weight format) across multiple files for faster entry.
- Handles **duplicate symbols within a file** by merging values while preserving traceability.
- Allows overwriting previously added files by re-entering their path.
- Provides two output formats:
    - **Detailed**: Shows value and weight per holding source, plus totals.
    - **Compact**: Only includes aggregated totals.
- Outputs always use **fractional weights** to simplify further calculations.
- Aggregated weights are based on **actual calculated values** (no normalization of original weights).
- Prevents overwriting of previous results by auto-incrementing output file names.
- Allows rerunning aggregation with or without clearing previously added data.
