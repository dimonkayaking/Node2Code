#nullable enable
using System.Collections.Generic;

namespace VisualScripting.Core.Generators
{
    public partial class SimpleCodeGenerator
    {
        private void PushScope() => _scopeStack.Push(new HashSet<string>());

        private void PopScope()
        {
            if (_scopeStack.Count > 1)
                _scopeStack.Pop();
        }

        private void DeclareInCurrentScope(string variableName)
        {
            if (string.IsNullOrEmpty(variableName))
                return;
            if (_scopeStack.Count == 0)
                PushScope();
            _scopeStack.Peek().Add(variableName);
        }

        private bool IsVisibleInAnyScope(string variableName)
        {
            if (string.IsNullOrEmpty(variableName))
                return false;
            foreach (var scope in _scopeStack)
            {
                if (scope.Contains(variableName))
                    return true;
            }
            return false;
        }
    }
}
