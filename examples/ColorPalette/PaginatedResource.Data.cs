/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ColorPalette
{
    public abstract class ResourceData
    {
        [Qt.Ignore]
        [JsonExtensionData]
        public Dictionary<string, JsonElement> Data { get; set; }

        public abstract object this[string role] { get; }

        public virtual void Add(PaginatedResource resPg)
        { }

        public virtual void Update(PaginatedResource resPg)
        { }

        public virtual void Remove(PaginatedResource resPg)
        { }
    }

    public interface IResourcePlugin
    {
        abstract static List<string> Roles { get; }
    }

    public sealed partial class PaginatedResource
    {
        private static List<Func<JsonElement, ResourceData>> Deserializers { get; } = new();
        private static HashSet<string> ResourceRoles { get; } = new();

        private List<ResourceData> Resources { get; set; } = new();

        internal static void RegisterResourceType<T>()
            where T : ResourceData, IResourcePlugin
        {
            foreach (string role in T.Roles)
                ResourceRoles.Add(role);
            Deserializers.Add(json => JsonSerializer.Deserialize<T>(json));
        }

        private void RefreshData(List<JsonElement> json)
        {
            Resources.Clear();
            foreach (var element in json) {
                ResourceData tentative = null;
                foreach (var fnDeserialize in Deserializers) {
                    if (fnDeserialize(element) is not { } candidate)
                        continue;
                    if (tentative == null) {
                        tentative = candidate;
                        continue;
                    }
                    int tentativeOverflow = tentative.Data?.Count ?? 0;
                    int candidateOverflow = candidate.Data?.Count ?? 0;
                    if (tentativeOverflow > candidateOverflow)
                        tentative = candidate;
                }
                if (tentative != null)
                    Resources.Add(tentative);
            }
        }
    }
}
