using System.Net;
using McpGateway.Models;
using McpGateway.Services;
using McpGateway.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace McpGateway.Tests.Services;

public class SwaggerServiceTests
{
    // ── Sample Swagger JSON ───────────────────────────────────────────────────

    private const string SampleSwagger = """
        {
          "openapi": "3.0.1",
          "info": { "title": "Test API", "version": "1.0" },
          "servers": [{ "url": "https://api.example.com" }],
          "paths": {
            "/api/fabric/{id}": {
              "get": {
                "operationId": "FabricRetrieveAsync",
                "summary": "Get fabric by ID",
                "description": "Retrieves fabric details",
                "tags": ["Fabric"],
                "parameters": [
                  {
                    "name": "id",
                    "in": "path",
                    "required": true,
                    "schema": { "type": "string" },
                    "description": "Fabric identifier"
                  }
                ]
              }
            },
            "/api/orders": {
              "post": {
                "operationId": "OrderCreate",
                "summary": "Create a new order",
                "tags": ["Orders"],
                "requestBody": {
                  "required": true,
                  "content": {
                    "application/json": {
                      "schema": {
                        "type": "object",
                        "properties": { "name": { "type": "string" } }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """;

    private const string RefSwagger = """
        {
          "openapi": "3.0.1",
          "info": { "title": "Ref API", "version": "1.0" },
          "components": {
            "schemas": {
              "Order": {
                "type": "object",
                "properties": {
                  "id": { "type": "integer" },
                  "name": { "type": "string" }
                }
              }
            }
          },
          "paths": {
            "/api/orders": {
              "post": {
                "operationId": "OrderCreate",
                "summary": "Create order",
                "tags": ["Orders"],
                "requestBody": {
                  "required": true,
                  "content": {
                    "application/json": {
                      "schema": { "$ref": "#/components/schemas/Order" }
                    }
                  }
                }
              }
            }
          }
        }
        """;

    // ── Factory helper ────────────────────────────────────────────────────────

    private static SwaggerService CreateService(
        string? swaggerJson,
        string sourceName = "TEST",
        FakeHttpMessageHandler? handler = null)
    {
        var hasSource = swaggerJson != null || handler != null;

        var sources = hasSource
            ? new List<ApiSourceConfig>
              {
                  new()
                  {
                      Name = sourceName,
                      BaseUrl = "https://api.example.com",
                      SwaggerJsonPath = "swagger.json"
                  }
              }
            : new List<ApiSourceConfig>();

        var settings = Options.Create(new MultiSourceApiSettings { Sources = sources });

        var factory = new Mock<IHttpClientFactory>();
        if (hasSource)
        {
            var h = handler ?? FakeHttpMessageHandler.ReturnsJson(swaggerJson!);
            factory.Setup(f => f.CreateClient(It.IsAny<string>()))
                   .Returns(() => new HttpClient(h));
        }

        var enrichment = new Mock<ILlmEnrichmentService>();
        enrichment
            .Setup(e => e.EnrichAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<SwaggerEndpoint>>()))
            .ReturnsAsync(new Dictionary<string, EnrichedEndpointData>());

