using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Lis.Core.Lis;

namespace Lis.Gui
{
    /// <summary>
    /// Minimal desktop viewer for inspecting LIS contents as text/tables.
    /// Charts are intentionally excluded to keep the UI lightweight.
    /// </summary>
    public sealed class MainForm : Form
    {
        private const string DefaultStatus = "Готово. Выберите LIS-файл.";
        private const string ReadingStatus = "Чтение LIS...";
        private const string ReadErrorStatus = "Ошибка при чтении LIS.";

        private readonly TextBox _filePathTextBox;
        private readonly Button _browseButton;
        private readonly Button _loadButton;
        private readonly CheckBox _curvesOnlyCheckBox;
        private readonly CheckBox _allowMalformedCheckBox;
        private readonly TextBox _selectedCurvesTextBox;
        private readonly Label _statusLabel;
        private readonly TextBox _reportTextBox;
        private readonly DataGridView _rawRecordsGrid;

        private sealed class LoadResult
        {
            public string StatusText { get; set; } = DefaultStatus;

            public string ReportText { get; set; } = string.Empty;

            public List<object[]> RawRows { get; } = new List<object[]>();
        }

        public MainForm()
        {
            ConfigureWindow();

            _filePathTextBox = CreatePathTextBox();
            _browseButton = CreateBrowseButton();
            _loadButton = CreateLoadButton();
            _curvesOnlyCheckBox = CreateCurvesOnlyCheckBox();
            _allowMalformedCheckBox = CreateAllowMalformedCheckBox();
            _selectedCurvesTextBox = CreateSelectedCurvesTextBox();
            _statusLabel = CreateStatusLabel();
            _reportTextBox = CreateReportTextBox();
            _rawRecordsGrid = CreateRawRecordsGrid();

            Controls.Add(BuildRootLayout());
        }

        private void OnBrowseClick(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "LIS files (*.lis)|*.lis|All files (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false,
                Title = "Выберите LIS-файл"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _filePathTextBox.Text = dialog.FileName;
            }
        }

        private async void OnLoadClick(object? sender, EventArgs e)
        {
            if (!TryGetValidatedInputPath(out string filePath))
            {
                return;
            }

            try
            {
                SetBusyState(true, ReadingStatus);
                ResetOutputViews();

                bool curvesOnly = _curvesOnlyCheckBox.Checked;
                bool allowMalformed = _allowMalformedCheckBox.Checked;
                IReadOnlyCollection<string>? selectedCurves = ParseSelectedCurves(_selectedCurvesTextBox.Text);
                LoadResult result = await Task.Run(() =>
                    LoadFile(filePath, selectedCurves, curvesOnly, allowMalformed));

                _reportTextBox.Text = result.ReportText;
                PopulateRawRecordsGrid(result.RawRows);
                _statusLabel.Text = result.StatusText;
            }
            catch (Exception ex)
            {
                _statusLabel.Text = ReadErrorStatus;
                _reportTextBox.Text = ex.ToString();
                _rawRecordsGrid.Rows.Clear();
            }
            finally
            {
                SetBusyState(false, _statusLabel.Text);
            }
        }

