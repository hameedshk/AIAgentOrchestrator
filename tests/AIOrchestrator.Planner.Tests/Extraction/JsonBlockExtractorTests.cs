using AIOrchestrator.Planner.Extraction;
using FluentAssertions;

namespace AIOrchestrator.Planner.Tests.Extraction;

public class JsonBlockExtractorTests
{
    [Fact]
    public void Extracts_json_from_fenced_code_block()
    {
        var input = "Here is your plan:\n```json\n{\"planVersion\":\"1\"}\n```\nDone.";
        var result = JsonBlockExtractor.Extract(input);
        result.Should().Be("{\"planVersion\":\"1\"}");
    }

    [Fact]
    public void Falls_back_to_raw_json_when_no_fence()
    {
        var input = "Some text {\"planVersion\":\"1\"} more text";
        var result = JsonBlockExtractor.Extract(input);
        result.Should().Contain("planVersion");
    }

    [Fact]
    public void Returns_null_when_no_json_found()
    {
        var result = JsonBlockExtractor.Extract("no json here at all");
        result.Should().BeNull();
    }

    [Fact]
    public void Handles_multiline_json_in_fence()
    {
        var input = "```json\n{\n  \"planVersion\": \"1\",\n  \"taskId\": \"abc\"\n}\n```";
        var result = JsonBlockExtractor.Extract(input);
        result.Should().Contain("planVersion").And.Contain("taskId");
    }
}
