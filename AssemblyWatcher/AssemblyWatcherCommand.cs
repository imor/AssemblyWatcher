using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.ILSpy;

namespace AssemblyWatcher.Plugin
{
    [ExportToolbarCommand(ToolTip = "Enables/disables automatic reloading of assemblies when they change on disk", ToolbarIcon = "eye.png", ToolbarCategory = "Open", Tag = ButtonTag)]
    public class AssemblyWatcherCommand : SimpleCommand
    {
        //Careful - this constructor is called from inside the MainWindow constructor, so MainWindow may not be initialized completely
        public AssemblyWatcherCommand()
        {
            MainWindow.Instance.Loaded += MainWindowLoaded;
            MainWindow.Instance.CurrentAssemblyListChanged += CurrentAssemblyListChanged;
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            FixToolbar();
        }

        private void FixToolbar()
        {
            int index = FindAutoReloadButtonIndex();
            ReplaceButtonWithCheckBox(index);
        }

        private void ReplaceButtonWithCheckBox(int index)
        {
            var toolbarItems = MainWindow.Instance.GetToolBarItems();
            var button = (Button)toolbarItems.GetItemAt(index);

            toolbarItems.RemoveAt(index);

            _checkBox = new CheckBox
            {
                Command = button.Command,
                ToolTip = button.ToolTip,
                Tag = button.Tag,
                Content = button.Content
            };

            _checkBox.Checked += CheckBoxCheckedChanged;
            _checkBox.Unchecked += CheckBoxCheckedChanged;

            toolbarItems.Insert(index, _checkBox);
        }

        private int FindAutoReloadButtonIndex()
        {
            var toolbarItems = MainWindow.Instance.GetToolBarItems();
            for (int i = 0; i < toolbarItems.Count; i++)
            {
                var toolbarItem = toolbarItems.GetItemAt(i) as Button;
                if (toolbarItem != null && toolbarItem.Tag != null && toolbarItem.Tag.Equals(ButtonTag))
                {
                    return i;
                }
            }
            throw new ApplicationException("Unable to find AutoReload button in the toolbar");
        }

        private void CurrentAssemblyListChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (_checkBox != null && _checkBox.IsChecked.HasValue && _checkBox.IsChecked.Value)
            {
                StopWatchingAllAssemblies();
                StartWatchingAllAssemblies();
            }
        }

        private void StopWatchingAllAssemblies()
        {
            foreach (var watcher in _watchers)
            {
                watcher.Changed -= FileChanged;
                watcher.Dispose();
            }
            _watchers.Clear();
        }

        private void StartWatchingAllAssemblies()
        {
            foreach (var assembly in MainWindow.Instance.CurrentAssemblyList.GetAssemblies())
            {
                if (File.Exists(assembly.FileName))
                    StartWatchingAssembly(assembly.FileName);
            }
        }

        private void StartWatchingAssembly(string filePath)
        {
            var toWatch = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);

            //TODO: FileSystemWatcher raises multiple Changed events when an assembly changes. Find out why and fix it.
            var watcher = new FileSystemWatcher
            {
                Path = toWatch,
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = fileName
            };

            _watchers.Add(watcher);

            watcher.Changed += FileChanged;
            watcher.EnableRaisingEvents = true;
        }

        private void FileChanged(object sender, FileSystemEventArgs e)
        {
            //TODO: Only reload the changed assembly
            Delegate d = new Action(delegate
            {
                NavigationCommands.Refresh.Execute(null, MainWindow.Instance.TextView);
            });
            Application.Current.Dispatcher.BeginInvoke(d);
        }

        public override void Execute(object parameter)
        {
            //This command is never executed. When the main window loads, this plugin replaces the Button in the toolbar with a CheckBox.
            //Hence the original Command (this class) that was associated with the button is also gone.
        }

        private void CheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            var checkBox = (CheckBox)sender;
            if (checkBox.IsChecked.HasValue && checkBox.IsChecked.Value)
            {
                StartWatchingAllAssemblies();
            }
            else
            {
                StopWatchingAllAssemblies();
            }
        }

        private CheckBox _checkBox;
        private const string ButtonTag = "AssemblyWatcherButton";
        private readonly IList<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
    }
}
