using Lira.Jql;
using Lira.Toggl;
using LiraPS.Arguments;
using LiraPS.Completers;
using LiraPS.Extensions;
using LiraPS.Transformers;
using System;
using System.Management.Automation;

namespace LiraPS.Toggl.Cmdlets
{

    [Cmdlet(VerbsCommon.Get, "LiraTogglEntry", DefaultParameterSetName = "PERIOD")]
    [Alias("Get-TogglEntries", "Get-TogglEntry", "Get-LiraTogglEntries")]
    [OutputType(typeof(TogglTimeEntry))]
    public sealed class GetTogglEntries :TogglCmdlet
    {
        [Parameter(Position = 0, ParameterSetName = "PERIOD")]
        public Period Period { get; set; } = Period.ThisMonth;

        [AllowNull]
        [JqlDateTransformer(mode: DateMode.Start)]
        [ArgumentCompleter(typeof(JqlDateStartArgumentCompleter))]
        [Parameter(ParameterSetName = "MANUALDATE", Mandatory =true)]
        [Alias("From")]
        public IJqlDate DateFrom { get; set; } = null!;

        [AllowNull]
        [JqlDateTransformer(mode: DateMode.Start)]
        [ArgumentCompleter(typeof(JqlDateEndArgumentCompleter))]
        [Parameter(ParameterSetName = "MANUALDATE")]
        [Alias("To")]
        public IJqlDate? DateTo { get; set; } = null;

        [Parameter]
        [Alias("Refresh", "Force")]
        public SwitchParameter ForceRefresh { get; set; }
        public TogglClient Client { get; private set; } = default!;

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            Client = TogglClient.GetTogglClient(GetTogglKey());
        }

        protected override void ProcessRecord()
        {
            if (ParameterSetName == "PERIOD")
            {
                Period.ToKeywordDates(out var from, out var to);
                DateFrom = from;
                DateTo = to;
            }
            var plainFrom = DateFrom.ToAccountDatetime(LiraSession.Client.AccountTimezone);
            var plainto = DateTo?.ToAccountDatetime(LiraSession.Client.AccountTimezone) ?? DateTimeOffset.Now;
            var entries = Client.GetEntries(plainFrom, plainto, ForceRefresh.IsPresent).GetAwaiter().GetResult();
            WriteObject(entries, enumerateCollection: true);
        }
    }
}
