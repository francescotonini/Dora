using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Dora.Models;

namespace Dora.Controls
{
    [TemplatePart(Name = "PART_ItemsHost", Type = typeof(ItemsControl))]
    public class MillerColumns : Control
    {
        private class DummyObject : FrameworkElement
        {
            public static readonly DependencyProperty ValueProperty =
                DependencyProperty.Register("Value", typeof(object), typeof(DummyObject), new PropertyMetadata(null));

            public object Value
            {
                get { return (object)GetValue(ValueProperty); }
                set { SetValue(ValueProperty, value); }
            }

            public DummyObject(object dc)
            {
                DataContext = dc;
            }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(MillerColumns), new PropertyMetadata(null, _OnItemsChanged));
        public static readonly DependencyProperty ChainProperty =
            DependencyProperty.Register("Chain", typeof(IEnumerable), typeof(MillerColumns), new PropertyMetadata(null));

        public static readonly DependencyProperty TrackProperty =
            DependencyProperty.RegisterAttached("Track", typeof(MillerColumns), typeof(MillerColumns), new PropertyMetadata(null, _OnTrackChanged));

        public event EventHandler<Item> ItemSelected;

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        public IEnumerable Chain
        {
            get { return (IEnumerable)GetValue(ChainProperty); }
            private set { SetValue(ChainProperty, value); }
        }

        public static MillerColumns GetTrack(DependencyObject obj)
        {
            return (MillerColumns)obj.GetValue(TrackProperty);
        }
        public static void SetTrack(DependencyObject obj, MillerColumns value)
        {
            obj.SetValue(TrackProperty, value);
        }

        static MillerColumns()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MillerColumns), new FrameworkPropertyMetadata(typeof(MillerColumns)));
        }

        private static void _OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MillerColumns)d)._UpdateChain();
        }

        private static void _OnTrackChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var listBox = (ListBox)d;
            var millerColumns = (MillerColumns)e.NewValue;
            listBox.SelectionChanged += (s, ea) => millerColumns._UpdateChain((ListBox)s);
            listBox.MouseDoubleClick += (s, ea) => 
            {
                ListBox lb = s as ListBox;
                Item item = lb?.SelectedItem as Item;

                if (item?.SubData == null)
                {
                    // Fire event
                    millerColumns.ItemSelected?.Invoke(millerColumns, item);
                }
            };
        }

        private void _UpdateChain(ListBox sender = null)
        {
            // If we don't have a source, we don't have anything to display.
            if (ItemsSource == null || !ItemsSource.Cast<object>().Any()) return;

            // If sender is null, ItemsSource changed.  Rebuild.
            if (sender == null)
            {
                Chain = new ObservableCollection<IEnumerable> { ItemsSource };
                return;
            }

            // Get the sender ListBox's data context (it's ItemSource)
            var enumerable = sender.DataContext as IEnumerable;
            if (enumerable == null) return; // This may happen during initialization.

            // We need to cast Chain so that we can modify it.
            var collection = (ObservableCollection<IEnumerable>)Chain;

            // Remove all ListBoxes after the one that changed.
            var index = collection.IndexOf(enumerable);
            if (index == -1) return;

            index++;
            while (collection.Count > index)
                collection.RemoveAt(index);

            // Now we need to get the list of children from the selected item. To do this,
            // we need to find the HierchicalDataTemplate for the specified type and
            // get its ItemsSource property.  The property is the raw binding, which we'll
            // need to resolve, given the item as a data context.  This requires a dummy
            // dependency object.

            // Get the selected item.
            var item = sender.SelectedItem;
            if (item == null) return;

            // Find the HierarchicalDataTemplate.
            var template = _FindTemplate(item.GetType());

            // Resolve the binding to get the new collections.
            var newSource = _EvaluateBinding(item, template.ItemsSource);

            // Add to Chain.
            if (newSource != null)
                collection.Add(newSource);
        }

        private static IEnumerable _EvaluateBinding(object item, BindingBase binding)
        {
            var dummy = new DummyObject(item);
            BindingOperations.SetBinding(dummy, DummyObject.ValueProperty, binding);
            return dummy.Value as IEnumerable;
        }

        private HierarchicalDataTemplate _FindTemplate(Type key)
        {
            HierarchicalDataTemplate template;
            return _SearchUpToWindow(key, out template) || _SearchDictionary(Application.Current.Resources, key, out template)
                       ? template
                       : null;
        }

        private bool _SearchUpToWindow(Type key, out HierarchicalDataTemplate template)
        {
            FrameworkElement currentHost = this;
            while (currentHost != null)
            {
                if (_SearchDictionary(currentHost.Resources, key, out template)) return true;
                currentHost = currentHost.Parent as FrameworkElement;
            }
            template = null;
            return false;
        }

        private static bool _SearchDictionary(ResourceDictionary resources, Type key, out HierarchicalDataTemplate template)
        {
            var resourceKey = resources.Keys.OfType<DataTemplateKey>().FirstOrDefault(k => (Type)k.DataType == key);
            if (resourceKey != null)
            {
                template = resources[resourceKey] as HierarchicalDataTemplate;
                return true;
            }
            foreach (var source in resources.MergedDictionaries)
            {
                if (_SearchDictionary(source, key, out template)) return true;
            }
            template = null;
            return false;
        }
    }
}
