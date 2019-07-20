namespace OmniSharp.Extensions.DebugAdapter.Protocol.Events
{
    public class SetDataBreakpointsResponse
    {
        /// <summary>
        /// Information about the data breakpoints.The array elements correspond to the elements of the input argument 'breakpoints' array.
        /// </summary>
        public Container<Breakpoint> Breakpoints { get; set; }
    }

}
