using System;
using GraphProcessor;

namespace CustomVisualScripting.Editor.Nodes.Base
{
    [Serializable]
    public abstract class BaseFlowNode : CustomBaseNode
    {
        [Input("execIn")]
        public object execIn;
    }
}
