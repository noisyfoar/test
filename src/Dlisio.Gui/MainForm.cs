using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Dlisio.Core.Lis;

namespace Dlisio.Gui
{
    public sealed class MainForm : Form
    {
        private readonly TextBox _filePathTextBox;
        private readonly Button _browseButton;
        private readonly Button _loadButton;
        private readonly CheckBox _curvesOnlyCheckBox;
        private readonly TextBox _selectedCurvesTextBox;
        private readonly Label _statusLabel;
        private readonly TextBox _reportTextBox;
        private readonly DataGridView _rawRecordsGrid;

        public MainForm()
        {
            Text = "LISIO.NET Viewer";
            StartPosition = FormStartPosition.CenterScreen;
            Width = 1100;
            Height = 760;
            MinimumSize = new System.Drawing.Size(900, 600);

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

            var filePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3
            };
            filePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            filePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            filePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _filePathTextBox = new TextBox
            {
                Dock = DockStyle.Fill
            };

            _browseButton = new Button
            {
                AutoSize = true,
                Text = "Выбрать LIS..."
            };
            _browseButton.Click += OnBrowseClick;

            _loadButton = new Button
            {
                AutoSize = true,
                Text = "Открыть"
            };
            _loadButton.Click += OnLoadClick;

            filePanel.Controls.Add(_filePathTextBox, 0, 0);
            filePanel.Controls.Add(_browseButton, 1, 0);
            filePanel.Controls.Add(_loadButton, 2, 0);

            var optionsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = true
            };

            _curvesOnlyCheckBox = new CheckBox
            {
                AutoSize = true,
                Text = "Только кривые (без frames)"
            };

            var selectedCurvesLabel = new Label
            {
                AutoSize = true,
                Padding = new Padding(10, 6, 0, 0),
                Text = "Выбранные curves (через запятую):"
            };

            _selectedCurvesTextBox = new TextBox
            {
                Width = 350
            };

            optionsPanel.Controls.Add(_curvesOnlyCheckBox);
            optionsPanel.Controls.Add(selectedCurvesLabel);
            optionsPanel.Controls.Add(_selectedCurvesTextBox);

            _statusLabel = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Text = "Готово. Выберите LIS-файл."
            };

            _reportTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false
            };

            _rawRecordsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _rawRecordsGrid.Columns.Add("Seq", "#");
            _rawRecordsGrid.Columns.Add("LogicalFile", "Logical File");
            _rawRecordsGrid.Columns.Add("Offset", "Offset");
            _rawRecordsGrid.Columns.Add("Type", "Type");
            _rawRecordsGrid.Columns.Add("Attributes", "Attributes");
            _rawRecordsGrid.Columns.Add("PhysicalRecords", "Physical Records");
            _rawRecordsGrid.Columns.Add("DataLength", "Data Length");
            _rawRecordsGrid.Columns.Add("Class", "Class");

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

            root.Controls.Add(filePanel, 0, 0);
            root.Controls.Add(optionsPanel, 0, 1);
            root.Controls.Add(_statusLabel, 0, 2);
            root.Controls.Add(tabs, 0, 3);

            Controls.Add(root);
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

        private void OnLoadClick(object? sender, EventArgs e)
        {
            string filePath = _filePathTextBox.Text.Trim();
            if (filePath.Length == 0)
            {
                _statusLabel.Text = "Ошибка: путь к файлу не задан.";
                return;
            }

            if (!File.Exists(filePath))
            {
                _statusLabel.Text = "Ошибка: файл не найден.";
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                _loadButton.Enabled = false;
                _reportTextBox.Text = string.Empty;
                _rawRecordsGrid.Rows.Clear();
                _statusLabel.Text = "Чтение LIS...";

                using var stream = File.OpenRead(filePath);
                var parser = new LisFileParser();
                var metrics = new LisReadMetrics();

                IReadOnlyCollection<string>? selected = ParseSelectedCurves(_selectedCurvesTextBox.Text);
                IReadOnlyList<LisLogicalFileData> parsed;
                if (_curvesOnlyCheckBox.Checked)
                {
                    parsed = parser.ParseCurves(stream, selected, metrics);
                }
                else
                {
                    var options = new LisReadOptions(
                        selectedCurveMnemonics: selected,
                        includeFrames: true,
                        includeCurves: false);
                    parsed = parser.Parse(stream, options, metrics);
                }

                _reportTextBox.Text = BuildReport(parsed, metrics, _curvesOnlyCheckBox.Checked);
                PopulateRawRecords(stream);
                _statusLabel.Text = "Готово.";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Ошибка при чтении LIS.";
                _reportTextBox.Text = ex.ToString();
                _rawRecordsGrid.Rows.Clear();
            }
            finally
            {
                _loadButton.Enabled = true;
                Cursor = Cursors.Default;
            }
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

        private void PopulateRawRecords(Stream stream)
        {
            stream.Position = 0;
            var index = new LisIndexer().Index(stream);
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

            _rawRecordsGrid.Rows.Clear();
            for (int i = 0; i < index.Records.Count; i++)
            {
                LisRecordInfo record = index.Records[i];
                string logicalFile = fileByOffset.TryGetValue(record.Offset, out int fileNumber)
                    ? fileNumber.ToString()
                    : "-";

                _rawRecordsGrid.Rows.Add(
                    i + 1,
                    logicalFile,
                    record.Offset,
                    record.Type.ToString(),
                    "0x" + record.HeaderAttributes.ToString("X2"),
                    record.PhysicalRecordCount,
                    record.DataLength,
                    record.IsImplicitRecord ? "implicit" : "explicit");
            }
        }

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
            sb.AppendLine("ParseElapsedMilliseconds: " + metrics.ParseElapsedMilliseconds);

            return sb.ToString();
        }
    }
}
