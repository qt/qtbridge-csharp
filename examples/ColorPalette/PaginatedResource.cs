/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.ComponentModel;

namespace ColorPalette
{
    public sealed partial class PaginatedResource : AbstractResource, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Path { get; set; }

        private int _Page = 1;
        public int Page
        {
            get => _Page;
            set
            {
                if (_Page == value || value < 1)
                    return;
                _Page = value;
                PropertyChanged?.Invoke(this, new(nameof(Page)));
                RefreshCurrentPage();
            }
        }

        public int Pages { get; private set; } = 0;

        public void RefreshCurrentPage()
        {
            if (ResourceApi == null) {
                ErrorMsg("Service not ready");
                return;
            }
            try {
                var response = Task.Run(() => ResourceApi.RefreshAsync(Path, Page)).Result;
                Pages = response.TotalPages;
                _Page = response.CurrentPage;
                RefreshData(response.Data);
                PropertyChanged?.Invoke(this, new(nameof(Pages)));
                PropertyChanged?.Invoke(this, new(nameof(Page)));
                PropertyChanged?.Invoke(this, new(nameof(Data)));
            } catch (Exception e) {
                HandleRequestException(e, apiException =>
                {
                    if (Page != 1) {
                        // A failed refresh. If we weren't on page 1, try that.
                        // Last resource on currentPage might have been deleted, causing a failure
                        Page = 1;
                    } else {
                        // Refresh failed and we're already on page 1 => clear data
                        ErrorMsg($"Refresh failed: {apiException.Message}");
                        Pages = 0;
                        PropertyChanged?.Invoke(this, new(nameof(Pages)));
                        if (Resources.Any()) {
                            Resources.Clear();
                            PropertyChanged?.Invoke(this, new(nameof(Data)));
                        }
                    }
                });
            }
        }

        public void Add(ResourceData resource)
        {
            if (ResourceApi == null) {
                ErrorMsg("Service not ready");
                return;
            }
            try {
                _ = Task.Run(() => ResourceApi
                    .AddAsync(Path, resource))
                    .Result;
                RefreshCurrentPage();
            } catch (Exception e) {
                HandleRequestException(e, apiException =>
                {
                    ErrorMsg($"Add failed: {apiException.Message}");
                });
            }
        }

        public void Update(int resourceId, ResourceData resource)
        {
            if (ResourceApi == null) {
                ErrorMsg("Service not ready");
                return;
            }
            try {
                _ = Task.Run(() => ResourceApi
                    .UpdateAsync(Path, resourceId, resource))
                    .Result;
                RefreshCurrentPage();
            } catch (Exception e) {
                HandleRequestException(e, apiException =>
                {
                    ErrorMsg($"Update failed: {apiException.Message}");
                });
            }
        }

        public void Remove(int resourceId)
        {
            if (ResourceApi == null) {
                ErrorMsg("Service not ready");
                return;
            }
            try {
                _ = Task.Run(() => ResourceApi
                    .RemoveAsync(Path, resourceId))
                    .Result;
                RefreshCurrentPage();
            } catch (Exception e) {
                HandleRequestException(e, apiException =>
                {
                    ErrorMsg($"Remove failed: {apiException.Message}");
                });
            }
        }
    }
}
