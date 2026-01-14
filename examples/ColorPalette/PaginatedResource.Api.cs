// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using System.Text.Json;
using System.Text.Json.Serialization;
using Refit;

namespace ColorPalette
{
    public sealed partial class PaginatedResource
    {
        private IResourceApi _ResourceApi;
        private IResourceApi ResourceApi
        {
            get
            {
                _ResourceApi ??= Service?.Create<IResourceApi>();
                return _ResourceApi;
            }
        }

        internal override void ResetConnection()
        {
            _ResourceApi = null;
        }

        internal interface IResourceApi
        {
            [Get("/{path}?page={page}")]
            Task<ResourcePage> RefreshAsync(
                [AliasAs("path")] string path, [AliasAs("page")] int page);

            [Post("/{path}")]
            Task<JsonElement> AddAsync(
                [AliasAs("path")] string path, [Body] ResourceData body);

            [Put("/{path}/{id}")]
            Task<JsonElement> UpdateAsync(
                [AliasAs("path")] string path, [AliasAs("id")] int id, [Body] ResourceData body);

            [Delete("/{path}/{id}")]
            Task<string> RemoveAsync(
                [AliasAs("path")] string path, [AliasAs("id")] int id);
        }

        internal class ResourcePage
        {
            [JsonPropertyName("page")]
            public int CurrentPage { get; set; }

            [JsonPropertyName("per_page")]
            public int ResourcesPerPage { get; set; }

            [JsonPropertyName("total_pages")]
            public int TotalPages { get; set; }

            [JsonPropertyName("total")]
            public int TotalResources { get; set; }

            [JsonPropertyName("data")]
            public List<JsonElement> Data { get; set; }
        }
    }
}
