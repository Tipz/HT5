using System.Net;
using System.Net.Http.Json;
using Together.Contracts;

namespace Together.Api.Tests;

public sealed class TripsApiTests(TogetherApiFactory factory) : IClassFixture<TogetherApiFactory>
{
    [Fact]
    public async Task Trips_RequireAuthentication()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(ApiRoutes.Trips);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task User_CanCreateReadAndDetectConflict_WhileOtherUserCannotRead()
    {
        using var owner = factory.CreateClient();
        await RegisterAndLogin(owner, $"owner-{Guid.NewGuid():N}@example.test");
        var request = new TripWriteRequest
        {
            Name = "Летняя поездка",
            StartDate = new DateOnly(2027, 7, 1),
            EndDate = new DateOnly(2027, 7, 8),
            Adults = 2,
            ChildAges = [4]
        };
        using var createdResponse = await owner.PostAsJsonAsync(ApiRoutes.Trips, request);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<TripResponse>();
        Assert.NotNull(created);
        Assert.Equal(1, created.Revision);

        using var conflictResponse = await owner.PutAsJsonAsync($"{ApiRoutes.Trips}/{created.Id}", request);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);

        using var other = factory.CreateClient();
        await RegisterAndLogin(other, $"other-{Guid.NewGuid():N}@example.test");
        using var hiddenResponse = await other.GetAsync($"{ApiRoutes.Trips}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, hiddenResponse.StatusCode);
    }

    [Fact]
    public async Task InvalidTrip_ReturnsValidationProblem()
    {
        using var client = factory.CreateClient();
        await RegisterAndLogin(client, $"validation-{Guid.NewGuid():N}@example.test");
        using var response = await client.PostAsJsonAsync(ApiRoutes.Trips, new TripWriteRequest());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task RegisterAndLogin(HttpClient client, string email)
    {
        const string password = "Strongpass123";
        using var register = await client.PostAsJsonAsync($"{ApiRoutes.Auth}/register", new { email, password });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        using var login = await client.PostAsJsonAsync($"{ApiRoutes.Auth}/login?useCookies=true", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }
}
