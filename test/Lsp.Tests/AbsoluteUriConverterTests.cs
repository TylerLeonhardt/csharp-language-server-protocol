﻿using FluentAssertions;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization.Converters;
using Xunit;
using Xunit.Abstractions;

namespace Lsp.Tests
{
    public class AbsoluteUriConverterTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public AbsoluteUriConverterTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Theory]
        [ClassData(typeof(DocumentUriTestData.StringUris))]
        public void Should_Deserialize_VSCode_Style_Uris(string uri, DocumentUri expected)
        {
            _testOutputHelper.WriteLine($"Given: {uri}");
            _testOutputHelper.WriteLine($"Expected: {expected}");
            var serializer = new JsonSerializerSettings() {
                Converters = {new DocumentUriConverter()}
            };
            JsonConvert.DeserializeObject<DocumentUri>($"\"{uri}\"", serializer).Should().Be(expected);
        }

        [Theory]
        [ClassData(typeof(DocumentUriTestData.StringUris))]
        public void Should_Serialize_VSCode_Style_Uris(string uri, DocumentUri expected)
        {
            _testOutputHelper.WriteLine($"Given: {uri}");
            _testOutputHelper.WriteLine($"Expected: {expected}");
            var serializer = new JsonSerializerSettings() {
                Converters = {new DocumentUriConverter()}
            };
            JsonConvert.SerializeObject(new DocumentUri(uri), serializer).Trim('"').Should().Be(expected.ToString());
        }
    }
}