        var search = new Mock<ISemanticSearchService>();
        search.Setup(s => s.IsAvailable).Returns(false);
        search.Setup(s => s.IndexAsync(
            It.IsAny<string>(),
            It.IsAny<IReadOnlyList<SwaggerEndpoint>>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        search.Setup(s => s.SearchAsync(
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(SwaggerEndpoint Endpoint, float Score)>());

        return new SwaggerService(
            settings,
            NullLogger<SwaggerService>.Instance,
            factory.Object,
            enrichment.Object,
            search.Object);
    }

    // ── Tests: basic loading ──────────────────────────────────────────────────

    [Fact]
    public async Task No_sources_configured_returns_empty_endpoint_list()
    {
        var service = CreateService(null);

        var endpoints = await service.GetAllEndpointsAsync();

        Assert.Empty(endpoints);
    }

    [Fact]
    public async Task GetAllEndpointsAsync_returns_all_parsed_endpoints()
    {
        var service = CreateService(SampleSwagger);

        var endpoints = await service.GetAllEndpointsAsync();

        Assert.Equal(2, endpoints.Count);
    }

    [Fact]
    public async Task AppSource_is_set_from_config_source_name()
    {
        var service = CreateService(SampleSwagger, sourceName: "PLM");

        var endpoints = await service.GetAllEndpointsAsync();

        Assert.All(endpoints, ep => Assert.Equal("PLM", ep.AppSource));
    }

    [Fact]
    public async Task BaseUrl_is_extracted_from_swagger_servers_array()
    {
        var service = CreateService(SampleSwagger);

        var baseUrl = await service.GetBaseUrlAsync();

        Assert.Equal("https://api.example.com", baseUrl);
    }

    // ── Tests: operationId handling ───────────────────────────────────────────

    [Fact]
    public async Task Async_suffix_is_stripped_from_operationId()
    {
        var service = CreateService(SampleSwagger);

        // Swagger has "FabricRetrieveAsync" — service must strip the suffix
        var ep = await service.GetEndpointByOperationIdAsync("FabricRetrieve");

        Assert.NotNull(ep);
        Assert.Equal("FabricRetrieve", ep.OperationId);
    }

    [Fact]
    public async Task GetEndpointByOperationIdAsync_returns_null_for_unknown_id()
    {
        var service = CreateService(SampleSwagger);

        var ep = await service.GetEndpointByOperationIdAsync("DoesNotExist");

        Assert.Null(ep);
    }

    [Fact]
    public async Task GetEndpointByOperationIdAsync_is_case_insensitive()
    {
        var service = CreateService(SampleSwagger);

        var ep = await service.GetEndpointByOperationIdAsync("fabricretrieve");

        Assert.NotNull(ep);
    }

    // ── Tests: HTTP method and path ───────────────────────────────────────────

    [Fact]
    public async Task Http_method_is_uppercased()
    {
        var service = CreateService(SampleSwagger);
        var ep = await service.GetEndpointByOperationIdAsync("FabricRetrieve");

        Assert.Equal("GET", ep!.Method);
    }

    [Fact]
    public async Task Path_is_preserved_exactly()
    {
        var service = CreateService(SampleSwagger);
        var ep = await service.GetEndpointByOperationIdAsync("FabricRetrieve");

        Assert.Equal("/api/fabric/{id}", ep!.Path);
    }

    // ── Tests: parameters ─────────────────────────────────────────────────────

    [Fact]
    public async Task Parameters_are_parsed_with_correct_fields()
    {
        var service = CreateService(SampleSwagger);
        var ep = await service.GetEndpointByOperationIdAsync("FabricRetrieve");

        Assert.Single(ep!.Parameters);
        var p = ep.Parameters[0];
        Assert.Equal("id", p.Name);
        Assert.Equal("path", p.In);
        Assert.Equal("string", p.Type);
        Assert.Equal("Fabric identifier", p.Description);
        Assert.True(p.Required);
    }

    // ── Tests: request body ───────────────────────────────────────────────────

    [Fact]
    public async Task RequiresRequestBody_is_true_when_required()
    {
        var service = CreateService(SampleSwagger);
        var ep = await service.GetEndpointByOperationIdAsync("OrderCreate");

        Assert.True(ep!.RequiresRequestBody);
    }

    [Fact]
    public async Task RequestBodySchema_is_populated()
    {
        var service = CreateService(SampleSwagger);
        var ep = await service.GetEndpointByOperationIdAsync("OrderCreate");

        Assert.NotNull(ep!.RequestBodySchema);
        Assert.Contains("properties", ep.RequestBodySchema);
    }

    [Fact]
    public async Task Dollar_ref_in_request_body_is_resolved()
    {
        var service = CreateService(RefSwagger);
        var ep = await service.GetEndpointByOperationIdAsync("OrderCreate");

        Assert.NotNull(ep!.RequestBodySchema);
        // Resolved schema should expand the Order component's properties
        Assert.Contains("id", ep.RequestBodySchema);
        Assert.Contains("name", ep.RequestBodySchema);
    }

    // ── Tests: tag filtering ──────────────────────────────────────────────────

    [Fact]
    public async Task GetEndpointsByTagAsync_returns_only_matching_endpoints()
    {
        var service = CreateService(SampleSwagger);

        var fabric = await service.GetEndpointsByTagAsync("Fabric");

        Assert.Single(fabric);
        Assert.Equal("FabricRetrieve", fabric[0].OperationId);
    }

    [Fact]
    public async Task GetEndpointsByTagAsync_returns_empty_for_unknown_tag()
    {
        var service = CreateService(SampleSwagger);

        var results = await service.GetEndpointsByTagAsync("NonExistent");

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAllTagsAsync_returns_sorted_unique_tags()
    {
        var service = CreateService(SampleSwagger);

        var tags = await service.GetAllTagsAsync();

        Assert.Equal(new[] { "Fabric", "Orders" }, tags);
    }

    [Fact]
    public async Task GetTagSummaryAsync_reflects_endpoint_counts()
    {
        var service = CreateService(SampleSwagger);

        var summary = await service.GetTagSummaryAsync();

        Assert.Contains(summary, s => s.Tag == "Fabric" && s.Count == 1);
        Assert.Contains(summary, s => s.Tag == "Orders" && s.Count == 1);
    }

    // ── Tests: app source filtering ───────────────────────────────────────────

    [Fact]
    public async Task GetEndpointsByAppAsync_returns_only_that_sources_endpoints()
    {
        var service = CreateService(SampleSwagger, sourceName: "PLM");

        var plm = await service.GetEndpointsByAppAsync("PLM");

        Assert.Equal(2, plm.Count);
    }

    [Fact]
    public async Task GetEndpointsByAppAsync_returns_empty_for_unknown_source()
    {
        var service = CreateService(SampleSwagger, sourceName: "PLM");

        var erp = await service.GetEndpointsByAppAsync("ERP");

        Assert.Empty(erp);
    }

    [Fact]
    public async Task GetAllAppSourcesAsync_returns_configured_source_names()
    {
        var service = CreateService(SampleSwagger, sourceName: "PLM");

        var sources = await service.GetAllAppSourcesAsync();

        Assert.Single(sources);
        Assert.Equal("PLM", sources[0]);
    }

    // ── Tests: keyword search (inverted index) ────────────────────────────────

    [Fact]
    public async Task SearchEndpointsAsync_finds_endpoint_by_tag_name()
    {
        var service = CreateService(SampleSwagger);

        var results = await service.SearchEndpointsAsync("Fabric");

        Assert.Contains(results, ep => ep.OperationId == "FabricRetrieve");
    }

    [Fact]
    public async Task SearchEndpointsAsync_finds_endpoint_by_camelcase_word()
    {
        var service = CreateService(SampleSwagger);

        // "Retrieve" is a CamelCase segment of "FabricRetrieve"
        var results = await service.SearchEndpointsAsync("Retrieve");

        Assert.Single(results);
        Assert.Equal("FabricRetrieve", results[0].OperationId);
    }

    [Fact]
    public async Task SearchEndpointsAsync_is_case_insensitive()
    {
        var service = CreateService(SampleSwagger);

        var results = await service.SearchEndpointsAsync("fabric");

        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task SearchEndpointsAsync_returns_empty_for_no_match()
    {
        var service = CreateService(SampleSwagger);

        var results = await service.SearchEndpointsAsync("ZzzNonExistentKeyword");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchEndpointsAsync_multi_token_query_intersects_results()
    {
        var service = CreateService(SampleSwagger);

        // "Fabric" and "Retrieve" should intersect to just FabricRetrieve
        var results = await service.SearchEndpointsAsync("Fabric Retrieve");

        Assert.Single(results);
        Assert.Equal("FabricRetrieve", results[0].OperationId);
    }

    [Fact]
    public async Task SearchEndpointsAsync_returns_empty_when_one_token_has_no_match()
    {
        var service = CreateService(SampleSwagger);

        // "Fabric" matches, but "ZzzXxx" does not — intersection is empty
        var results = await service.SearchEndpointsAsync("Fabric ZzzXxx");

        Assert.Empty(results);
    }

    // ── Tests: cache invalidation ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllEndpointsAsync_caches_on_first_call_and_reuses()
    {
        int fetchCount = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            Interlocked.Increment(ref fetchCount);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new System.Net.Http.StringContent(SampleSwagger,
                    System.Text.Encoding.UTF8, "application/json")
            };
        });

        var service = CreateService(SampleSwagger, handler: handler);

        await service.GetAllEndpointsAsync();
        await service.GetAllEndpointsAsync();
        await service.GetAllEndpointsAsync();

        Assert.Equal(1, fetchCount);
    }

    [Fact]
    public async Task InvalidateCacheAsync_causes_reload_on_next_call()
    {
        int fetchCount = 0;
        var handler = new FakeHttpMessageHandler(req =>
        {
            Interlocked.Increment(ref fetchCount);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new System.Net.Http.StringContent(SampleSwagger,
                    System.Text.Encoding.UTF8, "application/json")
            };
        });

        var service = CreateService(SampleSwagger, handler: handler);

        await service.GetAllEndpointsAsync();   // loads — 1 fetch
        Assert.Equal(1, fetchCount);

        await service.InvalidateCacheAsync();
        await service.GetAllEndpointsAsync();   // reloads — 2 fetches

        Assert.Equal(2, fetchCount);
    }

    // ── Tests: raw spec ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetRawSpecificationAsync_returns_original_json()
    {
        var service = CreateService(SampleSwagger);

        var raw = await service.GetRawSpecificationAsync();

        // Raw spec should contain the original swagger paths
        Assert.Contains("FabricRetrieveAsync", raw);
    }
}
