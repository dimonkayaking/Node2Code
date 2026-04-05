using System;
using GraphProcessor;

namespace CustomVisualScripting.Editor.Nodes.Base
{
    [Serializable]
    public abstract class BaseExecutionNode : CustomBaseNode
    {
        [Input("execIn", allowMultiple = false)]
        public object execIn;

        [Output("execOut", allowMultiple = false)]
        public object execOut;
    }
}