#nullable enable

using System;

namespace VisualScripting.Core.Models
{
    public static class PortIds
    {
        public const string ExecIn = "execIn";
        public const string ExecOut = "execOut";
        public const string FalseBranch = "falseBranch";

        public static string Normalize(string? rawPortId)
        {
            if (string.IsNullOrWhiteSpace(rawPortId))
                return string.Empty;

            var id = rawPortId.Trim();
            if (string.Equals(id, "execIn", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(id, "exec", StringComparison.OrdinalIgnoreCase))
                return ExecIn;

            if (string.Equals(id, "execOut", StringComparison.OrdinalIgnoreCase))
                return ExecOut;

            if (string.Equals(id, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(id, "falseBranch", StringComparison.OrdinalIgnoreCase))
                return FalseBranch;

            return id;
        }

        public static bool IsExecIn(string? portId) =>
            string.Equals(Normalize(portId), ExecIn, StringComparison.Ordinal);

        public static bool IsExecOut(string? portId) =>
            string.Equals(Normalize(portId), ExecOut, StringComparison.Ordinal);

        public static bool IsFalseBranch(string? portId) =>
            string.Equals(Normalize(portId), FalseBranch, StringComparison.Ordinal);
    }
}
