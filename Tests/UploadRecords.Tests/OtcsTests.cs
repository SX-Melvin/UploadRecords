using UploadRecords.Services;
using Xunit;

namespace UploadRecords.Tests;

[Collection("Workspace")]
public sealed class OtcsTests(TestWorkspace workspace)
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task EmptyResponsesAreRejected(string response)
    {
        using var handler = new FakeOtcsHandler { Respond = _ => response };
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.CreateClient().GetTicket());
        Assert.Contains("empty response", error.Message);
    }

    [Fact]
    public async Task AuthenticationSendsCredentialsAndReturnsTicket()
    {
        using var handler = new FakeOtcsHandler();
        var client = handler.CreateClient();
        Assert.Equal("test-ticket", (await client.GetTicket()).Ticket);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("POST", request.Method);
        Assert.Contains("username=test-user", request.Body);
        Assert.Equal("https://otcs.invalid", client.HostUrl);
        Assert.Equal("https://otcs.invalid", new Otcs("unused", "unused", "https://otcs.invalid").HostUrl);
    }

    [Fact]
    public async Task FileAndPermissionRequestsUseExpectedRoutesAndTicket()
    {
        using var handler = new FakeOtcsHandler();
        var client = handler.CreateClient();
        var file = workspace.FileRecord();
        Assert.Equal(42, (await client.CreateFile(file.Path, 7, "ticket")).Id);
        Assert.Contains("record.pdf", handler.Requests[0].Body);
        Assert.Null((await client.DeleteNodePermission(42, 9, "ticket")).Error);
        Assert.Null((await client.UpdateNodeOwnerPermission(42, ["see"], "ticket")).Error);
        Assert.Null((await client.DeleteNodePublicPermission(42, "ticket")).Error);
        Assert.Null((await client.DeleteNodeOwnerGroupPermission(42, "ticket")).Error);
        await client.UpdateNodePermissionBulk(42, [new() { RightID = 99, Permissions = ["see"] }], "ticket");
        Assert.All(handler.Requests, request => Assert.Equal("ticket", request.Ticket));
        Assert.Contains(handler.Requests, r => r.Method == "DELETE" && r.Path.EndsWith("/custom/9", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, r => r.Method == "PUT" && r.Path.EndsWith("/owner", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, r => r.Body.Contains("99"));
    }

    [Fact]
    public async Task ExistingFolderIsFoundWithoutResettingItsPermissions()
    {
        using var handler = new FakeOtcsHandler
        {
            Respond = r => r.Method == "POST" ? "{\"error\":\"already exists\"}" : "{\"results\":[{\"data\":{\"properties\":{\"id\":123,\"name\":\"existing\"}}}]}"
        };
        var response = await handler.CreateClient().CreateFolder("existing", 7, "ticket", [], []);
        Assert.Equal(123, response.Id);
        Assert.Null(response.Error);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("where_name=existing", handler.Requests[1].Path);
    }

    [Theory]
    [InlineData("{}", true)]
    [InlineData("{\"error\":\"update failed\"}", false)]
    public async Task ExistingCategoryIsUpdatedAndUpdateFailureIsRetained(string updateResponse, bool expectedSuccess)
    {
        using var handler = new FakeOtcsHandler { Respond = r => r.Method == "POST" ? "{\"error\":\"already exists\"}" : updateResponse };
        var response = await handler.CreateClient().ApplyCategoryOnNode(42, "{}", 8, "ticket");
        Assert.Equal(expectedSuccess, response.Error == null);
        Assert.Equal("/v1/nodes/42/categories/8", handler.Requests[1].Path);
        Assert.Equal("PUT", handler.Requests[1].Method);
    }

    [Fact]
    public async Task JsonNullDoesNotInventAnAuthenticationTicket()
    {
        using var handler = new FakeOtcsHandler { Respond = _ => "null" };
        var response = await handler.CreateClient().GetTicket();
        Assert.Null(response.Ticket);
    }
}
