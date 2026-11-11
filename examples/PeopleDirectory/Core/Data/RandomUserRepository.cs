// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PeopleDirectory.Core.Models;

namespace PeopleDirectory.Core.Data;

internal sealed class RandomUserRepository(HttpClient httpClient) : IUserRepository
{
    public async Task<IReadOnlyList<User>> GetUsersAsync(
        int count,
        CancellationToken cancellationToken = default
    )
    {
        count = Math.Clamp(count, 1, 100);
        var response = await httpClient
            .GetFromJsonAsync<ApiResponse>(
                $"?results={count}&noinfo&inc=name,email,phone,cell,gender,dob,location,nat,login",
                cancellationToken
            )
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("The API returned an empty response.");
        return [.. response.Results.Select(Map)];
    }

    private static string FirstTextElement(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "?"
            : System.Globalization.StringInfo.GetNextTextElement(value.Trim());
    }

    private static uint StableHash(Guid value)
    {
        var hash = 2166136261;

        foreach (var item in value.ToByteArray())
            hash = (hash ^ item) * 16777619;

        return hash;
    }

    private static User Map(ApiUser source)
    {
        var id = Guid.TryParse(source.Login.Uuid, out var parsedId) ? parsedId : Guid.NewGuid();

        var fullName = $"{source.Name.First} {source.Name.Last}";

        var initials =
            $"{FirstTextElement(source.Name.First)}" + $"{FirstTextElement(source.Name.Last)}";

        return new User(
            Id: id,
            FullName: fullName,
            Initials: initials.ToUpperInvariant(),
            AvatarColorIndex: (int)(StableHash(id) % 8),
            Email: source.Email,
            Phone: source.Phone,
            Cell: source.Cell,
            Gender: source.Gender,
            DateOfBirth: source.Dob.Date,
            Age: source.Dob.Age,
            Street: $"{source.Location.Street.Number} " + source.Location.Street.Name,
            City: source.Location.City,
            State: source.Location.State,
            Country: source.Location.Country,
            Postcode: source.Location.Postcode,
            Nationality: source.Nat
        );
    }

    private sealed record ApiResponse([property: JsonPropertyName("results")] ApiUser[] Results);

    private sealed record ApiUser(
        NameDto Name,
        string Email,
        string Phone,
        string Cell,
        string Gender,
        DobDto Dob,
        LocationDto Location,
        string Nat,
        LoginDto Login
    );

    private sealed record NameDto(string First, string Last);

    private sealed record DobDto(DateTimeOffset Date, int Age);

    private sealed record LocationDto(
        StreetDto Street,
        string City,
        string State,
        string Country,
        [property: JsonConverter(typeof(PostcodeConverter))] string Postcode
    );

    private sealed record StreetDto(int Number, string Name);

    private sealed record LoginDto(string Uuid);

    private sealed class PostcodeConverter : JsonConverter<string>
    {
        public override string Read(
            ref Utf8JsonReader reader,
            Type type,
            JsonSerializerOptions options
        )
        {
            return reader.TokenType == JsonTokenType.String
                ? reader.GetString() ?? string.Empty
                : reader.GetInt64().ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public override void Write(
            Utf8JsonWriter writer,
            string value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStringValue(value);
        }
    }
}
