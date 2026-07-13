/*
 * Copyright 2026 Esri
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.UtilityNetworks;

#if MAUI
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    internal abstract class UtilityAssociationWorkflowPage : INotifyPropertyChanged
    {
        private string? _errorMessage;
        private bool _isLoading;

        protected UtilityAssociationWorkflowPage(
            FeatureForm form,
            UtilityAssociationsFormElement element,
            UtilityAssociationsFilter filter,
            Action<object, object?> navigate)
        {
            Form = form;
            Element = element;
            Filter = filter;
            Navigate = navigate;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public abstract string Title { get; }

        public virtual string? Subtitle => null;

        public string? ErrorMessage
        {
            get => _errorMessage;
            internal set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            protected set => SetProperty(ref _isLoading, value);
        }

        protected FeatureForm Form { get; }

        protected UtilityAssociationsFormElement Element { get; }

        protected UtilityAssociationsFilter Filter { get; }

        protected Action<object, object?> Navigate { get; }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    internal sealed class UtilityAssociationFeatureSourceSelection : UtilityAssociationWorkflowPage
    {
        private readonly ObservableCollection<UtilityAssociationFeatureSource> _featureSources = new();
        private IReadOnlyList<UtilityAssociationFeatureSource> _filteredFeatureSources = Array.Empty<UtilityAssociationFeatureSource>();
        private UtilityAssociationFeatureSource? _selectedFeatureSource;
        private string _searchText = string.Empty;

        internal UtilityAssociationFeatureSourceSelection(
            FeatureForm form,
            UtilityAssociationsFormElement element,
            UtilityAssociationsFilter filter,
            Action<object, object?> navigate)
            : base(form, element, filter, navigate)
        {
            _ = LoadAsync();
        }

        public override string Title => Properties.Resources.GetString("FeatureFormUtilityAssociationsNetworkDataSource")!;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value ?? string.Empty))
                {
                    UpdateFilteredSources();
                }
            }
        }

        public IReadOnlyList<UtilityAssociationFeatureSource> FilteredFeatureSources
        {
            get => _filteredFeatureSources;
            internal set
            {
                if (SetProperty(ref _filteredFeatureSources, value))
                {
                    OnPropertyChanged(nameof(CountText));
                    OnPropertyChanged(nameof(HasNoResults));
                }
            }
        }

        public string CountText => string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            Properties.Resources.GetString("FeatureFormUtilityAssociationsResultCount")!,
            FilteredFeatureSources.Count);

        public bool HasNoResults => !IsLoading && FilteredFeatureSources.Count == 0;

        public UtilityAssociationFeatureSource? SelectedFeatureSource
        {
            get => _selectedFeatureSource;
            set
            {
                if (value is null || !SetProperty(ref _selectedFeatureSource, value))
                {
                    return;
                }

                Navigate(new UtilityAssociationAssetTypeSelection(Form, Element, Filter, value, Navigate), null);
                _selectedFeatureSource = null;
                OnPropertyChanged();
            }
        }

        private async Task LoadAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            OnPropertyChanged(nameof(HasNoResults));
            try
            {
                foreach (var source in await Element.GetAssociationFeatureSourcesAsync(Filter))
                {
                    _featureSources.Add(source);
                }

                UpdateFilteredSources();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(HasNoResults));
            }
        }

        private void UpdateFilteredSources()
        {
            FilteredFeatureSources = string.IsNullOrWhiteSpace(SearchText)
                ? _featureSources.ToList()
                : _featureSources.Where(source => source.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)).ToList();
        }
    }

    internal sealed class UtilityAssociationAssetTypeSelection : UtilityAssociationWorkflowPage
    {
        private readonly UtilityAssociationFeatureSource _source;
        private IReadOnlyList<UtilityAssetType> _filteredAssetTypes;
        private UtilityAssetType? _selectedAssetType;
        private string _searchText = string.Empty;

        internal UtilityAssociationAssetTypeSelection(
            FeatureForm form,
            UtilityAssociationsFormElement element,
            UtilityAssociationsFilter filter,
            UtilityAssociationFeatureSource source,
            Action<object, object?> navigate)
            : base(form, element, filter, navigate)
        {
            _source = source;
            _filteredAssetTypes = OrderAssetTypes(source.AssetTypes);
        }

        public override string Title => _source.Name;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value ?? string.Empty))
                {
                    UpdateFilteredAssetTypes();
                }
            }
        }

        public IReadOnlyList<UtilityAssetType> FilteredAssetTypes
        {
            get => _filteredAssetTypes;
            internal set
            {
                if (SetProperty(ref _filteredAssetTypes, value))
                {
                    OnPropertyChanged(nameof(CountText));
                    OnPropertyChanged(nameof(HasNoResults));
                }
            }
        }

        public string CountText => string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            Properties.Resources.GetString("FeatureFormUtilityAssociationsResultCount")!,
            FilteredAssetTypes.Count);

        public bool HasNoResults => FilteredAssetTypes.Count == 0;

        public UtilityAssetType? SelectedAssetType
        {
            get => _selectedAssetType;
            set
            {
                if (value is null || !SetProperty(ref _selectedAssetType, value))
                {
                    return;
                }

                Navigate(new UtilityAssociationFeatureCandidateSelection(Form, Element, Filter, _source, value, Navigate), null);
                _selectedAssetType = null;
                OnPropertyChanged();
            }
        }

        private void UpdateFilteredAssetTypes()
        {
            var assetTypes = string.IsNullOrWhiteSpace(SearchText)
                ? _source.AssetTypes
                : _source.AssetTypes.Where(assetType => assetType.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase));
            FilteredAssetTypes = OrderAssetTypes(assetTypes);
        }

        private static IReadOnlyList<UtilityAssetType> OrderAssetTypes(IEnumerable<UtilityAssetType> assetTypes)
            => assetTypes.OrderBy(assetType => assetType.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(assetType => assetType.AssetGroup.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
    }

    internal sealed class UtilityAssociationFeatureCandidateSelection : UtilityAssociationWorkflowPage
    {
        private readonly UtilityAssociationFeatureSource _source;
        private readonly UtilityAssetType _assetType;
        private readonly ObservableCollection<UtilityAssociationFeatureCandidate> _candidates = new();
        private IReadOnlyList<UtilityAssociationFeatureCandidate> _filteredCandidates = Array.Empty<UtilityAssociationFeatureCandidate>();
        private bool _isQuerying;
        private QueryParameters? _nextQueryParameters;
        private int _searchVersion;
        private UtilityAssociationFeatureCandidate? _selectedCandidate;
        private string _searchText = string.Empty;

        internal UtilityAssociationFeatureCandidateSelection(
            FeatureForm form,
            UtilityAssociationsFormElement element,
            UtilityAssociationsFilter filter,
            UtilityAssociationFeatureSource source,
            UtilityAssetType assetType,
            Action<object, object?> navigate)
            : base(form, element, filter, navigate)
        {
            _source = source;
            _assetType = assetType;
            LoadMoreCommand = new UtilityAssociationAsyncCommand(LoadMoreAsync, () => HasMore && !IsLoading);
            _ = LoadFirstPageAsync();
        }

        public override string Title => IsLoading
            ? Properties.Resources.GetString("FeatureFormUtilityAssociationsLoading")!
            : string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                Properties.Resources.GetString("FeatureFormUtilityAssociationsAvailableFeatures")!,
                FilteredCandidates.Count);

        public override string Subtitle => _assetType.Name;

        public ICommand LoadMoreCommand { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value ?? string.Empty))
                {
                    UpdateFilteredCandidates();
                    _searchVersion++;
                    _ = CompleteSearchAsync(_searchVersion);
                }
            }
        }

        public IReadOnlyList<UtilityAssociationFeatureCandidate> FilteredCandidates
        {
            get => _filteredCandidates;
            internal set
            {
                if (SetProperty(ref _filteredCandidates, value))
                {
                    OnPropertyChanged(nameof(Title));
                    OnPropertyChanged(nameof(HasNoResults));
                }
            }
        }

        public bool HasMore => _nextQueryParameters is not null && string.IsNullOrWhiteSpace(SearchText);

        public bool HasNoResults => !IsLoading && FilteredCandidates.Count == 0;

        public UtilityAssociationFeatureCandidate? SelectedCandidate
        {
            get => _selectedCandidate;
            set
            {
                if (value is null || !SetProperty(ref _selectedCandidate, value))
                {
                    return;
                }

                Navigate(new UtilityAssociationCreation(Form, Element, Filter, value), null);
                _selectedCandidate = null;
                OnPropertyChanged();
            }
        }

        private async Task LoadFirstPageAsync()
        {
            _isQuerying = true;
            IsLoading = true;
            ErrorMessage = null;
            NotifyQueryStateChanged();
            try
            {
                var result = await _source.QueryFeaturesAsync(_assetType);
                AppendResult(result);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
                _isQuerying = false;
                NotifyQueryStateChanged();
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    _ = CompleteSearchAsync(_searchVersion);
                }
            }
        }

        private async Task LoadMoreAsync()
        {
            if (_nextQueryParameters is null)
            {
                return;
            }

            await QueryNextPageAsync(CancellationToken.None);
        }

        private async Task CompleteSearchAsync(int searchVersion)
        {
            await Task.Delay(500);
            while (searchVersion == _searchVersion &&
                !string.IsNullOrWhiteSpace(SearchText) &&
                FilteredCandidates.Count == 0 &&
                _nextQueryParameters is not null)
            {
                await QueryNextPageAsync(CancellationToken.None);
            }
        }

        private async Task QueryNextPageAsync(CancellationToken cancellationToken)
        {
            if (_isQuerying)
            {
                return;
            }

            _isQuerying = true;
            try
            {
                var parameters = _nextQueryParameters;
                if (parameters is null)
                {
                    return;
                }

                IsLoading = true;
                ErrorMessage = null;
                NotifyQueryStateChanged();
                var result = await _source.QueryFeaturesAsync(_assetType, parameters, cancellationToken);
                AppendResult(result);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                _nextQueryParameters = null;
            }
            finally
            {
                IsLoading = false;
                NotifyQueryStateChanged();
                _isQuerying = false;
            }
        }

        private void AppendResult(UtilityAssociationFeatureSourceQueryResult result)
        {
            foreach (var candidate in result.Candidates)
            {
                _candidates.Add(candidate);
            }

            _nextQueryParameters = result.NextQueryParams;
            UpdateFilteredCandidates();
        }

        private void UpdateFilteredCandidates()
        {
            FilteredCandidates = string.IsNullOrWhiteSpace(SearchText)
                ? _candidates.ToList()
                : _candidates.Where(candidate => candidate.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)).ToList();
            OnPropertyChanged(nameof(HasMore));
            (LoadMoreCommand as UtilityAssociationAsyncCommand)?.RaiseCanExecuteChanged();
        }

        private void NotifyQueryStateChanged()
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(HasNoResults));
            OnPropertyChanged(nameof(HasMore));
            (LoadMoreCommand as UtilityAssociationAsyncCommand)?.RaiseCanExecuteChanged();
        }
    }

    internal sealed class UtilityAssociationAsyncCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private bool _isExecuting;

        internal UtilityAssociationAsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute ?? (() => true);
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => !_isExecuting && _canExecute();

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            _isExecuting = true;
            RaiseCanExecuteChanged();
            try
            {
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        internal void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
