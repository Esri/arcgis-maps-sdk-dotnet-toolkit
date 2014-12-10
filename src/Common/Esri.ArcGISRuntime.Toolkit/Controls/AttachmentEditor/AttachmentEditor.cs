// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.
#if !WINDOWS_PHONE_APP
using Esri.ArcGISRuntime.Data;
using System;
using System.IO;
using System.Threading.Tasks;
#if NETFX_CORE
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using System.Collections.Generic;
#else
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
#endif
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
	/// <summary>
	/// The <see cref="AttachmentEditor"/> allows for uploading, downloading, and deleting of attachment files associated with <see cref="Feature"/> in a <see cref="ArcGISFeatureTable"/>.
	/// </summary>
	[StyleTypedProperty(Property = "AttachmentList", StyleTargetType=typeof(FrameworkElement))]
	[TemplateVisualState(GroupName = "BusyStates", Name = "Loaded")]
	[TemplateVisualState(GroupName = "BusyStates", Name = "Busy")]
	public class AttachmentEditor : Control
	{
		private ObservableCollection<EditableAttachment> attachments = new ObservableCollection<EditableAttachment>();
		private bool canAdd;
		private bool canUpdate;
		private bool canDelete;

		#region Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="AttachmentEditor"/> class.
		/// </summary>
		public AttachmentEditor()
		{
#if NETFX_CORE
            DefaultStyleKey = typeof(AttachmentEditor);
#endif
			DataContext = this;
		}

		static AttachmentEditor()
		{
#if !NETFX_CORE
			DefaultStyleKeyProperty.OverrideMetadata(typeof(AttachmentEditor),
					new FrameworkPropertyMetadata(typeof(AttachmentEditor)));
#endif
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets or sets the <see cref="DataTemplate"/> used for each attachment item.
		/// </summary>
		public DataTemplate ItemTemplate
		{
			get { return (DataTemplate)GetValue(ItemTemplateProperty); }
			set { SetValue(ItemTemplateProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="ItemTemplate"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ItemTemplateProperty =
			DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(AttachmentEditor), null);

		/// <summary>
		/// Gets or sets the <see cref="ArcGISFeatureTable"/> that contains the <see cref="Feature"/> whose attachments will be displayed.
		/// </summary>
		public ArcGISFeatureTable FeatureTable
		{
			get { return (ArcGISFeatureTable)GetValue(FeatureTableProperty); }
			set { SetValue(FeatureTableProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="FeatureTable"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty FeatureTableProperty =
			DependencyProperty.Register("FeatureTable", typeof(ArcGISFeatureTable), typeof(AttachmentEditor), null);

		/// <summary>
		/// Gets or sets the ID of the <see cref="Feature"/> whose attachments will be displayed.
		/// </summary>
		public long FeatureID
		{
			get { return (long)GetValue(FeatureIdProperty); }
			set { SetValue(FeatureIdProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="FeatureID"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty FeatureIdProperty =
			DependencyProperty.Register("FeatureID", typeof(long), typeof(AttachmentEditor), new PropertyMetadata(long.MinValue, OnFeatureIdPropertyChanged));

		private static async void OnFeatureIdPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var attachmentEditor = (AttachmentEditor)d;
			await attachmentEditor.QueryAttachmentsAsync();
			await attachmentEditor.CheckEditableStateAsync();
		}
		#endregion

		#region Commands
		private DelegateCommand addCommand;
		/// <summary>
		/// Enables the <see cref="AttachmentEditor"/> to upload attachment files to <see cref="Feature"/>.
		/// </summary>
		public ICommand Add
		{
			get
			{
				if (addCommand == null)
					addCommand = new DelegateCommand(OnAdd, CanAdd);
				return addCommand;
			}
		}

		private DelegateCommand updateCommand;
		/// <summary>
		/// Enables the <see cref="AttachmentEditor"/> to update uploaded attachment files of <see cref="Feature"/>.
		/// </summary>
		public ICommand Update
		{
			get
			{
				if (updateCommand == null)
					updateCommand = new DelegateCommand(OnUpdate, CanUpdate);
				return updateCommand;
			}
		}

		private DelegateCommand deleteCommand;
		/// <summary>
		/// Enables the <see cref="AttachmentEditor"/> to delete uploaded attachment files of <see cref="Feature"/>.
		/// </summary>
		public ICommand Delete
		{
			get
			{
				if (deleteCommand == null)
					deleteCommand = new DelegateCommand(OnDelete, CanDelete);
				return deleteCommand;
			}
		}

		private DelegateCommand openCommand;
		/// <summary>
		/// Enables the <see cref="AttachmentEditor"/> to save uploaded attachment files of <see cref="Feature"/> locally.
		/// </summary>
		public ICommand Open
		{
			get
			{
				if (openCommand == null)
					openCommand = new DelegateCommand(OnOpen);
				return openCommand;
			}
		}
		#endregion

		#region Events
		/// <summary>
		/// Occurs when an operation to attachment failed.
		/// </summary>
		public event EventHandler<AttachmentEditorFailedEventArgs> AttachmentEditorFailed;
		private void OnAttachmentEditFailed(AttachmentOperation operation, Exception error)
		{
			if (AttachmentEditorFailed != null)
				AttachmentEditorFailed(this, new AttachmentEditorFailedEventArgs(operation, error));
		}
		#endregion

		/// <summary>
		/// When overridden in a derived class, is invoked whenever application code or internal processes call ApplyTemplate.
        /// </summary>
#if NETFX_CORE
        protected 
#else 
        public
#endif
			async override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			var attachmentList = GetTemplateChild("AttachmentList") as ItemsControl;
			if (attachmentList != null)
				attachmentList.ItemsSource = attachments;
			await QueryAttachmentsAsync();
			await CheckEditableStateAsync();
		}

#if NETFX_CORE
		private async Task<StorageFile> GetFileAsync()
		{
			var picker = new FileOpenPicker();
			picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
			picker.ViewMode = PickerViewMode.Thumbnail;
			picker.FileTypeFilter.Add(".tif");
			picker.FileTypeFilter.Add(".jpg");
			picker.FileTypeFilter.Add(".gif");
			picker.FileTypeFilter.Add(".png");
			picker.FileTypeFilter.Add(".bmp");
			return await picker.PickSingleFileAsync();
		}
		private async Task<StorageFile> SaveFileAsync(string fileName)
		{
			var picker = new FileSavePicker();
			picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
			picker.FileTypeChoices.Add("Image", new List<string>() { ".tif", ".jpg", ".gif", ".png", ".bmp" });
			picker.SuggestedFileName = string.IsNullOrWhiteSpace(fileName) ? "New Image" : fileName;
			return await picker.PickSaveFileAsync();
		}
#else
		private FileInfo GetFile()
		{
			var dialog = new OpenFileDialog();
			dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
			dialog.Multiselect = false;
			dialog.Filter = "Image Files|*.tif;*.jpg;*.gif;*.png;*.bmp";
			var result = dialog.ShowDialog();
			if (result.HasValue && result.Value)
				return new FileInfo(dialog.FileName);
			return null;
		}

		private FileInfo SaveFile(string fileName)
		{
			var dialog = new SaveFileDialog();
			dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
			dialog.Filter = "Image Files|*.tif;*.jpg;*.gif;*.png;*.bmp";
			dialog.FileName = string.IsNullOrWhiteSpace(fileName) ? "New Image" : fileName;
			var result = dialog.ShowDialog();
			if (result.HasValue && result.Value)
				return new FileInfo(dialog.FileName);
			return null;
		}
#endif

		private async Task SaveEditsAsync()
		{
			if (FeatureTable == null || !FeatureTable.HasEdits)
				return;
			try
			{
				if (FeatureTable is ServiceFeatureTable)
				{
					var serviceTable = (ServiceFeatureTable)FeatureTable;
					VisualStateManager.GoToState(this, "Busy", true);
					var result = await serviceTable.ApplyAttachmentEditsAsync();
				}
			}
			catch (Exception ex)
			{
				OnAttachmentEditFailed(AttachmentOperation.Save, ex);
			}
			VisualStateManager.GoToState(this, "Loaded", true);
		}

		private async Task QueryAttachmentsAsync()
		{
			if (FeatureTable == null || FeatureID == long.MinValue)
				return;
			attachments.Clear();
			AttachmentInfos result = null;
			try
			{
				VisualStateManager.GoToState(this, "Busy", true);
				result = await FeatureTable.QueryAttachmentsAsync(FeatureID);
			}
			catch (Exception ex)
			{
				OnAttachmentEditFailed(AttachmentOperation.Query, ex);
			}
			if (result != null && result.Infos != null)
			{
				foreach (var info in result.Infos)
					attachments.Add(new EditableAttachment(info, (DelegateCommand)Update, (DelegateCommand)Delete, (DelegateCommand)Open));
			}
			VisualStateManager.GoToState(this, "Loaded", true);
		}

		private void RaiseAllCommandsChanged()
		{
			((DelegateCommand)Add).RaiseCanExecuteChanged();
			((DelegateCommand)Update).RaiseCanExecuteChanged();
			((DelegateCommand)Delete).RaiseCanExecuteChanged();
		}

		private async Task CheckEditableStateAsync()
		{
			canAdd = canUpdate = canDelete = false;
			RaiseAllCommandsChanged();
			if (FeatureTable == null || FeatureID == long.MinValue)
				return;
			try
			{
				VisualStateManager.GoToState(this, "Busy", true);
				var feature = await FeatureTable.QueryAsync(FeatureID) as GeodatabaseFeature;
				if (feature != null)
				{
					canAdd = FeatureTable.CanAddAttachment(feature);
					canUpdate = FeatureTable.CanUpdateAttachment(feature);
					canDelete = FeatureTable.CanDeleteAttachment(feature);
					RaiseAllCommandsChanged();
				}
			}
			catch (Exception)
			{
			}
			VisualStateManager.GoToState(this, "Loaded", true);
		}

		private bool CanAdd(object parameter)
		{
			return canAdd;
		}

		private async void OnAdd(object parameter)
		{
			if (FeatureTable == null || FeatureID == long.MinValue )
				return;
#if NETFX_CORE
			var file = await GetFileAsync();
#else
			var file = GetFile();
#endif
			if (file == null)
				return;
			AttachmentResult result = null;
			try
			{
				VisualStateManager.GoToState(this, "Busy", true);
				using (var stream = 
#if NETFX_CORE 
					await file.OpenStreamForReadAsync()
#else
					file.OpenRead()
#endif
					)
				{
					result = await FeatureTable.AddAttachmentAsync(FeatureID, stream, file.Name);
				}
			}
			catch (Exception ex)
			{
				OnAttachmentEditFailed(AttachmentOperation.Add, ex);
			}
			if (result != null)
			{
				if (result.Error != null)
					OnAttachmentEditFailed(AttachmentOperation.Add, result.Error);
				else
				{
					await SaveEditsAsync();
					await QueryAttachmentsAsync();
				}
			}
			VisualStateManager.GoToState(this, "Loaded", true);
		}

		private bool CanUpdate(object parameter)
		{
			return canUpdate;
		}

		private async void OnUpdate(object parameter)
		{
			var info = ((EditableAttachment)parameter).Info;
			if (FeatureTable == null || FeatureID == long.MinValue || info == null)
				return;
#if NETFX_CORE
			var file = await GetFileAsync();
#else
			var file = GetFile();
#endif
			if (file == null)
				return;
			AttachmentResult result = null;
			try
			{
				VisualStateManager.GoToState(this, "Busy", true);
				using (var stream = 
#if NETFX_CORE 
					await file.OpenStreamForReadAsync()
#else
					file.OpenRead()
#endif
					)
				{
					result = await FeatureTable.UpdateAttachmentAsync(FeatureID, info.ID, stream, file.Name);
				}
			}
			catch (Exception ex)
			{
				OnAttachmentEditFailed(AttachmentOperation.Update, ex);
			}
			if (result != null)
			{
				if (result.Error != null)
					OnAttachmentEditFailed(AttachmentOperation.Update, result.Error);
				else
				{
					await SaveEditsAsync();
					await QueryAttachmentsAsync();
				}
			}
			VisualStateManager.GoToState(this, "Loaded", true);
		}

		private bool CanDelete(object parameter)
		{
			return canDelete;
		}

		private async void OnDelete(object parameter)
		{
			var info = ((EditableAttachment)parameter).Info;
			if (FeatureTable == null || FeatureID == long.MinValue || info == null)
				return;
			DeleteAttachmentResult result = null;
			try
			{
					result = await FeatureTable.DeleteAttachmentsAsync(FeatureID, new long[] { info.ID });
			}
			catch (Exception ex)
			{
				OnAttachmentEditFailed(AttachmentOperation.Delete, ex);
			}
			if (result != null && result.Results != null)
			{
				var failedDelete = result.Results.FirstOrDefault(r => r.Error != null);
				if (failedDelete != null)
					OnAttachmentEditFailed(AttachmentOperation.Delete, failedDelete.Error);
				else
				{
					await SaveEditsAsync();
					await QueryAttachmentsAsync();
				}
			}
		}

		private async void OnOpen(object parameter)
		{
			var info = ((EditableAttachment)parameter).Info;
			if (info == null)
				return;
#if NETFX_CORE
			var file = await SaveFileAsync(info.Name);
#else
			var file = SaveFile(info.Name);
#endif
			if (file == null)
				return;
			try
			{
				VisualStateManager.GoToState(this, "Busy", true);
				using (var source = await info.GetDataAsync())
				{
					if (source != Stream.Null)
					{
						using (var destination = 
							
#if NETFX_CORE 
					await file.OpenStreamForWriteAsync()
#else
					file.OpenWrite()
#endif
					)
						{
							await source.CopyToAsync(destination);
						}
					}
				}
			}
			catch (Exception ex)
			{
				OnAttachmentEditFailed(AttachmentOperation.Open, ex);
			}
			VisualStateManager.GoToState(this, "Loaded", true);
		}
	}

	internal class EditableAttachment
	{
		public EditableAttachment(AttachmentInfoItem info, DelegateCommand updateCommand, DelegateCommand deleteCommand, DelegateCommand openCommand)
		{
			Info = info;
			Update = updateCommand;
			Delete = deleteCommand;
			Open = openCommand;
		}

		public AttachmentInfoItem Info { get; private set; }
		
		public ICommand Update { get; private set; }

		public ICommand Delete { get; private set; }

		public ICommand Open { get; private set; }
	}

	internal class DelegateCommand : ICommand
	{
		private Predicate<object> _canExecute; 
		private Action<object> _method;

		public DelegateCommand(Action<object> method)
			: this(method, null)
		{
		}

		public DelegateCommand(Action<object> method, Predicate<object> canExecute)
		{
			_method = method; 
			_canExecute = canExecute;
		}

		public bool CanExecute(object parameter)
		{
			if (_canExecute == null) { return true; }
			return _canExecute(parameter);
		}

		public void Execute(object parameter)
		{
			_method.Invoke(parameter);
		}

		public event EventHandler CanExecuteChanged;

		protected virtual void OnCanExecuteChanged(EventArgs e)
		{
			if (CanExecuteChanged != null)
				CanExecuteChanged(this, e);
		}

		public void RaiseCanExecuteChanged()
		{
			OnCanExecuteChanged(EventArgs.Empty);
		}
	}


	/// <summary>
	/// The operation to attachment <see cref="AttachmentEditor.AttachmentEditorFailed"/>
	/// </summary>
	public enum AttachmentOperation
	{
		/// <summary>
		/// Query attachment files associated to a feature.
		/// </summary>
		Query,
		/// <summary>
		/// Add attachment file to associated to a feature.
		/// </summary>
		Add,
		/// <summary>
		/// Update existing attachment file associated to a feature.
		/// </summary>
		Update,
		/// <summary>
		/// Delete existing attachment file associated to a feature.
		/// </summary>
		Delete,
		/// <summary>
		/// Open attachment file associated to a feature by saving locally.
		/// </summary>
		Open,
		/// <summary>
		/// Save attachment file to associate with a feature.
		/// </summary>
		Save
	}

	/// <summary>
	/// Event argument for <see cref="AttachmentEditor.AttachmentEditorFailed"/>
	/// </summary>
	public class AttachmentEditorFailedEventArgs : EventArgs
	{
		internal AttachmentEditorFailedEventArgs(AttachmentOperation editType, Exception error)
		{
			EditType = editType;
			Error = error;
		}
		/// <summary>
		/// Gets the operation on which attachment editor failed.
		/// </summary>
		public AttachmentOperation EditType { get; private set; }
		/// <summary>
		/// Gets the exception associated with the failed operation.
		/// </summary>
		public Exception Error { get; private set; }
	}
}
#endif