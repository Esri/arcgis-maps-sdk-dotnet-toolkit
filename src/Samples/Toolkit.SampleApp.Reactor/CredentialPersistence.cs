using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Security;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Windows.Services.Maps.LocalSearch;

namespace Toolkit.SampleApp.Reactor
{
    internal class SecureStorageCredentialPersistence : CredentialPersistence, ILoadable
    {
        ConcurrentBag<Credential> _credentials = new ConcurrentBag<Credential>();

        public event EventHandler<EventArgs>? Loaded;
        public event EventHandler<LoadStatusEventArgs>? LoadStatusChanged;

        public Exception? LoadError { get; private set; }

        public LoadStatus LoadStatus { get; private set; }

        public SecureStorageCredentialPersistence()
        {
        }

        private Task? _loadTask;

        public Task LoadAsync()
        {
            return _loadTask ??= LoadAsync_Impl();
        }
        private async Task LoadAsync_Impl()
        {
            LoadStatus = LoadStatus.Loading;
            try
            {
                await LoadCredentialsAsync();
                LoadStatus = LoadStatus.Loaded;
                Loaded?.Invoke(this, EventArgs.Empty);
            }
            catch(System.Exception ex)
            {
                LoadStatus = LoadStatus.FailedToLoad;
                LoadError = ex;
            }
        }
        protected override void Add(Credential credential)
        {
            _credentials.Add(credential);
            SaveCredentials();
        }
        protected override void Remove(Credential credential)
        {
            _credentials.TryTake(out _);
            SaveCredentials();
        }
        protected override IEnumerable<Credential> GetCredentials()
        {
            return _credentials;
        }
        protected override void Update(Credential credential)
        {
            SaveCredentials();
        }
        protected override void Clear()
        {
            _credentials.Clear();
            SaveCredentials();
        }
        private async Task LoadCredentialsAsync()
        {
            _credentials.Clear();
            string? json = await SecureStorage.GetAsync("credentials");
            if (json is null) return;
            foreach (var cred in json.Split('\n'))
            {
                try
                {
                    _credentials.Add(Credential.FromJson(cred));
                }
                catch { }
            }
        }

        private void SaveCredentials()
        {
            // TODO: Make thread safe and avoid saving if there are multiple updates in a short time
            var json = string.Join("\n", _credentials.Select(c => c.ToJson()));
            _ = SecureStorage.SetAsync("credentials", json);
        }

        public void CancelLoad()
        {
        }


        public Task RetryLoadAsync()
        {
            LoadError = null;
            _loadTask = null;
            return LoadAsync();
        }
    }
}
