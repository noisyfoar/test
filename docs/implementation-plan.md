# План реализации LIS на C# (.NET Framework 4.8)

## Короткий вывод

Проект полностью переведён в **LIS-first** направление.
Иной исторический формат больше не является целью этой кодовой базы.

Основной фокус: надёжно читать LIS79, валидировать физическую раскладку и
постепенно добавлять декодирование содержимого записей.

---

## Что уже реализовано

1. Базовый LIS79 транспортный слой:
   - Physical Record Header (PRH),
   - Logical Record Header (LRH),
   - объединение логической записи из нескольких physical records.
2. Валидация:
   - корректность длин PR,
   - проверка predecessor/successor цепочки,
   - проверка допустимого типа LIS record.
3. Тесты:
   - unit-тесты на корректные и ошибочные кейсы парсинга.
4. Индексация logical records:
   - `LisIndexer`,
   - `LisRecordIndex` / `LisRecordInfo`,
   - разделение на explicit/implicit записи и фильтрация по типу.
5. Декодирование fixed/text records:
   - `LisFixedRecordParser`,
   - File Header/Trailer,
   - Reel/Tape Header/Trailer,
   - текстовые records.
6. Начальный DFSR-парсер:
   - `LisDfsrParser`,
   - entry blocks,
   - spec blocks subtype 0/1.

---

## Фазы работ дальше

### Фаза 0 — Стабилизация транспорта (LIS PR/LR layout)
- Добавить больше real-world кейсов (padding/обрывы/нестандартные трейлеры).
- Проверить совместимость поведения с эталонной реализацией на sample LIS.

### Фаза 1 — Индексация logical records (выполнено базово)
- [x] Индексация записей внутри файла с позициями и типами.
- [x] Разделение на implicit/explicit на уровне индекса.
- [x] Поддержка чтения последовательности logical files в одном физическом файле.
- [x] Индексация без materialize payload (skip-only path).

### Фаза 2 — Декодирование fixed/explicit records
- [x] File Header/Trailer, Reel Header/Trailer, Tape Header/Trailer.
- [x] Текстовые records (операторские комментарии, системные сообщения).

### Фаза 3 — DFSR/FData (кривые)
- [x] Базовый Data Format Specification Record (DFSR) parser:
  - entry blocks,
  - spec blocks subtype 0/1.
- [ ] Полный DFSR с расширенной валидацией и репкодовыми ограничениями.
- [x] Базовый parser implicit records (Normal/Alternate Data) в кадры/каналы.
- [ ] Полный parser кривых с поддержкой всех reprc и fast-channel сценариев.
- [x] Выборочная декодировка каналов по mnemonic (`selectedCurveMnemonics`).
- [x] Curves-only режим (без `Frames`) для снижения памяти.
- [x] Метрики чтения/декодирования (`LisReadMetrics`).

### Фаза 4 — API уровня потребителя
- [x] Базовый API для:
  - парсинга logical files (`LisLogicalFileParser`),
  - парсинга файла целиком (`LisFileParser`).
- [x] Расширить API до чтения curves/metadata в формате конечного пользовательского контракта.
- [x] Добавить raw import/export API:
  - `LisImporter` (LIS -> `LisDocument`),
  - `LisExporter` (`LisDocument` -> LIS),
  - `LisExportOptions` (контроль размера physical record).

### Фаза 5 — Оптимизация
- Профилирование на больших LIS.
- При необходимости: точечные C++ вставки.

---

## Риски

1. Исторические и “грязные” LIS-файлы с нестандартной раскладкой.
2. Различия между producer-реализациями в полях/записях.
3. Сложность декодирования DFSR/FData при fast-channel сценариях.

---

## Что считается ближайшим результатом

Ближайший milestone:
- стабильно прочитать LIS файл,
- [x] построить индекс logical records,
- [x] корректно декодировать базовые fixed records,
- [x] распарсить базовый DFSR (структурный уровень),
- [x] распарсить базовый FData (frame/channel level для fixed-size reprc),
- сохранить зелёный unit-test baseline.
