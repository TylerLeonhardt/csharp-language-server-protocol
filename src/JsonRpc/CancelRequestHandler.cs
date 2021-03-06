using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.Embedded.MediatR;
using OmniSharp.Extensions.JsonRpc;

namespace OmniSharp.Extensions.JsonRpc
{
    public class CancelRequestHandler<TDescriptor> : ICancelRequestHandler
    {
        private readonly IRequestRouter<TDescriptor> _requestRouter;

        public CancelRequestHandler(IRequestRouter<TDescriptor> requestRouter)
        {
            _requestRouter = requestRouter;
        }

        public Task<Unit> Handle(CancelParams notification, CancellationToken token)
        {
            _requestRouter.CancelRequest(notification.Id);
            return Unit.Task;
        }
    }
}
