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
using Esri.ArcGISRuntime.Mapping.FeatureForms;
using Esri.ArcGISRuntime.UtilityNetworks;

#if MAUI
using FeatureFormView = Esri.ArcGISRuntime.Toolkit.Maui.FeatureFormView;
namespace Esri.ArcGISRuntime.Toolkit.Maui.Primitives
#else
using FeatureFormView = Esri.ArcGISRuntime.Toolkit.UI.Controls.FeatureFormView;
namespace Esri.ArcGISRuntime.Toolkit.Primitives
#endif
{
    /// <summary>
    /// Provides the bindable state and creation logic for the final step of the add-association workflow.
    /// It loads the options supported by the selected utility network feature, exposes the applicable
    /// terminal, fraction-along-edge, and content-visibility choices, and creates the association before
    /// refreshing the form results and returning to the feature form.
    /// </summary>
    internal sealed class UtilityAssociationCreation : INotifyPropertyChanged
    {
        private readonly UtilityAssociationFeatureCandidate _candidate;
        private readonly UtilityAssociationsFormElement _element;
        private readonly UtilityAssociationsFilter _filter;
        private readonly FeatureForm _form;
        private bool _contentIsVisible;
        private string? _errorMessage;
        private double _fractionAlongEdgePercent;
        private bool _isAdding;
        private bool _isLoading = true;
        private UtilityAssociationFeatureOptions? _options;
        private UtilityTerminal? _selectedFromTerminal;
        private UtilityTerminal? _selectedToTerminal;

        internal UtilityAssociationCreation(
            FeatureForm form,
            UtilityAssociationsFormElement element,
            UtilityAssociationsFilter filter,
            UtilityAssociationFeatureCandidate candidate)
        {
            _form = form;
            _element = element;
            _filter = filter;
            _candidate = candidate;
            AddCommand = new UtilityAssociationAsyncCommand(AddAsync, () => _options is not null && !IsAdding);
            _ = LoadOptionsAsync();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Title => Properties.Resources.GetString("FeatureFormUtilityAssociationsNewAssociation")!;

        public string AssociationType => _filter.FilterType switch
        {
            UtilityAssociationsFilterType.Attachment or UtilityAssociationsFilterType.Structure
                => Properties.Resources.GetString("FeatureFormUtilityAssociationsAttachmentType")!,
            UtilityAssociationsFilterType.Container or UtilityAssociationsFilterType.Content
                => Properties.Resources.GetString("FeatureFormUtilityAssociationsContainmentType")!,
            _ => Properties.Resources.GetString("FeatureFormUtilityAssociationsConnectivityType")!,
        };

        public string FromElement => CandidateIsToElement ? _form.Title : _candidate.Title;

        public string ToElement => CandidateIsToElement ? _candidate.Title : _form.Title;

        public bool ShowContentVisibility =>
            _filter.FilterType is UtilityAssociationsFilterType.Container or UtilityAssociationsFilterType.Content;

        public bool ContentIsVisible
        {
            get => _contentIsVisible;
            set => SetProperty(ref _contentIsVisible, value);
        }

        public bool ShowFractionAlongEdge => _options?.IsFractionAlongEdgeValid == true;

        public double FractionAlongEdgePercent
        {
            get => _fractionAlongEdgePercent;
            set => SetProperty(ref _fractionAlongEdgePercent, value);
        }

        public ObservableCollection<UtilityTerminal> FromTerminals { get; } = new();

        public ObservableCollection<UtilityTerminal> ToTerminals { get; } = new();

        public IReadOnlyList<string> FromTerminalNames => FromTerminals.Select(terminal => terminal.Name).ToList();

        public IReadOnlyList<string> ToTerminalNames => ToTerminals.Select(terminal => terminal.Name).ToList();

        public bool ShowFromTerminals => FromTerminals.Count > 0;

        public bool ShowToTerminals => ToTerminals.Count > 0;

        public int SelectedFromTerminalIndex
        {
            get => SelectedFromTerminal is null ? -1 : FromTerminals.IndexOf(SelectedFromTerminal);
            set => SelectedFromTerminal = value >= 0 && value < FromTerminals.Count ? FromTerminals[value] : null;
        }

        public int SelectedToTerminalIndex
        {
            get => SelectedToTerminal is null ? -1 : ToTerminals.IndexOf(SelectedToTerminal);
            set => SelectedToTerminal = value >= 0 && value < ToTerminals.Count ? ToTerminals[value] : null;
        }

        public UtilityTerminal? SelectedFromTerminal
        {
            get => _selectedFromTerminal;
            set
            {
                if (SetProperty(ref _selectedFromTerminal, value))
                {
                    OnPropertyChanged(nameof(SelectedFromTerminalIndex));
                }
            }
        }

        public UtilityTerminal? SelectedToTerminal
        {
            get => _selectedToTerminal;
            set
            {
                if (SetProperty(ref _selectedToTerminal, value))
                {
                    OnPropertyChanged(nameof(SelectedToTerminalIndex));
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public bool IsAdding
        {
            get => _isAdding;
            private set
            {
                if (SetProperty(ref _isAdding, value))
                {
                    (AddCommand as UtilityAssociationAsyncCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            internal set => SetProperty(ref _errorMessage, value);
        }

        public ICommand AddCommand { get; }

        private bool CandidateIsToElement => _filter.FilterType is
            UtilityAssociationsFilterType.Attachment or
            UtilityAssociationsFilterType.Connectivity or
            UtilityAssociationsFilterType.Content;

        private async Task LoadOptionsAsync()
        {
            try
            {
                _options = await _element.GetOptionsForAssociationCandidateAsync(_candidate.Feature);
                var fromConfiguration = CandidateIsToElement
                    ? _options.FormFeatureTerminalConfiguration
                    : _options.CandidateFeatureTerminalConfiguration;
                var toConfiguration = CandidateIsToElement
                    ? _options.CandidateFeatureTerminalConfiguration
                    : _options.FormFeatureTerminalConfiguration;

                AddTerminals(FromTerminals, fromConfiguration);
                AddTerminals(ToTerminals, toConfiguration);
                OnPropertyChanged(nameof(FromTerminalNames));
                OnPropertyChanged(nameof(ToTerminalNames));
                SelectedFromTerminal = FromTerminals.FirstOrDefault();
                SelectedToTerminal = ToTerminals.FirstOrDefault();
                OnPropertyChanged(nameof(ShowContentVisibility));
                OnPropertyChanged(nameof(ShowFractionAlongEdge));
                OnPropertyChanged(nameof(ShowFromTerminals));
                OnPropertyChanged(nameof(ShowToTerminals));
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
                (AddCommand as UtilityAssociationAsyncCommand)?.RaiseCanExecuteChanged();
            }
        }

        private async Task AddAsync()
        {
            if (_options is null)
            {
                return;
            }

            IsAdding = true;
            ErrorMessage = null;
            try
            {
                if (!await _element.CanAddAssociationAsync(_candidate.Feature, _filter))
                {
                    ErrorMessage = Properties.Resources.GetString("FeatureFormUtilityAssociationsCannotAddAssociation");
                    return;
                }

                if (ShowContentVisibility)
                {
                    await _element.AddAssociationAsync(_candidate.Feature, _filter, ContentIsVisible);
                }
                else if (_filter.FilterType is UtilityAssociationsFilterType.Attachment or UtilityAssociationsFilterType.Structure)
                {
                    await _element.AddAssociationAsync(_candidate.Feature, _filter);
                }
                else if (_options.IsFractionAlongEdgeValid)
                {
                    var fraction = FractionAlongEdgePercent / 100d;
                    var terminal = SelectedFromTerminal ?? SelectedToTerminal;
                    if (terminal is null)
                    {
                        await _element.AddAssociationAsync(_candidate.Feature, _filter, fraction);
                    }
                    else
                    {
                        await _element.AddAssociationAsync(_candidate.Feature, _filter, fraction, terminal);
                    }
                }
                else if (SelectedFromTerminal is not null || SelectedToTerminal is not null)
                {
                    var featureTerminal = CandidateIsToElement ? SelectedToTerminal : SelectedFromTerminal;
                    var currentFeatureTerminal = CandidateIsToElement ? SelectedFromTerminal : SelectedToTerminal;
                    await _element.AddAssociationAsync(_candidate.Feature, _filter, featureTerminal, currentFeatureTerminal);
                }
                else
                {
                    await _element.AddAssociationAsync(_candidate.Feature, _filter);
                }

                await _element.FetchAssociationsFilterResultsAsync();
                await FeatureFormView.GetFeatureFormViewParentFromWorkflowAsync(this, 4);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsAdding = false;
            }
        }

        private static void AddTerminals(
            ObservableCollection<UtilityTerminal> destination,
            UtilityTerminalConfiguration? configuration)
        {
            if (configuration is null)
            {
                return;
            }

            foreach (var terminal in configuration.Terminals)
            {
                destination.Add(terminal);
            }
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
