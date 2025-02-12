﻿using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Utils;

namespace System.Windows.Controls {
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(MultiSelectTreeViewItem))]
    public class MultiSelectTreeView : ItemsControl {
        #region Constants and Fields

        public event EventHandler<PreviewSelectionChangedEventArgs> PreviewSelectionChanged;

        // TODO: Provide more details. Fire once for every single change and once for all groups of changes, with different flags
        public event EventHandler SelectionChanged;

        public static readonly DependencyProperty BackgroundSelectionRectangleProperty = DependencyProperty.Register("BackgroundSelectionRectangle", typeof(Brush), typeof(MultiSelectTreeView), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0x60, 0x33, 0x99, 0xFF))));
        public static readonly DependencyProperty BorderBrushSelectionRectangleProperty = DependencyProperty.Register("BorderBrushSelectionRectangle", typeof(Brush), typeof(MultiSelectTreeView), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x33, 0x99, 0xFF))));
        public static readonly DependencyProperty HoverHighlightingProperty = DependencyProperty.Register("HoverHighlighting", typeof(bool), typeof(MultiSelectTreeView), new PropertyMetadata(BoolBox.True));
        public static readonly DependencyProperty VerticalRulersProperty = DependencyProperty.Register("VerticalRulers", typeof(bool), typeof(MultiSelectTreeView), new PropertyMetadata(BoolBox.False));
        public static readonly DependencyProperty ItemIndentProperty = DependencyProperty.Register("ItemIndent", typeof(int), typeof(MultiSelectTreeView), new PropertyMetadata(13));
        public static readonly DependencyProperty IsKeyboardModeProperty = DependencyProperty.Register("IsKeyboardMode", typeof(bool), typeof(MultiSelectTreeView), new PropertyMetadata(BoolBox.False));
        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register("SelectedItems", typeof(IList), typeof(MultiSelectTreeView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemsPropertyChanged));
        public static readonly DependencyProperty AllowEditItemsProperty = DependencyProperty.Register("AllowEditItems", typeof(bool), typeof(MultiSelectTreeView), new PropertyMetadata(BoolBox.False));
        private static readonly DependencyPropertyKey LastSelectedItemPropertyKey = DependencyProperty.RegisterReadOnly("LastSelectedItem", typeof(object), typeof(MultiSelectTreeView), new PropertyMetadata(null));

        #endregion

        #region Constructors and Destructors

        static MultiSelectTreeView() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiSelectTreeView), new FrameworkPropertyMetadata(typeof(MultiSelectTreeView)));
        }

        public MultiSelectTreeView() {
            this.SelectedItems = new ObservableCollection<object>();
            this.Selection = new SelectionMultiple(this);
            this.Selection.PreviewSelectionChanged += (s, e) => {
                this.OnPreviewSelectionChanged(e);
            };
        }

        #endregion

        #region Public Properties

        public Brush BackgroundSelectionRectangle {
            get => (Brush) this.GetValue(BackgroundSelectionRectangleProperty);
            set => this.SetValue(BackgroundSelectionRectangleProperty, value);
        }

        public Brush BorderBrushSelectionRectangle {
            get => (Brush) this.GetValue(BorderBrushSelectionRectangleProperty);
            set => this.SetValue(BorderBrushSelectionRectangleProperty, value);
        }

        public bool HoverHighlighting {
            get => (bool) this.GetValue(HoverHighlightingProperty);
            set => this.SetValue(HoverHighlightingProperty, value.Box());
        }

        public bool VerticalRulers {
            get => (bool) this.GetValue(VerticalRulersProperty);
            set => this.SetValue(VerticalRulersProperty, value.Box());
        }

        public int ItemIndent {
            get => (int) this.GetValue(ItemIndentProperty);
            set => this.SetValue(ItemIndentProperty, value);
        }

        [Browsable(false)]
        public bool IsKeyboardMode {
            get => (bool) this.GetValue(IsKeyboardModeProperty);
            set => this.SetValue(IsKeyboardModeProperty, value.Box());
        }

        public bool AllowEditItems {
            get => (bool) this.GetValue(AllowEditItemsProperty);
            set => this.SetValue(AllowEditItemsProperty, value.Box());
        }

        /// <summary>
        ///    Gets the last selected item.
        /// </summary>
        public object LastSelectedItem {
            get => this.GetValue(LastSelectedItemPropertyKey.DependencyProperty);
            private set => this.SetValue(LastSelectedItemPropertyKey, value);
        }

        private MultiSelectTreeViewItem lastFocusedItem;

        /// <summary>
        /// Gets the last focused item.
        /// </summary>
        internal MultiSelectTreeViewItem LastFocusedItem {
            get => this.lastFocusedItem;
            set {
                // Only the last focused MultiSelectTreeViewItem may have IsTabStop = true
                // so that the keyboard focus only stops a single time for the MultiSelectTreeView control.
                if (this.lastFocusedItem != null) {
                    this.lastFocusedItem.IsTabStop = false;
                }

                this.lastFocusedItem = value;
                if (this.lastFocusedItem != null) {
                    this.lastFocusedItem.IsTabStop = true;
                }

                // The MultiSelectTreeView control only has the tab stop if none of its items has it.
                this.IsTabStop = this.lastFocusedItem == null;
            }
        }

        /// <summary>
        /// Gets or sets a list of selected items and can be bound to another list. If the source list
        /// implements <see cref="INotifyPropertyChanged"/> the changes are automatically taken over.
        /// </summary>
        public IList SelectedItems {
            get => (IList) this.GetValue(SelectedItemsProperty);
            set => this.SetValue(SelectedItemsProperty, value);
        }

        internal ISelectionStrategy Selection { get; private set; }

        #endregion

        #region Public Methods and Operators

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            this.Selection.ApplyTemplate();
        }

        public bool ClearSelection() {
            if (this.SelectedItems.Count > 0) {
                // Make a copy of the list and ignore changes to the selection while raising events
                foreach (object selItem in new ArrayList(this.SelectedItems)) {
                    PreviewSelectionChangedEventArgs e = new PreviewSelectionChangedEventArgs(false, selItem);
                    this.OnPreviewSelectionChanged(e);
                    if (e.CancelAny) {
                        return false;
                    }
                }

                this.SelectedItems.Clear();
            }

            return true;
        }

        public void FocusItem(object item, bool bringIntoView = false) {
            MultiSelectTreeViewItem node = this.GetTreeViewItemsFor(new List<object> {item}).FirstOrDefault();
            if (node != null) {
                FocusHelper.Focus(node, bringIntoView);
            }
        }

        public void BringItemIntoView(object item) {
            MultiSelectTreeViewItem node = this.GetTreeViewItemsFor(new List<object> {item}).First();
            node.headerBorder?.BringIntoView();
        }

        public bool SelectNextItem() {
            return this.Selection.SelectNextFromKey();
        }

        public bool SelectPreviousItem() {
            return this.Selection.SelectPreviousFromKey();
        }

        public bool SelectFirstItem() {
            return this.Selection.SelectFirstFromKey();
        }

        public bool SelectLastItem() {
            return this.Selection.SelectLastFromKey();
        }

        public bool SelectAllItems() {
            return this.Selection.SelectAllFromKey();
        }

        public bool SelectParentItem() {
            return this.Selection.SelectParentFromKey();
        }

        #endregion

        #region Methods

        internal bool DeselectRecursive(MultiSelectTreeViewItem item, bool includeSelf) {
            List<MultiSelectTreeViewItem> selectedChildren = new List<MultiSelectTreeViewItem>();
            if (includeSelf) {
                if (item.IsSelected) {
                    PreviewSelectionChangedEventArgs e = new PreviewSelectionChangedEventArgs(false, item.DataContext);
                    this.OnPreviewSelectionChanged(e);
                    if (e.CancelAny) {
                        return false;
                    }

                    selectedChildren.Add(item);
                }
            }

            if (!this.CollectDeselectRecursive(item, selectedChildren)) {
                return false;
            }

            foreach (MultiSelectTreeViewItem child in selectedChildren) {
                child.IsSelected = false;
            }

            return true;
        }

        private bool CollectDeselectRecursive(MultiSelectTreeViewItem item, List<MultiSelectTreeViewItem> selectedChildren) {
            foreach (object child in item.Items) {
                MultiSelectTreeViewItem tvi = item.ItemContainerGenerator.ContainerFromItem(child) as MultiSelectTreeViewItem;
                if (tvi != null) {
                    if (tvi.IsSelected) {
                        PreviewSelectionChangedEventArgs e = new PreviewSelectionChangedEventArgs(false, child);
                        this.OnPreviewSelectionChanged(e);
                        if (e.CancelAny) {
                            return false;
                        }

                        selectedChildren.Add(tvi);
                    }

                    if (!this.CollectDeselectRecursive(tvi, selectedChildren)) {
                        return false;
                    }
                }
            }

            return true;
        }

        internal bool ClearSelectionByRectangle() {
            foreach (object item in new ArrayList(this.SelectedItems)) {
                PreviewSelectionChangedEventArgs e = new PreviewSelectionChangedEventArgs(false, item);
                this.OnPreviewSelectionChanged(e);
                if (e.CancelAny)
                    return false;
            }

            this.SelectedItems.Clear();
            return true;
        }

        internal MultiSelectTreeViewItem GetNextItem(MultiSelectTreeViewItem item, List<MultiSelectTreeViewItem> items) {
            int indexOfCurrent = item != null ? items.IndexOf(item) : -1;
            for (int i = indexOfCurrent + 1; i < items.Count; i++) {
                if (items[i].IsVisible) {
                    return items[i];
                }
            }

            return null;
        }

        internal MultiSelectTreeViewItem GetPreviousItem(MultiSelectTreeViewItem item, List<MultiSelectTreeViewItem> items) {
            int indexOfCurrent = item != null ? items.IndexOf(item) : -1;
            for (int i = indexOfCurrent - 1; i >= 0; i--) {
                if (items[i].IsVisible) {
                    return items[i];
                }
            }

            return null;
        }

        internal MultiSelectTreeViewItem GetFirstItem(List<MultiSelectTreeViewItem> items) {
            foreach (MultiSelectTreeViewItem item in items) {
                if (item.IsVisible) {
                    return item;
                }
            }

            return null;
        }

        internal MultiSelectTreeViewItem GetLastItem(List<MultiSelectTreeViewItem> items) {
            for (int i = items.Count - 1; i >= 0; i--) {
                if (items[i].IsVisible) {
                    return items[i];
                }
            }

            return null;
        }

        protected override DependencyObject GetContainerForItemOverride() {
            return new MultiSelectTreeViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item) {
            return item is MultiSelectTreeViewItem;
        }

        protected override AutomationPeer OnCreateAutomationPeer() {
            return new MultiSelectTreeViewAutomationPeer(this);
        }

        private static void OnSelectedItemsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            MultiSelectTreeView treeView = (MultiSelectTreeView) d;
            if (e.OldValue != null) {
                if (e.OldValue is INotifyCollectionChanged collection) {
                    collection.CollectionChanged -= treeView.OnSelectedItemsChanged;
                }
            }

            if (e.NewValue != null) {
                if (e.NewValue is INotifyCollectionChanged collection) {
                    collection.CollectionChanged += treeView.OnSelectedItemsChanged;
                }
            }
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null && this.SelectedItems is IList selection) {
                        foreach (object item in e.OldItems) {
                            selection.Remove(item);
                            // Don't preview and ask, it is already gone so it must be removed from
                            // the SelectedItems list
                        }
                    }

                    break;
                case NotifyCollectionChangedAction.Reset:
                    // If the items list has considerably changed, the selection is probably
                    // useless anyway, clear it entirely.
                    this.SelectedItems?.Clear();
                    break;
            }

            base.OnItemsChanged(e);
        }

        public static List<MultiSelectTreeViewItem> GetEntireTreeRecursive(ItemsControl parent, bool includeInvisible, bool includeDisabled = true) {
            List<MultiSelectTreeViewItem> list = new List<MultiSelectTreeViewItem>(128);
            AccumulateEntireTreeRecursive(list, parent, includeInvisible, includeDisabled);
            return list;
        }

        /// <summary>
        /// Accumulates all items in the given parent's tree, recursively
        /// </summary>
        /// <param name="dst">The list in which found items are added to</param>
        /// <param name="parent">The starting container, which can be a MultiSelectTreeView or MultiSelectTreeViewItem</param>
        /// <param name="includeInvisible">Includes invisible items</param>
        /// <param name="includeDisabled">Includes disabled items</param>
        public static void AccumulateEntireTreeRecursive(List<MultiSelectTreeViewItem> dst, ItemsControl parent, bool includeInvisible, bool includeDisabled = true) {
            ItemContainerGenerator icg = parent.ItemContainerGenerator;
            if (icg.Status == GeneratorStatus.NotStarted) {
                return;
            }

            for (int i = 0, count = parent.Items.Count; i < count; i++) {
                MultiSelectTreeViewItem tve = (MultiSelectTreeViewItem) icg.ContainerFromIndex(i);
                if (tve == null) {
                    continue; // Container was not generated, therefore it is probably not visible, so we can ignore it.
                }

                if ((includeInvisible || tve.IsVisible) && (includeDisabled || tve.IsEnabled)) {
                    dst.Add(tve);
                    if (includeInvisible || tve.IsExpanded) {
                        AccumulateEntireTreeRecursive(dst, tve, includeInvisible, includeDisabled);
                    }
                }
            }
        }

        public static IEnumerable<MultiSelectTreeViewItem> EnumerableTreeRecursive(ItemsControl parent, bool includeInvisible, bool includeDisabled = true) {
            ItemContainerGenerator icg = parent.ItemContainerGenerator;
            if (icg.Status == GeneratorStatus.NotStarted) {
                yield break;
            }

            for (int i = 0, count = parent.Items.Count; i < count; i++) {
                MultiSelectTreeViewItem nextItem = (MultiSelectTreeViewItem) icg.ContainerFromIndex(i);
                if (nextItem == null) {
                    continue; // Container was not generated, therefore it is probably not visible, so we can ignore it.
                }

                if ((includeInvisible || nextItem.IsVisible) && (includeDisabled || nextItem.IsEnabled)) {
                    yield return nextItem;
                    if (includeInvisible || nextItem.IsExpanded) {
                        using (IEnumerator<MultiSelectTreeViewItem> enumerator = EnumerableTreeRecursive(nextItem, includeInvisible, includeDisabled).GetEnumerator()) {
                            while (enumerator.MoveNext()) {
                                yield return enumerator.Current;
                            }
                        }
                    }
                }
            }
        }

        public static MultiSelectTreeViewItem FindFirstInTreeRecursive(Predicate<MultiSelectTreeViewItem> accept, ItemsControl parent, bool includeInvisible, bool includeDisabled = true) {
            ItemContainerGenerator icg = parent.ItemContainerGenerator;
            if (icg.Status == GeneratorStatus.NotStarted) {
                return null;
            }

            for (int i = 0, count = parent.Items.Count; i < count; i++) {
                MultiSelectTreeViewItem nextItem = (MultiSelectTreeViewItem) icg.ContainerFromIndex(i);
                if (nextItem == null) {
                    continue; // Container was not generated, therefore it is probably not visible, so we can ignore it.
                }

                if ((includeInvisible || nextItem.IsVisible) && (includeDisabled || nextItem.IsEnabled)) {
                    if (accept == null || accept(nextItem)) {
                        return nextItem;
                    }

                    if (includeInvisible || nextItem.IsExpanded) {
                        MultiSelectTreeViewItem found = FindFirstInTreeRecursive(accept, nextItem, includeInvisible, includeDisabled);
                        if (found != null) {
                            return found;
                        }
                    }
                }
            }

            return null;
        }

        public static MultiSelectTreeViewItem FindLastInTreeRecursive(Predicate<MultiSelectTreeViewItem> accept, ItemsControl parent, bool includeInvisible, bool includeDisabled = true) {
            ItemContainerGenerator icg = parent.ItemContainerGenerator;
            if (icg.Status == GeneratorStatus.NotStarted) {
                return null;
            }

            int count = parent.Items.Count;
            for (int i = count - 1; i >= 0; i--) {
                MultiSelectTreeViewItem nextItem = (MultiSelectTreeViewItem) icg.ContainerFromIndex(i);
                if (nextItem == null) {
                    continue; // Container was not generated, therefore it is probably not visible, so we can ignore it.
                }

                if ((includeInvisible || nextItem.IsVisible) && (includeDisabled || nextItem.IsEnabled)) {
                    if (includeInvisible || nextItem.IsExpanded) {
                        MultiSelectTreeViewItem found = FindLastInTreeRecursive(accept, nextItem, includeInvisible, includeDisabled);
                        if (found != null) {
                            return found;
                        }
                    }

                    if (accept == null || accept(nextItem)) {
                        return nextItem;
                    }
                }
            }

            return null;
        }

        internal IEnumerable<MultiSelectTreeViewItem> GetNodesToSelectBetween(MultiSelectTreeViewItem firstNode, MultiSelectTreeViewItem lastNode) {
            List<MultiSelectTreeViewItem> allNodes = GetEntireTreeRecursive(this, false, false);
            int firstIndex = allNodes.IndexOf(firstNode);
            int lastIndex = allNodes.IndexOf(lastNode);

            if (firstIndex >= allNodes.Count) {
                throw new InvalidOperationException("First node index " + firstIndex + "greater or equal than count " + allNodes.Count + ".");
            }

            if (lastIndex >= allNodes.Count) {
                throw new InvalidOperationException("Last node index " + lastIndex + " greater or equal than count " + allNodes.Count + ".");
            }

            List<MultiSelectTreeViewItem> nodesToSelect = new List<MultiSelectTreeViewItem>();

            if (lastIndex == firstIndex) {
                return new List<MultiSelectTreeViewItem> {firstNode};
            }

            if (lastIndex > firstIndex) {
                for (int i = firstIndex; i <= lastIndex; i++) {
                    if (allNodes[i].IsVisible) {
                        nodesToSelect.Add(allNodes[i]);
                    }
                }
            }
            else {
                for (int i = firstIndex; i >= lastIndex; i--) {
                    if (allNodes[i].IsVisible) {
                        nodesToSelect.Add(allNodes[i]);
                    }
                }
            }

            return nodesToSelect;
        }

        /// <summary>
        /// Finds the treeview item for each of the specified data items.
        /// </summary>
        /// <param name="dataItems">List of data items to search for.</param>
        /// <returns></returns>
        public IEnumerable<MultiSelectTreeViewItem> GetTreeViewItemsFor(IEnumerable dataItems) {
            if (dataItems == null) {
                return Enumerable.Empty<MultiSelectTreeViewItem>();
            }

            const int SizeUntilArrayOutperformsHashSet = 8;

            if (dataItems is ICollection) {
                ICollection collection = (ICollection) dataItems;
                if (collection.Count < SizeUntilArrayOutperformsHashSet) {
                    object[] array = new object[collection.Count];
                    collection.CopyTo(array, 0);
                    return this.GetTreeViewItemsFor_SmallCollection(array);
                }
                else {
                    return this.GetTreeViewItemsFor_LargeCollection(collection);
                }
            }
            else {
                ArrayList arrayList = new ArrayList();
                foreach (object o in dataItems)
                    arrayList.Add(o);
                if (arrayList.Count < SizeUntilArrayOutperformsHashSet) {
                    return this.GetTreeViewItemsFor_SmallCollection(arrayList.ToArray());
                }
                else {
                    return this.GetTreeViewItemsFor_LargeCollection(arrayList);
                }
            }
        }

        private IEnumerable<MultiSelectTreeViewItem> GetTreeViewItemsFor_SmallCollection(object[] collection) {
            if (collection == null || collection.Length < 1) {
                yield break;
            }

            using (IEnumerator<MultiSelectTreeViewItem> enumerator = EnumerableTreeRecursive(this, true).GetEnumerator()) {
                while (enumerator.MoveNext()) {
                    MultiSelectTreeViewItem next = enumerator.Current;
                    if (next != null) {
                        foreach (object obj in collection) {
                            if (obj == next.DataContext) {
                                yield return next;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<MultiSelectTreeViewItem> GetTreeViewItemsFor_LargeCollection(ICollection collection) {
            if (collection == null) {
                yield break;
            }

            ReferenceSet<object> items = new ReferenceSet<object>();
            foreach (object item in collection) {
                items.Add(item);
            }

            using (IEnumerator<MultiSelectTreeViewItem> enumerator = EnumerableTreeRecursive(this, true).GetEnumerator()) {
                while (enumerator.MoveNext()) {
                    MultiSelectTreeViewItem next = enumerator.Current;
                    if (next != null && items.Contains(next.DataContext)) {
                        yield return next;
                    }
                }
            }
        }

        /// <summary>
        /// Gets all data items referenced in all treeview items of the entire control.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<object> GetAllDataItems() => GetEntireTreeRecursive(this, true).Select(x => x.DataContext);

        // this eventhandler reacts on the firing control to, in order to update the own status
        private void OnSelectedItemsChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
#if DEBUG
                    // Make sure we don't confuse MultiSelectTreeViewItems and their DataContexts while development
                    if (e.NewItems.OfType<MultiSelectTreeViewItem>().Any())
                        throw new ArgumentException("A MultiSelectTreeViewItem instance was added to the SelectedItems collection. Only their DataContext instances must be added to this list!");
#endif
                    object last = null;
                    foreach (MultiSelectTreeViewItem item in this.GetTreeViewItemsFor(e.NewItems)) {
                        if (!item.IsSelected) {
                            item.IsSelected = true;
                        }

                        last = item.DataContext;
                    }

                    this.LastSelectedItem = last;
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (MultiSelectTreeViewItem item in this.GetTreeViewItemsFor(e.OldItems)) {
                        item.IsSelected = false;
                        if (item.DataContext == this.LastSelectedItem) {
                            if (this.SelectedItems.Count > 0) {
                                this.LastSelectedItem = this.SelectedItems[this.SelectedItems.Count - 1];
                            }
                            else {
                                this.LastSelectedItem = null;
                            }
                        }
                    }

                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (MultiSelectTreeViewItem item in GetEntireTreeRecursive(this, true)) {
                        if (item.IsSelected) {
                            item.IsSelected = false;
                        }
                    }

                    this.LastSelectedItem = null;
                    break;
                default: throw new InvalidOperationException();
            }

            this.OnSelectionChanged();
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (!e.Handled) {
                // Basically, this should not be needed anymore. It allows selecting an item with
                // the keyboard when the MultiSelectTreeView control has the focus. If there were already
                // items when the control was focused, an item has already been focused (and
                // subsequent key presses won't land here but at the item).
                Key key = e.Key;
                switch (key) {
                    case Key.Up:
                        // Select last item
                        MultiSelectTreeViewItem lastNode = FindLastInTreeRecursive(null, this, false);
                        if (lastNode != null) {
                            this.Selection.Select(lastNode);
                            e.Handled = true;
                        }

                        break;
                    case Key.Down:
                        // Select first item
                        MultiSelectTreeViewItem firstNode = FindFirstInTreeRecursive(null, this, false);
                        if (firstNode != null) {
                            this.Selection.Select(firstNode);
                            e.Handled = true;
                        }

                        break;
                }
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e) {
            base.OnPreviewKeyDown(e);
            if (!this.IsKeyboardMode) {
                this.IsKeyboardMode = true;
                //System.Diagnostics.Debug.WriteLine("Changing to keyboard mode from PreviewKeyDown");
            }
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e) {
            base.OnPreviewKeyDown(e);
            if (!this.IsKeyboardMode) {
                this.IsKeyboardMode = true;
                //System.Diagnostics.Debug.WriteLine("Changing to keyboard mode from PreviewKeyUp");
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e) {
            base.OnPreviewMouseDown(e);
            if (this.IsKeyboardMode) {
                this.IsKeyboardMode = false;
                //System.Diagnostics.Debug.WriteLine("Changing to mouse mode");
            }
        }

        protected override void OnGotFocus(RoutedEventArgs e) {
            //System.Diagnostics.Debug.WriteLine("MultiSelectTreeView.OnGotFocus()");
            //System.Diagnostics.Debug.WriteLine(Environment.StackTrace);

            base.OnGotFocus(e);

            // If the MultiSelectTreeView control has gotten the focus, it needs to pass it to an
            // item instead. If there was an item focused before, return to that. Otherwise just
            // focus this first item in the list if any. If there are no items at all, the
            // MultiSelectTreeView control just keeps the focus.
            // In any case, the focussing must occur when the current event processing is finished,
            // i.e. be queued in the dispatcher. Otherwise the TreeView could keep its focus
            // because other focus things are still going on and interfering this final request.

            MultiSelectTreeViewItem lastFocusedItem = this.LastFocusedItem;
            if (lastFocusedItem != null) {
                this.Dispatcher.BeginInvoke((Action) (() => FocusHelper.Focus(lastFocusedItem)));
            }
            else {
                MultiSelectTreeViewItem firstNode = FindFirstInTreeRecursive(null, this, false);
                if (firstNode != null) {
                    this.Dispatcher.BeginInvoke((Action) (() => FocusHelper.Focus(firstNode)));
                }
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e) {
            base.OnMouseDown(e);

            // This happens when a mouse button was pressed in an area which is not covered by an
            // item. Then, it should be focused which in turn passes on the focus to an item.
            this.Focus();
        }

        protected void OnPreviewSelectionChanged(PreviewSelectionChangedEventArgs e) {
            EventHandler<PreviewSelectionChangedEventArgs> handler = this.PreviewSelectionChanged;
            if (handler != null) {
                handler(this, e);
            }
        }

        protected void OnSelectionChanged() {
            EventHandler handler = this.SelectionChanged;
            if (handler != null) {
                handler(this, EventArgs.Empty);
            }
        }

        #endregion
    }
}