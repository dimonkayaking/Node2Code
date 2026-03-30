using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Unity
{
    [System.Serializable, NodeMenuItem("Unity/Set Position")]
    public class SetPositionNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.UnitySetPosition;

        [Input("GameObject")]
        public GameObject gameObject;

        [Input("Position")]
        public new Vector3 position;

        [Output("Out")]
        public GameObject output;  // ← убрал new

        public override string name => "Set Position";
        
        protected override void Process()
        {
            output = gameObject;
            if (gameObject != null)
            {
                gameObject.transform.position = position;
            }
        }
    }
}