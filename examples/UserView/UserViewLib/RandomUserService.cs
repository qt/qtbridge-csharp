/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using Refit;
using System.Text.Json.Serialization;

namespace UserViewLib
{
    public class RandomUserService
    {
        [JsonPropertyName("results")]
        public List<User> Users { get; set; }

        public interface IUserService
        {
            [Get("/api/?dataType=json&inc=name,email,picture")]
            Task<RandomUserService> FetchAsync([AliasAs("results")] int count);
        }

        public static IUserService Service { get; }
            = RestService.For<IUserService>("https://randomuser.me/");

        public static async Task<List<User>> FetchAsync(int count)
        {
            if (count <= 0)
                return [];
            var result = await Service.FetchAsync(count);
            return result.Users;
        }

        public static List<User> Fetch(int count)
        {
            if (count <= 0)
                return [];
            var task = Task.Run(async () => await FetchAsync(count));
            return task.GetAwaiter().GetResult();
        }

        public static User Fetch() => Fetch(1).FirstOrDefault();
    }
}
