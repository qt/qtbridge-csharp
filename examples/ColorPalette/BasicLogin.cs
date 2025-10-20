/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.ComponentModel;
using System.Text.Json.Serialization;
using Refit;

namespace ColorPalette
{
    public sealed class BasicLogin : AbstractResource, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string LoginPath { get; set; }
        public string LogoutPath { get; set; }

        private string _User;
        public string User
        {
            get => _User;
            set
            {
                if (_User == value)
                    return;
                _User = value;
                PropertyChanged?.Invoke(this, new(nameof(User)));
                PropertyChanged?.Invoke(this, new(nameof(LoggedIn)));
            }
        }

        public bool LoggedIn => _User != null;

        public void Login(string email, string password)
        {
            if (LoginApi == null) {
                ErrorMsg("Service not ready");
                return;
            }
            try {
                var response = Task.Run(() => LoginApi.LoginAsync(LoginPath, new()
                {
                    Email = email,
                    Password = password,
                }
                )).Result;
                Service.AuthToken = response.Token;
                User = email;
            } catch (Exception e) {
                ErrorMsg($"Login request failed: {e.Message}");
            }
        }

        public void Logout()
        {
            if (LoginApi == null) {
                ErrorMsg("Service not ready");
                return;
            }
            try {
                var response = Task.Run(() => LoginApi.LogoutAsync(LogoutPath, new()
                {
                    Email = User,
                }
                )).Result;
                Service.AuthToken = null;
                User = null;
            } catch (Exception e) {
                ErrorMsg($"Logout request failed: {e.Message}");
            }
        }

        private ILoginApi _LoginApi;
        private ILoginApi LoginApi
        {
            get
            {
                return _LoginApi ??= Service?.Create<ILoginApi>();
            }
        }

        internal override void ResetConnection()
        {
            _LoginApi = null;
        }

        internal interface ILoginApi
        {
            [Post("/{path}")]
            Task<LoginResponse> LoginAsync([AliasAs("path")] string path, [Body] LoginRequest req);

            [Post("/{path}")]
            Task<string> LogoutAsync([AliasAs("path")] string path, [Body] LoginRequest req);
        }

        internal class LoginRequest
        {
            [JsonPropertyName("email")]
            public string Email { get; set; }

            [JsonPropertyName("password")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string Password { get; set; }
        }

        internal class LoginResponse
        {
            [JsonPropertyName("token")]
            public string Token { get; set; }
        }
    }
}
