// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/
using System;
using System.Collections.Generic;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Xamarin.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// The BookmarksView view presents bookmarks, either from a list defined by <see cref="BookmarksOverride" /> or
    /// the Map or Scene shown in the associated <see cref="GeoView" />.
    /// </summary>
    public class BookmarksView : TemplatedView
    {
        private ListView? _presentingView;
        private BookmarksViewDataSource _dataSource = new BookmarksViewDataSource();

        private static readonly DataTemplate DefaultDataTemplate;
        private static readonly ControlTemplate DefaultControlTemplate;

        static BookmarksView()
        {
            DefaultDataTemplate = new DataTemplate(() =>
            {
                var defaultCell = new TextCell();
                defaultCell.SetBinding(TextCell.TextProperty, nameof(Bookmark.Name));
                return defaultCell;
            });

            string template = @"<ControlTemplate xmlns=""http://xamarin.com/schemas/2014/forms"" xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"" xmlns:esriTK=""clr-namespace:Esri.ArcGISRuntime.Toolkit.Xamarin.Forms"">
                                    <ListView x:Name=""PresentingView"" HorizontalOptions=""FillAndExpand"" VerticalOptions=""FillAndExpand"">
                                        <x:Arguments>
                                            <ListViewCachingStrategy>RecycleElement</ListViewCachingStrategy>
                                        </x:Arguments>
                                    </ListView>
                                </ControlTemplate>";
            DefaultControlTemplate = Extensions.LoadFromXaml(new ControlTemplate(), template);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BookmarksView"/> class.
        /// </summary>
        public BookmarksView()
        {
            ItemTemplate = DefaultDataTemplate;

            ControlTemplate = DefaultControlTemplate;
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_presentingView != null)
            {
                _presentingView.ItemSelected -= Internal_bookmarkSelected;
            }

            _presentingView = GetTemplateChild("PresentingView") as ListView;

            if (_presentingView != null)
            {
                _presentingView.ItemSelected += Internal_bookmarkSelected;
                _presentingView.ItemTemplate = ItemTemplate;
                _presentingView.ItemsSource = _dataSource;
            }
        }

        /// <summary>
        /// Gets or sets the data template that renders bookmark entries in the list.
        /// </summary>
        public DataTemplate? ItemTemplate
        {
            get { return GetValue(ItemTemplateProperty) as DataTemplate; }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the list of bookmarks to display.
        /// Otherwise, the bookmarks from the Map or Scene shown in the associated <see cref="GeoView" /> are displayed.
        /// </summary>
        /// <remarks>If set to a <see cref="System.Collections.Specialized.INotifyCollectionChanged" />, the view will be updated with collection changes.</remarks>
        /// <seealso cref="BookmarksOverrideProperty" />
        public IEnumerable<Bookmark>? BookmarksOverride
        {
            get { return GetValue(BookmarksOverrideProperty) as IEnumerable<Bookmark>; }
            set { SetValue(BookmarksOverrideProperty, value); }
        }

        /// <summary>
        /// Gets or sets the geoview that contain the layers whose symbology and description will be displayed.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        /// <seealso cref="GeoViewProperty"/>
        public GeoView? GeoView
        {
            get { return GetValue(GeoViewProperty) as GeoView; }
            set { SetValue(GeoViewProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeoView"/> bindable property.
        /// </summary>
        public static readonly BindableProperty GeoViewProperty =
            BindableProperty.Create(nameof(GeoView), typeof(GeoView), typeof(BookmarksView), null, BindingMode.OneWay, null, propertyChanged: GeoViewChanged);

        /// <summary>
        /// Identifies the <see cref="BookmarksOverride"/> bindable property.
        /// </summary>
        public static readonly BindableProperty BookmarksOverrideProperty =
            BindableProperty.Create(nameof(BookmarksOverride), typeof(IEnumerable<Bookmark>), typeof(BookmarksView), null, BindingMode.OneWay, null, propertyChanged: BookmarksOverrideChanged);

        /// <summary>
        /// Identifies the <see cref="ItemTemplate" /> bindable property.
        /// </summary>
        public static readonly BindableProperty ItemTemplateProperty =
            BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(BookmarksView), DefaultDataTemplate, BindingMode.OneWay, null, propertyChanged: ItemTemplateChanged);

        /// <summary>
        /// Handles property changes for the <see cref="BookmarksOverride" /> bindable property.
        /// </summary>
        private static void BookmarksOverrideChanged(BindableObject sender, object? oldValue, object? newValue)
        {
            ((BookmarksView)sender)._dataSource.SetOverrideList(newValue as IEnumerable<Bookmark>);
        }

        /// <summary>
        /// Handles property changes for the <see cref="GeoView" /> bindable property.
        /// </summary>
        private static void GeoViewChanged(BindableObject sender, object? oldValue, object? newValue)
        {
            BookmarksView bookmarkView = (BookmarksView)sender;

            bookmarkView._dataSource.SetGeoView(newValue as GeoView);
        }

        /// <summary>
        /// Handles property changes for the <see cref="ItemTemplate" /> bindable property.
        /// </summary>
        private static void ItemTemplateChanged(BindableObject sender, object? oldValue, object? newValue)
        {
            BookmarksView bookmarkView = (BookmarksView)sender;

            if (bookmarkView._presentingView != null)
            {
                bookmarkView._presentingView.ItemTemplate = newValue as DataTemplate;
            }
        }

        /// <summary>
        /// Selects the bookmark and navigates to it in the associated <see cref="GeoView" />.
        /// </summary>
        /// <param name="bookmark">Bookmark to navigate to. Must be non-null with a valid viewpoint.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="bookmark"/> is <c>null</c>.</exception>
        private void SelectAndNavigateToBookmark(Bookmark bookmark)
        {
            if (bookmark.Viewpoint == null)
            {
                throw new ArgumentException("Bookmark viewpoint is null");
            }

            GeoView?.SetViewpointAsync(bookmark.Viewpoint);

            BookmarkSelected?.Invoke(this, bookmark);
        }

        /// <summary>
        /// Handles selection on the underlying list view.
        /// </summary>
        private void Internal_bookmarkSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is Bookmark bm)
            {
                SelectAndNavigateToBookmark(bm);
            }

            if (e.SelectedItem != null)
            {
                ((ListView)sender).SelectedItem = null;
            }
        }

        /// <summary>
        /// Raised whenever a bookmark is selected.
        /// </summary>
        public event EventHandler<Bookmark>? BookmarkSelected;
    }
}