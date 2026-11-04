using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace SimuLearn.Tests;

/// <summary>
/// Boots the real API against a throwaway SQLite database with the demo seed, so integration tests
/// exercise the same middleware, authentication and persistence pipeline as production.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string SeedPassword = "SimuLearn2026!";

    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"simulearn-tests-{Guid.CreateVersion7():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting("Database:Provider", "sqlite");
        builder.UseSetting("Database:Migrate", "true");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_databasePath}");
        builder.UseSetting("Jwt:Issuer", "simulearn");
        builder.UseSetting("Jwt:Audience", "simulearn-client");
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-with-enough-entropy-0123456789");
        builder.UseSetting("Seed:Enabled", "true");
        builder.UseSetting("Seed:Password", SeedPassword);
        builder.UseSetting("Ai:Enabled", "false");
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email, CancellationToken cancellationToken)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = SeedPassword },
            cancellationToken);

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var token = document.RootElement.GetProperty("accessToken").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        // Pooled SQLite handles keep the file open; release them before deleting.
        SqliteConnection.ClearAllPools();

        foreach (var suffix in new[] { string.Empty, "-shm", "-wal" })
        {
            var path = _databasePath + suffix;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}

internal static class JsonHelpers
{
    public static async Task<JsonElement> ReadJsonAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.Clone();
    }

    public static string? String(this JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    public static int? Int(this JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.TryGetInt32(out var number) ? number : null;

    public static double? Double(this JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.TryGetDouble(out var number) ? number : null;
}

public sealed class LearningLoopTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Anonymous_requests_are_rejected()
    {
        var client = factory.CreateClient();

        var catalog = await client.GetAsync("/api/v1/catalog", Ct);
        var session = await client.GetAsync("/api/v1/sessions/00000000-0000-0000-0000-000000000001", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, catalog.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, session.StatusCode);
    }

    [Fact]
    public async Task Health_endpoint_reports_a_reachable_database()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/health", Ct);
        var payload = await response.ReadJsonAsync(Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", payload.String("status"));
    }

    [Fact]
    public async Task Catalog_exposes_only_the_allowlisted_models()
    {
        var client = await factory.CreateAuthenticatedClientAsync("student@simulearn.demo", Ct);

        var payload = await (await client.GetAsync("/api/v1/catalog", Ct)).ReadJsonAsync(Ct);
        var models = payload.GetProperty("simulations")
            .EnumerateArray()
            .Select(model => model.String("modelKey"))
            .ToArray();

        Assert.Equal(2, models.Length);
        Assert.Contains("simple-dc-circuit-v1", models);
        Assert.Contains("inclined-plane-v1", models);
    }

    [Fact]
    public async Task Natural_language_request_produces_a_session_ready_scene()
    {
        var client = await factory.CreateAuthenticatedClientAsync("student@simulearn.demo", Ct);

        var response = await client.PostAsJsonAsync("/api/v1/simulation-requests", new
        {
            prompt = "How does doubling the resistance change the current in a simple circuit?",
            subject = "Physics",
            level = "Secondary",
        }, Ct);

        var payload = await response.ReadJsonAsync(Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Completed", payload.String("status"));
        Assert.Equal("simple-dc-circuit-v1", payload.GetProperty("interpretation").String("modelKey"));
        Assert.True(payload.GetProperty("interpretation").TryGetProperty("usedFallback", out _));
        Assert.Equal(2, payload.GetProperty("scene").GetProperty("controls").GetArrayLength());
    }

    [Fact]
    public async Task Session_commands_are_versioned_validated_and_explained()
    {
        var client = await factory.CreateAuthenticatedClientAsync("student@simulearn.demo", Ct);

        var created = await (await client.PostAsJsonAsync("/api/v1/sessions", new
        {
            modelKey = "simple-dc-circuit-v1",
        }, Ct)).ReadJsonAsync(Ct);

        var sessionId = created.String("id");
        var version = created.Int("version");
        Assert.NotNull(sessionId);
        Assert.Equal(1, version);

        var command = await client.PostAsJsonAsync($"/api/v1/sessions/{sessionId}/commands", new
        {
            command = new { type = "set", parameter = "resistanceOhm", value = 12 },
            expectedVersion = version,
        }, Ct);

        var result = await command.ReadJsonAsync(Ct);

        Assert.Equal(HttpStatusCode.OK, command.StatusCode);
        Assert.Equal(2, result.Int("version"));
        Assert.Equal(12d, result.GetProperty("state").Double("resistanceOhm"));
        Assert.Equal(1d, result.GetProperty("result").GetProperty("observations")[0].Double("value"));
        Assert.False(string.IsNullOrWhiteSpace(result.String("explanation")));

        var stale = await client.PostAsJsonAsync($"/api/v1/sessions/{sessionId}/commands", new
        {
            command = new { type = "set", parameter = "resistanceOhm", value = 14 },
            expectedVersion = version,
        }, Ct);

        var outOfRange = await client.PostAsJsonAsync($"/api/v1/sessions/{sessionId}/commands", new
        {
            command = new { type = "set", parameter = "resistanceOhm", value = 1000 },
        }, Ct);

        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, outOfRange.StatusCode);
    }

    [Fact]
    public async Task Idempotency_key_prevents_duplicate_sessions_and_rejects_reuse()
    {
        var client = await factory.CreateAuthenticatedClientAsync("student@simulearn.demo", Ct);
        var key = $"test-{Guid.CreateVersion7():N}";

        async Task<HttpResponseMessage> StartAsync(object body)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/sessions")
            {
                Content = JsonContent.Create(body),
            };
            request.Headers.Add("Idempotency-Key", key);
            return await client.SendAsync(request, Ct);
        }

        var first = await (await StartAsync(new { modelKey = "simple-dc-circuit-v1" })).ReadJsonAsync(Ct);
        var replay = await (await StartAsync(new { modelKey = "simple-dc-circuit-v1" })).ReadJsonAsync(Ct);
        var conflict = await StartAsync(new { modelKey = "inclined-plane-v1" });

        Assert.Equal(first.String("id"), replay.String("id"));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task Malformed_bodies_are_reported_as_client_errors()
    {
        var client = await factory.CreateAuthenticatedClientAsync("student@simulearn.demo", Ct);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/sessions")
        {
            Content = new StringContent("""{"modelKey": }""", Encoding.UTF8, "application/json"),
        };

        var response = await client.SendAsync(request, Ct);
        var payload = await response.ReadJsonAsync(Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_request", payload.String("code"));
    }

    [Fact]
    public async Task Students_cannot_author_content_or_read_other_sessions()
    {
        var student = await factory.CreateAuthenticatedClientAsync("student@simulearn.demo", Ct);
        var teacher = await factory.CreateAuthenticatedClientAsync("teacher@simulearn.demo", Ct);

        var lesson = await student.PostAsJsonAsync("/api/v1/lessons", new
        {
            title = "Student authored lesson",
            subjectCode = "PHY",
            modelKey = "simple-dc-circuit-v1",
        }, Ct);

        var created = await (await student.PostAsJsonAsync("/api/v1/sessions", new
        {
            modelKey = "simple-dc-circuit-v1",
        }, Ct)).ReadJsonAsync(Ct);

        var foreignRead = await teacher.GetAsync($"/api/v1/sessions/{created.String("id")}", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, lesson.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, foreignRead.StatusCode);
    }

    [Fact]
    public async Task Lesson_must_pass_review_before_it_can_be_published()
    {
        var teacher = await factory.CreateAuthenticatedClientAsync("teacher@simulearn.demo", Ct);
        var reviewer = await factory.CreateAuthenticatedClientAsync("reviewer@simulearn.demo", Ct);
        var student = await factory.CreateAuthenticatedClientAsync("student@simulearn.demo", Ct);

        var created = await (await teacher.PostAsJsonAsync("/api/v1/lessons", new
        {
            title = "Ohm's law exploration",
            subjectCode = "PHY",
            modelKey = "simple-dc-circuit-v1",
            objectiveCodes = new[] { "PHY-ELEC-OHMS-LAW" },
            initialState = new { voltageV = 12, resistanceOhm = 6 },
        }, Ct)).ReadJsonAsync(Ct);

        var lessonId = created.String("id");
        Assert.NotNull(lessonId);
        Assert.Equal("Draft", created.String("status"));

        var earlyPublish = await teacher.PostAsync($"/api/v1/lessons/{lessonId}/publish", null, Ct);
        var submit = await teacher.PostAsync($"/api/v1/lessons/{lessonId}/submit", null, Ct);
        var review = await reviewer.PostAsJsonAsync($"/api/v1/lessons/{lessonId}/review", new
        {
            decision = "Approved",
            comments = "Objective is clear and measurable.",
        }, Ct);
        var publish = await teacher.PostAsync($"/api/v1/lessons/{lessonId}/publish", null, Ct);
        var published = await publish.ReadJsonAsync(Ct);
        var visible = await student.GetAsync($"/api/v1/lessons/{lessonId}", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, earlyPublish.StatusCode);
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
        Assert.Equal(HttpStatusCode.OK, review.StatusCode);
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);
        Assert.Equal(1, published.Int("publishedVersion"));
        Assert.Equal("Published", published.String("status"));
        Assert.Equal(HttpStatusCode.OK, visible.StatusCode);
    }

    [Fact]
    public async Task Tutor_answers_are_grounded_in_the_calculated_state()
    {
        var client = await factory.CreateAuthenticatedClientAsync("student@simulearn.demo", Ct);

        var created = await (await client.PostAsJsonAsync("/api/v1/sessions", new
        {
            modelKey = "simple-dc-circuit-v1",
        }, Ct)).ReadJsonAsync(Ct);

        var response = await client.PostAsJsonAsync($"/api/v1/sessions/{created.String("id")}/messages", new
        {
            message = "Why is the current what it is?",
            level = "Standard",
        }, Ct);

        var payload = await response.ReadJsonAsync(Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(payload.String("reply")));
        Assert.True(payload.GetProperty("groundedFacts").GetArrayLength() > 0);
        Assert.True(payload.GetProperty("usedFallback").GetBoolean());
    }

    [Fact]
    public async Task Reviewers_and_administrators_can_audit_but_teachers_cannot()
    {
        var teacher = await factory.CreateAuthenticatedClientAsync("teacher@simulearn.demo", Ct);
        var admin = await factory.CreateAuthenticatedClientAsync("admin@simulearn.demo", Ct);

        var teacherUsers = await teacher.GetAsync("/api/v1/admin/users", Ct);
        var adminUsers = await admin.GetAsync("/api/v1/admin/users", Ct);
        var audit = await admin.GetAsync("/api/v1/admin/audit", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, teacherUsers.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminUsers.StatusCode);
        Assert.Equal(HttpStatusCode.OK, audit.StatusCode);
        Assert.True((await audit.ReadJsonAsync(Ct)).GetProperty("items").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Assessment_attempts_are_scored_server_side()
    {
        var client = await factory.CreateAuthenticatedClientAsync("student@simulearn.demo", Ct);

        var assessments = await (await client.GetAsync("/api/v1/assessments", Ct)).ReadJsonAsync(Ct);
        var assessmentId = assessments[0].String("id");
        Assert.NotNull(assessmentId);

        var assessment = await (await client.GetAsync(
            $"/api/v1/assessments/{assessmentId}",
            Ct)).ReadJsonAsync(Ct);
        var question = assessment.GetProperty("questions")[0];
        var selected = new[] { question.GetProperty("options")[0].String("id") };

        var response = await client.PostAsJsonAsync($"/api/v1/assessments/{assessmentId}/attempts", new
        {
            answers = new[]
            {
                new { questionId = question.String("id"), selectedOptionIds = selected },
            },
        }, Ct);

        var result = await response.ReadJsonAsync(Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result.Double("percentage"));
        Assert.True(result.GetProperty("feedback").GetArrayLength() >= 1);

        var history = await (await client.GetAsync("/api/v1/assessments/attempts/mine", Ct)).ReadJsonAsync(Ct);
        Assert.True(history.GetProperty("items").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Class_rosters_are_visible_to_teachers_only()
    {
        var teacher = await factory.CreateAuthenticatedClientAsync("teacher@simulearn.demo", Ct);
        var student = await factory.CreateAuthenticatedClientAsync("student@simulearn.demo", Ct);

        var classes = await (await teacher.GetAsync("/api/v1/classes", Ct)).ReadJsonAsync(Ct);
        var classId = classes[0].String("id");

        var teacherRoster = await teacher.GetAsync($"/api/v1/classes/{classId}/members", Ct);
        var studentRoster = await student.GetAsync($"/api/v1/classes/{classId}/members", Ct);
        var report = await teacher.GetAsync($"/api/v1/reports/classes/{classId}", Ct);

        Assert.Equal(HttpStatusCode.OK, teacherRoster.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, studentRoster.StatusCode);
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.True((await report.ReadJsonAsync(Ct)).GetProperty("students").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Registering_an_existing_email_is_rejected()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "student@simulearn.demo",
            password = "AnotherPassword1!",
            displayName = "Duplicate Learner",
        }, Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Weak_passwords_are_rejected()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = $"weak-{Guid.NewGuid():N}@simulearn.demo",
            password = "short",
            displayName = "Weak Password",
        }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
