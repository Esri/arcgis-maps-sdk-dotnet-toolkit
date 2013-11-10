// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Net;
using System.IO;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml;
using Esri.ArcGISRuntime.Security;

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
	/// <summary>
	/// The SignInDialog Control challenges the user for a credential
	/// when trying to access secured ArcGIS services.
	/// The SignInDialog Control can manage Network, Certificate or Token credential.
	/// </summary>
	/// <remarks>
	/// This control is designed to work with the <see cref="IdentityManager" />.
	/// The IdentityManager can be actived with code like:
	/// ESRI.ArcGIS.Client.IdentityManager.Current.ChallengeMethod = ESRI.ArcGIS.Client.Toolkit.SignInDialog.DoSignIn;
	/// or 
	/// ESRI.ArcGIS.Client.IdentityManager.Current.ChallengeMethodEx = ESRI.ArcGIS.Client.Toolkit.SignInDialog.DoSignInEx;
	/// In this case, the SignInDialog is created and activated in a child window.
	/// It's also possible to put the SignInDialog in the Visual Tree and to write your own challenge method activating this SignInDialog.
	/// </remarks>
	[TemplatePart(Name = "RichTextBoxMessage", Type = typeof(RichTextBox))]
	[TemplatePart(Name = "RichTextBoxErrorMessage", Type = typeof(RichTextBox))]
	public class SignInDialog : Control, INotifyPropertyChanged
	{

		#region Constructors
		private RichTextBox _rtbMessage;
		private RichTextBox _rtbErrorMessage;
		private string _rtbMessageInitialXaml;
		private string _rtbErrorMessageInitialXaml;
		private long _requestID; // flag allowing the reuse of the same dialog after cancelling a request

		/// <summary>
		/// Initializes a new instance of the <see cref="SignInDialog"/> control.
		/// </summary>
		public SignInDialog()
		{
			GenerateCredentialCommand = new GenerateCredentialCommandImpl(this);
			CancelCommand = new CancelCommandImpl(this);
			DataContext = this;
		}

		/// <summary>
		/// Static initialization for the <see cref="SignInDialog"/> control.
		/// </summary>
		static SignInDialog()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(SignInDialog),
				new FrameworkPropertyMetadata(typeof(SignInDialog)));
		}
		#endregion

		#region Override OnApplyTemplate
		/// <summary>
		/// When overridden in a derived class, is invoked whenever application
		/// code or internal processes (such as a rebuilding layout pass) call 
		/// <see cref="T:System.Windows.Controls.Control.ApplyTemplate"/>. In
		/// simplest terms, this means the method is called just before a UI
		/// element displays in an application.
		/// </summary>t
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			_rtbMessage = GetTemplateChild("RichTextBoxMessage") as RichTextBox;
			_rtbErrorMessage = GetTemplateChild("RichTextBoxErrorMessage") as RichTextBox;
			if (_rtbMessage != null)
				_rtbMessageInitialXaml = XamlWriter.Save(_rtbMessage.Document);
			if (_rtbErrorMessage != null)
				_rtbErrorMessageInitialXaml = XamlWriter.Save(_rtbErrorMessage.Document);
			SetRichTextBoxMessage();
			SetRichTextBoxErrorMessage();

			// Set focus 
			var elt = GetTemplateChild("FirstFocus") as UIElement;
 			if (elt != null)
				elt.Focus();
		}

		#endregion

		#region Public property CredentialRequestInfo
		private IdentityManager.CredentialRequestInfo _credentialRequestInfo;
		/// <summary>
		/// Gets the information about the ArcGIS service that needs a credential for getting access to. This property is set by calling <see cref="GetCredentialAsync"/>.
		/// </summary>
		/// <value>
		/// The credential request info.
		/// </value>
		public IdentityManager.CredentialRequestInfo CredentialRequestInfo
		{
			get { return _credentialRequestInfo; }
			private set
			{
				const string propertyName = "CredentialRequestInfo";
				if (_credentialRequestInfo != value)
				{
					_credentialRequestInfo = value;

					SetRichTextBoxMessage();
					SetRichTextBoxErrorMessage();
					OnPropertyChanged(propertyName);
					UpdateCanGenerate();
				}
			}
		}
		#endregion

		/// <summary>
		/// Gets a credential asynchronously.
		/// </summary>
		/// <param name="credentialRequestInfo">The credential request info.</param>
		/// <returns></returns>
		/// <exception cref="System.ArgumentNullException">credentialRequestInfo</exception>
		public async Task<IdentityManager.Credential> GetCredentialAsync(IdentityManager.CredentialRequestInfo credentialRequestInfo)
		{
			if (credentialRequestInfo == null)
				throw new ArgumentNullException("credentialRequestInfo");
			Cancel(); // cancel previous task
			CredentialRequestInfo = credentialRequestInfo;
			Tcs = new TaskCompletionSource<IdentityManager.Credential>();

			using (credentialRequestInfo.CancellationToken.Register(Cancel, true))
			{
				return await Tcs.Task;
			}
		}

		#region Public property UserName
		private string _userName = string.Empty;
		/// <summary>
		/// Gets or sets the name of the user.
		/// </summary>
		/// <value>
		/// The name of the user.
		/// </value>
		public string UserName
		{
			get { return _userName; }
			set
			{
				const string propertyName = "UserName";
				if (_userName != value)
				{
					_userName = value;

					OnPropertyChanged(propertyName);
					UpdateCanGenerate();
					if (string.IsNullOrEmpty(value))
						throw new ArgumentNullException();
				}
			}
		}
		#endregion

		#region Public property Password
		private string _password = string.Empty;
		/// <summary>
		/// Gets or sets the password uised to get the token.
		/// </summary>
		/// <value>
		/// The password.
		/// </value>
		public string Password
		{
			get { return _password; }
			set
			{
				const string propertyName = "Password";
				if (_password != value)
				{
					_password = value;
					OnPropertyChanged(propertyName);
					UpdateCanGenerate();

					if (string.IsNullOrEmpty(value))
						throw new ArgumentNullException();
				}
			}
		}
		#endregion

		#region Public Read-Only property IsBusy
		private bool _isBusy;
		/// <summary>
		/// Gets a value indicating whether this instance is busy getting a token.
		/// </summary>
		/// <value>
		///   <c>true</c> if this instance is busy; otherwise, <c>false</c>.
		/// </value>
		public bool IsBusy
		{
			get { return _isBusy; }
			private set
			{
				if (_isBusy != value)
				{
					_isBusy = value;
					OnPropertyChanged("IsBusy");
					UpdateCanGenerate();
				}
			}
		}
		#endregion

		#region Public Read-Only Property ErrorMessage
		private string _errorMessage;

		/// <summary>
		/// Gets the error that occured during the latest token generation.
		/// </summary>
		/// <value>
		/// The error message.
		/// </value>
		public string ErrorMessage
		{
			get { return _errorMessage; }
			private set
			{
				if (_errorMessage != value)
				{
					_errorMessage = value;
					SetRichTextBoxErrorMessage();
					OnPropertyChanged("ErrorMessage");
				}
			}
		}
		#endregion

		#region Dependency Property Title
		/// <summary>
		/// Gets or sets the title that can be displayed by the SignInDialog or by the container of the SignInDialog.
		/// </summary>
		/// <remarks>The default SignInDialog template doesn't display the Title.</remarks>
		/// <value>
		/// The title.
		/// </value>
		public string Title
		{
			// This is a DP so the value can be initialized in XAML allowing easy L10N
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="Title"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty TitleProperty =
			DependencyProperty.Register("Title", typeof(string), typeof(SignInDialog), null);

		#endregion

		/// <summary>
		/// Gets the generate credential command allowing to generate a credential asynchronously.
		/// </summary>
		/// <remarks>This command is generally executed on the 'OK' button.</remarks>
		public ICommand GenerateCredentialCommand { get; private set; }

		/// <summary>
		/// Gets the cancel command allowing to cancel the ongoing token request.
		/// </summary>
		public ICommand CancelCommand { get; private set; }

		#region DoSignIn static challenge method

		/// <summary>
		/// Static challenge method leaveraging the SignInDialog in a child window.
		/// </summary>
		public static Task<IdentityManager.Credential> DoSignIn(IdentityManager.CredentialRequestInfo credentialRequestInfo)
		{
			Dispatcher d = Application.Current == null ? null : Application.Current.Dispatcher;
			Task<IdentityManager.Credential> doSignInTask;

			if (d != null && !d.CheckAccess())
			{
				//Ensure we are showing up the SignInDialog on the UI thread
				var tcs = new TaskCompletionSource<IdentityManager.Credential>();
				d.BeginInvoke((Action)(async () =>
				{
					try
					{
						IdentityManager.Credential crd = await DoSignInInUIThread(credentialRequestInfo);
						tcs.TrySetResult(crd);
					}
					catch (Exception error)
					{
						tcs.TrySetException(error);
					}
				}));
				doSignInTask = tcs.Task;
			}
			else
				doSignInTask = DoSignInInUIThread(credentialRequestInfo);

			return doSignInTask.ContinueWith(t =>
			{
				// Flatten the exceptions
				if (t.Exception != null)
					throw t.Exception.Flatten().InnerException;
				return t.Result;
			});
		}

		private static Task<IdentityManager.Credential> DoSignInInUIThread(IdentityManager.CredentialRequestInfo credentialRequestInfo)
		{
			// Create the ChildWindow that contains the SignInDialog
			var childWindow = new Window
			{
				ShowInTaskbar = false,
				WindowStartupLocation = WindowStartupLocation.CenterOwner,
				WindowStyle = WindowStyle.ToolWindow,
				SizeToContent = SizeToContent.WidthAndHeight,
				ResizeMode = ResizeMode.NoResize,
				WindowState = WindowState.Normal
			};

			if (Application.Current != null && Application.Current.MainWindow != null)
			{
				try
				{
					childWindow.Owner = Application.Current.MainWindow;
				}
				catch
				{
					// May fire an exception when used inside an excel or powerpoint addins
				}
			}

			DependencyProperty titleProperty = Window.TitleProperty;

			// Create the SignInDialog with the parameters given as arguments
			var signInDialog = new SignInDialog
			{
				Width = 300
			};

			childWindow.Content = signInDialog;

			// Bind the Title so the ChildWindow Title is the SignInDialog title (that will be initialized later)
			var binding = new Binding("Title") { Source = signInDialog };
			childWindow.SetBinding(titleProperty, binding);
			childWindow.Closed += (s, e) => signInDialog.Cancel(); // be sure the SignInDialog is deactivated when closing the childwindow using the X


			// initialize the task that gets the credential and then close the window
			var ts = TaskScheduler.FromCurrentSynchronizationContext();
			var doSignInTask = signInDialog.GetCredentialAsync(credentialRequestInfo).ContinueWith(task =>
			{
				childWindow.Close();
				return task.Result;
			}, ts); 

			// Show the window
			childWindow.ShowDialog();

			return doSignInTask;
		}

		#endregion

		#region NetworkCredential Generation

		private void GenerateNetworkCredential()
		{
			if (IsReady)
			{
				var credential = new IdentityManager.Credential { Credentials = new NetworkCredential(UserName, Password) };
				Debug.Assert(Tcs != null); // due to test on IsReady
				Tcs.TrySetResult(credential);
				Tcs = null;
				ErrorMessage = null;
			}
		}
		#endregion

		#region Token Generation

		private async void GenerateToken()
		{
			if (!IsReady)
				return;

			IsBusy = true;
			ErrorMessage = null;

			long requestID = ++_requestID;
			Exception error = null;
			IdentityManager.Credential credential = null;
			try
			{
				credential = await IdentityManager.Current.GenerateCredentialAsync(_credentialRequestInfo.ServiceUri, UserName, Password, _credentialRequestInfo.GenerateTokenOptions);
			}
			catch (Exception e)
			{
				error = e;
			}
			if (requestID != _requestID || Tcs == null)
				return; // No more the current request

			IsBusy = false;

			if (error == null)
			{
				Tcs.TrySetResult(credential);
				Tcs = null;
			}
			else
			{
				// Display the error message and let the user try again
				string message = error.Message;
				if (string.IsNullOrEmpty(message) && error.InnerException != null)
					message = error.InnerException.Message;
				ErrorMessage = message;
			}
		}
		#endregion

		#region INotifyPropertyChanged Members

		/// <summary>
		/// Occurs when a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		internal void OnPropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler propertyChanged = PropertyChanged;
			if (propertyChanged != null)
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion

		#region Private helper methods to generate message in RichTextBox

		private void SetRichTextBoxMessage()
		{
			MakeReplacements(_rtbMessage, _rtbMessageInitialXaml);
		}

		private void SetRichTextBoxErrorMessage()
		{
			MakeReplacements(_rtbErrorMessage, _rtbErrorMessageInitialXaml);
		}

		private void MakeReplacements(RichTextBox richTextBox, string xaml)
		{
			if (richTextBox == null || string.IsNullOrEmpty(xaml) || CredentialRequestInfo == null)
				return;

			string url = _credentialRequestInfo.ServiceUri;

			if (!string.IsNullOrEmpty(url))
			{
				string resourceName = GetResourceName(url);
				IdentityManager.ServerInfo serverInfo = IdentityManager.Current.FindServerInfo(url);
				string server = serverInfo == null ? Regex.Match(url, "http.?//[^/]*").ToString() : serverInfo.ServerUri;
				xaml = xaml.Replace("$RESOURCENAME", XamlEncode(resourceName));
				xaml = xaml.Replace("$URL", XamlEncode(url));
				xaml = xaml.Replace("$SERVER", XamlEncode(server));
			}
			xaml = xaml.Replace("$AUTHENTICATIONTYPE", _credentialRequestInfo.AuthenticationType.ToString());
			xaml = xaml.Replace("$ERRORMESSAGE", XamlEncode(ErrorMessage));

			string previousError = _credentialRequestInfo.Response != null ? _credentialRequestInfo.Response.ReasonPhrase : null;
			xaml = xaml.Replace("$PREVIOUSERROR", XamlEncode(previousError));
			var stringReader = new StringReader(xaml);
			XmlReader xmlReader = XmlReader.Create(stringReader);
			richTextBox.Document = XamlReader.Load(xmlReader) as FlowDocument;
		}

		private static string XamlEncode(string inputStr)
		{
			return string.IsNullOrEmpty(inputStr)
				       ? inputStr
				       : inputStr.Replace("&", "&amp;")
				                 .Replace("<", "&lt;")
				                 .Replace(">", "&gt;")
				                 .Replace("\"", "&quot;")
				                 .Replace("'", "&apos;");
		}

		private static string GetResourceName(string url)
		{
			if (url.IndexOf("/rest/services", StringComparison.OrdinalIgnoreCase) > 0)
				return GetSuffix(url);

			url = Regex.Replace(url, "http.?//[^/]*", "");
			url = Regex.Replace(url, ".*/items/([^/]+).*", "$1");
			return url;
		}

		private static string GetSuffix(string url)
		{
			url = Regex.Replace(url, "http.+/rest/services/?", "", RegexOptions.IgnoreCase);
			url = Regex.Replace(url, "(/(MapServer|GeocodeServer|GPServer|GeometryServer|ImageServer|NAServer|FeatureServer|GeoDataServer|GlobeServer|MobileServer)).*", "$1", RegexOptions.IgnoreCase);
			return url;
		}
		#endregion

		// Private properties
		private TaskCompletionSource<IdentityManager.Credential> _tcs;
		private TaskCompletionSource<IdentityManager.Credential> Tcs
		{
			get { return _tcs; }
			set
			{
				if (_tcs != value)
				{
					_tcs = value;
					UpdateCanCancel();
					UpdateCanGenerate();
				}
			}
		}

		// Private methods
		private bool IsReady
		{
			get { return !string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password) && !IsBusy && CredentialRequestInfo != null && Tcs != null; }
		}

		private void UpdateCanGenerate()
		{
			((GenerateCredentialCommandImpl)GenerateCredentialCommand).OnCanExecuteChanged();
		}

		private void UpdateCanCancel()
		{
			((CancelCommandImpl)CancelCommand).OnCanExecuteChanged();
		}

		private void Cancel()
		{
			IsBusy = false;
			if (Tcs != null)
			{
				Tcs.TrySetCanceled();
				Tcs = null;
			}
		}

		#region Commands implementation
		private class GenerateCredentialCommandImpl : ICommand
		{
			private readonly SignInDialog _signInDialog;

			internal GenerateCredentialCommandImpl(SignInDialog signInDialog)
			{
				_signInDialog = signInDialog;
			}

			public bool CanExecute(object parameter)
			{
				return _signInDialog.IsReady;
			}

			public event EventHandler CanExecuteChanged;
			internal void OnCanExecuteChanged()
			{
				if (CanExecuteChanged != null)
					CanExecuteChanged(this, EventArgs.Empty);
			}

			public void Execute(object parameter)
			{
				if (_signInDialog._credentialRequestInfo != null)
				{
					if (_signInDialog._credentialRequestInfo.AuthenticationType == IdentityManager.AuthenticationType.Token)
						_signInDialog.GenerateToken();
					else
						_signInDialog.GenerateNetworkCredential();
				}
			}
		}

		private class CancelCommandImpl : ICommand
		{
			private readonly SignInDialog _signInDialog;

			internal CancelCommandImpl(SignInDialog signInDialog)
			{
				_signInDialog = signInDialog;
			}

			public bool CanExecute(object parameter)
			{
				return _signInDialog.Tcs != null;
			}

			public event EventHandler CanExecuteChanged;
			internal void OnCanExecuteChanged()
			{
				if (CanExecuteChanged != null)
					CanExecuteChanged(this, EventArgs.Empty);
			}

			public void Execute(object parameter)
			{
				_signInDialog.Cancel();
			}
		}
		#endregion

	}

	/// <summary>
 /// *FOR INTERNAL USE ONLY* Helper to execute a command when user types 'Enter' and to update the binding source when the text changes.
	/// </summary>
 /// <exclude/>
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal class TextInputManager
	{
		/// <summary>
		/// Gets the command.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <returns></returns>
		public static ICommand GetEnterCommand(DependencyObject obj)
		{
			return (ICommand)obj.GetValue(EnterCommandProperty);
		}

		/// <summary>
		/// Sets the command.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <param name="value">The value.</param>
		public static void SetEnterCommand(DependencyObject obj, ICommand value)
		{
			obj.SetValue(EnterCommandProperty, value);
		}

		/// <summary>
		/// Identifies the Command attached property.
		/// </summary>
		public static readonly DependencyProperty EnterCommandProperty =
			DependencyProperty.RegisterAttached("EnterCommand", typeof(ICommand), typeof(TextInputManager), new PropertyMetadata(OnEnterCommandChanged));

		private static void OnEnterCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is TextBox)
			{
				var textBox = d as TextBox;
				if (e.OldValue != null)
					textBox.KeyUp -= KeyUpHandler;
				if (e.NewValue != null)
					textBox.KeyUp += KeyUpHandler;
			}
			else if (d is PasswordBox)
			{
				var textBox = d as PasswordBox;
				if (e.OldValue != null)
					textBox.KeyUp -= KeyUpHandler;
				if (e.NewValue != null)
					textBox.KeyUp += KeyUpHandler;
			}
		}

		static void KeyUpHandler(object sender, KeyEventArgs e)
		{
			if (!(sender is DependencyObject))
				return;

			if (e.Key == Key.Enter)
			{
				ICommand command = GetEnterCommand((DependencyObject)sender);

				if (command != null && command.CanExecute(null))
				{
					command.Execute(null);
					e.Handled = true;
				}
			}
			else
			{
				var passwordBox = sender as PasswordBox;
				if (passwordBox != null)
				{
					SetPasswordText(passwordBox, passwordBox.Password);
				}
			}
		}


		/// <summary>
		/// Identifies the PasswordText attached property.
		/// In WPF this a workaround for the Password property that is not a DP.
		/// </summary>
		public static readonly DependencyProperty PasswordTextProperty =
			DependencyProperty.RegisterAttached("PasswordText", typeof(string), typeof(TextInputManager), new PropertyMetadata(OnPasswordTextChanged));

		private static void OnPasswordTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{

			if (d is PasswordBox)
			{
				var textBox = d as PasswordBox;
				var value = (string) e.NewValue;
				if (textBox.Password != value)
					textBox.Password = value;
			}
		}

		/// <summary>
		/// Gets the password text.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <returns></returns>
		public static string GetPasswordText(DependencyObject obj)
		{
			return (string)obj.GetValue(PasswordTextProperty);
		}

		/// <summary>
		/// Sets the password text.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <param name="value">The value.</param>
		public static void SetPasswordText(DependencyObject obj, string value)
		{
			obj.SetValue(PasswordTextProperty, value);
		}
	}
} 

