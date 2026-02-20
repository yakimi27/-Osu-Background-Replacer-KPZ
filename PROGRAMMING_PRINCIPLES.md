# Аналіз дотримання принципів програмування у проєкті

Цей документ описує, як архітектура та код проєкту **Osu! Background Replacer** відповідають ключовим принципам розробки програмного забезпечення. 

## 1. Single Responsibility Principle (SRP) — Принцип єдиної відповідальності

**Опис:** Кожен клас або модуль відповідає лише за одну частину логіки і має лише одну причину для змін. У проєкті обов'язки чітко розділені між різними класами, що полегшує підтримку та тестування коду.

* Файл **[`FolderOperations.cs`](./OsuBackgroundReplacerMain/Logic/FolderOperations.cs)**: Відповідає виключно за логіку вибору директорій.
* Файл **[`ImageOperations.cs`](./OsuBackgroundReplacerMain/Logic/ImageOperations.cs)**: Містить лише логіку для вибору файлів зображень.
* Файл **[`Operations.cs`](./OsuBackgroundReplacerMain/Logic/Operations.cs)**: Ізолює бізнес-логіку заміни фонів від інтерфейсу користувача.

**Приклад з [`FolderOperations.cs`](./OsuBackgroundReplacerMain/Logic/FolderOperations.cs#L14-L36)**:
Цей клас не знає про існування зображень чи UI-елементів (окрім виклику помилок), він займається лише отриманням шляху до папки:
```csharp
public static async Task ChooseFolderManually(Window window)
{
    var folderPicker = new FolderPicker();
    // ... ініціалізація та вибір папки
    StorageFolder folder = await folderPicker.PickSingleFolderAsync();
    if (folder != null)
    {
        SelectedFolderPath = folder.Path;
    }
}
```
## 2. Don't Repeat Yourself (DRY) — Не повторюйся ##

**Опис:**  Принцип полягає в уникненні дублювання коду шляхом винесення спільної логіки в окремі методи. У проєкті це чудово продемонстровано на системі виклику діалогових вікон.

* Файл **[``MainWindow.xaml.cs``](./OsuBackgroundReplacerMain/MainWindow.xaml.cs)**: Створено універсальні методи ShowDialogAsync та ShowDialogInternal. Замість того, щоб щоразу писати перевірку потоку DispatcherQueue та конфігурувати ContentDialog, логіка написана один раз.

**Приклад** перевикористання у **[``Operations.cs``](./OsuBackgroundReplacerMain/Logic/Operations.cs#L72-L75)**:
```csharp
// Виклик діалогу успіху без дублювання UI-коду
MainWindow.Current.DispatcherQueue.TryEnqueue(async () =>
{
    await MainWindow.ShowDialogAsync($"Successfully changed {replacedFiles.Count} files.", "Done");
});
```
Такі ж виклики використовуються у блоках catch у файлах [`FolderOperations.cs`](./OsuBackgroundReplacerMain/Logic/FolderOperations.cs) та [`ImageOperations.cs`](./OsuBackgroundReplacerMain/Logic/ImageOperations.cs).

## 3. Separation of Concerns (SoC) — Розділення інтересів ##

**Опис:**  Програма розділена на логічні шари, які мінімально перекриваються. У проєкті чітко відділено візуальний інтерфейс (UI) від бізнес-логіки.

* Шар UI: **[``MainWindow.xaml.cs``](./OsuBackgroundReplacerMain/MainWindow.xaml)** та **[``MainWindow.xaml.cs``](./OsuBackgroundReplacerMain/MainWindow.xaml.cs)** знаходяться у головному просторі імен OsuBackgroundReplacerMain. Вони займаються лише ініціалізацією вікна, подіями натискання кнопок та drag-and-drop.
* Шар бізнес-логіки: Файли логіки винесені в окремий простір імен **[`OsuBackgroundReplacerMain.Logic`](./OsuBackgroundReplacerMain/Logic/)**.

**Приклад** виклику логіки з UI ([``MainWindow.xaml.cs``](./OsuBackgroundReplacerMain/MainWindow.xaml.cs#L74-L99)):
UI просто передає управління логічному шару і не займається пошуком файлів самостійно:

```csharp
private async void Replace_Click(object sender, RoutedEventArgs e)
{
    // ... перевірки
    var progress = new Progress<int>(p => ReplacingProgressBar.Value = p);
    ReplacingProgressBar.Visibility = Visibility.Visible;
    
    // Делегування роботи логічному шару
    List<string> replacedFiles = await Operations.Replacement(progress);
    
    ReplacingProgressBar.Visibility = Visibility.Collapsed;
    ActivityLog.ItemsSource = replacedFiles;
}
```

## 4. Asynchronous Non-blocking I/O — Асинхронність та чуйність UI ##

**Опис:**  Важкі операції з файловою системою (читання директорій, копіювання файлів) не повинні блокувати головний потік інтерфейсу користувача. Проєкт активно використовує асинхронне програмування для збереження чуйності UI.

* Файл **[`Operations.cs`](./OsuBackgroundReplacerMain/Logic/Operations.cs)**: Використання конструкції await Task.Run(...) для делегування важких завдань фоновим потокам.

**Приклад з [`Operations.cs`](./OsuBackgroundReplacerMain/Logic/Operations.cs#L26-L32)**:
```csharp
// Пошук файлів у фоновому потоці
var allImageFiles = await Task.Run(() =>
    Directory.GetDirectories(FolderOperations.SelectedFolderPath)
    .SelectMany(folder => Directory.GetFiles(folder, "*.*")
// ...
// Копіювання файлу у фоновому потоці
await Task.Run(() => File.Copy(ImageOperations.SelectedImagePath, newFilePath, true));
```

## 5. Keep It Simple, Stupid (KISS) — Роби це простіше ##

**Опис:**  Система працює найкраще, якщо залишається простою. Замість створення складних патернів (наприклад, Observer) для відстеження прогресу виконання операцій, використано стандартний і зрозумілий підхід.


* Файл **[`Operations.cs`](./OsuBackgroundReplacerMain/Logic/Operations.cs)**.cs та **[``MainWindow.xaml.cs``](./OsuBackgroundReplacerMain/MainWindow.xaml.cs)**: Використання інтерфейсу IProgress<T>.

**Приклад** передачі прогресу:
У UI ([``MainWindow.xaml.cs``](./OsuBackgroundReplacerMain/MainWindow.xaml.cs#L92)) створюється простий об'єкт прогресу:

```csharp
var progress = new Progress<int>(p => ReplacingProgressBar.Value = p);
```
У бізнес-логіці ([`Operations.cs`](./OsuBackgroundReplacerMain/Logic/Operations.cs#L65-L67)) прогрес оновлюється без знання про те, як саме він відображається у UI:

```csharp
current++;
int percent = (total > 0) ? (int)((double)current / total * 100) : 0;
progress?.Report(percent);
```