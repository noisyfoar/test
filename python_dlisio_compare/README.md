# Python + dlisio comparison project

Этот мини-проект сравнивает результаты чтения LIS-файла между:

1. `Lis.Core` (через C# CLI `Lis.Compare.Cli`)
2. `python + dlisio`

## Что сравнивается

- количество logical files;
- для каждого logical file:
  - `FileHeaderName`,
  - количество DFSR;
- для каждого DFSR:
  - `sample_rates`,
  - состав каналов (mnemonics),
  - `samples` и `representation code` по каналам.

> Примечание: `dlisio` и `Lis.Core` имеют разные внутренние модели кадра/кривых, поэтому сравнение сфокусировано на структурных и канальных данных.

## Установка

```bash
cd python_dlisio_compare
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

## Запуск

```bash
python compare_with_dlisio.py /path/to/file.lis --repo-root /workspace
```

Параметры:

- `lis_file` — путь к LIS файлу (обязательный)
- `--repo-root` — корень репозитория (по умолчанию автоопределение)
- `--core-json` — путь куда сохранить JSON summary от `Lis.Core` (по умолчанию `core_summary.json`)

## Выход

- `exit code 0`: критических расхождений не найдено;
- `exit code 1`: найдены критические расхождения (например, отсутствующие каналы или разное число DFSR);
- `exit code 2`: ошибка запуска/входных параметров.
