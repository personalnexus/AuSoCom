    using Avalonia.Controls;
    using Avalonia.Platform.Storage;
    using AuSoCom.Application;
    using AuSoCom.Audio;
    using AuSoCom.Controls;
    using AuSoCom.Domain;
    using AuSoCom.Settings;
    using System.Linq;

namespace AuSoCom;

public partial class MainWindow : Window
{
        private readonly SettingsStore _settingsStore = new();
        private readonly SceneApplicationService _sceneService = new();
        private readonly IAudioEngine _audio = new NAudioEngine();
        private readonly SettingsDocument _settings = new();
        private string? _settingsPath;
        private Guid? _activeSceneId;

        public MainWindow() : this(null)
        {
        }

        public MainWindow(IReadOnlyList<string>? args = null)
    {
        InitializeComponent();
        Closed += (_, _) => _audio.Dispose();
            if (args is { Count: > 0 } &&
                File.Exists(args[0]))
            {
                OpenSettings(args[0]);
            }
            RefreshDevices();
            RefreshAllLists();
        }

        private async void OpenClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open AuSoCom settings",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("JSON settings") { Patterns = ["*.json"] }
                ]
            });
            var path = files.FirstOrDefault()?.TryGetLocalPath();
            if (path is not null) OpenSettings(path);
        }

        private void OpenSettings(string path)
        {
            try
            {
                var loaded = _settingsStore.Load(path);
                _settings.Files.Clear();
                _settings.Files.AddRange(loaded.Files);
                _settings.Devices.Clear();
                _settings.Devices.AddRange(loaded.Devices);
                _settings.Scenes.Clear();
                _settings.Scenes.AddRange(loaded.Scenes);
                _settings.Show.SceneIds.Clear();
                _settings.Show.SceneIds.AddRange(loaded.Show.SceneIds);
                _settings.View = loaded.View;
                _settingsPath = path;
                Title = $"AuSoCom - {Path.GetFileName(path)}";
                RefreshAllLists();
            }
            catch (Exception exception)
            {
                ShowError("Open failed", exception.Message);
            }
        }

        private void SaveClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_settingsPath is null)
            {
                SaveAsClicked(sender, e);
                return;
            }
            SaveSettings(_settingsPath);
        }

        private async void SaveAsClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save AuSoCom settings",
                SuggestedFileName = "AuSoCom.json",
                FileTypeChoices =
                [
                    new FilePickerFileType("JSON settings") { Patterns = ["*.json"] }
                ]
            });
            var path = file?.TryGetLocalPath();
            if (path is not null)
            {
                _settingsPath = path;
                SaveSettings(path);
            }
        }

        private void SaveSettings(string path)
        {
            try
            {
                _settingsStore.Save(path, _settings);
                Title = $"AuSoCom - {Path.GetFileName(path)}";
            }
            catch (Exception exception)
            {
                ShowError("Save failed", exception.Message);
            }
        }

        private async void AddFilesClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Add media files",
                AllowMultiple = true,
                FileTypeFilter =
                [
                    new FilePickerFileType("Audio") { Patterns = ["*.mp3", "*.wav", "*.flac", "*.aiff", "*.aif"] }
                ]
            });
            var addedFiles = new List<MediaFile>();
            foreach (var file in files)
            {
                var path = file.TryGetLocalPath();
                if (path is null) continue;
                var mediaFile = new MediaFile
                {
                    ShortName = Path.GetFileNameWithoutExtension(path),
                    Path = path
                };
                _settings.Files.Add(mediaFile);
                addedFiles.Add(mediaFile);
            }
            RefreshFiles();
            if (addedFiles.Count > 0)
            {
                var addedFilesSet = addedFiles.ToHashSet();
                FilesGrid.SelectedItems.Clear();
                foreach (var item in FilesGrid.ItemsSource!.OfType<MediaFileItem>()
                             .Where(item => addedFilesSet.Contains(item.File)))
                    FilesGrid.SelectedItems.Add(item);
            }
        }

        private void RemoveFilesClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var selectedFiles = GetSelectedMediaFiles();
            if (selectedFiles.Length == 0) return;

            var selectedIds = selectedFiles.Select(file => file.Id).ToHashSet();
            _settings.Files.RemoveAll(file => selectedIds.Contains(file.Id));
            foreach (var scene in _settings.Scenes)
            {
                foreach (var fileId in selectedIds)
                    _sceneService.RemoveElement(scene, fileId);
            }

            RefreshFiles();
            RefreshScenes();
        }

        private async void CreateSceneClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var files = GetSelectedMediaFiles();
            if (files.Length == 0)
            {
                ShowError("No files selected", "Select one or more files before creating a scene.");
                return;
            }

            var dialog = new SceneCreationDialog();
            var options = await dialog.ShowDialog<SceneCreationOptions>(this);
            if (options is null) return;

            var scenes = new List<Scene>();
            if (options.OneSceneForAllFiles)
            {
                scenes.Add(_sceneService.CreateSceneForFiles(
                    $"Scene {_settings.Scenes.Count + 1}",
                    _settings.Devices,
                    files));
            }
            else
            {
                foreach (var file in files)
                {
                    scenes.Add(_sceneService.CreateSceneForFiles(
                        file.ShortName,
                        _settings.Devices,
                        [file]));
                }
            }

            foreach (var scene in scenes)
            {
                scene.AutoStart = options.AutoStartScenes;
                _settings.Scenes.Add(scene);
                if (options.AppendToShow)
                    _settings.Show.Add(scene.Id);
            }

            RefreshScenes(scenes[^1].Id);
            if (options.AppendToShow)
                RefreshShow();
            MainTabs.SelectedItem = options.AppendToShow ? ShowTab : ScenesTab;
        }

        private void PlayFileClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (FilesGrid.SelectedItem is not MediaFileItem item) return;
            var output = FileOutput.SelectedItem as string;
            try
            {
                _audio.Play(item.File, output);
            }
            catch (Exception exception)
            {
                ShowError("Playback failed", exception.Message);
            }
        }

        private MediaFile[] GetSelectedMediaFiles() =>
            FilesGrid.SelectedItems
                .OfType<MediaFileItem>()
                .Select(item => item.File)
                .DistinctBy(file => file.Id)
                .ToArray();

        private void RefreshInputsClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => RefreshDevices();

        private void RefreshOutputsClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => RefreshDevices();

        private void RefreshDevices()
        {
            var existingDevices = _settings.Devices.ToArray();
            _settings.Devices.RemoveAll(device =>
                device.Role is DeviceRole.InputStereo or DeviceRole.InputLeft or DeviceRole.InputRight ||
                device.Role is DeviceRole.OutputStereo or DeviceRole.OutputLeft or DeviceRole.OutputRight);

            AddDeviceVariants(_audio.GetInputDevices(), isInput: true, existingDevices);
            AddDeviceVariants(_audio.GetOutputDevices(), isInput: false, existingDevices);
            RefreshDeviceLists();
        }

        private void AddDeviceVariants(
            IReadOnlyList<AudioDeviceDescriptor> devices,
            bool isInput,
            IReadOnlyList<Device> existingDevices)
        {
            foreach (var device in devices)
            {
                var stereoRole = isInput ? DeviceRole.InputStereo : DeviceRole.OutputStereo;
                var leftRole = isInput ? DeviceRole.InputLeft : DeviceRole.OutputLeft;
                var rightRole = isInput ? DeviceRole.InputRight : DeviceRole.OutputRight;
                AddDeviceVariant(device, stereoRole, "Stereo", existingDevices);
                AddDeviceVariant(device, leftRole, "Left", existingDevices);
                AddDeviceVariant(device, rightRole, "Right", existingDevices);
            }
        }

        private void AddDeviceVariant(
            AudioDeviceDescriptor descriptor,
            DeviceRole role,
            string channel,
            IReadOnlyList<Device> existingDevices)
        {
            var fullName = $"{descriptor.Name} - {channel}";
            var existingDevice = existingDevices.FirstOrDefault(device =>
                device.Role == role &&
                (device.DeviceId == descriptor.Id ||
                 device.DeviceId == descriptor.LegacyId ||
                 device.SystemName == descriptor.Id ||
                 device.SystemName == descriptor.LegacyId ||
                 device.SystemName == fullName ||
                 device.ShortName == fullName));
            _settings.Devices.Add(new Device
            {
                Id = existingDevice?.Id ?? Guid.NewGuid(),
                DeviceId = descriptor.Id,
                SystemName = fullName,
                ShortName = existingDevice?.ShortName ?? fullName,
                Role = role,
                Favorite = existingDevice?.Favorite ?? false,
                AutoAdd = existingDevice?.AutoAdd ?? false
            });
        }

        private void AddSceneClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var scene = _sceneService.CreateScene($"Scene {_settings.Scenes.Count + 1}", _settings.Devices);
            _settings.Scenes.Add(scene);
            RefreshScenes(scene.Id);
        }

        private void RemoveSceneClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ScenesGrid.SelectedItem is not SceneItem item) return;
            _settings.Scenes.RemoveAll(scene => scene.Id == item.Scene.Id);
            _settings.Show.Remove(item.Scene.Id);
            RefreshScenes();
            RefreshShow();
        }

        private void DuplicateSceneClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ScenesGrid.SelectedItem is not SceneItem item) return;
            var duplicate = _sceneService.Duplicate(item.Scene);
            _settings.Scenes.Add(duplicate);
            RefreshScenes(duplicate.Id);
        }

        private async void AddToShowClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var sceneIds = await ShowScenePicker();
            foreach (var sceneId in sceneIds)
                _settings.Show.Add(sceneId);
            if (sceneIds.Length > 0)
                RefreshShow();
        }

        private void RemoveFromShowClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ShowGrid.SelectedItem is not ShowItem item) return;
            _settings.Show.RemoveAt(item.Position - 1);
            RefreshShow();
        }

        private void MoveShowUpClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ShowGrid.SelectedItem is not ShowItem item) return;
            _settings.Show.MoveUpAt(item.Position - 1);
            RefreshShow();
        }

        private void MoveShowDownClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ShowGrid.SelectedItem is not ShowItem item) return;
            _settings.Show.MoveDownAt(item.Position - 1);
            RefreshShow();
        }

        private async void BackClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (!await StopActiveSceneAsync()) return;
            _settings.Show.Back();
            StartSelectedSceneIfAutomatic();
            RefreshShow(selectActive: true);
        }

        private async void StartClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var sceneId = ShowGrid.SelectedItem is ShowItem item
                ? _settings.Show.StartAtIndex(item.Position - 1)
                : _settings.Show.Start();
            if (sceneId is null) return;
            if (!await StopActiveSceneAsync()) return;
            StartScene(sceneId.Value);
            RefreshShow(selectActive: true);
        }

        private async void NextClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (!await StopActiveSceneAsync()) return;
            _settings.Show.Next();
            StartSelectedSceneIfAutomatic();
            RefreshShow(selectActive: true);
        }

        private void StartSelectedSceneIfAutomatic()
        {
            if (_settings.Show.CurrentSceneId is not { } sceneId) return;
            var scene = _settings.Scenes.FirstOrDefault(candidate => candidate.Id == sceneId);
            if (scene?.AutoStart == true)
                StartScene(sceneId);
        }

        private void StartScene(Guid sceneId)
        {
            var scene = _settings.Scenes.FirstOrDefault(candidate => candidate.Id == sceneId);
            if (scene is null)
            {
                ShowError("Start failed", "The selected scene could not be found.");
                return;
            }

            foreach (var input in _settings.Devices.Where(device =>
                         scene.InputIds.Contains(device.Id) &&
                         device.Role is DeviceRole.InputStereo or DeviceRole.InputLeft or DeviceRole.InputRight))
                scene.SetInputMuted(input.Id, false);

            if ((ScenesGrid.SelectedItem as SceneItem)?.Scene.Id == scene.Id)
                RefreshRouting();

            try
            {
                _audio.StartScene(scene, _settings.Files, _settings.Devices);
                _activeSceneId = scene.Id;
            }
            catch (Exception exception)
            {
                _activeSceneId = null;
                ShowError("Start failed", exception.Message);
            }
        }

        private async Task<bool> StopActiveSceneAsync()
        {
            if (_activeSceneId is not { } activeSceneId) return true;
            var scene = _settings.Scenes.FirstOrDefault(candidate => candidate.Id == activeSceneId);
            try
            {
                await _audio.StopAsync(scene?.FadeOut);
            }
            catch (Exception exception)
            {
                ShowError("Stop failed", exception.Message);
                return false;
            }

            _activeSceneId = null;
            return true;
        }

        private void ScenesGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            RefreshRouting();
        }

        private void ShowGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            SyncSceneSelectionToShow();
            RefreshSelectedShowControls();
        }

        private void SyncSceneSelectionToShow()
        {
            if (ShowGrid.SelectedItem is ShowItem item)
            {
                var sceneItem = (ScenesGrid.ItemsSource as IEnumerable<SceneItem>)?
                    .FirstOrDefault(candidate => candidate.Scene.Id == item.SceneId);
                if (sceneItem is not null)
                    ScenesGrid.SelectedItem = sceneItem;
            }
        }

        private void RoutingGridCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
        {
            RefreshRouting();
        }

        private void FilesGridCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
        {
            RefreshScenes();
        }

        private void InputsGridCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
        {
            RefreshScenes();
        }

        private void OutputsGridCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
        {
            RefreshScenes();
            RefreshFileOutputChoices();
        }

        private void ScenesGridCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
        {
            var error = (e.Row.DataContext as SceneItem)?.ConsumeInputError();
            RefreshShow();
            if (error is not null)
                ShowError("Invalid fade duration", error);
        }

        private async void AddInputToSceneClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ScenesGrid.SelectedItem is not SceneItem selected) return;
            var inputChoices = _settings.Devices
                .Where(device => device.Role is DeviceRole.InputStereo or DeviceRole.InputLeft or DeviceRole.InputRight)
                .Where(device => !selected.Scene.InputIds.Contains(device.Id))
                .Select(device => new PickerChoice(
                    device.Id,
                    $"Input device: {device.ShortName}",
                    $"{device.ShortName} {device.SystemName}",
                    device.Favorite))
                .Concat(_settings.Files
                    .Where(file => !selected.Scene.InputIds.Contains(file.Id))
                    .Select(file => new PickerChoice(file.Id, $"File: {file.ShortName}", $"{file.ShortName} {file.Path}")))
                .ToArray();
            var inputIds = await ShowChoicePicker("Add input to scene", inputChoices);
            foreach (var inputId in inputIds)
            {
                _sceneService.AddElement(selected.Scene, inputId, isInput: true);
            }
            if (inputIds.Length > 0)
                RefreshScenes(selected.Scene.Id);
        }

        private async void RemoveInputFromSceneClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ScenesGrid.SelectedItem is not SceneItem selected) return;
            var inputChoices = _settings.Devices
                .Where(device => selected.Scene.InputIds.Contains(device.Id))
                .Select(device => new PickerChoice(
                    device.Id,
                    $"Input device: {device.ShortName}",
                    $"{device.ShortName} {device.SystemName}",
                    device.Favorite))
                .Concat(_settings.Files
                    .Where(file => selected.Scene.InputIds.Contains(file.Id))
                    .Select(file => new PickerChoice(file.Id, $"File: {file.ShortName}", $"{file.ShortName} {file.Path}")))
                .ToArray();
            var inputIds = await ShowChoicePicker("Remove input from scene", inputChoices);
            foreach (var inputId in inputIds)
            {
                _sceneService.RemoveElement(selected.Scene, inputId);
            }
            if (inputIds.Length > 0)
                RefreshScenes(selected.Scene.Id);
        }

        private async void AddOutputToSceneClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ScenesGrid.SelectedItem is not SceneItem selected) return;
            var candidates = _settings.Devices
                .Where(device => device.Role is DeviceRole.OutputStereo or DeviceRole.OutputLeft or DeviceRole.OutputRight)
                .Where(device => !selected.Scene.OutputIds.Contains(device.Id))
                .ToArray();
            var deviceIds = await ShowDevicePicker("Add output to scene", candidates);
            foreach (var deviceId in deviceIds)
            {
                _sceneService.AddElement(selected.Scene, deviceId, isInput: false);
            }
            if (deviceIds.Length > 0)
                RefreshScenes(selected.Scene.Id);
        }

        private async void RemoveOutputFromSceneClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (ScenesGrid.SelectedItem is not SceneItem selected) return;
            var candidates = _settings.Devices
                .Where(device => selected.Scene.OutputIds.Contains(device.Id))
                .ToArray();
            var deviceIds = await ShowDevicePicker("Remove output from scene", candidates);
            foreach (var deviceId in deviceIds)
            {
                _sceneService.RemoveElement(selected.Scene, deviceId);
            }
            if (deviceIds.Length > 0)
                RefreshScenes(selected.Scene.Id);
        }

        private Task<Guid[]> ShowDevicePicker(string title, IReadOnlyList<Device> devices) =>
            ShowChoicePicker(title, devices
                .Select(device => new PickerChoice(
                    device.Id,
                    device.ShortName,
                    $"{device.ShortName} {device.SystemName}",
                    device.Favorite))
                .ToArray());

        private async Task<Guid[]> ShowChoicePicker(string title, IReadOnlyList<PickerChoice> choices)
        {
            var result = new TaskCompletionSource<Guid[]>();
            var search = new TextBox { PlaceholderText = "Search..." };
            var list = new ListBox { SelectionMode = SelectionMode.Multiple };
            var orderedChoices = choices
                .OrderByDescending(choice => choice.IsFavorite)
                .ThenBy(choice => choice.Label, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            list.ItemsSource = orderedChoices;
            search.TextChanged += (_, _) =>
            {
                var filter = search.Text?.Trim() ?? string.Empty;
                list.ItemsSource = orderedChoices
                    .Where(choice => string.IsNullOrEmpty(filter) ||
                        choice.Label.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                        choice.SearchText.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            };

            var select = new Button { Content = "Select", IsDefault = true };
            var cancel = new Button { Content = "Cancel", IsCancel = true };
            var dialog = new Window
            {
                Title = title,
                Width = 520,
                Height = 420,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var top = new StackPanel { Margin = new Avalonia.Thickness(8), Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6 };
            search.Width = 300;
            top.Children.Add(search);
            top.Children.Add(select);
            top.Children.Add(cancel);
            DockPanel.SetDock(top, Avalonia.Controls.Dock.Top);
            var content = new DockPanel();
            content.Children.Add(top);
            content.Children.Add(list);
            dialog.Content = content;

            select.Click += (_, _) =>
            {
                result.TrySetResult((list.SelectedItems?.OfType<PickerChoice>() ?? [])
                    .Select(choice => choice.Id)
                    .ToArray());
                dialog.Close();
            };
            cancel.Click += (_, _) =>
            {
                result.TrySetResult([]);
                dialog.Close();
            };
            dialog.Closed += (_, _) => result.TrySetResult([]);
            await dialog.ShowDialog(this);
            return await result.Task;
        }

        private async Task<Guid[]> ShowScenePicker()
        {
            var result = new TaskCompletionSource<Guid[]>();
            var search = new TextBox { PlaceholderText = "Search..." };
            var list = new ListBox { SelectionMode = SelectionMode.Multiple };
            list.ItemsSource = _settings.Scenes.Select(scene => new SceneItem(scene, _settings.Devices, _settings.Files)).ToArray();
            search.TextChanged += (_, _) =>
            {
                var filter = search.Text?.Trim() ?? string.Empty;
                list.ItemsSource = _settings.Scenes
                    .Where(scene => string.IsNullOrEmpty(filter) ||
                        scene.ShortName.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .Select(scene => new SceneItem(scene, _settings.Devices, _settings.Files))
                    .ToArray();
            };

            var select = new Button { Content = "Select", IsDefault = true };
            var cancel = new Button { Content = "Cancel", IsCancel = true };
            var dialog = new Window
            {
                Title = "Add scenes to show",
                Width = 520,
                Height = 420,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            var top = new StackPanel
            {
                Margin = new Avalonia.Thickness(8),
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 6
            };
            search.Width = 300;
            top.Children.Add(search);
            top.Children.Add(select);
            top.Children.Add(cancel);
            DockPanel.SetDock(top, Avalonia.Controls.Dock.Top);
            var content = new DockPanel();
            content.Children.Add(top);
            content.Children.Add(list);
            dialog.Content = content;

            select.Click += (_, _) =>
            {
                result.TrySetResult((list.SelectedItems?.OfType<SceneItem>() ?? [])
                    .Select(item => item.Scene.Id)
                    .ToArray());
                dialog.Close();
            };
            cancel.Click += (_, _) =>
            {
                result.TrySetResult([]);
                dialog.Close();
            };
            dialog.Closed += (_, _) => result.TrySetResult([]);
            await dialog.ShowDialog(this);
            return await result.Task;
        }

        private void RefreshAllLists()
        {
            RefreshFiles();
            RefreshDeviceLists();
            RefreshScenes();
            RefreshShow();
        }

        private void RefreshFiles()
        {
            FilesGrid.ItemsSource = _settings.Files.Select(file => new MediaFileItem(file)).ToArray();
        }

        private void RefreshDeviceLists()
        {
            InputsGrid.ItemsSource = _settings.Devices
                .Where(device => device.Role is DeviceRole.InputStereo or DeviceRole.InputLeft or DeviceRole.InputRight)
                .Select(device => new DeviceItem(device))
                .ToArray();
            OutputsGrid.ItemsSource = _settings.Devices
                .Where(device => device.Role is DeviceRole.OutputStereo or DeviceRole.OutputLeft or DeviceRole.OutputRight)
                .Select(device => new DeviceItem(device))
                .ToArray();
            RefreshFileOutputChoices();
        }

        private void RefreshFileOutputChoices()
        {
            var selectedOutput = FileOutput.SelectedItem as string;
            var outputDevices = _settings.Devices
                .Where(device => device.Role is DeviceRole.OutputStereo or DeviceRole.OutputLeft or DeviceRole.OutputRight);
            var outputs = outputDevices
                .Where(device => device.Favorite)
                .Select(device => device.ShortName)
                .Concat(outputDevices
                    .Where(device => !device.Favorite)
                    .Select(device => device.ShortName));
            var choices = _audio.GetAsioDrivers()
                .ToArray();
            var orderedChoices = outputs
                .Concat(choices)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            FileOutput.ItemsSource = orderedChoices;
            if (selectedOutput is not null && orderedChoices.Contains(selectedOutput, StringComparer.Ordinal))
                FileOutput.SelectedItem = selectedOutput;
        }

        private void RefreshScenes(Guid? selectedSceneId = null)
        {
            selectedSceneId ??= (ScenesGrid.SelectedItem as SceneItem)?.Scene.Id;
            var items = _settings.Scenes.Select(scene => new SceneItem(scene, _settings.Devices, _settings.Files)).ToArray();
            ScenesGrid.ItemsSource = items;
            ScenesGrid.SelectedItem = items.FirstOrDefault(item => item.Scene.Id == selectedSceneId);
            RefreshRouting();
        }

        private void RefreshRouting()
        {
            var scene = (ScenesGrid.SelectedItem as SceneItem)?.Scene;
            RefreshSceneControls(scene, SceneControls);
            RefreshSelectedShowControls();
        }

        private void RefreshSelectedShowControls()
        {
            var scene = ShowGrid.SelectedItem is ShowItem item
                ? _settings.Scenes.FirstOrDefault(candidate => candidate.Id == item.SceneId)
                : null;
            RefreshSceneControls(scene, ShowSceneControls);
        }

        private void RefreshSceneControls(
            Scene? scene,
            SceneControlsView controls)
        {
            var inputPanel = controls.InputPanel;
            var outputPanel = controls.OutputPanel;
            var routingGrid = controls.RoutingGrid;
            inputPanel.Children.Clear();
            outputPanel.Children.Clear();
            if (scene is null)
            {
                routingGrid.ItemsSource = Array.Empty<RoutingItem>();
                return;
            }

            foreach (var inputId in scene.InputIds)
            {
                AddVolumeControl(
                    inputPanel,
                    GetInputName(inputId),
                    scene.GetInputVolume(inputId),
                    scene.IsInputMuted(inputId),
                    volume => scene.SetInputVolume(inputId, volume),
                    isMuted => scene.SetInputMuted(inputId, isMuted));
            }

            foreach (var outputId in scene.OutputIds)
            {
                AddVolumeControl(
                    outputPanel,
                    GetOutputName(outputId),
                    scene.GetOutputVolume(outputId),
                    scene.IsOutputMuted(outputId),
                    volume => scene.SetOutputVolume(outputId, volume),
                    isMuted => scene.SetOutputMuted(outputId, isMuted));
            }

            routingGrid.ItemsSource = scene.OutputIds
                .SelectMany(outputId => scene.InputIds.Select(inputId =>
                    new RoutingItem(scene, outputId, inputId,
                        GetOutputName(outputId),
                        GetInputName(inputId))))
                .ToArray();
        }

        private string GetInputName(Guid inputId) =>
            _settings.Files.FirstOrDefault(file => file.Id == inputId)?.ShortName ??
            _settings.Devices.FirstOrDefault(device => device.Id == inputId)?.ShortName ??
            "Unknown input";

        private string GetOutputName(Guid outputId) =>
            _settings.Devices.FirstOrDefault(device => device.Id == outputId)?.ShortName ??
            "Unknown output";

        private static void AddVolumeControl(
            StackPanel panel,
            string name,
            double volume,
            bool isMuted,
            Action<double> setVolume,
            Action<bool> setMuted)
        {
            var muteButton = new Button
            {
                Content = isMuted ? "Muted" : "Mute",
                MinWidth = 72
            };
            var slider = new Slider
            {
                Orientation = Avalonia.Layout.Orientation.Vertical,
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                Value = volume,
                Height = 140
            };
            var volumeText = new TextBlock
            {
                Text = $"{volume:0}%",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            slider.ValueChanged += (_, args) =>
            {
                setVolume(args.NewValue);
                volumeText.Text = $"{args.NewValue:0}%";
            };
            muteButton.Click += (_, _) =>
            {
                isMuted = !isMuted;
                setMuted(isMuted);
                muteButton.Content = isMuted ? "Muted" : "Mute";
            };

            var control = new StackPanel
            {
                Spacing = 4,
                Margin = new Avalonia.Thickness(4),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            control.Children.Add(muteButton);
            control.Children.Add(slider);
            control.Children.Add(volumeText);
            control.Children.Add(new TextBlock
            {
                Text = name,
                Width = 96,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                TextAlignment = Avalonia.Media.TextAlignment.Center
            });
            panel.Children.Add(control);
        }

        private void RefreshShow(bool selectActive = false)
        {
            var selectedIndex = selectActive
                ? _settings.Show.CurrentSceneIndex ?? -1
                : ShowGrid.SelectedIndex;
            var items = _settings.Show.SceneIds
                .Select((sceneId, index) =>
                {
                    var scene = _settings.Scenes.FirstOrDefault(candidate => candidate.Id == sceneId);
                    return new ShowItem(
                        sceneId,
                        index + 1,
                        scene?.ShortName ?? "Missing scene",
                        _settings.Show.CurrentSceneIndex == index);
                })
                .ToArray();
            ShowGrid.ItemsSource = items;
            if (selectedIndex >= 0 && selectedIndex < items.Length)
                ShowGrid.SelectedIndex = selectedIndex;
            SyncSceneSelectionToShow();
            RefreshSelectedShowControls();
        }

        private async void ShowError(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 420,
                Height = 160,
                Content = new TextBlock
                {
                    Text = message,
                    Margin = new Avalonia.Thickness(20),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                }
            };
            await dialog.ShowDialog(this);
        }

        private sealed class MediaFileItem
        {
            private readonly MediaFile _file;

            public MediaFileItem(MediaFile file) => _file = file;

            public MediaFile File => _file;
            public string ShortName
            {
                get => _file.ShortName;
                set => _file.ShortName = value;
            }
            public string Path => _file.Path;
            public string StartTime
            {
                get => _file.StartTime?.ToString(@"hh\:mm\:ss\.fff") ?? string.Empty;
                set
                {
                    _file.StartTime = string.IsNullOrWhiteSpace(value)
                        ? null
                        : TimeSpan.TryParse(value, out var time) ? time : _file.StartTime;
                }
            }
            public string EndTime
            {
                get => _file.EndTime?.ToString(@"hh\:mm\:ss\.fff") ?? string.Empty;
                set
                {
                    _file.EndTime = string.IsNullOrWhiteSpace(value)
                        ? null
                        : TimeSpan.TryParse(value, out var time) ? time : _file.EndTime;
                }
            }

            public override string ToString() => $"{ShortName} — {Path}";
        }

        private sealed class DeviceItem
        {
            public DeviceItem(Device device) => Device = device;

            public Device Device { get; }
            public string ShortName
            {
                get => Device.ShortName;
                set => Device.ShortName = value;
            }
            public string SystemName => Device.SystemName;
            public bool Favorite
            {
                get => Device.Favorite;
                set => Device.Favorite = value;
            }
            public bool AutoAdd
            {
                get => Device.AutoAdd;
                set => Device.AutoAdd = value;
            }

            public override string ToString() => ShortName;
        }

        private sealed record PickerChoice(Guid Id, string Label, string SearchText, bool IsFavorite = false)
        {
            public override string ToString() => Label;
        }

        private sealed class SceneItem : System.ComponentModel.INotifyPropertyChanged
        {
            private readonly IReadOnlyList<Device> _devices;
            private readonly IReadOnlyList<MediaFile> _files;

            public SceneItem(Scene scene, IReadOnlyList<Device> devices, IReadOnlyList<MediaFile> files)
            {
                Scene = scene;
                _devices = devices;
                _files = files;
            }

            public Scene Scene { get; }
            public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
            public string ShortName
            {
                get => Scene.ShortName;
                set => Scene.ShortName = value;
            }
            public string Inputs => string.Join(", ", Scene.InputIds
                .Select(id => _files.FirstOrDefault(file => file.Id == id)?.ShortName ??
                              _devices.FirstOrDefault(device => device.Id == id)?.ShortName)
                .Where(name => !string.IsNullOrEmpty(name)));
            public string Outputs => string.Join(", ", Scene.OutputIds
                .Select(id => _devices.FirstOrDefault(device => device.Id == id)?.ShortName)
                .Where(name => !string.IsNullOrEmpty(name)));
            public string FadeIn
            {
                get => Scene.FadeIn is { } duration
                    ? $"{duration.TotalMilliseconds.ToString("0.###", System.Globalization.CultureInfo.CurrentCulture)} ms"
                    : string.Empty;
                set => SetFadeDuration(value, isFadeIn: true);
            }
            public string FadeOut
            {
                get => Scene.FadeOut is { } duration
                    ? $"{duration.TotalMilliseconds.ToString("0.###", System.Globalization.CultureInfo.CurrentCulture)} ms"
                    : string.Empty;
                set => SetFadeDuration(value, isFadeIn: false);
            }
            public string? LastInputError { get; private set; }

            public string? ConsumeInputError()
            {
                var error = LastInputError;
                LastInputError = null;
                return error;
            }

            public bool AutoStart
            {
                get => Scene.AutoStart;
                set => Scene.AutoStart = value;
            }

            public override string ToString() => ShortName;

            private void SetFadeDuration(string? value, bool isFadeIn)
            {
                if (FadeDurationParser.TryParse(value, out var duration))
                {
                    if (isFadeIn)
                        Scene.FadeIn = duration;
                    else
                        Scene.FadeOut = duration;
                    LastInputError = null;
                }
                else
                {
                    LastInputError = "Enter a non-negative duration as milliseconds (for example 250 ms), seconds (1.5 s), or mm:ss.fff.";
                }

                var propertyName = isFadeIn ? nameof(FadeIn) : nameof(FadeOut);
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }

        private sealed class RoutingItem
        {
            private readonly Scene _scene;
            private readonly Guid _outputId;
            private readonly Guid _inputId;

            public RoutingItem(Scene scene, Guid outputId, Guid inputId, string outputName, string inputName)
            {
                _scene = scene;
                _outputId = outputId;
                _inputId = inputId;
                OutputName = outputName;
                InputName = inputName;
            }

            public string OutputName { get; }
            public string InputName { get; }
            public double Amount
            {
                get => _scene.GetRouting(_inputId, _outputId);
                set
                {
                    if (value is >= 0 and <= 100)
                        _scene.SetRouting(_inputId, _outputId, value);
                }
            }
        }

        private sealed record ShowItem(Guid SceneId, int Position, string SceneName, bool IsActive)
        {
            public string Status => IsActive ? "Active" : string.Empty;
            public override string ToString() => $"{Position}. {SceneName}";
        }
    }