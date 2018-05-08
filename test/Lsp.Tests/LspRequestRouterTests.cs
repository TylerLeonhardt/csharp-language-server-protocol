using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using OmniSharp.Extensions.JsonRpc.Server;
using OmniSharp.Extensions.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;
using OmniSharp.Extensions.LanguageServer.Server;
using OmniSharp.Extensions.LanguageServer.Server.Abstractions;
using OmniSharp.Extensions.LanguageServer.Server.Handlers;
using OmniSharp.Extensions.LanguageServer.Server.Matchers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using HandlerCollection = OmniSharp.Extensions.LanguageServer.Server.HandlerCollection;

namespace Lsp.Tests
{
    public class LspRequestRouterTests
    {
        private readonly TestLoggerFactory _testLoggerFactory;
        //private readonly IHandlerMatcherCollection handlerMatcherCollection = new HandlerMatcherCollection();

        public LspRequestRouterTests(ITestOutputHelper testOutputHelper)
        {
            _testLoggerFactory = new TestLoggerFactory(testOutputHelper);
        }

        [Fact]
        public async Task ShouldRouteToCorrect_Notification()
        {
            var textDocumentSyncHandler = TextDocumentSyncHandlerExtensions.With(DocumentSelector.ForPattern("**/*.cs"));
            textDocumentSyncHandler.Handle(Arg.Any<DidSaveTextDocumentParams>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            var collection = new HandlerCollection { textDocumentSyncHandler };
            var handlerMatcherCollection = new IHandlerMatcher[] {
                new TextDocumentMatcher(_testLoggerFactory.CreateLogger<TextDocumentMatcher>(), collection.TextDocumentSyncHandlers)
            };
            var mediator = new LspRequestRouter(collection, _testLoggerFactory, handlerMatcherCollection, new Serializer(), Substitute.For<IServiceScopeFactory>());

            var @params = new DidSaveTextDocumentParams() {
                TextDocument = new TextDocumentIdentifier(new Uri("file:///c:/test/123.cs"))
            };

            var request = new Notification(DocumentNames.DidSave, JObject.Parse(JsonConvert.SerializeObject(@params, new Serializer(ClientVersion.Lsp3).Settings)));

            await mediator.RouteNotification(mediator.GetDescriptor(request), request);

            await textDocumentSyncHandler.Received(1).Handle(Arg.Any<DidSaveTextDocumentParams>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ShouldRouteToCorrect_Notification_WithManyHandlers()
        {
            var textDocumentSyncHandler = TextDocumentSyncHandlerExtensions.With(DocumentSelector.ForPattern("**/*.cs"));
            var textDocumentSyncHandler2 = TextDocumentSyncHandlerExtensions.With(DocumentSelector.ForPattern("**/*.cake"));
            textDocumentSyncHandler.Handle(Arg.Any<DidSaveTextDocumentParams>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            textDocumentSyncHandler2.Handle(Arg.Any<DidSaveTextDocumentParams>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            var collection = new HandlerCollection { textDocumentSyncHandler, textDocumentSyncHandler2 };
            var handlerMatcherCollection = new IHandlerMatcher[] {
                new TextDocumentMatcher(_testLoggerFactory.CreateLogger<TextDocumentMatcher>(), collection.TextDocumentSyncHandlers)
            };
            var mediator = new LspRequestRouter(collection, _testLoggerFactory, handlerMatcherCollection, new Serializer(), Substitute.For<IServiceScopeFactory>());

            var @params = new DidSaveTextDocumentParams() {
                TextDocument = new TextDocumentIdentifier(new Uri("file:///c:/test/123.cake"))
            };

            var request = new Notification(DocumentNames.DidSave, JObject.Parse(JsonConvert.SerializeObject(@params, new Serializer(ClientVersion.Lsp3).Settings)));

            await mediator.RouteNotification(mediator.GetDescriptor(request), request);

            await textDocumentSyncHandler.Received(0).Handle(Arg.Any<DidSaveTextDocumentParams>(), Arg.Any<CancellationToken>());
            await textDocumentSyncHandler2.Received(1).Handle(Arg.Any<DidSaveTextDocumentParams>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ShouldRouteToCorrect_Request()
        {
            var textDocumentSyncHandler = TextDocumentSyncHandlerExtensions.With(DocumentSelector.ForPattern("**/*.cs"));
            textDocumentSyncHandler.Handle(Arg.Any<DidSaveTextDocumentParams>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            var codeActionHandler = Substitute.For<ICodeActionHandler>();
            codeActionHandler.GetRegistrationOptions().Returns(new TextDocumentRegistrationOptions() { DocumentSelector = DocumentSelector.ForPattern("**/*.cs") });
            codeActionHandler
                .Handle(Arg.Any<CodeActionParams>(), Arg.Any<CancellationToken>())
                .Returns(new CommandContainer());

            var collection = new HandlerCollection { textDocumentSyncHandler, codeActionHandler };
            var handlerMatcherCollection = new IHandlerMatcher[] {
                new TextDocumentMatcher(_testLoggerFactory.CreateLogger<TextDocumentMatcher>(), collection.TextDocumentSyncHandlers)
            };
            var mediator = new LspRequestRouter(collection, _testLoggerFactory, handlerMatcherCollection, new Serializer(), Substitute.For<IServiceScopeFactory>());

            var id = Guid.NewGuid().ToString();
            var @params = new DidSaveTextDocumentParams() {
                TextDocument = new TextDocumentIdentifier(new Uri("file:///c:/test/123.cs"))
            };

            var request = new Request(id, DocumentNames.CodeAction, JObject.Parse(JsonConvert.SerializeObject(@params, new Serializer(ClientVersion.Lsp3).Settings)));

            await mediator.RouteRequest(mediator.GetDescriptor(request), request);

            await codeActionHandler.Received(1).Handle(Arg.Any<CodeActionParams>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ShouldRouteToCorrect_Request_WithManyHandlers()
        {
            var textDocumentSyncHandler = TextDocumentSyncHandlerExtensions.With(DocumentSelector.ForPattern("**/*.cs"));
            var textDocumentSyncHandler2 = TextDocumentSyncHandlerExtensions.With(DocumentSelector.ForPattern("**/*.cake"));
            textDocumentSyncHandler.Handle(Arg.Any<DidSaveTextDocumentParams>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            textDocumentSyncHandler2.Handle(Arg.Any<DidSaveTextDocumentParams>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            var codeActionHandler = Substitute.For<ICodeActionHandler>();
            codeActionHandler.GetRegistrationOptions().Returns(new TextDocumentRegistrationOptions() { DocumentSelector = DocumentSelector.ForPattern("**/*.cs") });
            codeActionHandler
                .Handle(Arg.Any<CodeActionParams>(), Arg.Any<CancellationToken>())
                .Returns(new CommandContainer());

            var codeActionHandler2 = Substitute.For<ICodeActionHandler>();
            codeActionHandler2.GetRegistrationOptions().Returns(new TextDocumentRegistrationOptions() { DocumentSelector = DocumentSelector.ForPattern("**/*.cake") });
            codeActionHandler2
                .Handle(Arg.Any<CodeActionParams>(), Arg.Any<CancellationToken>())
                .Returns(new CommandContainer());

            var collection = new HandlerCollection { textDocumentSyncHandler, textDocumentSyncHandler2, codeActionHandler, codeActionHandler2 };
            var handlerMatcherCollection = new IHandlerMatcher[] {
                new TextDocumentMatcher(_testLoggerFactory.CreateLogger<TextDocumentMatcher>(), collection.TextDocumentSyncHandlers)
            };
            var mediator = new LspRequestRouter(collection, _testLoggerFactory, handlerMatcherCollection, new Serializer(), Substitute.For<IServiceScopeFactory>());

            var id = Guid.NewGuid().ToString();
            var @params = new CodeActionParams() {
                TextDocument = new TextDocumentIdentifier(new Uri("file:///c:/test/123.cake"))
            };

            var request = new Request(id, DocumentNames.CodeAction, JObject.Parse(JsonConvert.SerializeObject(@params, new Serializer(ClientVersion.Lsp3).Settings)));

            await mediator.RouteRequest(mediator.GetDescriptor(request), request);

            await codeActionHandler.Received(0).Handle(Arg.Any<CodeActionParams>(), Arg.Any<CancellationToken>());
            await codeActionHandler2.Received(1).Handle(Arg.Any<CodeActionParams>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ShouldRouteToCorrect_Request_WithManyHandlers_CodeLensHandler()
        {
            var textDocumentSyncHandler = TextDocumentSyncHandlerExtensions.With(DocumentSelector.ForPattern("**/*.cs"));
            var textDocumentSyncHandler2 = TextDocumentSyncHandlerExtensions.With(DocumentSelector.ForPattern("**/*.cake"));
            textDocumentSyncHandler.Handle(Arg.Any<DidSaveTextDocumentParams>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            textDocumentSyncHandler2.Handle(Arg.Any<DidSaveTextDocumentParams>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

            var codeActionHandler = Substitute.For<ICodeLensHandler>();
            codeActionHandler.GetRegistrationOptions().Returns(new CodeLensRegistrationOptions() { DocumentSelector = DocumentSelector.ForPattern("**/*.cs") });
            codeActionHandler
                .Handle(Arg.Any<CodeLensParams>(), Arg.Any<CancellationToken>())
                .Returns(new CodeLensContainer());

            var codeActionHandler2 = Substitute.For<ICodeLensHandler>();
            codeActionHandler2.GetRegistrationOptions().Returns(new CodeLensRegistrationOptions() { DocumentSelector = DocumentSelector.ForPattern("**/*.cake") });
            codeActionHandler2
                .Handle(Arg.Any<CodeLensParams>(), Arg.Any<CancellationToken>())
                .Returns(new CodeLensContainer());

            var collection = new HandlerCollection { textDocumentSyncHandler, textDocumentSyncHandler2, codeActionHandler, codeActionHandler2 };
            var handlerMatcherCollection = new IHandlerMatcher[] {
                new TextDocumentMatcher(_testLoggerFactory.CreateLogger<TextDocumentMatcher>(), collection.TextDocumentSyncHandlers)
            };
            var mediator = new LspRequestRouter(collection, _testLoggerFactory, handlerMatcherCollection, new Serializer(), Substitute.For<IServiceScopeFactory>());

            var id = Guid.NewGuid().ToString();
            var @params = new CodeLensParams() {
                TextDocument = new TextDocumentIdentifier(new Uri("file:///c:/test/123.cs"))
            };

            var request = new Request(id, DocumentNames.CodeLens, JObject.Parse(JsonConvert.SerializeObject(@params, new Serializer(ClientVersion.Lsp3).Settings)));

            await mediator.RouteRequest(mediator.GetDescriptor(request), request);

            await codeActionHandler2.Received(0).Handle(Arg.Any<CodeLensParams>(), Arg.Any<CancellationToken>());
            await codeActionHandler.Received(1).Handle(Arg.Any<CodeLensParams>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ShouldRouteTo_CorrectRequestWhenGivenNullParams()
        {
            var handler = Substitute.For<IShutdownHandler>();
            handler
                .Handle(Arg.Any<IRequest>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            var collection = new HandlerCollection { handler };
            var handlerMatcherCollection = new IHandlerMatcher[] {
                new TextDocumentMatcher(_testLoggerFactory.CreateLogger<TextDocumentMatcher>(), collection.TextDocumentSyncHandlers)
            };
            var mediator = new LspRequestRouter(collection, _testLoggerFactory, handlerMatcherCollection, new Serializer(), Substitute.For<IServiceScopeFactory>());

            var id = Guid.NewGuid().ToString();
            var request = new Request(id, GeneralNames.Shutdown, new JObject());

            await mediator.RouteRequest(mediator.GetDescriptor(request), request);

            await handler.Received(1).Handle(Arg.Any<IRequest>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ShouldHandle_Request_WithNullParameters()
        {
            bool wasShutDown = false;

            var shutdownHandler = new ShutdownHandler();
            shutdownHandler.Shutdown += shutdownRequested =>
            {
                wasShutDown = true;
            };

            var collection = new HandlerCollection { shutdownHandler };
            var handlerMatcherCollection = new IHandlerMatcher[] {
                new TextDocumentMatcher(_testLoggerFactory.CreateLogger<TextDocumentMatcher>(), collection.TextDocumentSyncHandlers)
            };
            var mediator = new LspRequestRouter(collection, _testLoggerFactory, handlerMatcherCollection, new Serializer(), Substitute.For<IServiceScopeFactory>());

            JToken @params = JValue.CreateNull(); // If the "params" property present but null, this will be JTokenType.Null.

            var id = Guid.NewGuid().ToString();
            var request = new Request(id, GeneralNames.Shutdown, @params);

            await mediator.RouteRequest(mediator.GetDescriptor(request), request);

            Assert.True(wasShutDown, "WasShutDown");
        }

        [Fact]
        public async Task ShouldHandle_Request_WithMissingParameters()
        {
            bool wasShutDown = false;

            var shutdownHandler = new ShutdownHandler();
            shutdownHandler.Shutdown += shutdownRequested =>
            {
                wasShutDown = true;
            };

            var collection = new HandlerCollection { shutdownHandler };
            var handlerMatcherCollection = new IHandlerMatcher[] {
                new TextDocumentMatcher(_testLoggerFactory.CreateLogger<TextDocumentMatcher>(), collection.TextDocumentSyncHandlers)
            };
            var mediator = new LspRequestRouter(collection, _testLoggerFactory, handlerMatcherCollection, new Serializer(), Substitute.For<IServiceScopeFactory>());

            JToken @params = null; // If the "params" property was missing entirely, this will be null.

            var id = Guid.NewGuid().ToString();
            var request = new Request(id, GeneralNames.Shutdown, @params);

            await mediator.RouteRequest(mediator.GetDescriptor(request), request);

            Assert.True(wasShutDown, "WasShutDown");
        }
    }
}
