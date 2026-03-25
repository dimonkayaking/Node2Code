using UnityEngine;
using CustomVisualScripting.Runtime.Assets;
using CustomVisualScripting.Runtime.Execution;

namespace CustomVisualScripting.Runtime.Components
{
    public class VisualScriptBehaviour : MonoBehaviour
    {
        [SerializeField] private GraphAsset _graphAsset;
        [SerializeField] private bool _runOnStart = true;
        [SerializeField] private bool _runOnUpdate = false;
        
        private GraphRunner _runner;
        private bool _isRunning;
        
        void Awake()
        {
            _runner = new GraphRunner();
        }
        
        void Start()
        {
            if (_runOnStart && _graphAsset != null)
            {
                Run();
            }
        }
        
        void Update()
        {
            if (_runOnUpdate && _isRunning)
            {
                Run();
            }
        }
        
        public void Run()
        {
            if (_graphAsset == null || _graphAsset.graphData == null)
            {
                Debug.LogWarning("[VisualScriptBehaviour] Нет графа для выполнения");
                return;
            }
            
            _isRunning = true;
            _runner.Run(_graphAsset.graphData);
        }
        
        public void Stop()
        {
            _isRunning = false;
            _runner.Clear();
        }
        
        public void SetGraph(GraphAsset graph)
        {
            _graphAsset = graph;
        }
        
        public void SetVariable(string name, object value)
        {
            _runner?.SetVariable(name, value);
        }
        
        public object GetVariable(string name)
        {
            return _runner?.GetVariable(name);
        }
        
        void OnDisable()
        {
            Stop();
        }
        
        void OnDestroy()
        {
            _runner?.Clear();
            _runner = null;
        }
    }
}