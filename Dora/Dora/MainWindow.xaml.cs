using Dora.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Diagnostics;

using MessageBox = System.Windows.MessageBox;

namespace Dora
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<Item> Items
        {
            get { return (ObservableCollection<Item>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Items.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items", typeof(ObservableCollection<Item>), typeof(MainWindow), new PropertyMetadata(null));

        public MainWindow()
        {
            this.InitializeComponent();

            // Asks for a folder (this.path changes)
            this.OpenDialogAndExecuteFirstExploration();

            // Bind selected event
            this.columns.ItemSelected += Columns_ItemSelected;

            // Watch a folder
            this.watcher = new FileSystemWatcher(this.path);
            this.watcher.Created += Watcher_Changes;
            this.watcher.Renamed += Watcher_Changes;
            this.watcher.Deleted += Watcher_Changes;
            this.watcher.EnableRaisingEvents = true;
        }

        private void Columns_ItemSelected(object sender, Item e)
        {
            // Open
            Process.Start(e.FullPath);
        }

        private void InvokeOnUI(Action action)
        {
            this.Dispatcher.Invoke(action);
        }

        private void OpenDialogAndExecuteFirstExploration()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            DialogResult dialogResult = dialog.ShowDialog();

            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                // Save path and execute first exploration
                this.path = dialog.SelectedPath;
                string[] fullNames = Directory.GetFiles(this.path).ToArray();
                string[] names = Directory.GetFiles(this.path).Select(Path.GetFileName).ToArray();
                for (int i = 0; i < names.Length; i++)
                {
                    AddToTree(fullNames[i], names[i].Split('-'), null);
                }
            }
            else
            {
                MessageBox.Show("Seleziona una cartella!", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);

                // Repeat
                this.OpenDialogAndExecuteFirstExploration();
            }
        }

        private void Watcher_Changes(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                // Remove from tree
                this.InvokeOnUI(() => { this.RemoveFromTree(e.Name.Split('-'), null); });
            }
            else if (e.ChangeType == WatcherChangeTypes.Created)
            {
                // Add to tree
                this.InvokeOnUI(() => { this.AddToTree(e.FullPath, e.Name.Split('-'), null); });
            }
            else if (e.ChangeType == WatcherChangeTypes.Renamed)
            {
                // Remove old from tree, add new to tree
                this.InvokeOnUI(() =>
                {
                    RenamedEventArgs renamedEventArgs = e as RenamedEventArgs;

                    this.RemoveFromTree(renamedEventArgs.OldName.Split('-'), null);
                    this.AddToTree(renamedEventArgs.FullPath, renamedEventArgs.Name.Split('-'), null);
                });
            }
        }

        private void AddToTree(string path, string[] nameSplitted, Item parent)
        {
            /* Recipe:
             * 
             * Get first item in the array (if there are no items, stop)
             * Check if item is already in the tree
             * * if yes, repeat the recipe with the second item of the array
             * * if not, add item on the tree and repeat the recipe with the second item of the array
             */ 

            if (nameSplitted.Length == 0)
            {
                return;
            }

            string name = nameSplitted[0];
            Item item = null;
            if (parent == null)
            {
                item = Items?.Where(x => x.Name == name.Trim()).FirstOrDefault();
            }
            else
            {
                item = parent.SubData?.Where(x => x.Name == name.Trim()).FirstOrDefault();
            }

            if (item == null)
            {
                Item newItem = new Item()
                {
                    Name = name.Trim(),
                    FullPath = path
                };

                // Try to get icon
                try
                {
                    Icon systemIcon = System.Drawing.Icon.ExtractAssociatedIcon(path);
                    BitmapSource bmpSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                            systemIcon.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                    systemIcon.Dispose();

                    newItem.Icon = bmpSrc;
                }
                catch (FileNotFoundException) { }

                if (parent == null)
                {
                    if (this.Items == null)
                    {
                        this.Items = new ObservableCollection<Item>(new List<Item>() { newItem });
                    }
                    else
                    {
                        this.Items.Add(newItem);
                    }
                }
                else if (parent.SubData == null)
                {
                    parent.SubData = new ObservableCollection<Item>(new List<Item>() { newItem });
                }
                else
                {
                    ((ObservableCollection<Item>)parent.SubData).Add(newItem);
                }

                AddToTree(path, nameSplitted.Skip(1).ToArray(), newItem);
            }
            else
            {
                AddToTree(path, nameSplitted.Skip(1).ToArray(), item);
            }
        }

        private Item RemoveFromTree(string[] nameSplitted, Item parent)
        {
            /* Recipe:
             * 
             * Navigate until you reach the leaf. Delete every item backwards if there are no other items to show
             */

            if (nameSplitted.Length == 0)
            {
                return parent;
            }

            string name = nameSplitted[0];
            Item item = null;
            if (parent == null)
            {
                item = Items?.Where(x => x.Name == name.Trim()).FirstOrDefault();
            }
            else
            {
                item = parent.SubData?.Where(x => x.Name == name.Trim()).FirstOrDefault();
            }

            if (item != null)
            {
                Item itemToRemove = this.RemoveFromTree(nameSplitted.Skip(1).ToArray(), item);

                if (itemToRemove != null)
                {
                    if (parent != null)
                    {
                        ((ObservableCollection<Item>)parent.SubData).Remove(itemToRemove);
                        if (parent.SubData.Count() == 0)
                        {
                            return parent;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        Items.Remove(itemToRemove);
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return parent;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        private FileSystemWatcher watcher;
        private string path;
    }
}
