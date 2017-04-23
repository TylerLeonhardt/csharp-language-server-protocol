﻿using JsonRpc;
using Lsp.Models;
// ReSharper disable CheckNamespace

namespace Lsp.Protocol
{
    [Method("textDocument/didSave")]
    public interface IDidSaveTextDocumentHandler : IRegistrableNotificationHandler<DidSaveTextDocumentParams, TextDocumentSaveRegistrationOptions> { }
}