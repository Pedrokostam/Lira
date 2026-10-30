using System.Diagnostics.CodeAnalysis;

namespace Lira.Toggl
{
    internal class TogglDtoClient
    {
        public long Id { get; init; }
        private string _name = "";
        [AllowNull]
        public string Name { get => _name; set => _name = value is null ? "" : value; }
    }
}
