# План: Доработка деталей из сборки с вставкой крепежа и экспортом DXF

## Общий процесс

1. **Сканирование сборки** → 2 коллекции:
   - `NeedsReworkParts` — детали, которые нужно доработать
   - `ModifierFastenerParts` — крепёж/вставки для доработки

2. **Доработка каждой NeedsRework детали:**
   - Открываем документ детали (visible, т.к. нужно сохранить)
   - Вставляем в неё **все** ModifierFastener детали через `IParts7.AddFromFile`
   - Позиционируем каждый вставленный ModifierFastener по **относительным координатам** (разница PlacementMatrix крепежа и детали в исходной сборке)
   - Перестраиваем модель (`RebuildModel`)
   - Экспортируем DXF (развёртка листового тела) в ExportPath
   - Сохраняем изменённую деталь

## Файлы для изменения

- **`Kompas/KompasExporter.cs`** — новый метод `ReworkAndExportParts()`
- **`MainForm.cs`** — вызов нового метода после `ScanAssemblyForSpecialParts()`

## Изменения в KompasExporter.cs

### Новый метод `ReworkAndExportParts()`

```csharp
public void ReworkAndExportParts()
{
    if (NeedsReworkParts.Count == 0 || ModifierFastenerParts.Count == 0)
        return;

    foreach (var reworkPart in NeedsReworkParts)
    {
        // 1. Открыть документ детали (видимо, т.к. будем сохранять)
        IKompasDocument3D partDoc = (IKompasDocument3D)documents.Open(reworkPart.FullName, false, false);
        if (partDoc == null) continue;

        try
        {
            IPart7 topPart = partDoc.TopPart;
            IParts7 parts = topPart.Parts;

            // 2. Вставить все ModifierFastener
            foreach (var fastener in ModifierFastenerParts)
            {
                IPart7 inserted = parts.AddFromFile(fastener.FullName, true, false);
                if (inserted != null)
                {
                    // 3. Вычислить относительное положение:
                    //    матрица_крепежа_в_сборке * обратная_матрица_детали_в_сборке
                    double[] relativeMatrix = ComputeRelativeMatrix(
                        fastener.PlacementMatrix, reworkPart.PlacementMatrix);

                    if (relativeMatrix != null)
                    {
                        IPlacement3D placement = (IPlacement3D)inserted.Placement;
                        placement.InitByMatrix3D(relativeMatrix);
                        inserted.UpdatePlacement(true);
                    }
                }
            }

            // 4. Перестроить модель
            topPart.RebuildModel();

            // 5. Экспорт DXF (развёртка)
            var dxfNode = new ObjectAssemblyKompas
            {
                Designation = reworkPart.Designation,
                Name = reworkPart.Name,
                DxfFilePath = Path.Combine(ExportPath,
                    Path.GetFileNameWithoutExtension(reworkPart.FullName) + ".dxf")
            };
            ExportDxf(partDoc, dxfNode);

            // 6. Сохранить деталь
            IKompasDocument kompasDoc = (IKompasDocument)partDoc;
            kompasDoc.Save();
        }
        finally
        {
            // Закрыть документ
            IKompasDocument kompasDoc = (IKompasDocument)partDoc;
            kompasDoc.Close(false);
        }
    }
}
```

### Новый метод `ComputeRelativeMatrix()`

Вычисляет относительное положение крепежа к детали:
- Берём PlacementMatrix крепежа (положение в сборке)
- Берём PlacementMatrix детали (положение в сборке)
- Относительная матрица = M_fastener * inverse(M_part)

Матрица 4x4 (хранится как double[16]):
- Элементы [0..8] — матрица поворота 3x3
- Элементы [9..11] — смещение (translation)

### Изменение в ScanPartsRecursively

Текущий код сканирования с кэшированием уже работает корректно. Кэш предотвращает повторное открытие одинаковых деталей при чтении свойств. Но для `PlacementMatrix` каждая вставка уникальна — это уже учтено (матрица берётся для каждого `item` индивидуально).

## Изменения в MainForm.cs

После вызова `ScanAssemblyForSpecialParts()` добавить вызов:
```csharp
kompasExporter.ReworkAndExportParts();
```

## Этапы реализации

### Этап 1 (текущий): Сканирование + Доработка + Сохранение
- Сканирование сборки (уже реализовано)
- Новый метод `ReworkParts()`: открытие деталей, вставка крепежа, перестроение, сохранение

### Этап 2 (позже): Экспорт DXF
- Будет добавлен отдельно когда доберёмся

## Важные моменты

1. **Относительные координаты**: PlacementMatrix в сборке — абсолютное положение компонента. Для вставки крепежа внутрь детали вычисляем разницу матриц (M_fastener * inverse(M_part)).

2. **Сохранение в исходник**: деталь сохраняется после доработки обратно в свой файл.

3. **Уникальные NeedsRework**: если несколько одинаковых деталей (Name+Marking) помечены NeedsRework — файл-источник один, доработка нужна только один раз. Кэш по FullName предотвращает повторную обработку.

4. **ModifierFastenerParts — НЕ открывать**: в списке хранятся детали-модификаторы для доработки. Таких деталей в сборке может быть много (одинаковых вставок). Открывать их все не нужно — для вставки в NeedsRework деталь достаточно знать путь к файлу (`FullName`) и положение в сборке (`PlacementMatrix`). `AddFromFile` принимает путь к файлу, а не открытый документ. Координаты (PlacementMatrix) собираются при сканировании без открытия документа.

## Верификация

- Запустить приложение, открыть сборку в КОМПАС
- Вызвать ScanAssemblyForSpecialParts → проверить что коллекции заполнились
- Вызвать ReworkParts → проверить что:
  - Детали открываются
  - Крепёж вставляется в правильные координаты
  - Исходные детали сохранены с доработкой