        private static TextBox CreatePathTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill
            };
        }

        private Button CreateBrowseButton()
        {
            var button = new Button
            {
                AutoSize = true,
                Text = "Выбрать LIS..."
            };
            button.Click += OnBrowseClick;
            return button;
        }

        private Button CreateLoadButton()
        {
            var button = new Button
            {
                AutoSize = true,
                Text = "Открыть"
            };
            button.Click += OnLoadClick;
            return button;
        }

        private static CheckBox CreateCurvesOnlyCheckBox()
        {
            return new CheckBox
            {
                AutoSize = true,
                Text = "Только кривые (без frames)"
            };
        }

        private static CheckBox CreateAllowMalformedCheckBox()
        {
            return new CheckBox
            {
                AutoSize = true,
                Text = "Разрешить повреждённые данные (tolerant-режим)"
            };
        }

        private static TextBox CreateSelectedCurvesTextBox()
        {
            return new TextBox
            {
                Width = 350
            };
        }

        private static Label CreateStatusLabel()
        {
            return new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Text = DefaultStatus
            };
        }

        private static TextBox CreateReportTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false
            };
        }

        private static DataGridView CreateRawRecordsGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            AddGridColumn(grid, "Seq", "#");
            AddGridColumn(grid, "LogicalFile", "Logical File");
            AddGridColumn(grid, "Offset", "Offset");
            AddGridColumn(grid, "Type", "Type");
            AddGridColumn(grid, "Attributes", "Attributes");
            AddGridColumn(grid, "PhysicalRecords", "Physical Records");
            AddGridColumn(grid, "DataLength", "Data Length");
            AddGridColumn(grid, "Class", "Class");
            return grid;
        }

        private static void AddGridColumn(DataGridView grid, string name, string caption)
        {
            grid.Columns.Add(name, caption);
        }

        private void ConfigureWindow()
        {
            Text = "Lis.NET Viewer";
            StartPosition = FormStartPosition.CenterScreen;
            Width = 1100;
            Height = 760;
            MinimumSize = new Size(900, 600);
        }

        private Control BuildRootLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(10)
            };

            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            root.Controls.Add(BuildFilePanel(), 0, 0);
            root.Controls.Add(BuildOptionsPanel(), 0, 1);
            root.Controls.Add(_statusLabel, 0, 2);
            root.Controls.Add(BuildTabs(), 0, 3);
            return root;
        }

        private Control BuildFilePanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3
            };

            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            panel.Controls.Add(_filePathTextBox, 0, 0);
            panel.Controls.Add(_browseButton, 1, 0);
            panel.Controls.Add(_loadButton, 2, 0);
            return panel;
        }

        private Control BuildOptionsPanel()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = true
            };

            panel.Controls.Add(_curvesOnlyCheckBox);
            panel.Controls.Add(_allowMalformedCheckBox);
            panel.Controls.Add(new Label
            {
                AutoSize = true,
                Padding = new Padding(10, 6, 0, 0),
                Text = "Выбранные curves (через запятую):"
            });
            panel.Controls.Add(_selectedCurvesTextBox);
            return panel;
        }

        private Control BuildTabs()
        {
            var tabs = new TabControl
            {
                Dock = DockStyle.Fill
            };

            var summaryTab = new TabPage("Сводка");
            summaryTab.Controls.Add(_reportTextBox);

            var rawTab = new TabPage("Raw records");
            rawTab.Controls.Add(_rawRecordsGrid);

            tabs.TabPages.Add(summaryTab);
            tabs.TabPages.Add(rawTab);
            return tabs;
        }

        private bool TryGetValidatedInputPath(out string path)
        {
            path = _filePathTextBox.Text.Trim();
            if (path.Length == 0)
            {
                _statusLabel.Text = "Ошибка: путь к файлу не задан.";
                return false;
            }

            if (!File.Exists(path))
            {
                _statusLabel.Text = "Ошибка: файл не найден.";
                return false;
            }

            return true;
        }

        private void SetBusyState(bool isBusy, string statusText)
        {
            Cursor = isBusy ? Cursors.WaitCursor : Cursors.Default;
            _loadButton.Enabled = !isBusy;
            _statusLabel.Text = statusText;
        }

        private void ResetOutputViews()
        {
            _reportTextBox.Text = string.Empty;
            _rawRecordsGrid.Rows.Clear();
        }

        private LoadResult LoadFile(
            string filePath,
            IReadOnlyCollection<string>? selectedCurves,
            bool curvesOnly,
            bool allowMalformedData)
        {
            using var stream = File.OpenRead(filePath);
            LisReadMetrics metrics = new LisReadMetrics();
            IReadOnlyList<LisLogicalFileData> parsed;
            bool usedAutomaticFallback = false;
            string? strictErrorText = null;

            try
            {
                parsed = ParseFiles(stream, selectedCurves, metrics, curvesOnly, allowMalformedData);
            }
            catch (LisParseException ex) when (!allowMalformedData)
            {
                // Автоматический fallback для «грязных» LIS-файлов.
                stream.Position = 0;
                metrics = new LisReadMetrics();
                parsed = ParseFiles(stream, selectedCurves, metrics, curvesOnly, allowMalformedData: true);
                usedAutomaticFallback = true;
                strictErrorText = ex.ToString();
            }

            var result = new LoadResult
            {
                StatusText = usedAutomaticFallback
                    ? ReadErrorStatus + " Автоматически включён tolerant-режим."
                    : DefaultStatus
            };

            string report = BuildReport(parsed, metrics, curvesOnly);
            if (usedAutomaticFallback && strictErrorText != null)
            {
                report = "Исходная ошибка strict-режима:\r\n" + strictErrorText + "\r\n\r\n" + report;
            }

            result.ReportText = report;

            stream.Position = 0;
            result.RawRows.AddRange(BuildRawRecordRows(stream));
            return result;
        }

        private IReadOnlyList<LisLogicalFileData> ParseFiles(
            Stream stream,
            IReadOnlyCollection<string>? selectedCurves,
            LisReadMetrics metrics,
            bool curvesOnly,
            bool allowMalformedData)
        {
            var parser = new LisFileParser();
            if (curvesOnly)
            {
                var options = new LisReadOptions(
                    selectedCurveMnemonics: selectedCurves,
                    includeFrames: false,
                    includeCurves: true,
                    allowMalformedData: allowMalformedData);
                return parser.Parse(stream, options, metrics);
            }

            var strictOptions = new LisReadOptions(
                selectedCurveMnemonics: selectedCurves,
                includeFrames: true,
                includeCurves: false,
                allowMalformedData: allowMalformedData);
            return parser.Parse(stream, strictOptions, metrics);
        }

        private static IReadOnlyCollection<string>? ParseSelectedCurves(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            string[] parts = input.Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var selected = new List<string>(parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                string value = parts[i].Trim();
                if (value.Length > 0)
                {
                    selected.Add(value);
                }
            }

            return selected.Count == 0 ? null : selected;
        }

        private static List<object[]> BuildRawRecordRows(Stream stream)
        {
            stream.Position = 0;
            var index = new LisIndexer().Index(stream, allowMalformedData: true);
            var logicalFiles = new LisLogicalFilePartitioner().Partition(index);

            var fileByOffset = new Dictionary<long, int>();
            for (int i = 0; i < logicalFiles.Count; i++)
            {
                IReadOnlyList<LisRecordInfo> records = logicalFiles[i].Records;
                for (int r = 0; r < records.Count; r++)
                {
                    fileByOffset[records[r].Offset] = i + 1;
                }
            }

            var rows = new List<object[]>(index.Records.Count);
            for (int i = 0; i < index.Records.Count; i++)
            {
                LisRecordInfo record = index.Records[i];
                string logicalFile = fileByOffset.TryGetValue(record.Offset, out int fileNumber)
                    ? fileNumber.ToString()
                    : "-";

                rows.Add(new object[]
                {
                    i + 1,
                    logicalFile,
                    record.Offset,
                    record.Type.ToString(),
                    "0x" + record.HeaderAttributes.ToString("X2"),
                    record.PhysicalRecordCount,
                    record.DataLength,
                    record.IsImplicitRecord ? "implicit" : "explicit"
                });
            }

            return rows;
        }

        private void PopulateRawRecordsGrid(IReadOnlyList<object[]> rows)
        {
            _rawRecordsGrid.Rows.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                _rawRecordsGrid.Rows.Add(rows[i]);
            }
        }

        /// <summary>
        /// Builds a human-readable text report for quick diagnostics and support.
        /// </summary>
        private static string BuildReport(IReadOnlyList<LisLogicalFileData> files, LisReadMetrics metrics, bool curvesOnly)
        {
            var sb = new StringBuilder();
            sb.AppendLine("LIS parse report");
            sb.AppendLine("================");
            sb.AppendLine("Logical files: " + files.Count);
            sb.AppendLine();

            for (int i = 0; i < files.Count; i++)
            {
                LisLogicalFileData file = files[i];
                sb.AppendLine("File #" + (i + 1));
                sb.AppendLine("  FileName: " + (file.FileHeader?.FileName ?? "<none>"));
                sb.AppendLine("  Text records: " + file.TextRecords.Count);
                sb.AppendLine("  DFSR records: " + file.DataFormatSpecifications.Count);
                sb.AppendLine("  Frames: " + file.Frames.Count);
                sb.AppendLine("  Curves: " + file.Curves.Count);

                if (curvesOnly && file.Curves.Count > 0)
                {
                    sb.AppendLine("  Curve sample counts:");
                    foreach (KeyValuePair<string, IReadOnlyList<object>> curve in file.Curves)
                    {
                        sb.AppendLine("    - " + curve.Key + ": " + curve.Value.Count);
                    }
                }
                else if (!curvesOnly && file.Frames.Count > 0)
                {
                    sb.AppendLine("  Channels in first frame:");
                    IReadOnlyList<LisFrameChannelData> channels = file.Frames[0].Channels;
                    for (int c = 0; c < channels.Count; c++)
                    {
                        sb.AppendLine("    - " + channels[c].Mnemonic + " (" + channels[c].Samples.Length + " samples/frame)");
                    }
                }

                sb.AppendLine();
            }

            sb.AppendLine("Metrics");
            sb.AppendLine("-------");
            sb.AppendLine("LogicalRecordsRead: " + metrics.LogicalRecordsRead);
            sb.AppendLine("FdataBytesRead: " + metrics.FdataBytesRead);
            sb.AppendLine("SamplesDecoded: " + metrics.SamplesDecoded);
            sb.AppendLine("SamplesSkipped: " + metrics.SamplesSkipped);
            sb.AppendLine("MalformedRecordsSkipped: " + metrics.MalformedRecordsSkipped);
            sb.AppendLine("ParseElapsedMilliseconds: " + metrics.ParseElapsedMilliseconds);

            return sb.ToString();
        }
    }
}
